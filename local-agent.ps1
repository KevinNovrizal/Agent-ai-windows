param([Parameter(Position=0)][string]$Workspace=(Get-Location).Path,[Parameter(ValueFromRemainingArguments=$true)][string[]]$AgentArgs)
$ErrorActionPreference='Stop'; $root=Split-Path -Parent $PSScriptRoot; $key=Join-Path $root 'work\server_access_user'; $known=Join-Path $root 'work\known_hosts'
if(!(Test-Path $key)){throw "Kunci SSH tidak ditemukan: $key"}; if(!(Test-Path $Workspace -PathType Container)){throw "Workspace tidak ditemukan: $Workspace"}
$name=Split-Path -Leaf $Workspace; $remote=(ssh -i $key -o BatchMode=yes -o UserKnownHostsFile=$known kevin@192.168.201.238 "mkdir -p /home/kevin/workspaces/$name && realpath /home/kevin/workspaces/$name").Trim()
scp -r -i $key -o BatchMode=yes -o UserKnownHostsFile=$known "$Workspace\*" "kevin@192.168.201.238:$remote/"; if($LASTEXITCODE){throw 'Sinkronisasi ke server gagal.'}
$quoted=($AgentArgs|%{'"'+($_ -replace '"','\"')+'"'}) -join ' '; ssh -tt -i $key -o UserKnownHostsFile=$known kevin@192.168.201.238 "cd /home/kevin/local-coding-agent && ./agent '$remote' $quoted"; $code=$LASTEXITCODE
scp -r -i $key -o BatchMode=yes -o UserKnownHostsFile=$known "kevin@192.168.201.238:$remote/*" "$Workspace\"; exit $code
