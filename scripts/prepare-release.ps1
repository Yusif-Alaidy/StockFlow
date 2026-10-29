$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Tool restore failed" }
    dotnet build backend/stockFlow/stockFlow.slnx -c Release
    if ($LASTEXITCODE -ne 0) { throw "Release build failed" }
    New-Item artifacts -ItemType Directory -Force | Out-Null
    dotnet ef migrations script --idempotent --project backend/stockFlow/src/stockFlow.Infrastructure --startup-project backend/stockFlow/src/stockFlow.Api --context StockFlowDbContext --configuration Release --no-build --output artifacts/migrations.sql
    if ($LASTEXITCODE -ne 0) { throw "Migration script generation failed" }
    dotnet publish backend/stockFlow/src/stockFlow.Api/stockFlow.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw "Publish build failed" }
    Write-Host "Prepared artifacts/migrations.sql and artifacts/publish. No database changed and nothing uploaded."
} finally { Pop-Location }
