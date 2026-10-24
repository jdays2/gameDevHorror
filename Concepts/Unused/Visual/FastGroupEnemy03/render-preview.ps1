Add-Type -AssemblyName System.Drawing
$taskCanvas=[System.Drawing.Bitmap]::new(1100,700)
$taskG=[System.Drawing.Graphics]::FromImage($taskCanvas)
$taskG.Clear([System.Drawing.Color]::FromArgb(28,33,37))
$taskG.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskG.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$taskFont=[System.Drawing.Font]::new('Segoe UI',21,[System.Drawing.FontStyle]::Bold)
$taskSmall=[System.Drawing.Font]::new('Segoe UI',13)
$taskWhite=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235,231,224))
$taskMuted=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(173,184,190))
$taskFiles=@('01-mouth-v9-blood.png','01-mouth-v9-bend-study.png')
$taskNames=@('Текущая форма · v9','Эскиз изгиба и другого шага')
$taskG.DrawString('TASK-005 · цельная гибкая форма', $taskFont,$taskWhite,20,13)
$taskG.DrawString('Концепт и проверочная поза. Это не готовый риг или кадры анимации Unity.', $taskSmall,$taskMuted,22,51)
for($taskIndex=0;$taskIndex -lt 2;$taskIndex++) {
    $taskX=20+$taskIndex*550
    $taskG.DrawString($taskNames[$taskIndex],$taskSmall,$taskWhite,$taskX,92)
    $taskImage=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $taskFiles[$taskIndex]))
    $taskScale=510/[Math]::Max($taskImage.Width,$taskImage.Height)
    $taskWidth=$taskImage.Width*$taskScale
    $taskHeight=$taskImage.Height*$taskScale
    $taskG.DrawImage($taskImage,[float]($taskX+255-$taskWidth/2),[float](300-$taskHeight/2),[float]$taskWidth,[float]$taskHeight)
    foreach($taskSize in @(144,80)) {
        $taskCenter=$taskX+$(if($taskSize -eq 144){155}else{365})
        $taskRatio=$taskSize/[Math]::Max($taskImage.Width,$taskImage.Height)
        $taskW=$taskImage.Width*$taskRatio
        $taskH=$taskImage.Height*$taskRatio
        $taskG.DrawImage($taskImage,[float]($taskCenter-$taskW/2),[float](577-$taskH/2),[float]$taskW,[float]$taskH)
        $taskG.DrawString(($taskSize.ToString()+' px'),$taskSmall,$taskMuted,($taskCenter-25),650)
    }
    $taskImage.Dispose()
}
$taskCanvas.Save((Join-Path $PSScriptRoot 'review-v9.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskG.Dispose();$taskCanvas.Dispose();$taskFont.Dispose();$taskSmall.Dispose();$taskWhite.Dispose();$taskMuted.Dispose()
