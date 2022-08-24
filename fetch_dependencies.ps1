$FFXIV_ACT_Plugin_Url = 'https://github.com/ravahn/FFXIV_ACT_Plugin/raw/master/Releases/FFXIV_ACT_Plugin_SDK_2.6.6.3.zip'
$ArchiveName = $(Split-Path -Path $FFXIV_ACT_Plugin_Url -Leaf)
Invoke-WebRequest -Uri $FFXIV_ACT_Plugin_Url -OutFile $ArchiveName
$OutDir = './external_dependencies'
New-Item -Path $OutDir -ItemType Directory
Expand-Archive $ArchiveName -DestinationPath $OutDir
Remove-Item -Path $ArchiveName