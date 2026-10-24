param([string]$OutputDirectory = $PSScriptRoot)
Add-Type -AssemblyName System.Drawing
$taskTypeface = 'Segoe UI'
$taskWhite = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(232,231,224))
$taskMuted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(175,184,190))
$taskTitle = [System.Drawing.Font]::new($taskTypeface,28,[System.Drawing.FontStyle]::Bold)
$taskHeading = [System.Drawing.Font]::new($taskTypeface,22,[System.Drawing.FontStyle]::Bold)
$taskText = [System.Drawing.Font]::new($taskTypeface,14)
$taskSmall = [System.Drawing.Font]::new($taskTypeface,12)
$taskFiles = @('A-tendon.png','B-brace.png','C-spindle.png')
$taskNames = @('A · Жгут — рекомендуется','B · Скоба','C · Веретено')
$taskSubtitles = @('4 лапы · длинный корпус · задний толчок','4 лапы · широкая дуга · задняя выемка','6 лап · узкое тело · три пары опор')
$taskImages = @($taskFiles | ForEach-Object { [System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $_)) })
function Draw-Source($Graphics, $Image, [float]$CenterX, [float]$CenterY, [float]$LongEdge, [float]$Angle = 0) {
    $taskRatio = $LongEdge / [Math]::Max($Image.Width, $Image.Height)
    $taskWidth = $Image.Width * $taskRatio
    $taskHeight = $Image.Height * $taskRatio
    $taskSaved = $Graphics.Save()
    $Graphics.TranslateTransform($CenterX, $CenterY)
    $Graphics.RotateTransform($Angle)
    $Graphics.DrawImage($Image, -$taskWidth / 2, -$taskHeight / 2, $taskWidth, $taskHeight)
    $Graphics.Restore($taskSaved)
}
$taskSheet = [System.Drawing.Bitmap]::new(1440,820)
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskSheet)
$taskGraphics.Clear([System.Drawing.Color]::FromArgb(23,27,31))
$taskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$taskGraphics.DrawString('TASK-005 · Быстрый групповой враг', $taskTitle, $taskWhite, 28,18)
$taskGraphics.DrawString('Три концепта строго сверху · 24.10.2026 · выбор и авторская оценка остаются владельцу', $taskText, $taskMuted, 30,65)
for($taskIndex=0; $taskIndex -lt 3; $taskIndex++) {
    $taskLeft = 24 + $taskIndex * 472
    $taskGraphics.FillRectangle([System.Drawing.Brushes]::DarkSlateGray,$taskLeft,102,448,366)
    Draw-Source $taskGraphics $taskImages[$taskIndex] ($taskLeft+224) 285 330
    $taskGraphics.DrawString($taskNames[$taskIndex],$taskHeading,$taskWhite,($taskLeft+12),484)
    $taskGraphics.DrawString($taskSubtitles[$taskIndex],$taskSmall,$taskMuted,($taskLeft+12),530)
    $taskGraphics.DrawString('Уменьшение без изменения исходного PNG',$taskSmall,$taskMuted,($taskLeft+12),570)
    Draw-Source $taskGraphics $taskImages[$taskIndex] ($taskLeft+76) 682 144
    Draw-Source $taskGraphics $taskImages[$taskIndex] ($taskLeft+230) 682 108
    Draw-Source $taskGraphics $taskImages[$taskIndex] ($taskLeft+370) 682 64
    $taskGraphics.DrawString('144 px',$taskSmall,$taskMuted,($taskLeft+50),766)
    $taskGraphics.DrawString('108 px',$taskSmall,$taskMuted,($taskLeft+205),766)
    $taskGraphics.DrawString('64 px',$taskSmall,$taskMuted,($taskLeft+350),766)
}
$taskSheet.Save((Join-Path $OutputDirectory 'comparison.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose()
$taskSheet.Dispose()
$taskSheet = [System.Drawing.Bitmap]::new(1440,550)
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskSheet)
$taskGraphics.Clear([System.Drawing.Color]::FromArgb(23,27,31))
$taskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$taskGraphics.DrawString('Четыре особи · проверка контура и прозрачности', $taskTitle,$taskWhite,28,18)
$taskGraphics.DrawString('Статичная композиция 108 px по длинной стороне холста. Это не Unity/URP и не проверка AI.', $taskText,$taskMuted,30,65)
$taskBackgrounds = @([System.Drawing.Color]::FromArgb(42,57,67),[System.Drawing.Color]::FromArgb(170,169,160),[System.Drawing.Color]::FromArgb(14,17,21))
for($taskIndex=0; $taskIndex -lt 3; $taskIndex++) {
    $taskLeft = 24 + $taskIndex * 472
    $taskGraphics.DrawString($taskNames[$taskIndex],$taskHeading,$taskWhite,($taskLeft+10),100)
    for($taskRow=0; $taskRow -lt 3; $taskRow++) {
        $taskBrush = [System.Drawing.SolidBrush]::new($taskBackgrounds[$taskRow])
        $taskY = 151 + 125*$taskRow
        $taskGraphics.FillRectangle($taskBrush,$taskLeft,$taskY,448,118)
        $taskBrush.Dispose()
        for($taskMember=0; $taskMember -lt 4; $taskMember++) {
            Draw-Source $taskGraphics $taskImages[$taskIndex] ($taskLeft+55+$taskMember*111) ($taskY+59) 108 (20+$taskMember*75)
        }
    }
}
$taskSheet.Save((Join-Path $OutputDirectory 'group-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose()
$taskSheet.Dispose()
$taskImages | ForEach-Object { $_.Dispose() }
$taskWhite.Dispose(); $taskMuted.Dispose()
$taskTitle.Dispose(); $taskHeading.Dispose(); $taskText.Dispose(); $taskSmall.Dispose()
Write-Output 'Rendered comparison.png and group-preview.png from unchanged sources.'
