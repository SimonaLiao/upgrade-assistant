$dotnetRoot = Join-Path $RepoRoot '.dotnet'
$dotnetSdkVersion = $GlobalJson.tools.dotnet

try {
  
  # dotnet workload install maui --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install android --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install ios --disable-parallel --verbosity diag --temp-dir $dotnetRoot

  dotnet tool install -g Redth.Net.Maui.Check
  maui-check --ci --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2

  # maui-check --main --ci --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2


  # dotnet workload install maui --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload repair --sdk-version $dotnetSdkVersion
  # dotnet workload update --sdk-version $dotnetSdkVersion
  # dotnet workload install android-aot --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install ios --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0