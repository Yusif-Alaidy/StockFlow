param([Parameter(Mandatory=$true)][string]$Email)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
$previousEmail = $env:STOCKFLOW_BOOTSTRAP_EMAIL
$previousPassword = $env:STOCKFLOW_BOOTSTRAP_PASSWORD
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    # Local API User Secrets load only in Development. A supplied connection/environment is preserved.
    if (-not $env:ASPNETCORE_ENVIRONMENT -and -not $env:ConnectionStrings__DefaultConnection) {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
    }
    $secure = Read-Host "New Admin password (12+ characters, letter and digit)" -AsSecureString
    $credential = New-Object System.Management.Automation.PSCredential($Email, $secure)
    $env:STOCKFLOW_BOOTSTRAP_EMAIL = $Email
    $env:STOCKFLOW_BOOTSTRAP_PASSWORD = $credential.GetNetworkCredential().Password
    dotnet run --project backend/stockFlow/src/stockFlow.Api --no-launch-profile -- --bootstrap-admin
    if ($LASTEXITCODE -ne 0) { throw "Bootstrap failed. Confirm connection, migrations and that Users is empty." }
} finally {
    $env:STOCKFLOW_BOOTSTRAP_EMAIL = $previousEmail
    $env:STOCKFLOW_BOOTSTRAP_PASSWORD = $previousPassword
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $credential = $null; $secure = $null
    Pop-Location
}
