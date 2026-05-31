Write-Host "Building MDView (Portable)..." -ForegroundColor Cyan

# Clean dist folder
if (Test-Path "dist") {
    Remove-Item -Recurse -Force "dist"
}
New-Item -ItemType Directory -Path "dist" | Out-Null

# Restore dependencies
dotnet restore src/NativeMDView.csproj

# Publish as single portable exe
dotnet publish src/NativeMDView.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o dist

# Copy docs
Copy-Item readme.md dist/
Copy-Item release.md dist/

Write-Host "`nBuild complete!" -ForegroundColor Green
Write-Host "Output: dist/MDView.exe" -ForegroundColor Yellow
$size = (Get-Item "dist/MDView.exe").Length / 1MB
Write-Host "Size: $('{0:N1}' -f $size) MB" -ForegroundColor Yellow
Write-Host "`nFiles in dist:" -ForegroundColor Yellow
Get-ChildItem dist/ | ForEach-Object { Write-Host "  $($_.Name) ($('{0:N1}' -f ($_.Length / 1KB)) KB)" }
