@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0local-agent.ps1" %*
