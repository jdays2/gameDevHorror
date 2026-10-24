param([string]$Art='hybrid-v14.png',[string]$Prefix='topology-v1')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$rig=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'skeleton-shared.json') -Raw | ConvertFrom-Json
$anatomy=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'muscle-topology.json') -Raw | ConvertFrom-Json
$w=[int]$rig.canvas.width;$h=[int]$rig.canvas.height
$nodes=@{};foreach($n in $rig.nodes){$nodes[$n.id]=$n}
$colors=@{M01='#b676df';M02='#ef6969';M03='#f7b568';M04='#67b8ef';M05='#66dbb1';M06='#e6d16b';M07='#e6d16b';M08='#e6d16b'}
function Point($v){[System.Drawing.PointF]::new([float]$v.x,[float]$v.y)}
function CurvePoint($a,$m,$z,[double]$t){$u=1-$t;[pscustomobject]@{x=$u*$u*$a.x+2*$u*$t*$m.x+$t*$t*$z.x;y=$u*$u*$a.y+2*$u*$t*$m.y+$t*$t*$z.y}}
$bone=[System.Drawing.Bitmap]::new($w,$h)
$bg=[System.Drawing.Graphics]::FromImage($bone);$bg.SmoothingMode='AntiAlias'
$pen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(235,230,239,248),4)
foreach($n in $rig.nodes){if($n.parent){$p=$nodes[$n.parent];$bg.DrawLine($pen,[float]$p.x,[float]$p.y,[float]$n.x,[float]$n.y)};$bg.FillEllipse([System.Drawing.Brushes]::White,[float]($n.x-5),[float]($n.y-5),10,10)}
$bg.Dispose();$pen.Dispose();$bone.Save((Join-Path $PSScriptRoot 'bones-shared-layer.png'))
$layer=[System.Drawing.Bitmap]::new($w,$h)
$g=[System.Drawing.Graphics]::FromImage($layer);$g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit'
$font=[System.Drawing.Font]::new('Segoe UI',12,[System.Drawing.FontStyle]::Bold)
function DrawMuscle($g,$a,$m,$z,[double]$radius,$color,[string]$id){
 $dx=$z.x-$a.x;$dy=$z.y-$a.y;$len=[Math]::Sqrt($dx*$dx+$dy*$dy);$nx=-$dy/$len;$ny=$dx/$len
 $p=[System.Drawing.Drawing2D.GraphicsPath]::new()
 [System.Drawing.PointF[]]$pts=@(
 [System.Drawing.PointF]::new([float]$a.x,[float]$a.y),
 [System.Drawing.PointF]::new([float]($m.x+$nx*$radius),[float]($m.y+$ny*$radius)),
 [System.Drawing.PointF]::new([float]$z.x,[float]$z.y),
 [System.Drawing.PointF]::new([float]($m.x-$nx*$radius),[float]($m.y-$ny*$radius)))
 $p.AddClosedCurve($pts,0.5)
 $b=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(135,$color));$outline=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(235,$color),2)
 $g.FillPath($b,$p);$g.DrawPath($outline,$p)
 $fiber=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200,$color),1.3)
 foreach($off in @(-.45,0,.45)){
   $fiberPts=[System.Collections.Generic.List[System.Drawing.PointF]]::new()
   for($i=0;$i -le 16;$i++){$t=$i/16;$v=CurvePoint $a $m $z $t;$r=[Math]::Sin($t*[Math]::PI)*$radius*$off;$fiberPts.Add([System.Drawing.PointF]::new([float]($v.x+$nx*$r),[float]($v.y+$ny*$r)))}
   $g.DrawLines($fiber,$fiberPts.ToArray())
 }
 $arrow=[System.Drawing.Pen]::new([System.Drawing.Color]::White,1.7);$arrow.CustomEndCap=[System.Drawing.Drawing2D.AdjustableArrowCap]::new(3,4)
 $q1=CurvePoint $a $m $z .42;$q2=CurvePoint $a $m $z .65
 $g.DrawLine($arrow,[float]$q1.x,[float]$q1.y,[float]$q2.x,[float]$q2.y)
 $g.FillEllipse([System.Drawing.Brushes]::Black,[float]($a.x-5),[float]($a.y-5),10,10)
 $g.DrawEllipse([System.Drawing.Pens]::White,[float]($a.x-5),[float]($a.y-5),10,10)
 $g.FillRectangle([System.Drawing.Brushes]::White,[float]($z.x-3),[float]($z.y-3),6,6)
 $q=CurvePoint $a $m $z .48
 $g.FillEllipse([System.Drawing.Brushes]::Black,[float]($q.x-12),[float]($q.y-11),24,22)
 $g.DrawString($id,$font,[System.Drawing.Brushes]::White,[float]($q.x-9),[float]($q.y-10))
 $arrow.Dispose();$fiber.Dispose();$outline.Dispose();$b.Dispose();$p.Dispose()
}
foreach($m in $anatomy.bodyGroups){$a=@{x=$m.a[0];y=$m.a[1]};$mid=@{x=$m.m[0];y=$m.m[1]};$z=@{x=$m.z[0];y=$m.z[1]};DrawMuscle $g $a $mid $z $m.width ([System.Drawing.ColorTranslator]::FromHtml('#6b9cab')) $m.id.Substring(1)}
foreach($limb in $anatomy.limbs){foreach($m in $limb.groups){
 $c=[System.Drawing.ColorTranslator]::FromHtml($colors[$m.templateId])
 DrawMuscle $g $m.origin $m.belly $m.insertion $m.width $c $m.templateId.Substring(1)
 if($m.tendon){$p=[System.Drawing.Pen]::new([System.Drawing.Color]::Ivory,2.5);$g.DrawLine($p,(Point $m.tendon.from),(Point $m.tendon.to));$p.Dispose()}
}}
$g.Dispose();$font.Dispose();$layer.Save((Join-Path $PSScriptRoot 'muscle-topology-layer.png'))
$artwork=$null;if($Art){$artwork=[System.Drawing.Image]::FromFile((Join-Path $PSScriptRoot $Art))}
foreach($mode in @('bones','muscles','bones-muscles','control')){
 $out=[System.Drawing.Bitmap]::new($w,$h);$og=[System.Drawing.Graphics]::FromImage($out);$og.SmoothingMode='AntiAlias';$og.Clear([System.Drawing.Color]::FromArgb(28,33,38))
 if($artwork -and $mode -ne 'control'){$og.DrawImage($artwork,0,0,$w,$h)}
 if($mode -in @('bones','bones-muscles','control')){$og.DrawImage($bone,0,0)}
 if($mode -in @('muscles','bones-muscles','control')){$og.DrawImage($layer,0,0)}
 if($mode -eq 'control'){
 $tf=[System.Drawing.Font]::new('Segoe UI',13,[System.Drawing.FontStyle]::Regular);$tb=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::Gainsboro)
 $legend=@'
КОНТРОЛЬ МЫШЦ
Общая схема двух лап

01  стабилизатор основания
02  ближний сгибатель
03  ближний разгибатель
04  дальний сгибатель
05  дальний разгибатель
06–08  пучки трёх пальцев

На лапу: 8 групп
и 3 сухожилия пальцев

○ начало  □ крепление
→ направление волокон

Общие точки крепления,
форма и толщина групп.
Поза меняет углы.

Голубой: группы спины

Условная анатомия
вымышленного существа.
'@
 $og.DrawString($legend,$tf,$tb,[System.Drawing.RectangleF]::new(1175,220,350,780));$tb.Dispose();$tf.Dispose()
 }
 $out.Save((Join-Path $PSScriptRoot ($Prefix+'-'+$mode+'.png')));$og.Dispose();$out.Dispose()
}
if($artwork){$artwork.Dispose()};$bone.Dispose();$layer.Dispose()
