param(
    [string]$GameDir,
    [string]$ModDll,
    [string]$ModConfig = "$PSScriptRoot\..\..\zzRandomAffixesMod\Config"
)
$ErrorActionPreference = 'Stop'

# Resolution order matches the csproj: -GameDir, then SDTD_GAMEDIR, then the known install
# locations. The first candidate is the path this script used to default to, so it keeps
# working with no arguments on that machine.
if (-not $GameDir) {
    $candidates = @(
        $env:SDTD_GAMEDIR,
        'D:\SteamLibrary\steamapps\common\7 Days To Die',
        'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die',
        'C:\Steam\steamapps\common\7 Days To Die'
    )
    # Interpolated rather than Join-Path, and -ErrorAction SilentlyContinue, because probing a
    # candidate on a drive this machine does not have is a throw under $ErrorActionPreference.
    $GameDir = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath "$_\7DaysToDie_Data\Managed\Assembly-CSharp.dll" -ErrorAction SilentlyContinue) } | Select-Object -First 1
}
if (-not $GameDir) { throw 'Could not find the game. Pass -GameDir or set SDTD_GAMEDIR.' }

# Debug, Release, and the DLL that ships in the repo — whichever was built last. Taking them in
# a fixed order silently verifies a stale bin\Debug left over from an older branch and reports
# breaks that no longer exist, which is the same class of mistake this script exists to catch.
if (-not $ModDll) {
    $candidates = @(
        "$PSScriptRoot\..\WeaponAffixesProject\bin\Debug\WeaponAffixesProject.dll",
        "$PSScriptRoot\..\WeaponAffixesProject\bin\Release\WeaponAffixesProject.dll",
        "$PSScriptRoot\..\..\zzRandomAffixesMod\WeaponAffixesProject.dll"
    )
    $ModDll = $candidates |
        Where-Object { Test-Path -LiteralPath $_ -ErrorAction SilentlyContinue } |
        Sort-Object { (Get-Item -LiteralPath $_).LastWriteTimeUtc } -Descending |
        Select-Object -First 1
}
if (-not $ModDll) { throw 'No mod assembly found. Build the project or pass -ModDll.' }

Write-Output "Game : $GameDir"
Write-Output "Mod  : $ModDll"
Add-Type -Path (Join-Path $GameDir 'Mods\0_TFP_Harmony\Mono.Cecil.dll')
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory((Join-Path $GameDir '7DaysToDie_Data\Managed'))
$resolver.AddSearchDirectory((Join-Path $GameDir 'Mods\0_TFP_Harmony'))
$resolver.AddSearchDirectory((Split-Path $ModDll))
$reader = [Mono.Cecil.ReaderParameters]::new()
$reader.AssemblyResolver = $resolver
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ModDll, $reader)
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDir '7DaysToDie_Data\Managed\Assembly-CSharp.dll'), $reader)
$failures = [Collections.Generic.List[string]]::new()

# A removed type only shows up as an unresolved member if the mod happens to call something on
# it. Used as a parameter, local or generic argument it resolves to nothing and goes unnoticed,
# so check the type references in their own right.
$typeCount = 0
foreach ($type in $mod.MainModule.GetTypeReferences()) {
    if ($null -eq $type.Scope -or $type.Scope.Name -ne 'Assembly-CSharp') { continue }
    $typeCount++
    try {
        if ($null -eq $type.Resolve()) { $failures.Add("Unresolved type: $($type.FullName)") }
    } catch { $failures.Add("Unresolved type: $($type.FullName) -- $_") }
}
Write-Output "Checked $typeCount compiled game type references."

$count = 0
foreach ($member in $mod.MainModule.GetMemberReferences()) {
    if ($member.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
    $count++
    $resolved = $null
    try { $resolved = $member.Resolve() } catch { }
    if ($null -ne $resolved) { continue }

    # Call out the trap V3.3 sprang: a public field turned into a property. The source still
    # compiles once rebuilt, so there is no build-time signal, and a shipped DLL built against
    # the old version throws MissingFieldException at runtime instead.
    $hint = ''
    if ($member -is [Mono.Cecil.FieldReference]) {
        $declaring = $null
        try { $declaring = $member.DeclaringType.Resolve() } catch { }
        if ($null -ne $declaring -and @($declaring.Properties | Where-Object Name -eq $member.Name).Count -gt 0) {
            $hint = ' -- it is now a property, not a field; recompile against this version'
        }
    }
    $failures.Add("Unresolved member: $member$hint")
}
Write-Output "Checked $count compiled game member references."

# Harmony's injected parameters are not parameters of the target method.
$injected = @('__instance', '__result', '__state', '__originalMethod', '__runOriginal', '__args', '__exception')

function Read-PatchAttributes($attributes) {
    $targetType = $null
    $targetMethod = $null
    foreach ($attr in $attributes) {
        if ($attr.AttributeType.FullName -ne 'HarmonyLib.HarmonyPatch') { continue }
        foreach ($arg in $attr.ConstructorArguments) {
            if ($arg.Type.FullName -eq 'System.Type') { $targetType = $arg.Value }
            if ($arg.Type.FullName -eq 'System.String') { $targetMethod = $arg.Value }
        }
    }
    return [pscustomobject]@{ TargetType = $targetType; TargetMethod = $targetMethod }
}

function Check-Patch($targetType, $targetMethod, $patchMethod) {
    if ($null -eq $targetType -or $null -eq $targetMethod) { return }
    $definition = $targetType.Resolve()
    $targets = @()
    while ($null -ne $definition) {
        $targets = @($definition.Methods | Where-Object Name -eq $targetMethod)
        if ($targets.Count -gt 0) { break }
        if ($null -eq $definition.BaseType) { break }
        $definition = $definition.BaseType.Resolve()
    }
    if ($targets.Count -eq 0) {
        $failures.Add("Missing Harmony target: $targetType::$targetMethod")
        return
    }
    if ($null -eq $patchMethod) { return }

    # A parameter name that matches nothing on the target is silently passed as null by Harmony,
    # so a renamed game parameter turns into a quiet behaviour change rather than an error.
    # With overloads a name only has to match one of them.
    $targetParams = @($targets | ForEach-Object { $_.Parameters.Name }) | Select-Object -Unique
    $allStatic = @($targets | Where-Object { -not $_.IsStatic }).Count -eq 0
    foreach ($parameter in $patchMethod.Parameters) {
        $name = $parameter.Name
        if ($name -eq '__instance' -and $allStatic) {
            $failures.Add("Static target: $targetType::$targetMethod injects __instance but the target is static")
            continue
        }
        if ($injected -contains $name -or $name.StartsWith('___')) { continue }
        if ($targetParams -notcontains $name) {
            $failures.Add("Bad patch parameter: $targetType::$targetMethod has no '$name' (has: $($targetParams -join ', '))")
        }
    }
}

function Check-Type($type) {
    # [HarmonyPatch(typeof(X))] on the class and [HarmonyPatch("Y")] on the method stack into one
    # target, so the method's attributes fall back to the class's.
    $classInfo = Read-PatchAttributes $type.CustomAttributes
    $patchMethods = @($type.Methods | Where-Object {
        $_.Name -in @('Prefix','Postfix','Transpiler','Finalizer') -or
        @($_.CustomAttributes | Where-Object { $_.AttributeType.Name -in @('HarmonyPrefix','HarmonyPostfix','HarmonyTranspiler','HarmonyFinalizer') }).Count -gt 0
    })

    foreach ($method in $patchMethods) {
        $methodInfo = Read-PatchAttributes $method.CustomAttributes
        $targetType = if ($null -ne $methodInfo.TargetType) { $methodInfo.TargetType } else { $classInfo.TargetType }
        $targetMethod = if ($null -ne $methodInfo.TargetMethod) { $methodInfo.TargetMethod } else { $classInfo.TargetMethod }
        Check-Patch $targetType $targetMethod $method
    }
    # A patch class with no conventionally named patch method still has its target checked.
    if ($patchMethods.Count -eq 0) { Check-Patch $classInfo.TargetType $classInfo.TargetMethod $null }

    foreach ($nested in $type.NestedTypes) { Check-Type $nested }
}
foreach ($type in $mod.MainModule.Types) { Check-Type $type }
# These runtime-selected targets cannot be checked through compiled member references.
foreach ($pair in @(@('Equipment','SetSlotItem'),@('XUiC_ItemActionList','AddActionListEntry'))) {
    $type = $game.MainModule.Types | Where-Object Name -eq $pair[0]
    if (@($type.Methods | Where-Object Name -eq $pair[1]).Count -eq 0) {
        $failures.Add("Missing runtime target: $($pair[0])::$($pair[1])")
    }
}
Write-Output 'Checked Harmony and runtime-selected targets.'

$patchCount = 0
foreach ($file in Get-ChildItem -LiteralPath $ModConfig -Recurse -Filter '*.xml') {
    $relative = $file.FullName.Substring((Resolve-Path $ModConfig).Path.Length + 1)
    $vanillaPath = Join-Path (Join-Path $GameDir 'Data\Config') $relative
    if (!(Test-Path -LiteralPath $vanillaPath)) { $failures.Add("Missing vanilla XML: $relative"); continue }
    $doc = [xml](Get-Content -LiteralPath $vanillaPath -Raw)
    $patch = [xml](Get-Content -LiteralPath $file.FullName -Raw)
    foreach ($op in $patch.DocumentElement.ChildNodes) {
        if ($op.NodeType -ne [Xml.XmlNodeType]::Element -or !$op.HasAttribute('xpath')) { continue }
        $patchCount++
        $xpath = $op.GetAttribute('xpath')
        $targets = @($doc.SelectNodes($xpath))
        if ($targets.Count -eq 0) { $failures.Add("Unmatched XML patch: $relative $($op.Name) $xpath"); continue }
        foreach ($target in $targets) {
            switch ($op.Name) {
                'append' { foreach ($child in $op.ChildNodes) { [void]$target.AppendChild($doc.ImportNode($child,$true)) } }
                'remove' { [void]$target.ParentNode.RemoveChild($target) }
                'set' { $target.InnerText = $op.InnerText }
                'setattribute' { $target.SetAttribute($op.GetAttribute('name'),$op.InnerText) }
            }
        }
    }
}
Write-Output "Checked $patchCount XML patch targets."
if ($failures.Count) { $failures | Write-Output; throw "$($failures.Count) compatibility checks failed." }
Write-Output 'All compatibility checks passed.'
