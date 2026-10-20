param(
    [string]$LayoutPath='Builds/CanThoMap/20261016-PeripheralLayout/Candidate02/CanThoCampus.layout.json',
    [string]$OutputPath='Builds/CanThoMap/20261016-PeripheralLayout/PeripheralPlan.svg'
)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=[IO.Path]::GetFullPath((Join-Path $workspace $OutputPath))
if(-not $output.StartsWith($workspace+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must stay in project.'}
$layout=Get-Content -Raw -LiteralPath (Join-Path $workspace $LayoutPath) | ConvertFrom-Json
if($layout.artRevision -notin @(7,8)){throw 'Expected peripheral revision7 or8.'}
$svg=[Collections.Generic.List[string]]::new()
$svg.Add('<svg xmlns="http://www.w3.org/2000/svg" width="1240" height="1160" viewBox="0 0 1240 1160">')
$svg.Add('<rect width="1240" height="1160" fill="#142522"/><style>text{font-family:Arial,sans-serif;fill:#edf4e7} .street{font-size:15px;font-weight:bold;paint-order:stroke;stroke:#142522;stroke-width:4px;stroke-linejoin:round} .small{font-size:13px;fill:#bdd0c4}</style>')
$svg.Add('<text x="45" y="42" font-size="27" font-weight="bold">ĐH CẦN THƠ — SƠ ĐỒ NGOẠI VI GAMEPLAY</text>')
$svg.Add('<text x="45" y="67" class="small">Đối chiếu Google Maps • giữ campus theo sơ đồ người dùng • khoảng cách nén, chưa phải 1:1</text>')
# Invariant numeric output regardless of the machine's regional decimal separator.
function XY([double]$x,[double]$z){return ((50+($x+560)*1.19).ToString('F2',[Globalization.CultureInfo]::InvariantCulture)+','+(100+(450-$z)*1.19).ToString('F2',[Globalization.CultureInfo]::InvariantCulture))}
function Points($points){return (($points | ForEach-Object {XY $_.x $_.z}) -join ' ')}
$svg.Add('<rect x="50" y="100" width="1118.6" height="999.6" fill="#314738" stroke="#65795e"/>')
$svg.Add('<polygon points="'+(Points $layout.campusBoundary)+'" fill="#476c55" stroke="#c0cc9c" stroke-width="2"/>')
$svg.Add('<polygon points="'+(Points $layout.waterBoundary)+'" fill="#257b8c" stroke="#7ab7b6" stroke-width="4"/>')
foreach($road in $layout.peripheralRoads){
    $points=Points $road.points
    $svg.Add('<polyline points="'+$points+'" fill="none" stroke="#b2b18d" stroke-width="'+(($road.width+5)*1.19).ToString([Globalization.CultureInfo]::InvariantCulture)+'" stroke-linejoin="round"/>')
    $svg.Add('<polyline points="'+$points+'" fill="none" stroke="#172424" stroke-width="'+($road.width*1.19).ToString([Globalization.CultureInfo]::InvariantCulture)+'" stroke-linejoin="round"/>')
}
foreach($lane in $layout.localLanes){
    $svg.Add('<polyline points="'+(Points $lane.points)+'" fill="none" stroke="#999f8b" stroke-width="5"/>')
}
foreach($tree in $layout.streetscapeTrees){
    $p=(XY $tree.x $tree.z).Split(',')
    $svg.Add('<circle cx="'+$p[0]+'" cy="'+$p[1]+'" r="3.5" fill="#659160"/>')
}
foreach($house in $layout.neighborhoodHouses){
    $yaw=$house.euler.y*[Math]::PI/180;$corners=@()
    foreach($pair in @(@(-1,-1),@(1,-1),@(1,1),@(-1,1))){
        $a=$pair[0]*$house.size.x/2;$b=$pair[1]*$house.size.z/2
        $corners+=XY ($house.position.x+[Math]::Cos($yaw)*$a+[Math]::Sin($yaw)*$b) ($house.position.z-[Math]::Sin($yaw)*$a+[Math]::Cos($yaw)*$b)
    }
    $svg.Add('<polygon points="'+($corners -join ' ')+'" fill="#d8c79d" stroke="#6e6c55" stroke-width=".6"/>')
}
foreach($box in $layout.collisions){
    if($box.size.x -lt 7 -or $box.size.z -lt 7 -or $box.size.y -lt 3 -or $box.name -like 'District house*'){continue}
    if($box.position.x -lt -240 -or $box.position.x -gt 185 -or $box.position.z -lt -45 -or $box.position.z -gt 250){continue}
    $p=XY ($box.position.x-$box.size.x/2) ($box.position.z+$box.size.z/2);$coords=$p.Split(',')
    $svg.Add('<rect x="'+$coords[0]+'" y="'+$coords[1]+'" width="'+($box.size.x*1.19).ToString([Globalization.CultureInfo]::InvariantCulture)+'" height="'+($box.size.z*1.19).ToString([Globalization.CultureInfo]::InvariantCulture)+'" fill="#9eac8b" opacity=".8"/>')
}
function Label([string]$text,[double]$x,[double]$z,[double]$angle=0){
    $p=(XY $x $z).Split(',');$safe=[Security.SecurityElement]::Escape($text)
    $svg.Add('<text x="'+$p[0]+'" y="'+$p[1]+'" class="street" text-anchor="middle" transform="rotate('+$angle.ToString([Globalization.CultureInfo]::InvariantCulture)+' '+$p[0]+' '+$p[1]+')">'+$safe+'</text>')
}
Label 'ĐƯỜNG 3 THÁNG 2' -300 -64
Label 'BÚNG XÁNG / HẺM 51' -294 2 -85
Label 'HƯỚNG NGUYỄN VĂN CỪ' -10 420 4
Label 'MẬU THÂN' 245 130 87
Label 'TRẦN VĂN HOÀI' -80 -190 85
Label 'MẠC THIÊN TÍCH' 86 -118 40
Label 'HỒ BÚNG XÁNG' -347 242
Label '/ RẠCH RAU MUỐNG' -347 222
Label 'ĐẠI HỌC CẦN THƠ' -25 104
foreach($gate in $layout.gates){$p=(XY $gate.position.x -43).Split(',');$svg.Add('<circle cx="'+$p[0]+'" cy="'+$p[1]+'" r="5" fill="#f6bd63"/>');Label ('CỔNG '+$gate.id) $gate.position.x -29}
$svg.Add('<path d="M1080 195 L1127 156" fill="none" stroke="#f0d088" stroke-width="4"/><path d="M1115 157 L1127 156 L1124 168" fill="none" stroke="#f0d088" stroke-width="4"/><text x="1137" y="153" font-size="18" fill="#f0d088">Bắc ≈</text>')
$svg.Add('<text x="50" y="1133" class="small">Nhà màu vàng: ngoại vi • Campus màu xanh • Hình vẽ từ layout do dự án dựng, không phải ảnh hoặc tile Google Maps</text></svg>')
[IO.File]::WriteAllLines($output,$svg,[Text.UTF8Encoding]::new($false))
[xml]$check=Get-Content -Raw -LiteralPath $output
if($check.svg.polygon.Count -lt 160){throw 'Missing geometry in diagram.'}
Write-Output ('PERIPHERAL_SVG_PASS '+$output)
