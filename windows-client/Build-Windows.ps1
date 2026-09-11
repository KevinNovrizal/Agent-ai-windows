$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /out:'.\AgentLokal.exe' /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll '.\AgentLokal.cs' '.\Connection.cs'
if($LASTEXITCODE -ne 0){throw 'Kompilasi gagal'}
