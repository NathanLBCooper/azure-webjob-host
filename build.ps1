param (
    [string]$build_number = "0",
    [string]$branch = "master"
)

$OUTPUT = Join-Path $PSScriptRoot "build"
$VERSION = "0.1." + $build_number
$PACKAGEVERSION = $VERSION
$branch = $branch.Replace("/merge","pr")

if(!$branch.EndsWith("master")) {
    $PACKAGEVERSION += "-" + $branch
}

Write-Host "Building version " $VERSION "for branch" $branch

Get-ChildItem -Path $OUTPUT -Include * | remove-Item -recurse
dotnet clean AzureWebjobHost.sln

if(!$LASTEXITCODE) { dotnet restore AzureWebjobHost.sln }
if(!$LASTEXITCODE) { dotnet build AzureWebjobHost.sln -c Release }

if(!$LASTEXITCODE) { dotnet test --no-build --verbosity normal test\AzureWebjobHostTests\AzureWebjobHostTests.csproj -c Release }

if(!$LASTEXITCODE) { dotnet pack src\AzureWebjobHost\AzureWebjobHost.csproj --configuration RELEASE --output $OUTPUT\nupkgs /p:Version=$PACKAGEVERSION  /p:FileVersion=$VERSION /p:AssemblyVersion=$VERSION }

exit $LASTEXITCODE