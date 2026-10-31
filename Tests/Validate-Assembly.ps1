param([string]$AssemblyPath = (Join-Path $PSScriptRoot '../Assemblies/AMJAgriculture.Dining.dll'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata
$stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $AssemblyPath).Path)
try {
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $references = @($metadata.AssemblyReferences | ForEach-Object { $metadata.GetString($metadata.GetAssemblyReference($_).Name) })
        if ($references | Where-Object { $_ -like 'DailyMenu*' }) { throw 'Daily Menu assembly reference remains' }
        Write-Output ('PASS assembly references: ' + ($references -join ', '))
    } finally { $pe.Dispose() }
} finally { $stream.Dispose() }
