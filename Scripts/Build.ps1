param(
    [Parameter(Mandatory=$true)][string]$RimWorldManagedDir,
    [Parameter(Mandatory=$true)][string]$HarmonyAssembly,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../build')
)
$ErrorActionPreference = 'Stop'
$dotnet = (Get-Command dotnet).Source
$sdkRoot = Join-Path (Split-Path $dotnet) 'sdk'
$compiler = Get-ChildItem $sdkRoot -Filter csc.dll -Recurse | Sort-Object FullName | Select-Object -Last 1
if (-not $compiler) { throw 'Roslyn compiler not found in installed .NET SDK.' }
$references = @(Get-ChildItem -LiteralPath $RimWorldManagedDir -Filter *.dll | ForEach-Object { $_.FullName })
$references += @((Resolve-Path -LiteralPath $HarmonyAssembly).Path)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = Join-Path (Resolve-Path -LiteralPath $OutputDirectory).Path 'AMJAgriculture.Dining.dll'
$sources = @(Get-ChildItem (Join-Path $PSScriptRoot '../Source/AMJAgriculture.Dining') -Filter *.cs | ForEach-Object { $_.FullName })
$arguments = @($compiler.FullName, '-nologo', '-target:library', '-nostdlib+', '-langversion:latest', '-deterministic+', ('-out:' + $output))
$arguments += @($references | ForEach-Object { '-r:' + $_ })
$arguments += $sources
& $dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Dining compilation failed.' }
Write-Output "Built $output"
