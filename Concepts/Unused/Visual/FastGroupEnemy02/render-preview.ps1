Add-Type -AssemblyName System.Drawing
$taskBitmap = [System.Drawing.Bitmap]::new(2000,1040)
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
$taskGraphics.Clear([System.Drawing.Color]::FromArgb(25,29,32))
$taskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$taskFont = [System.Drawing.Font]::new('Segoe UI',26,[System.Drawing.FontStyle]::Bold)
$taskLabel = [System.Drawing.Font]::new('Segoe UI',22,[System.Drawing.FontStyle]::Bold)
$taskSmall = [System.Drawing.Font]::new('Segoe UI',14)
$taskBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235,232,226))
$taskMuted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(166,175,180))
$taskPanel = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(40,46,51))
$taskGraphics.DrawString('10 форм · вид строго сверху', $taskFont,$taskBrush,24,14)
$taskGraphics.DrawString('Emergency Shift · TASK-005 · быстрые эскизы для выбора силуэта', $taskSmall,$taskMuted,27,58)
$taskNames=@('Стрела','Серп','Вилка','Ромб','Капля','Двойной узел','Лента','Вертушка','Пластина','Игла')
for ($taskIndex=0; $taskIndex -lt 10; $taskIndex++) {
    $taskColumn=$taskIndex % 5
    $taskRow=[Math]::Floor($taskIndex / 5)
    $taskX=12+$taskColumn*398
    $taskY=98+$taskRow*466
    $taskGraphics.FillRectangle($taskPanel,$taskX,$taskY,386,450)
    $taskNumber=($taskIndex+1).ToString('00')
    $taskGraphics.DrawString($taskNumber,$taskLabel,$taskBrush,($taskX+14),($taskY+10))
    $taskGraphics.DrawString($taskNames[$taskIndex],$taskSmall,$taskMuted,($taskX+63),($taskY+18))
    $taskImage=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot ($taskNumber+'-design.png')))
    $taskRatio=340/[Math]::Max($taskImage.Width,$taskImage.Height)
    $taskWidth=$taskImage.Width*$taskRatio
    $taskHeight=$taskImage.Height*$taskRatio
    $taskGraphics.DrawImage($taskImage,[float]($taskX+193-$taskWidth/2),[float]($taskY+254-$taskHeight/2),[float]$taskWidth,[float]$taskHeight)
    $taskImage.Dispose()
}
$taskBitmap.Save((Join-Path $PSScriptRoot 'contact-sheet.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose(); $taskBitmap.Dispose()
$taskFont.Dispose(); $taskLabel.Dispose(); $taskSmall.Dispose()
$taskBrush.Dispose(); $taskMuted.Dispose(); $taskPanel.Dispose()
