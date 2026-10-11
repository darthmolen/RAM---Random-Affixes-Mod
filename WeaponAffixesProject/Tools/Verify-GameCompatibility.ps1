param(
    [string]$GameDir,
    [string]$ModDll,
    [string]$ModConfig,
    [string]$RepoRoot,
    # Mods whose xpaths target another mod's XML rather than vanilla's. Their targets cannot
    # exist in a vanilla config by definition, so checking them against one is meaningless:
    # zzzLBDRCompPatch anchors on perkQualityJoe, which LBDR adds.
    [string[]]$SkipConfig = @('zzzLBDRCompPatch')
)
$ErrorActionPreference = 'Stop'

# Resolution order matches the csproj: -GameDir, then SDTD_GAMEDIR, then the known install
# locations. The first candidate is the path this script used to default to, so it keeps
# working with no arguments on that machine.
#
# SDTD_GAMEDIR is authoritative when set, exactly as the csproj treats it: a wrong value has to
# fail, not fall through. Falling back would verify one installation while the build compiled
# against another, and agreeing with the build is the whole point of the check.
function Test-GameDir($path) {
    # Interpolated rather than Join-Path, and -ErrorAction SilentlyContinue, because probing a
    # candidate on a drive this machine does not have is a throw under $ErrorActionPreference.
    return $path -and (Test-Path -LiteralPath "$path\7DaysToDie_Data\Managed\Assembly-CSharp.dll" -ErrorAction SilentlyContinue)
}
if (-not $GameDir -and $env:SDTD_GAMEDIR) {
    $GameDir = $env:SDTD_GAMEDIR
    if (-not (Test-GameDir $GameDir)) {
        throw "SDTD_GAMEDIR is set to '$GameDir' but Assembly-CSharp.dll is not there. The build would fail the same way."
    }
}
if (-not $GameDir) {
    $GameDir = @(
        'D:\SteamLibrary\steamapps\common\7 Days To Die',
        'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die',
        'C:\Steam\steamapps\common\7 Days To Die'
    ) | Where-Object { Test-GameDir $_ } | Select-Object -First 1
}
if (-not $GameDir) { throw 'Could not find the game. Pass -GameDir or set SDTD_GAMEDIR.' }
if (-not (Test-GameDir $GameDir)) { throw "Assembly-CSharp.dll not found under GameDir '$GameDir'." }

if (-not $RepoRoot) { $RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path }

Write-Output "Game : $GameDir"
Add-Type -Path (Join-Path $GameDir 'Mods\0_TFP_Harmony\Mono.Cecil.dll')
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory((Join-Path $GameDir '7DaysToDie_Data\Managed'))
$resolver.AddSearchDirectory((Join-Path $GameDir 'Mods\0_TFP_Harmony'))
$reader = [Mono.Cecil.ReaderParameters]::new()
$reader.AssemblyResolver = $resolver
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDir '7DaysToDie_Data\Managed\Assembly-CSharp.dll'), $reader)
$failures = [Collections.Generic.List[string]]::new()

# Harmony's injected parameters are not parameters of the target method.
$injected = @('__instance', '__result', '__state', '__originalMethod', '__runOriginal', '__args', '__exception')

function Read-PatchAttributes($attributes) {
    $targetType = $null
    $targetMethod = $null
    $argumentTypes = $null
    foreach ($attr in $attributes) {
        if ($attr.AttributeType.FullName -ne 'HarmonyLib.HarmonyPatch') { continue }
        foreach ($arg in $attr.ConstructorArguments) {
            if ($arg.Type.FullName -eq 'System.Type') { $targetType = $arg.Value }
            if ($arg.Type.FullName -eq 'System.String') { $targetMethod = $arg.Value }
            # [HarmonyPatch(typeof(X), "Y", new Type[] { ... })] names one specific overload.
            # Without it an overload-targeted patch is only matched by name, so the declared
            # overload can disappear while a different one keeps the check passing.
            if ($arg.Type.FullName -eq 'System.Type[]' -and $null -ne $arg.Value) {
                $argumentTypes = @($arg.Value | ForEach-Object { $_.Value })
            }
        }
    }
    return [pscustomobject]@{ TargetType = $targetType; TargetMethod = $targetMethod; ArgumentTypes = $argumentTypes }
}

function Check-Patch($info, $patchMethod, $checkParameterNames) {
    $targetType = $info.TargetType
    $targetMethod = $info.TargetMethod
    if ($null -eq $targetType -or $null -eq $targetMethod) { return }

    $label = "$targetType::$targetMethod"
    $definition = $targetType.Resolve()
    $targets = @()
    while ($null -ne $definition) {
        $targets = @($definition.Methods | Where-Object Name -eq $targetMethod)
        if ($targets.Count -gt 0) { break }
        if ($null -eq $definition.BaseType) { break }
        $definition = $definition.BaseType.Resolve()
    }
    if ($targets.Count -eq 0) {
        $failures.Add("Missing Harmony target: $label")
        return
    }

    if ($null -ne $info.ArgumentTypes) {
        $wanted = @($info.ArgumentTypes | ForEach-Object { $_.FullName })
        $label = "$label($($wanted -join ', '))"
        $targets = @($targets | Where-Object {
            $actual = @($_.Parameters | ForEach-Object { $_.ParameterType.FullName })
            $actual.Count -eq $wanted.Count -and @(Compare-Object $actual $wanted -SyncWindow 0).Count -eq 0
        })
        if ($targets.Count -eq 0) {
            $failures.Add("Missing Harmony overload: $label")
            return
        }
    }

    if ($null -eq $patchMethod) { return }

    # Reverse patches declare the original's signature and are bound positionally, so their
    # parameter names are the author's choice and mean nothing to Harmony. Only prefix/postfix
    # style patches get names matched against the target.
    if (-not $checkParameterNames) { return }

    # A parameter name that matches nothing on the target is silently passed as null by Harmony,
    # so a renamed game parameter turns into a quiet behaviour change rather than an error.
    # With overloads a name only has to match one of the candidates left above.
    $targetParams = @($targets | ForEach-Object { $_.Parameters.Name }) | Select-Object -Unique
    $allStatic = @($targets | Where-Object { -not $_.IsStatic }).Count -eq 0
    foreach ($parameter in $patchMethod.Parameters) {
        $name = $parameter.Name
        if ($name -eq '__instance' -and $allStatic) {
            $failures.Add("Static target: $label injects __instance but the target is static")
            continue
        }
        if ($injected -contains $name -or $name.StartsWith('___')) { continue }
        if ($targetParams -notcontains $name) {
            $failures.Add("Bad patch parameter: $label has no '$name' (has: $($targetParams -join ', '))")
        }
    }
}

function Check-Type($type) {
    # [HarmonyPatch(typeof(X))] on the class and [HarmonyPatch("Y")] on the method stack into one
    # target, so the method's attributes fall back to the class's.
    $classInfo = Read-PatchAttributes $type.CustomAttributes

    # A reverse patch carries its target on the method and is named whatever the author likes, so
    # it matches none of the conventions below. AffixGiantSlayer's CallBase is one, and leaving
    # it out meant EntityAlive.DamageEntity could be removed with this check still passing.
    $patchMethods = @($type.Methods | Where-Object {
        $attributeNames = @($_.CustomAttributes | ForEach-Object { $_.AttributeType.Name })
        $_.Name -in @('Prefix','Postfix','Transpiler','Finalizer') -or
        @($attributeNames | Where-Object { $_ -in @('HarmonyPrefix','HarmonyPostfix','HarmonyTranspiler','HarmonyFinalizer','HarmonyReversePatch') }).Count -gt 0
    })

    foreach ($method in $patchMethods) {
        $methodInfo = Read-PatchAttributes $method.CustomAttributes
        $merged = [pscustomobject]@{
            TargetType    = if ($null -ne $methodInfo.TargetType) { $methodInfo.TargetType } else { $classInfo.TargetType }
            TargetMethod  = if ($null -ne $methodInfo.TargetMethod) { $methodInfo.TargetMethod } else { $classInfo.TargetMethod }
            ArgumentTypes = if ($null -ne $methodInfo.ArgumentTypes) { $methodInfo.ArgumentTypes } else { $classInfo.ArgumentTypes }
        }
        # Only prefix/postfix/finalizer parameters are matched against the target's by name.
        #
        # A reverse patch declares the original's signature and binds positionally. A transpiler
        # takes Harmony's own convention parameters -- IEnumerable<CodeInstruction> instructions,
        # ILGenerator generator, MethodBase original -- none of which is a parameter of the method
        # being patched. Checking either against the target produces pure noise: OcbCrookedDeco's
        # transpiler over BlockShapeNew.renderFace reported "has no 'instructions'" before this.
        $attributeNames = @($method.CustomAttributes | ForEach-Object { $_.AttributeType.Name })
        $bindsPositionally = ($attributeNames -contains 'HarmonyReversePatch') -or
                             ($attributeNames -contains 'HarmonyTranspiler') -or
                             ($method.Name -eq 'Transpiler')
        Check-Patch $merged $method (-not $bindsPositionally)
    }
    # A patch class with no conventionally named patch method still has its target checked.
    if ($patchMethods.Count -eq 0) { Check-Patch $classInfo $null $false }

    foreach ($nested in $type.NestedTypes) { Check-Type $nested }
}
function Check-Assembly($dllPath, $label) {
    $resolver.AddSearchDirectory((Split-Path $dllPath))
    $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dllPath, $reader)

    # A removed type only shows up as an unresolved member if the mod happens to call something
    # on it. Used as a parameter, local or generic argument it resolves to nothing and goes
    # unnoticed, so check the type references in their own right.
    $typeCount = 0
    foreach ($type in $asm.MainModule.GetTypeReferences()) {
        if ($null -eq $type.Scope -or $type.Scope.Name -ne 'Assembly-CSharp') { continue }
        $typeCount++
        try {
            if ($null -eq $type.Resolve()) { $failures.Add("[$label] Unresolved type: $($type.FullName)") }
        } catch { $failures.Add("[$label] Unresolved type: $($type.FullName) -- $_") }
    }

    $memberCount = 0
    foreach ($member in $asm.MainModule.GetMemberReferences()) {
        if ($member.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
        $memberCount++
        $resolved = $null
        try { $resolved = $member.Resolve() } catch { }
        if ($null -ne $resolved) { continue }

        # Call out the trap V3.3 sprang: a public field turned into a property. The source still
        # compiles once rebuilt, so there is no build-time signal, and a shipped DLL built
        # against the old version throws MissingFieldException at runtime instead.
        $hint = ''
        if ($member -is [Mono.Cecil.FieldReference]) {
            $declaring = $null
            try { $declaring = $member.DeclaringType.Resolve() } catch { }
            if ($null -ne $declaring -and @($declaring.Properties | Where-Object Name -eq $member.Name).Count -gt 0) {
                $hint = ' -- it is now a property, not a field; recompile against this version'
            }
        }
        $failures.Add("[$label] Unresolved member: $member$hint")
    }

    foreach ($type in $asm.MainModule.Types) { Check-Type $type }
    Write-Output "  $label : $typeCount type ref(s), $memberCount member ref(s), Harmony targets."
}

# The ops this emulates. Anything else has its xpath checked but leaves the document unchanged,
# which is reported rather than passed over -- a skipped op can cascade into later xpaths not
# matching, and silence there reads as a broken mod instead of an incomplete emulator.
$emulatedOps = @('append', 'remove', 'set', 'setattribute', 'insertBefore', 'insertAfter')

function Check-ModConfig($configDir, $label) {
    $patchCount = 0
    $notEmulated = @{}
    $configRoot = (Resolve-Path -LiteralPath $configDir).Path
    foreach ($file in Get-ChildItem -LiteralPath $configDir -Recurse -Filter '*.xml') {
        $relative = $file.FullName.Substring($configRoot.Length + 1)
        $vanillaPath = Join-Path (Join-Path $GameDir 'Data\Config') $relative
        # A mod is free to add a config file vanilla does not have; there is simply nothing to
        # check its xpaths against.
        if (!(Test-Path -LiteralPath $vanillaPath)) { continue }
        $doc = [xml](Get-Content -LiteralPath $vanillaPath -Raw)
        $patch = [xml](Get-Content -LiteralPath $file.FullName -Raw)
        foreach ($op in $patch.DocumentElement.ChildNodes) {
            if ($op.NodeType -ne [Xml.XmlNodeType]::Element -or !$op.HasAttribute('xpath')) { continue }
            $patchCount++
            $xpath = $op.GetAttribute('xpath')
            $targets = @($doc.SelectNodes($xpath))
            # LocalName, not Name: PowerShell's XML adapter resolves .Name to the value of an
            # attribute called "name" when the element has one, so every <setattribute name="x">
            # reported itself as "x" and matched no branch of the switch below -- meaning
            # setattribute ops were silently never applied.
            $opName = $op.LocalName
            if ($targets.Count -eq 0) {
                $failures.Add("[$label] Unmatched XML patch: $relative $opName $xpath")
                continue
            }
            if ($emulatedOps -notcontains $opName) {
                $notEmulated[$opName] = 1 + $notEmulated[$opName]
                continue
            }
            foreach ($target in $targets) {
                switch ($opName) {
                    'append' { foreach ($child in $op.ChildNodes) { [void]$target.AppendChild($doc.ImportNode($child,$true)) } }
                    'remove' { [void]$target.ParentNode.RemoveChild($target) }
                    'set' { $target.InnerText = $op.InnerText }
                    'setattribute' { $target.SetAttribute($op.GetAttribute('name'),$op.InnerText) }
                    # Without these two, a mod that removes a node and reinserts a replacement
                    # loses the replacement, and every later xpath into it reports unmatched.
                    # AttributeLevels20 does exactly that with the five attributes.
                    'insertBefore' {
                        foreach ($child in $op.ChildNodes) {
                            if ($child.NodeType -ne [Xml.XmlNodeType]::Element) { continue }
                            [void]$target.ParentNode.InsertBefore($doc.ImportNode($child,$true), $target)
                        }
                    }
                    'insertAfter' {
                        $anchor = $target
                        foreach ($child in $op.ChildNodes) {
                            if ($child.NodeType -ne [Xml.XmlNodeType]::Element) { continue }
                            $imported = $doc.ImportNode($child,$true)
                            [void]$target.ParentNode.InsertAfter($imported, $anchor)
                            $anchor = $imported
                        }
                    }
                }
            }
        }
    }
    $note = ''
    if ($notEmulated.Count -gt 0) {
        $detail = ($notEmulated.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', '
        $note = "  (xpath checked, document not mutated: $detail)"
    }
    Write-Output "  $label : $patchCount xpath(s).$note"
}

# ---------------------------------------------------------------------------------------
# What to check. An explicit -ModDll or -ModConfig checks just that; otherwise every mod in
# the repo, because this repository holds eight of them and checking only RAM says nothing
# about the other seven.
# ---------------------------------------------------------------------------------------

Write-Output ''
if ($ModDll -or $ModConfig) {
    if ($ModDll) { Check-Assembly $ModDll (Split-Path $ModDll -Leaf) }
    if ($ModConfig) { Check-ModConfig $ModConfig (Split-Path (Split-Path $ModConfig -Parent) -Leaf) }
} else {
    $modFolders = @(Get-ChildItem -LiteralPath $RepoRoot -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'ModInfo.xml') })
    Write-Output "Found $($modFolders.Count) mod(s) under $RepoRoot"
    Write-Output ''
    foreach ($folder in $modFolders) {
        foreach ($dll in @(Get-ChildItem -LiteralPath $folder.FullName -Filter '*.dll' -Recurse -ErrorAction SilentlyContinue)) {
            Check-Assembly $dll.FullName "$($folder.Name)/$($dll.Name)"
        }
        $cfg = Join-Path $folder.FullName 'Config'
        if (Test-Path -LiteralPath $cfg) {
            if ($SkipConfig -contains $folder.Name) {
                Write-Output "  $($folder.Name) : config skipped (patches another mod, not vanilla)."
            } else {
                Check-ModConfig $cfg $folder.Name
            }
        }
    }
}

# These runtime-selected targets cannot be checked through compiled member references.
foreach ($pair in @(@('Equipment','SetSlotItem'),@('XUiC_ItemActionList','AddActionListEntry'))) {
    $type = $game.MainModule.Types | Where-Object Name -eq $pair[0]
    if (@($type.Methods | Where-Object Name -eq $pair[1]).Count -eq 0) {
        $failures.Add("Missing runtime target: $($pair[0])::$($pair[1])")
    }
}

Write-Output ''
if ($failures.Count) { $failures | Write-Output; throw "$($failures.Count) compatibility checks failed." }
Write-Output 'All compatibility checks passed.'
