param([string]$Art='', [string]$Prefix='plan-v13')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRig=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'skeleton-v13.json') -Raw|ConvertFrom-Json
$taskMuscles=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'muscles-v13.json') -Raw|ConvertFrom-Json
$taskW=[int]$taskRig.canvas.width;$taskH=[int]$taskRig.canvas.height
$taskNodes=@{};foreach($taskNode in $taskRig.nodes){$taskNodes[$taskNode.id]=$taskNode}
$taskColors=@{body=[System.Drawing.Color]::FromArgb(65,205,240);upper=[System.Drawing.Color]::FromArgb(75,230,125);lower=[System.Drawing.Color]::FromArgb(255,170,55)}
$taskBoneLayer=[System.Drawing.Bitmap]::new($taskW,$taskH)
$taskBoneG=[System.Drawing.Graphics]::FromImage($taskBoneLayer);$taskBoneG.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
foreach($taskNode in $taskRig.nodes){$taskColor=$taskColors[$taskNode.group];$taskPen=[System.Drawing.Pen]::new($taskColor,6);$taskBrush=[System.Drawing.SolidBrush]::new($taskColor);if($taskNode.parent){$taskP=$taskNodes[$taskNode.parent];$taskBoneG.DrawLine($taskPen,[float]$taskP.x,[float]$taskP.y,[float]$taskNode.x,[float]$taskNode.y)};$taskBoneG.FillEllipse($taskBrush,[float]($taskNode.x-7),[float]($taskNode.y-7),14,14);$taskPen.Dispose();$taskBrush.Dispose()}
$taskBoneG.Dispose()
$taskBoneLayer.Save((Join-Path $PSScriptRoot 'bones-layer.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskMuscleLayer=[System.Drawing.Bitmap]::new($taskW,$taskH)
$taskMG=[System.Drawing.Graphics]::FromImage($taskMuscleLayer);$taskMG.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
foreach($taskM in $taskMuscles.elements){
    $taskA=$taskNodes[$taskM.start];$taskB=$taskNodes[$taskM.end]
    $taskDX=$taskB.x-$taskA.x;$taskDY=$taskB.y-$taskA.y;$taskLen=[Math]::Sqrt($taskDX*$taskDX+$taskDY*$taskDY)
    $taskNX=-$taskDY/$taskLen;$taskNY=$taskDX/$taskLen;$taskMX=($taskA.x+$taskB.x)/2;$taskMY=($taskA.y+$taskB.y)/2
    [System.Drawing.PointF[]]$taskPts=@(
        [System.Drawing.PointF]::new([float]($taskA.x+$taskNX*$taskM.r0),[float]($taskA.y+$taskNY*$taskM.r0)),
        [System.Drawing.PointF]::new([float]($taskMX+$taskNX*$taskM.rm),[float]($taskMY+$taskNY*$taskM.rm)),
        [System.Drawing.PointF]::new([float]($taskB.x+$taskNX*$taskM.r1),[float]($taskB.y+$taskNY*$taskM.r1)),
        [System.Drawing.PointF]::new([float]($taskB.x-$taskNX*$taskM.r1),[float]($taskB.y-$taskNY*$taskM.r1)),
        [System.Drawing.PointF]::new([float]($taskMX-$taskNX*$taskM.rm),[float]($taskMY-$taskNY*$taskM.rm)),
        [System.Drawing.PointF]::new([float]($taskA.x-$taskNX*$taskM.r0),[float]($taskA.y-$taskNY*$taskM.r0))
    )
    $taskPath=[System.Drawing.Drawing2D.GraphicsPath]::new();$taskPath.AddClosedCurve($taskPts,0.25)
    $taskColor=$taskColors[$taskM.group]
    $taskBrush=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(95,$taskColor))
    $taskPen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(210,$taskColor),3)
    $taskMG.FillPath($taskBrush,$taskPath);$taskMG.DrawPath($taskPen,$taskPath)
    $taskBrush.Dispose();$taskPen.Dispose();$taskPath.Dispose()
}
$taskMG.Dispose()
$taskMuscleLayer.Save((Join-Path $PSScriptRoot 'muscles-layer.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$taskCombined=[System.Drawing.Bitmap]::new($taskW,$taskH)
$taskG=[System.Drawing.Graphics]::FromImage($taskCombined);$taskG.Clear([System.Drawing.Color]::FromArgb(28,33,38))
if($Art){$taskImage=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $Art));$taskG.DrawImage($taskImage,0,0,$taskW,$taskH);$taskImage.Dispose()}
$taskG.DrawImage($taskBoneLayer,0,0);$taskG.DrawImage($taskMuscleLayer,0,0)
$taskCombined.Save((Join-Path $PSScriptRoot ($Prefix+'-bones-muscles.png')),[System.Drawing.Imaging.ImageFormat]::Png)
$taskG.Dispose();$taskCombined.Dispose();$taskBoneLayer.Dispose();$taskMuscleLayer.Dispose()
