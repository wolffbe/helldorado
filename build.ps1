# Builds Helldorado.exe (the drop-in fix) from .\src using the .NET Framework C# compiler (ships with Windows).
$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$src = Join-Path $PSScriptRoot "src\Helldorado.cs"
$out = Join-Path $PSScriptRoot "Helldorado.exe"
& $csc /nologo /optimize+ /target:winexe "/out:$out" /r:System.Windows.Forms.dll /r:System.dll $src
Write-Host "Built: $out"
