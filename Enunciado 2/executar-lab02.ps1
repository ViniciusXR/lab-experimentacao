# LAB 02 — equivale a: dotnet run na Sprint 3 (S1 → S2 → S3).
# Repasse argumentos: .\executar-lab02.ps1 --apenas-sprint3
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
dotnet run --project (Join-Path $root "Sprint 3\Sprint3.csproj") -- @args
