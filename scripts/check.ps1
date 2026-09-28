$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    foreach ($tool in @('dotnet', 'node')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Install $tool before running checks. See README.md." }
    }
    dotnet build Dawn/Dawn.csproj --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
    node tests/dawnBehavior.test.cjs
    if ($LASTEXITCODE -ne 0) { throw 'Behavior checks failed.' }
    dotnet run --project tests/MemoryRegression/MemoryRegression.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Memory regression failed.' }
    dotnet run --project tests/MemoryRegression/MemoryRegression.csproj --no-build -- --startup
    if ($LASTEXITCODE -ne 0) { throw 'Startup smoke test failed.' }
    Write-Host 'All offline checks passed.'
}
finally { Pop-Location }
