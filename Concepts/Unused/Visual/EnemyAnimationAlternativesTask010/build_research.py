"""TASK-010: read-only source evidence and an explicitly schematic offline study.

No Blender/Unity invocation, no texture deformation, no new production artwork.
Run from any directory with the bundled Python (Pillow, NumPy).
"""
from pathlib import Path
import base64
import hashlib
import json
import math
import re
import shutil
from datetime import datetime, timezone

import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[3]
VALIDATION = PROJECT / "Docs/Validation/EnemyAnimationAlternativesTask010"
VALIDATION.mkdir(parents=True, exist_ok=True)
FONT = "C:/Windows/Fonts/arial.ttf"


def dump(path, obj):
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding="utf-8")


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rel(path):
    return path.relative_to(PROJECT).as_posix()


def unit(v):
    return v / np.linalg.norm(v)


def smooth(t):
    t = np.clip(t, 0, 1)
    return float(t * t * t * (10 + t * (-15 + 6 * t)))


def rot(yaw):
    c, s = math.cos(yaw), math.sin(yaw)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


anatomy_path = PROJECT / "Concepts/Used/Stalker/Locomotion02/anatomy.json"
anatomy = json.loads(anatomy_path.read_text(encoding="utf-8-sig"))
PPU = anatomy["ppu"]
PIVOT = np.array(anatomy["pivot"])
inputs = {anatomy_path}


def point(xy, z):
    return np.array([(xy[0] - PIVOT[0]) / PPU, (PIVOT[1] - xy[1]) / PPU, z])


limbs = []
for item in anatomy["limbs"]:
    limbs.append(dict(id=item["id"], hip=point(item["hip"], .13),
                      pole=point(item["knee"], .10), foot=point(item["tip"], 0),
                      a=item["upperLength"], b=item["lowerLength"],
                      tripod=0 if item["id"] in ("LF", "RM", "LR") else 1))

GUID = {}
for meta in (PROJECT / "Assets/EmergencyShift/Art/Enemies").rglob("*.meta"):
    match = re.search(r"^guid: ([a-f0-9]+)", meta.read_text(encoding="utf-8-sig"), re.M)
    if match:
        GUID[match[1]] = Path(str(meta)[:-5])


def native_sprite(guid, output):
    """Copy a whole native Sprite rect, preserving RGBA and anatomical registration."""
    asset = GUID[guid]
    text = asset.read_text(encoding="utf-8-sig")
    rect = re.search(r"m_Rect:\s+serializedVersion: \d+\s+x: ([\d.]+)\s+y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)", text)
    x, y, w, h = [int(float(s)) for s in rect.groups()]
    pivot = re.search(r"m_Pivot: \{x: ([\d.]+), y: ([\d.]+)\}", text)
    px, py = [float(s) for s in pivot.groups()]
    texture = GUID[re.search(r"texture: \{fileID: 2800000, guid: ([a-f0-9]+)", text)[1]]
    image = Image.open(texture).convert("RGBA")
    crop = image.crop((x, image.height - y - h, x + w, image.height - y))
    origin = (round(288 - px * w), round(288 - (1 - py) * h))
    canvas = Image.new("RGBA", (576, 576))
    canvas.paste(crop, origin)
    canvas.save(OUT / output)
    restored = canvas.crop((origin[0], origin[1], origin[0] + w, origin[1] + h))
    assert np.array_equal(np.array(crop), np.array(restored))
    inputs.update([asset, Path(str(asset) + ".meta"), texture, Path(str(texture) + ".meta")])
    return dict(output=output, sprite=rel(asset), texture=rel(texture), guid=guid,
                rectUnityBottomLeft=[x, y, w, h], pivotNormalized=[px, py], paste=origin,
                exactWholeRectRGBA=True, canvas=[576, 576], anatomicalOrigin=[288, 288],
                note="Whole Sprite extraction for reference only; no limb cuts, pixel painting or rescaling.")


references = [native_sprite("ca3a99397a45b8840a5bde92d6b59c6c", "control-stalker.png"),
              native_sprite("0902cff050e84261a87d556347173c9e", "control-sleeper.png")]
historical = PROJECT / "Concepts/Used/Stalker/Locomotion02/MotionReview/after-00.jpg"
shutil.copyfile(historical, OUT / "historical-locomotion02.jpg")
inputs.add(historical)
for path in ["Concepts/Used/Stalker/Locomotion02/README.md",
             "Concepts/Used/Stalker/Locomotion03Sprites/README.md",
             "Concepts/Used/Stalker/FixedSprites06/README.md",
             "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab",
             "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab",
             "Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs",
             "Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs"]:
    inputs.add(PROJECT / path)

baseline_file = VALIDATION / "source-preservation.json"
current = {rel(path): sha(path) for path in sorted(inputs)}
if baseline_file.exists():
    old = json.loads(baseline_file.read_text(encoding="utf-8"))["inputs"]
    assert old == current, "Research input changed; refresh evidence intentionally, do not overwrite baseline."
else:
    dump(baseline_file, {"capturedUtc": datetime.now(timezone.utc).isoformat(), "inputs": current,
                         "scope": "Selected read-only inputs only, not a claim about all other chats or all Assets."})
dump(VALIDATION / "references.json", references)


def solve(hip, foot, pole, a, b):
    delta = foot - hip
    distance = float(np.linalg.norm(delta))
    if not abs(a - b) + 1e-6 < distance < a + b - 1e-6:
        raise ValueError(f"Unreachable target: {distance}, lengths {a}/{b}")
    axis = delta / distance
    projected = pole - hip - np.dot(pole - hip, axis) * axis
    direction = unit(projected)
    along = (a * a - b * b + distance * distance) / (2 * distance)
    radius = math.sqrt(max(0, a * a - along * along))
    knee = hip + axis * along + direction * radius
    return knee


STRIDE = .60
DUTY = .60


def walk_foot(item, t):
    offset = .5 * item["tripod"]
    phase = t + offset
    cycle = math.floor(phase)
    u = phase - cycle
    start = cycle - offset
    planted = item["foot"] + np.array([0, STRIDE * (start + .30), 0])
    if u < DUTY:
        return planted, True
    q = (u - DUTY) / (1 - DUTY)
    foot = planted + np.array([0, STRIDE * smooth(q), .075 * math.sin(math.pi * q) ** 2])
    return foot, False


def fold_goal(item):
    sign = -1 if item["id"].startswith("L") else 1
    pair = item["id"][1]
    return np.array([sign * (.44 if pair != "M" else .47), {"F": .43, "M": -.03, "R": -.46}[pair], 0])


def make_pose(kind, t):
    root = np.zeros(3)
    yaw = 0.
    fold = 0.
    mouth = 0.
    if kind == "walk":
        root[1] = STRIDE * t
    elif kind == "fold":
        fold = smooth(t)
        root[2] = -.045 * fold
    elif kind == "unfold":
        fold = 1 - smooth(t)
        root[2] = -.045 * fold
    elif kind == "attack":
        root[1] = .18 * (smooth((t - .38) / .24) - smooth((t - .62) / .23))
        mouth = math.sin(math.pi * min(1, t / .85)) ** 2
    elif kind == "turn":
        yaw = math.radians(14) * smooth(t)
    matrix = rot(yaw)
    parts = []
    for item in limbs:
        hip = root + matrix @ item["hip"]
        pole = root + matrix @ item["pole"]
        if kind == "attack":
            # A declared elevated bend plane avoids routing a knee below the floor.
            # This is a smooth authoring choice, never a per-frame pole-sign flip.
            pole[2] += .50 * math.sin(math.pi * t)
        planted = True
        if kind == "walk":
            foot, planted = walk_foot(item, t)
        elif kind in ("fold", "unfold"):
            # Only one tripod changes contact at a time. The other remains planted.
            delay = item["tripod"] * .50
            q = np.clip((t - delay) / .50, 0, 1)
            blend = smooth(q)
            a, b = item["foot"], fold_goal(item)
            if kind == "unfold":
                a, b = b, a
            foot = a * (1 - blend) + b * blend
            foot[2] += .06 * math.sin(math.pi * q) ** 2
            planted = q <= 0 or q >= 1
        elif kind == "attack" and item["id"][1] == "M":
            # Establish forward support BEFORE raising the front pair.
            transfer = smooth(t / .18) - smooth((t - .85) / .15)
            foot = item["foot"].copy()
            foot[1] += (.23 - item["foot"][1]) * transfer
            q = t / .18 if t < .18 else ((t - .85) / .15 if t > .85 else 0)
            foot[2] = .06 * math.sin(math.pi * np.clip(q, 0, 1)) ** 2
            planted = .18 <= t <= .85 or t <= 0 or t >= 1
        elif kind == "attack" and item["id"][1] == "F":
            sign = -1 if item["id"].startswith("L") else 1
            neutral = item["foot"]
            chamber = np.array([sign * .65, .36, .14])
            strike = np.array([sign * .32, .90, .08])
            if t < .20:
                foot = neutral.copy()
            elif t < .40:
                blend = smooth((t - .20) / .20)
                foot = neutral * (1 - blend) + chamber * blend
            elif t < .62:
                blend = smooth((t - .40) / .22)
                foot = chamber * (1 - blend) + strike * blend
            elif t < .85:
                blend = smooth((t - .62) / .23)
                foot = strike * (1 - blend) + neutral * blend
            else:
                foot = neutral.copy()
            planted = t <= .20 or t >= .85
        else:
            foot = item["foot"].copy()
        knee = solve(hip, foot, pole, item["a"], item["b"])
        parts.append(dict(id=item["id"], hip=hip.tolist(), knee=knee.tolist(), foot=foot.tolist(),
                          planted=bool(planted), a=item["a"], b=item["b"]))
    return dict(kind=kind, time=float(t), root=root.tolist(), yaw=yaw, fold=fold, mouth=mouth,
                limbs=parts)


KINDS = ["walk", "fold", "unfold", "attack", "turn"]
data = {kind: [make_pose(kind, i / 48) for i in range(49)] for kind in KINDS}
dump(OUT / "poses.json", dict(
    status="KINEMATIC_SCHEMATIC_ONLY_NOT_A_STALKER_ASSET", samplesPerClip=49,
    stride=STRIDE, duty=DUTY, proxyOrigin="Locomotion02/anatomy.json; synthetic, not recovered height measurements",
    caveats=["Not a Blender mesh/rig/render or hand-painted animation.",
             "No muscle, skin, collision or production anatomical PASS.",
             "Both candidate panels use the SAME numeric pose data to compare production ownership, not art quality.",
             "Root distance/tempo are a controlled one-second schematic, not current AI or attack timings.",
             "14-degree planted yaw is a bounded pose test, not an arbitrary turn/stop solver."], clips=data))
(OUT / "poses.js").write_text("window.RESEARCH_POSES = " + json.dumps(data) + ";\n", encoding="utf-8")


def hull(points):
    points = sorted(set(tuple(p[:2]) for p in points))
    def cross(o, a, b):
        return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
    low, high = [], []
    for p in points:
        while len(low) >= 2 and cross(low[-2], low[-1], p) <= 0:
            low.pop()
        low.append(p)
    for p in reversed(points):
        while len(high) >= 2 and cross(high[-2], high[-1], p) <= 0:
            high.pop()
        high.append(p)
    return low[:-1] + high[:-1]


def inside(poly, p):
    return all((b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0]) >= -1e-10
               for a, b in zip(poly, poly[1:] + poly[:1]))


metrics = {}
for kind, clip in data.items():
    max_bone_error, min_supports, outside = 0., 6, 0
    for pose in clip:
        supports = [part["foot"] for part in pose["limbs"] if part["planted"]]
        min_supports = min(min_supports, len(supports))
        outside += not inside(hull(supports), pose["root"])
        for part in pose["limbs"]:
            for a, b, target in [("hip", "knee", part["a"]), ("knee", "foot", part["b"])]:
                max_bone_error = max(max_bone_error, abs(np.linalg.norm(np.array(part[a])-part[b])-target))
    slip = 0.
    for i in range(1, len(clip)):
        for before, after in zip(clip[i-1]["limbs"], clip[i]["limbs"]):
            if before["planted"] and after["planted"]:
                slip = max(slip, float(np.linalg.norm(np.array(before["foot"])-after["foot"])))
    metrics[kind] = dict(frames=len(clip), minPlantedFeet=min_supports,
                         maxBoneLengthErrorLocalUnits=max_bone_error,
                         maxBetweenSamplesPlantedFootDisplacementLocalUnits=slip,
                         rootProjectionOutsidePlantedConvexHullFrames=outside,
                         minimumJointHeight=min(part[key][2] for pose in clip for part in pose["limbs"] for key in ("hip", "knee", "foot")),
                         caveat="Hull tests a declared root point, not a measured mass distribution or dynamic stability.")
    assert max_bone_error < 1e-12 and slip < 1e-12 and min_supports >= 3 and outside == 0
    assert metrics[kind]["minimumJointHeight"] >= -1e-12


def font(size):
    return ImageFont.truetype(FONT, size)


def render_panel(pose, mode, size=430):
    canvas = Image.new("RGBA", (size, size), "#17252c")
    d = ImageDraw.Draw(canvas)
    scale = size / 2.8
    root = np.array(pose["root"])
    def project(p):
        return (size/2+(p[0]-root[0])*scale, size/2-(p[1]-root[1])*scale)
    for n in np.arange(-1.2, 1.21, .2):
        d.line([project([n+root[0], -1.4+root[1], 0]), project([n+root[0], 1.4+root[1], 0])], fill="#223840")
        d.line([project([-1.4+root[0], n+root[1], 0]), project([1.4+root[0], n+root[1], 0])], fill="#223840")
    supports = hull([part["foot"] for part in pose["limbs"] if part["planted"]])
    d.polygon([project(p) for p in supports], fill="#243d3d")
    for part in pose["limbs"]:
        points = [project(part[key]) for key in ("hip", "knee", "foot")]
        if mode == "D":
            d.line(points, fill="#7fb7d0", width=4)
            for p in points:
                d.ellipse((p[0]-4, p[1]-4, p[0]+4, p[1]+4), fill="#e7d092")
        else:
            d.line(points, fill="#b69aaa", width=16, joint="curve")
            for p in points[:-1]:
                d.ellipse((p[0]-8, p[1]-8, p[0]+8, p[1]+8), fill="#b69aaa")
            d.line(points, fill="#f1d6df", width=2)
            for a, b in zip(points, points[1:]):
                v = np.array(b)-a
                normal = unit(np.array([-v[1], v[0]])) * 7
                for q in [.25, .5, .75]:
                    c = np.array(a)*(1-q)+np.array(b)*q
                    d.line([tuple(c-normal), tuple(c+normal)], fill="#716977", width=1)
        foot = points[-1]
        color = "#80d7a5" if part["planted"] else "#ecb26d"
        d.ellipse((foot[0]-5, foot[1]-5, foot[0]+5, foot[1]+5), outline=color, width=2)
        d.text((foot[0]+7, foot[1]-7), part["id"], font=font(12), fill=color)
    body = [np.array([-0.29,-.23,.13]), np.array([.29,-.23,.13]), np.array([0,.30,.13])]
    body = [root + rot(pose["yaw"]) @ p for p in body]
    d.polygon([project(p) for p in body], fill="#a8c1cb" if mode == "D" else "#c7aab9")
    center = project(root)
    r = 14 + 7 * pose["mouth"]
    d.ellipse((center[0]-r, center[1]-r, center[0]+r, center[1]+r), fill="#142027", outline="#e6c6d4", width=2)
    d.text((10, 10), "D · 3D-ориентир" if mode == "D" else "C · принцип 3D-кожи", font=font(13 if size<300 else 20), fill="#ffffff")
    d.text((10, size-23), "СХЕМА · НЕ АРТ", font=font(12 if size<300 else 17), fill="#f1c48d")
    return canvas


labels = {"walk":"ШАГ · 0.25", "fold":"СКЛАДЫВАНИЕ · 1.00", "unfold":"РАСКРЫТИЕ · 0.50", "attack":"АТАКА · 0.62"}
sheet = Image.new("RGB", (1370, 1060), "#0c171e")
d = ImageDraw.Draw(sheet)
d.text((30,20), "TASK-010 · одна постановка, два разных способа получить изображение", font=font(26), fill="white")
d.text((30,62), "Схемы методов, НЕ художественные пробы. Старый цельный Sprite — отдельный контроль качества.", font=font(18), fill="#b6c8d0")
control = Image.open(OUT/"control-stalker.png")
mini = control.resize((360,360), Image.Resampling.LANCZOS)
sheet.paste(mini,(970,115),mini)
d.text((1000,105), "СТАРЫЙ РИСУНОК",font=font(19),fill="white")
d.text((970,480), "Точный целый Walk00,",font=font(18),fill="#b6c8d0")
d.text((970,508), "показан со снижением масштаба.",font=font(18),fill="#b6c8d0")
d.text((970,555), "D: манекен → цельная",font=font(19),fill="#b6d9e9")
d.text((970,583), "ручная перерисовка.",font=font(19),fill="#b6d9e9")
d.text((970,630), "C: цельная 3D-кожа →",font=font(19),fill="#e1bdcd")
d.text((970,658), "ортографический PNG.",font=font(19),fill="#e1bdcd")
d.text((970,715), "Зелёный = опора",font=font(18),fill="#80d7a5")
d.text((970,743), "Оранжевый = перенос",font=font(18),fill="#ecb26d")
d.text((970,795), "У D рисунки ещё не созданы.",font=font(17),fill="#f1c48d")
d.text((970,824), "У C нет готового mesh/skin.",font=font(17),fill="#f1c48d")
for column, (kind, index) in enumerate([("walk",12),("fold",48),("unfold",24),("attack",30)]):
    x, y = 30+(column%2)*450, 110+(column//2)*465
    d.text((x,y), labels[kind],font=font(18),fill="#e4edf2")
    for mode, yy in [("D", y+32),("C",y+240)]:
        # The same projection and scale in both schematic candidates.
        panel=render_panel(data[kind][index],mode,205)
        sheet.paste(panel.convert("RGB"),(x,yy))
        # Side/front skeleton projections make the declared heights explicit.
        side=Image.new("RGB",(225,205),"#17252c")
        sd=ImageDraw.Draw(side)
        pose=data[kind][index]
        sd.text((8,8),"СБОКУ · Z",font=font(14),fill="#b8cad5")
        sd.line((8,165,215,165),fill="#647782")
        for part in pose["limbs"]:
            pts=[(110+(part[k][1]-pose["root"][1])*85,165-part[k][2]*260) for k in ("hip","knee","foot")]
            sd.line(pts,fill="#9fb8c5",width=2)
        sd.text((8,180),"Проектные высоты",font=font(12),fill="#f1c48d")
        sheet.paste(side,(x+210,yy))
sheet.save(OUT/"comparison-board.png")

# Short lossless diagrams: these do not manipulate the reference artwork.
for kind in ["walk", "fold", "unfold", "attack"]:
    frames=[]
    for pose in (data[kind][:-1] if kind == "walk" else data[kind]):
        panel=Image.new("RGB",(860,480),"#0c171e")
        panel.paste(render_panel(pose,"D").convert("RGB"),(0,45))
        panel.paste(render_panel(pose,"C").convert("RGB"),(430,45))
        dr=ImageDraw.Draw(panel)
        dr.text((18,12),f"TASK-010 · {kind} · СИНХРОННЫЕ СХЕМЫ, НЕ НОВЫЕ СПРАЙТЫ",font=font(19),fill="white")
        frames.append(panel)
    frames[0].save(OUT/f"schematic-{kind}.png",save_all=True,append_images=frames[1:],duration=1000/48,loop=0 if kind=="walk" else 1,disposal=2)

dump(VALIDATION/"numeric-checks.json",dict(status="SCHEMATIC_KINEMATICS_CHECKED_ONLY",metrics=metrics,
    muscleGate="NOT_APPLICABLE_TO_SCHEMATIC; no production drawings or muscle claim",
    skinGate="NOT_TESTED; no actual continuous mesh or new painted skin",
    ownerArtAcceptance=False, unityInvoked=False, packagesInstalled=False,
    sceneAuthoringReview="Неприменим: объекты сцен не менялись."))
print(json.dumps(dict(outputs=rel(OUT), selectedInputs=len(current), clips=len(data), metrics=metrics),ensure_ascii=False,indent=2))
