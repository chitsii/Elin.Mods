param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ApiSurveyRoot = $env:ELIN_API_SURVEY_CS
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ApiSurveyRoot)) {
    throw 'ApiSurveyRoot is required. Pass -ApiSurveyRoot <path-to-elin-dll-api-survey\cs> or set ELIN_API_SURVEY_CS.'
}

if (-not (Test-Path -LiteralPath $ApiSurveyRoot -PathType Container)) {
    throw "ApiSurveyRoot does not exist or is not a directory: $ApiSurveyRoot"
}

function Assert-Match {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Pattern
    )

    if ($Text -notmatch $Pattern) {
        throw "FAIL: $Name"
    }
}

function Assert-NoMatch {
    param(
        [string]$Name,
        [string]$Text,
        [string]$Pattern
    )

    if ($Text -match $Pattern) {
        throw "FAIL: $Name"
    }
}

$statsHunger = Get-Content -Raw (Join-Path $ApiSurveyRoot 'StatsHunger\StatsHunger.decompiled.cs')
$chara = Get-Content -Raw (Join-Path $ApiSurveyRoot 'Chara\Chara.decompiled.cs')
$foodEffect = Get-Content -Raw (Join-Path $ApiSurveyRoot 'FoodEffect\FoodEffect.decompiled.cs')

$modConfig = Get-Content -Raw (Join-Path $RepoRoot 'Elin_AutoEatSleep\src\ModConfig.cs')
$autoEatLogic = Get-Content -Raw (Join-Path $RepoRoot 'Elin_AutoEatSleep\src\AutoEatLogic.cs')
$project = Get-Content -Raw (Join-Path $RepoRoot 'Elin_AutoEatSleep\src\Elin_AutoEatSleep.csproj')

Assert-Match 'public API keeps StatsHunger.Hungry at phase 3' $statsHunger 'public\s+const\s+int\s+Hungry\s*=\s*3\s*;'
Assert-Match 'Chara.InstantEat delegates to FoodEffect.Proc' $chara 'public\s+void\s+InstantEat[\s\S]*?FoodEffect\.Proc\(this,\s*t\);'
Assert-Match 'FoodEffect.Proc can change hunger during InstantEat' $foodEffect 'c\.hunger\.Mod\(-num3\);'

Assert-Match 'default HungerThreshold follows StatsHunger.Hungry' $modConfig 'HungerThreshold\s*=\s*config\.Bind\("AutoEat",\s*"HungerThreshold",\s*3,'
Assert-Match 'AutoEat has an InstantEat reentry guard' $autoEatLogic 'private\s+static\s+bool\s+_isEating\s*;'
Assert-Match 'AutoEat skips nested CheckAutoEat calls while InstantEat is active' $autoEatLogic 'if\s*\(_isEating\)\s*return\s*;'
Assert-Match 'InstantEat guard is released in finally' $autoEatLogic 'finally\s*\{\s*_isEating\s*=\s*false;\s*\}'

Assert-NoMatch 'project does not depend on unavailable BepInEx NuGet packages' $project 'PackageReference\s+Include="BepInEx\.'
Assert-Match 'project references installed BepInEx.Core from the game path' $project '<Reference\s+Include="BepInEx\.Core">'
Assert-Match 'project references installed BepInEx.Unity from the game path' $project '<Reference\s+Include="BepInEx\.Unity">'
Assert-NoMatch 'normal build target does not deploy into game folders' $project '<Target\s+Name="PostBuild"[\s\S]*?(BepInEx\\plugins|Package\\Elin_AutoEatSleep|xcopy)'
Assert-NoMatch 'explicit Deploy target does not double-load through BepInEx plugins' $project 'DeployPluginPath|BepInExPath\)\\plugins|DestinationFolder="\$\(DeployPluginPath\)"'

Write-Host 'AutoEatSleep source-linked regression checks passed.'
