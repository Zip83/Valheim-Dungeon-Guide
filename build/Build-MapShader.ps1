param([Parameter(Mandatory=$true)][string]$UnityEditor)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repository 'assets/map-shader'
& $UnityEditor -batchmode -nographics -quit -projectPath $project -executeMethod BuildMapShader.Build -logFile -
if ($LASTEXITCODE -ne 0) { throw 'Unity shader build failed.' }
$bundle = Join-Path $project 'BundleOutput/dungeonguide-ui'
if (!(Test-Path -LiteralPath $bundle)) { throw 'Unity produced no shader bundle.' }
$destination = Join-Path $repository 'src/DungeonGuide/Content'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath $bundle -Destination (Join-Path $destination 'dungeonguide-ui')
Write-Output 'Built the optional UI shader; package dungeonguide-ui beside DungeonGuide.dll.'
