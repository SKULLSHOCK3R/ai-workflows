# Build-Release.ps1 - compile GoWTriggers.exe with Windows' own C# compiler, run its self-test, and make the
# release folder and zip (forward-slash entry names, round-trip verified).
# PowerShell 5.1. Usage: powershell -NoProfile -ExecutionPolicy Bypass -File Build-Release.ps1 -OutDir <folder>
param([Parameter(Mandatory = $true)][string]$OutDir, [string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path -LiteralPath $OutDir).Path      # .NET calls below ignore PowerShell's current folder
$src = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$name = "GoWTriggers-$Version"
$stage = Join-Path $OutDir $name
if (Test-Path -LiteralPath $stage) { throw "already exists: $stage (choose a new version or folder)" }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

& $csc /nologo /optimize /platform:x64 /reference:System.Web.Extensions.dll "/out:$stage\GoWTriggers.exe" "$src\GoWTriggers.cs"
if ($LASTEXITCODE -ne 0) { throw "compile failed ($LASTEXITCODE)" }
& "$stage\GoWTriggers.exe" --selftest
if ($LASTEXITCODE -ne 0) { throw "self-test failed" }
foreach ($f in 'GoWTriggers.cs', 'README.txt', 'GoWTriggers.example.json') { Copy-Item -LiteralPath "$src\$f" -Destination "$stage\$f" }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = Join-Path $OutDir "$name.zip"
$fs = [IO.File]::Open($zip, [IO.FileMode]::CreateNew)
$za = New-Object IO.Compression.ZipArchive($fs, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in Get-ChildItem -LiteralPath $stage -File) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $f.FullName, "$name/$($f.Name)") | Out-Null
    }
} finally { $za.Dispose(); $fs.Dispose() }

# round trip: every entry's bytes match the staged file
$za = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($e in $za.Entries) {
        if ($e.FullName -match '\\') { throw "backslash in entry name: $($e.FullName)" }
        $ms = New-Object IO.MemoryStream; $s = $e.Open(); $s.CopyTo($ms); $s.Dispose()
        $disk = [IO.File]::ReadAllBytes((Join-Path $stage $e.Name))
        $sha = [Security.Cryptography.SHA256]::Create()
        $a = [BitConverter]::ToString($sha.ComputeHash($ms.ToArray())); $b = [BitConverter]::ToString($sha.ComputeHash($disk))
        if ($a -ne $b) { throw "zip entry differs: $($e.FullName)" }
        "{0,-40} {1,8} bytes  ok" -f $e.FullName, $disk.Length
    }
} finally { $za.Dispose() }
"zip: $zip  SHA-256 " + (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
