param([string]$Art='hybrid-v14.png')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$out=[System.Drawing.Bitmap]::new(2048,1640)
$g=[System.Drawing.Graphics]::FromImage($out);$g.Clear([System.Drawing.Color]::FromArgb(28,33,38));$g.InterpolationMode='HighQualityBicubic';$g.TextRenderingHint='AntiAliasGridFit'
$title=[System.Drawing.Font]::new('Segoe UI',25,[System.Drawing.FontStyle]::Bold)
$small=[System.Drawing.Font]::new('Segoe UI',18)
$g.DrawString('TASK-005 · Кости → мышцы → отдельный контроль креплений',$title,[System.Drawing.Brushes]::White,30,20)
$panels=@(
 @{path=$Art;x=0;y=90;title='1. Кожа v14 · требует подгонки'},
 @{path='v14-control-bones.png';x=1024;y=90;title='2. Кости · общий шаблон двух лап'},
 @{path='v14-control-control.png';x=0;y=850;title='3. Мышцы · группы, волокна, крепления'},
 @{path='v14-control-bones-muscles.png';x=1024;y=850;title='4. Наложение · видны расхождения'}
)
foreach($p in $panels){$g.DrawString($p.title,$small,[System.Drawing.Brushes]::Gainsboro,[float]($p.x+22),[float]$p.y);$im=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $p.path));$g.DrawImage($im,[int]$p.x,[int]($p.y+40),1024,683);$im.Dispose()}
$g.DrawString('Контрольная схема согласована. Рисунок пока не прошёл проверку анатомии.',$small,[System.Drawing.Brushes]::Gold,30,1590)
$out.Save((Join-Path $PSScriptRoot 'anatomy-review.png'))
$g.Dispose();$out.Dispose();$title.Dispose();$small.Dispose()
