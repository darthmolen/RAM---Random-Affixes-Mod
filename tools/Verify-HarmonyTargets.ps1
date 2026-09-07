<#
.SYNOPSIS
Checks every Harmony patch in a built mod assembly against the game's Assembly-CSharp.dll.

.DESCRIPTION
A patch whose target method no longer exists takes down mod init at game launch, and the only
sign is a line buried in output_log. This resolves each [HarmonyPatch] target statically so a
game update surfaces the break here instead, and reports:

  * target types and methods that no longer exist
  * prefix/postfix parameters that do not match any parameter on the target method
  * __instance injected into a patch of a static method

Patch classes that pick their targets at runtime (TargetMethod / TargetMethods) cannot be
resolved statically and are listed as "manual" for review.

.EXAMPLE
pwsh tools/Verify-HarmonyTargets.ps1
pwsh tools/Verify-HarmonyTargets.ps1 -ModAssembly path\to\Mod.dll -GameDir "D:\...\7 Days To Die"
#>
[CmdletBinding()]
param(
    [string] $ModAssembly,
    [string] $GameDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $GameDir) {
    $candidates = @(
        $env:SDTD_GAMEDIR,
        'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die',
        'C:\Steam\steamapps\common\7 Days To Die'
    )
    $GameDir = $candidates | Where-Object { $_ -and (Test-Path (Join-Path $_ '7DaysToDie_Data\Managed\Assembly-CSharp.dll')) } | Select-Object -First 1
}
if (-not $GameDir) { throw "Could not find the game. Pass -GameDir or set SDTD_GAMEDIR." }

if (-not $ModAssembly) {
    $ModAssembly = Join-Path $repoRoot 'WeaponAffixesProject\WeaponAffixesProject\bin\Debug\WeaponAffixesProject.dll'
}
if (-not (Test-Path $ModAssembly)) { throw "Mod assembly not found: $ModAssembly. Build the project first." }

$gameAsmPath = Join-Path $GameDir '7DaysToDie_Data\Managed\Assembly-CSharp.dll'
Add-Type -Path (Join-Path $GameDir 'Mods\0_TFP_Harmony\Mono.Cecil.dll')

Write-Host "Game : $gameAsmPath"
Write-Host "Mod  : $ModAssembly`n"

$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($gameAsmPath)
$mod  = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ModAssembly)
$gameModule = $game.MainModule

# Harmony's injected parameters are not parameters of the target method.
$injected = @('__instance', '__result', '__state', '__originalMethod', '__runOriginal', '__args', '__exception')

$problems = New-Object System.Collections.Generic.List[string]
$manual   = New-Object System.Collections.Generic.List[string]
$checked  = 0

function Get-PatchInfo($provider) {
    # Collapses the [HarmonyPatch] attributes on one member into a (type, method) pair.
    # They stack: [HarmonyPatch(typeof(X))] + [HarmonyPatch(nameof(X.Y))] is the same as
    # [HarmonyPatch(typeof(X), "Y")].
    $declaringType = $null; $methodName = $null; $any = $false
    foreach ($a in $provider.CustomAttributes) {
        if ($a.AttributeType.Name -ne 'HarmonyPatch') { continue }
        $any = $true
        foreach ($arg in $a.ConstructorArguments) {
            $v = $arg.Value
            if ($arg.Type.FullName -eq 'System.Type' -and $v) { $declaringType = $v.FullName }
            elseif ($arg.Type.FullName -eq 'System.String' -and $v) { $methodName = [string]$v }
        }
    }
    if (-not $any) { return $null }
    [pscustomobject]@{ DeclaringType = $declaringType; MethodName = $methodName }
}

function Resolve-GameType($fullName) {
    if (-not $fullName) { return $null }
    $t = $gameModule.GetType($fullName)
    if ($t) { return $t }
    # Cecil wants nested types as Outer/Inner; the attribute records Outer+Inner.
    return $gameModule.GetType(($fullName -replace '\+', '/'))
}

function Find-TargetMethods($type, $name) {
    $t = $type; $found = @()
    while ($t) {
        $found += @($t.Methods | Where-Object { $_.Name -eq $name })
        if ($found.Count -gt 0) { break }
        if (-not $t.BaseType) { break }
        try { $t = $t.BaseType.Resolve() } catch { break }
    }
    return $found
}

$allTypes = New-Object System.Collections.Generic.List[object]
function Add-TypeRecursive($t) {
    $allTypes.Add($t) | Out-Null
    foreach ($n in $t.NestedTypes) { Add-TypeRecursive $n }
}
foreach ($t in $mod.MainModule.Types) { Add-TypeRecursive $t }

foreach ($type in $allTypes) {
    $classInfo = Get-PatchInfo $type
    $patchMethods = @($type.Methods | Where-Object {
        $_.Name -in @('Prefix','Postfix','Transpiler','Finalizer') -or
        ($_.CustomAttributes | Where-Object { $_.AttributeType.Name -in @('HarmonyPrefix','HarmonyPostfix','HarmonyTranspiler','HarmonyFinalizer') })
    })
    if (-not $classInfo -and $patchMethods.Count -eq 0) { continue }

    $usesRuntimeTargets = @($type.Methods | Where-Object { $_.Name -in @('TargetMethod','TargetMethods') }).Count -gt 0
    if ($usesRuntimeTargets) {
        $manual.Add("$($type.FullName) - selects targets at runtime via TargetMethod/TargetMethods") | Out-Null
        continue
    }
    if (-not $classInfo) { continue }

    foreach ($pm in $patchMethods) {
        $methodInfo = Get-PatchInfo $pm
        $declType = if ($methodInfo -and $methodInfo.DeclaringType) { $methodInfo.DeclaringType } else { $classInfo.DeclaringType }
        $methName = if ($methodInfo -and $methodInfo.MethodName)   { $methodInfo.MethodName }   else { $classInfo.MethodName }
        if (-not $declType -or -not $methName) {
            $manual.Add("$($type.FullName).$($pm.Name) - could not read a target from its attributes") | Out-Null
            continue
        }

        $checked++
        $label = "$($type.FullName).$($pm.Name) -> $declType.$methName"

        $gt = Resolve-GameType $declType
        if (-not $gt) { $problems.Add("MISSING TYPE   $label") | Out-Null; continue }

        $targets = Find-TargetMethods $gt $methName
        if ($targets.Count -eq 0) { $problems.Add("MISSING METHOD $label") | Out-Null; continue }

        # With overloads, a parameter only has to match one of them.
        $targetParamNames = @($targets | ForEach-Object { $_.Parameters.Name }) | Select-Object -Unique
        $allStatic = @($targets | Where-Object { -not $_.IsStatic }).Count -eq 0

        foreach ($p in $pm.Parameters) {
            $n = $p.Name
            if ($n -eq '__instance' -and $allStatic) {
                $problems.Add("STATIC TARGET  $label - injects __instance but the target is static") | Out-Null
                continue
            }
            if ($n -in $injected -or $n.StartsWith('___')) { continue }
            if ($n -notin $targetParamNames) {
                $problems.Add("BAD PARAM      $label - '$n' is not a parameter of the target (has: $($targetParamNames -join ', '))") | Out-Null
            }
        }
    }
}

Write-Host "Statically resolved $checked patch method(s)." -ForegroundColor Cyan
if ($manual.Count -gt 0) {
    Write-Host "`nNeeds manual review:" -ForegroundColor Yellow
    $manual | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}
if ($problems.Count -gt 0) {
    Write-Host "`n$($problems.Count) problem(s):" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
Write-Host "`nAll statically resolvable Harmony targets exist." -ForegroundColor Green
exit 0
