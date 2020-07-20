New-Item -ItemType Directory -Force -Path ".\TorchBinaries\Plugins\FactionInfo"
copy-item -path ".\FactionInfo\bin\Debug\*" -Destination ".\TorchBinaries\Plugins\FactionInfo" -Force