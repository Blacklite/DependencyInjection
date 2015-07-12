$ENV:DNX_FEED='https://www.nuget.org/api/v2'
if (Test-Path artifacts) {
    Remove-Item artifacts -Force -Recurse
}
mkdir artifacts > $null

if (!(Get-Command "dvnm")) {
    $Branch='dev';
    iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'));

    $ENV:PATH="$ENV:PATH;$ENV:USERPROFILE\.dnx\bin";
    $ENV:DNX_HOME="$ENV:USERPROFILE\.dnx";
}

dnvm install 1.0.0-beta5
dnvm use 1.0.0-beta5
$ENV:PATH="$ENV:USERPROFILE\.dnx\runtimes\dnx-clr-win-x86.1.0.0-beta5\bin;$ENV:PATH"

$configuration="Debug"
$ENV:DNX_BUILD_VERSION="alpha"

if ($ENV:APPVEYOR) {
    $paddedBuildNumber = $ENV:APPVEYOR_BUILD_NUMBER.PadLeft(5, '0');
    IF ($ENV:APPVEYOR_REPO_TAG -eq "false" -or $ENV:APPVEYOR_REPO_TAG -eq "False") {
        if ($ENV:APPVEYOR_REPO_BRANCH -eq "master") {
            $ENV:DNX_BUILD_VERSION="beta-$paddedBuildNumber";
        } else {
            $ENV:DNX_BUILD_VERSION="beta-branch-$ENV:APPVEYOR_REPO_BRANCH-$paddedBuildNumber";
        }
    }
    IF ($ENV:APPVEYOR_REPO_TAG -eq "true" -or $ENV:APPVEYOR_REPO_TAG -eq "True") {
        $ENV:DNX_BUILD_VERSION=$paddedBuildNumber;
    }
}

echo "DNX_BUILD_VERSION: $ENV:DNX_BUILD_VERSION"

dnu restore

if ($lastexitcode -gt 0) { exit $lastexitcode }

$buildFailed = $false;
foreach ($srcFolder in Get-ChildItem src\* -Directory) {
    dnu build $srcFolder.FullName
    if ($lastexitcode -gt 0) { $buildFailed = $lastexitcode; }
}

if ($buildFailed) {
    exit 1;
}

$testsFailed = $false;
foreach ($testFolder in Get-ChildItem test\* -Directory) {
    dnx $testFolder.FullName test
    if ($lastexitcode -gt 0) { $testsFailed = $lastexitcode; }
}

if ($testsFailed) {
    exit 1;
}

$packFailed = $false;
foreach ($srcFolder in Get-ChildItem src\* -Directory) {
    dnu pack $srcFolder.FullName --out artifacts\build --configuration $configuration
    if ($lastexitcode -gt 0) { $packFailed = $lastexitcode; }
}

if ($packFailed) {
    exit 1;
}

foreach ($item in Get-ChildItem artifacts\build\$configuration\*.nupkg -Exclude *.symbols.nupkg) {
    copy $item.FullName artifacts\build
}

mkdir artifacts\symbols > $null

foreach ($item in Get-ChildItem artifacts\build\$configuration\*.symbols.nupkg) {
    copy $item.FullName artifacts\symbols
}

Remove-Item artifacts\build\$configuration -Force -Recurse
