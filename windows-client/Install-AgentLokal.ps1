param([string]$KeyPath,[string]$KnownHostsPath)
$ErrorActionPreference='Stop'
$installDir=Join-Path $env:LOCALAPPDATA 'AgentLokal'
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'AgentLokal.exe') -Destination (Join-Path $installDir 'AgentLokal.exe') -Force
if($KeyPath -and $KnownHostsPath){
  $keyTarget=Join-Path $installDir 'server_access'
  $account=[Security.Principal.WindowsIdentity]::GetCurrent().Name
  if(Test-Path -LiteralPath $keyTarget){& icacls.exe $keyTarget /grant:r "${account}:F" | Out-Null}
  Copy-Item -LiteralPath $KeyPath -Destination $keyTarget -Force
  & icacls.exe $keyTarget /inheritance:r /grant:r "${account}:F" | Out-Null
  if($LASTEXITCODE -ne 0){throw 'Gagal membatasi izin kunci SSH.'}
  Copy-Item -LiteralPath $KnownHostsPath -Destination (Join-Path $installDir 'known_hosts') -Force
  @{host='192.168.201.238';user='kevin';key=$keyTarget;known_hosts=(Join-Path $installDir 'known_hosts')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $installDir 'settings.json') -Encoding UTF8
}
$shell=New-Object -ComObject WScript.Shell
$shortcut=$shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Agent Lokal.lnk'))
$shortcut.TargetPath=Join-Path $installDir 'AgentLokal.exe'
$shortcut.WorkingDirectory=$installDir
$shortcut.Description='Agent coding lokal melalui server RTX 5060'
$shortcut.Save()
Write-Output "Terpasang: $installDir. Gunakan Koneksi / pindah PC untuk impor profil."
