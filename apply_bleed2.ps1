$files = Get-ChildItem "Content/Buffs" -Filter "*.png" -File -Recurse | Where-Object { $_.FullName -notlike "*\Backup\*" }
foreach ($f in $files) {
    magick "$($f.FullName)" -set option:distort:viewport "%[fx:w+4]x%[fx:h+4]-2-2" -virtual-pixel Edge -filter point -distort SRT 0 +repage "$($f.FullName)"
}
