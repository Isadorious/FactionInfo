Param([string]$SolutionDir)
New-Item -ItemType Directory -Force -Path "$SolutionDir\TorchBinaries\Plugins\FactionInfo"
copy-item -path "$SolutionDir\FactionInfo\bin\x64\Debug\*" -Destination "$SolutionDir\TorchBinaries\Plugins\FactionInfo" -Force