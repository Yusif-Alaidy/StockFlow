param([string]$TestConnection = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
$previous = $env:STOCKFLOW_TEST_SQL
try {
    $env:STOCKFLOW_TEST_SQL = $TestConnection
    dotnet restore backend/stockFlow/stockFlow.slnx
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
    dotnet build backend/stockFlow/stockFlow.slnx --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    dotnet test backend/stockFlow/stockFlow.slnx --no-build --no-restore --logger "trx;LogFilePrefix=StockFlow"
    if ($LASTEXITCODE -ne 0) { throw "Tests failed. Check TestResults and the first error." }
} finally { $env:STOCKFLOW_TEST_SQL = $previous; Pop-Location }
