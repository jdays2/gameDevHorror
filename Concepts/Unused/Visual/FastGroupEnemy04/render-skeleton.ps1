param([string]$Skeleton='skeleton-plan.json',[string]$Art='', [string]$Output='skeleton-plan.png')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRig=Get-Content -LiteralPath (Join-Path $PSScriptRoot $Skeleton) -Raw | ConvertFrom-Json
$taskCanvas=[System.Drawing.Bitmap]::new([int]$taskRig.canvas.width,[int]$taskRig.canvas.height)
$taskG=[System.Drawing.Graphics]::FromImage($taskCanvas)
$taskG.Clear([System.Drawing.Color]::FromArgb(28,33,38))
$taskG.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$taskG.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
if($Art) { $taskImage=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $Art)); $taskG.DrawImage($taskImage,0,0,$taskCanvas.Width,$taskCanvas.Height); $taskImage.Dispose() }
$taskColors=@{body=[System.Drawing.Color]::FromArgb(70,220,245);upper=[System.Drawing.Color]::FromArgb(100,245,140);lower=[System.Drawing.Color]::FromArgb(255,180,60)}
$taskFont=[System.Drawing.Font]::new('Segoe UI',16,[System.Drawing.FontStyle]::Bold)
$taskNodes=@{}
foreach($taskNode in $taskRig.nodes) { $taskNodes[$taskNode.id]=$taskNode }
foreach($taskNode in $taskRig.nodes) {
    $taskColor=$taskColors[$taskNode.group]
    $taskPen=[System.Drawing.Pen]::new($taskColor,6)
    $taskBrush=[System.Drawing.SolidBrush]::new($taskColor)
    if($taskNode.parent) {
        $taskParent=$taskNodes[$taskNode.parent]
        $taskG.DrawLine([System.Drawing.Pens]::Black,[float]$taskParent.x,[float]$taskParent.y,[float]$taskNode.x,[float]$taskNode.y)
        $taskG.DrawLine($taskPen,[float]$taskParent.x,[float]$taskParent.y,[float]$taskNode.x,[float]$taskNode.y)
    }
    $taskG.FillEllipse([System.Drawing.Brushes]::Black,[float]($taskNode.x-10),[float]($taskNode.y-10),20,20)
    $taskG.FillEllipse($taskBrush,[float]($taskNode.x-6),[float]($taskNode.y-6),12,12)
    if($taskNode.id -notmatch 'toe|claw') {
        $taskG.DrawString($taskNode.id,$taskFont,[System.Drawing.Brushes]::Black,[float]($taskNode.x+11),[float]($taskNode.y+9))
        $taskG.DrawString($taskNode.id,$taskFont,$taskBrush,[float]($taskNode.x+10),[float]($taskNode.y+8))
    }
    $taskPen.Dispose();$taskBrush.Dispose()
}
$taskG.DrawString('2D CONCEPT SKELETON / BODY + 2 LIMBS / NOT A UNITY RIG',$taskFont,[System.Drawing.Brushes]::White,20,18)
$taskCanvas.Save((Join-Path $PSScriptRoot $Output),[System.Drawing.Imaging.ImageFormat]::Png)
$taskFont.Dispose();$taskG.Dispose();$taskCanvas.Dispose()
