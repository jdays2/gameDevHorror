# Downscale original Unity game captures for review; no artwork, content or aspect ratio is altered.
Add-Type -AssemblyName System.Drawing
function Export-DoorFrame($doorSourcePath, $doorOutputPath) {
        $doorSource = [System.Drawing.Image]::FromFile((Join-Path (Get-Location) $doorSourcePath))
        $doorWidth = 1600
        $doorHeight = [int]($doorSource.Height * $doorWidth / $doorSource.Width)
        $doorPreview = [System.Drawing.Bitmap]::new($doorWidth, $doorHeight)
        $doorGraphics = [System.Drawing.Graphics]::FromImage($doorPreview)
        try {
            $doorGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $doorGraphics.DrawImage($doorSource, 0, 0, $doorWidth, $doorHeight)
            $doorPreview.Save((Join-Path (Get-Location) $doorOutputPath), [System.Drawing.Imaging.ImageFormat]::Jpeg)
        }
        finally {
            $doorGraphics.Dispose()
            $doorPreview.Dispose()
            $doorSource.Dispose()
        }
}
$doorOutput = 'Docs/Validation/DoorCrush01'
foreach ($doorSpecies in @('stalker', 'sleeper')) {
    foreach ($doorFrame in @('000', '002', '012')) {
        Export-DoorFrame "work/door-crush01/frames-v2-$doorSpecies/$doorFrame.png" "$doorOutput/$doorSpecies-$doorFrame.jpg"
    }
}
Export-DoorFrame 'work/door-crush01/final-layout.png' "$doorOutput/layout.jpg"
