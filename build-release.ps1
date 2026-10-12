$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$project = "FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj"
$publish = "FerreSys.Infrastructure.UI\bin\Release\net10.0-windows\win-x64\publish"

dotnet publish $project -c Release -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

vpk pack -u FerreSysApp -v 0.1.0 -o .\releases -e FerreSys.exe -p $publish
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
