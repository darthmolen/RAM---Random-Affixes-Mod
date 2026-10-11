param(
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\7 Days To Die',
    [string]$ModDll = "$PSScriptRoot\..\WeaponAffixesProject\bin\Debug\WeaponAffixesProject.dll",
    [string]$ModConfig = "$PSScriptRoot\..\..\zzRandomAffixesMod\Config"
)
$ErrorActionPreference = 'Stop'
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
$count = 0
foreach ($member in $mod.MainModule.GetMemberReferences()) {
    if ($member.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
    $count++
    try {
        if ($null -eq $member.Resolve()) { $failures.Add("Unresolved member: $member") }
    } catch { $failures.Add("Unresolved member: $member -- $_") }
}
Write-Output "Checked $count compiled game member references."

function Check-PatchAttributes($attributes) {
    $targetType = $null
    $targetMethod = $null
    foreach ($attr in $attributes) {
        if ($attr.AttributeType.FullName -ne 'HarmonyLib.HarmonyPatch') { continue }
        foreach ($arg in $attr.ConstructorArguments) {
            if ($arg.Type.FullName -eq 'System.Type') { $targetType = $arg.Value }
            if ($arg.Type.FullName -eq 'System.String') { $targetMethod = $arg.Value }
        }
    }
    if ($null -eq $targetType -or $null -eq $targetMethod) { return }
    $definition = $targetType.Resolve()
    $found = $false
    while ($null -ne $definition) {
        if (@($definition.Methods | Where-Object Name -eq $targetMethod).Count -gt 0) { $found = $true; break }
        if ($null -eq $definition.BaseType) { break }
        $definition = $definition.BaseType.Resolve()
    }
    if (!$found) { $failures.Add("Missing Harmony target: $targetType::$targetMethod") }
}
function Check-Type($type) {
    Check-PatchAttributes $type.CustomAttributes
    foreach ($method in $type.Methods) { Check-PatchAttributes $method.CustomAttributes }
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
