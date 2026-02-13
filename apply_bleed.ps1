$files = Get-ChildItem "Content/Buffs" -Filter "*.png" -File -Recurse | Where-Object { $_.FullName -notlike "*\Backup\*" }
foreach ($f in $files) {
    magick $f.FullName -set option:distort:viewport "%[fx:w+2]x%[fx:h+2]-1-1" -virtual-pixel Edge -filter point -distort SRT 0 +repage $f.FullName
}
