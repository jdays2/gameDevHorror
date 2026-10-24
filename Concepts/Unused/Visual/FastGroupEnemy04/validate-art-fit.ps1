param([string]$Art='hybrid-v14.png',[string]$Output='art-fit-v14.json')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$rig=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'skeleton-shared.json') -Raw | ConvertFrom-Json
$map=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'muscle-topology.json') -Raw | ConvertFrom-Json
$img=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot $Art))
$nodes=@{};foreach($n in $rig.nodes){$nodes[$n.id]=$n}
function ReadAlpha([double]$x,[double]$y){
 $ix=[int][Math]::Round($x);$iy=[int][Math]::Round($y)
 if($ix -lt 0 -or $iy -lt 0 -or $ix -ge $img.Width -or $iy -ge $img.Height){return 0}
 return [int]$img.GetPixel($ix,$iy).A
}
$bones=@()
foreach($p in @('U','L')){
 foreach($pair in @(@('root','knee'),@('knee','ankle'),@('ankle','foot'))){
 $a=$nodes[$p+'_'+$pair[0]];$z=$nodes[$p+'_'+$pair[1]];$hit=0
 for($i=0;$i -le 40;$i++){$t=$i/40;if((ReadAlpha ($a.x+($z.x-$a.x)*$t) ($a.y+($z.y-$a.y)*$t)) -ge 128){$hit++}}
 $bones += [pscustomobject]@{id=$p+'_'+$pair[0]+'->'+$pair[1];length=[Math]::Sqrt([Math]::Pow(($z.x-$a.x),2)+[Math]::Pow(($z.y-$a.y),2));alphaCoverage=$hit/41}
 }
}
$groups=@()
foreach($limb in $map.limbs){foreach($m in $limb.groups){
 $a=$m.origin;$c=$m.belly;$z=$m.insertion
 $dx=$z.x-$a.x;$dy=$z.y-$a.y;$len=[Math]::Sqrt($dx*$dx+$dy*$dy);$nx=-$dy/$len;$ny=$dx/$len;$hit=0;$total=0
 for($i=0;$i -le 20;$i++){$t=$i/20;$u=1-$t;$x=$u*$u*$a.x+2*$u*$t*$c.x+$t*$t*$z.x;$y=$u*$u*$a.y+2*$u*$t*$c.y+$t*$t*$z.y
 foreach($off in @(-.5,0,.5)){$r=[Math]::Sin($t*[Math]::PI)*$m.width*$off;if((ReadAlpha ($x+$nx*$r) ($y+$ny*$r)) -ge 128){$hit++};$total++}}
 $groups += [pscustomobject]@{id=$m.id;originInside=((ReadAlpha $a.x $a.y) -ge 128);insertionInside=((ReadAlpha $z.x $z.y) -ge 128);sampledEnvelopeAlphaCoverage=$hit/$total}
}}
$report=[pscustomobject]@{art=$Art;threshold=128;meaning='Compare intended 2D guides to artwork alpha. Coverage is not evidence of correct visible muscle count, joint centering or rig deformation.';bones=$bones;muscleGroups=$groups;decision='VISUAL REVIEW REQUIRED - numeric alpha fit cannot approve anatomy'}
$report|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $PSScriptRoot $Output) -Encoding UTF8
$img.Dispose()
$report|ConvertTo-Json -Depth 12
