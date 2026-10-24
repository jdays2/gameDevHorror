"""Produce a reproducible design map from the read-only native TASK-001 capture."""
from collections import Counter, deque
import json
from pathlib import Path
import re

from html import escape

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Docs/Validation/Task001LevelDesign"
scene = json.loads((OUT / "authored-scene.json").read_text(encoding="utf-8-sig"))
objects = scene["objects"]


def component(obj, kind):
    return next((item for item in obj["components"] if item["type"] == kind), None)


def fields(item):
    return item["data"]["MonoBehaviour"]


floors = [obj for obj in objects if re.search(r"/\w+ Floor \d+$", obj["path"])]
enemies = [obj for obj in objects if component(obj, "ShadowStalker2D")]
doors = [obj for obj in objects if component(obj, "Door2D")]
pickups = [obj for obj in objects if any("Pickup" in item["type"] for item in obj["components"])]

# Edges transcribed from actual door labels and touching floor rectangles, not an old sketch.
# A graph edge is access logic, not proof of movement clearance or survival.
edges = [
    ("S", "C0", ""), ("C0", "H", ""), ("H", "ER", ""),
    ("ER", "ED", ""), ("ED", "G", ""), ("G", "R", ""),
    ("R", "H", "green"), ("H", "P", "green"), ("P", "A", ""),
    ("A", "B", ""), ("B", "W", ""), ("W", "S", "blue"),
    ("H", "O1", "blue"), ("H", "O2", "blue"), ("H", "O3", "blue"),
]


def reachable(keys, excluded=()):
    reached = {"S"}
    pending = deque(["S"])
    while pending:
        node = pending.popleft()
        for a, b, key in edges:
            other = b if a == node else a if b == node else None
            if other is None or other in excluded or other in reached or key and key not in keys:
                continue
            reached.add(other)
            pending.append(other)
    return sorted(reached)


enemy_groups = Counter(obj["path"].split("/")[1] for obj in enemies)
summary = {
    "captureUtc": scene["capturedUtc"],
    "source": "In-memory authored capture; no gameplay timing or player-test claims",
    "scene": scene["scene"],
    "unsavedAtCapture": scene["isDirty"],
    "floorRectangles": len(floors),
    "enemies": len(enemies),
    "enemyGroups": dict(enemy_groups),
    "doors": len(doors),
    "lockedDoors": sum(component(obj, "DoorAccess2D") is not None for obj in doors),
    "graphEdges": [{"from": a, "to": b, "requires": key or None} for a, b, key in edges],
    "reachability": {
        "noCards": reachable(set()),
        "greenOnly": reachable({"green"}),
        "greenAndBlue": reachable({"green", "blue"}),
        "withoutOptionalRooms": reachable({"green", "blue"}, {"O1", "O2", "O3"}),
        "greenReachableInitially": "G" in reachable(set()),
        "blueBlockedInitially": "B" not in reachable(set()),
        "blueReachableWithGreen": "B" in reachable({"green"}),
        "optionalRoomsRequiredForBlue": "B" not in reachable({"green"}, {"O1", "O2", "O3"}),
    },
    "pickups": [],
}
for obj in pickups:
    for item in obj["components"]:
        if "Pickup" not in item["type"]:
            continue
        summary["pickups"].append({"path": obj["path"], "xy": obj["position"][:2], "type": item["type"],
                                   "fields": fields(item), "references": item["references"]})
(OUT / "analysis.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1120" viewBox="0 0 1600 1120">',
       '<rect width="1600" height="1120" fill="#f3f5f8"/>',
       '<style>text{font-family:Arial,sans-serif;fill:#344356}</style>',
       '<rect x="45" y="130" width="1140" height="855" fill="#e5e9ef"/>']


def point(x, y):
    return 45 + (x + 10) * 19, 130 + (22 - y) * 19


def rect(x, y, w, h, color):
    px, py = point(x-w/2, y+h/2)
    svg.append(f'<rect x="{px:.2f}" y="{py:.2f}" width="{w*19:.2f}" height="{h*19:.2f}" fill="{color}"/>')


def text(x, y, value, size=17, weight="normal", anchor="start"):
    svg.append(f'<text x="{x:.2f}" y="{y:.2f}" font-size="{size}" font-weight="{weight}" text-anchor="{anchor}">{escape(value)}</text>')

for obj in floors:
    x, y, _ = obj["floor"]["center"]
    w, h, _ = obj["floor"]["size"]
    code = obj["path"].split("/")[-1].split()[0]
    color = "#c9dbea" if code in {"W", "C0", "P", "ER", "ED", "R"} else "#fffdf5"
    rect(x, y, w, h, color)

for obj in objects:
    for collider in obj["colliders"]:
        if collider["isTrigger"] or collider["layer"] != "LightObstacles" or collider["type"] != "BoxCollider2D":
            continue
        x, y, _ = collider["center"]
        w, h, _ = collider["size"]
        rect(x, y, w, h, "#3f4650")

route = [(0,0), (3,0), (9,0), (17,0), (17,-4), (30,-4), (37,.5), (45.9,.5),
         (45.9,-6), (45.9,-12), (43.25,-14.66), (37,-12.3), (33.45,-12.3),
         (33.45,-4.5), (29,-4), (18,-4), (18.65,3.8), (18.65,8.75), (18.65,14.3),
         (10.5,14.3), (6.7,14.3), (4,18.25), (-6.25,18.25), (-6.5,12), (-6.5,4),
         (-6.2,-1.85), (0,-1.85)]
points = " ".join(f"{point(x,y)[0]:.2f},{point(x,y)[1]:.2f}" for x,y in route)
svg.append(f'<polyline points="{points}" fill="none" stroke="#9256ab" stroke-width="2.2" stroke-dasharray="6 6"/>')

for obj in doors:
    x, y = obj["position"][:2]
    access = component(obj, "DoorAccess2D")
    key = fields(access)["requiredCardId"] if access else ""
    color = {"blue": "#2d78c4", "green": "#279569", "": "#737b87"}[key]
    leaf = next(item for item in objects if item["path"] == obj["path"] + "/Leaf")
    width, height = leaf["colliders"][0]["size"][:2]
    vertical = height > width
    p1 = point(x, y-1.5) if vertical else point(x-1.5, y)
    p2 = point(x, y+1.5) if vertical else point(x+1.5, y)
    svg.append(f'<line x1="{p1[0]}" y1="{p1[1]}" x2="{p2[0]}" y2="{p2[1]}" stroke="{color}" stroke-width="6"/>')

for obj in enemies:
    x, y = obj["position"][:2]
    px, py = point(x, y)
    svg.append(f'<circle cx="{px}" cy="{py}" r="5" fill="#bf4b56"/>')

for obj in pickups:
    x, y = obj["position"][:2]
    item = next(item for item in obj["components"] if "Pickup" in item["type"])
    color = "#c58a1d"
    if item["type"] == "AccessCardPickup2D":
        color = {"blue": "#2d78c4", "green": "#279569"}[fields(item)["cardId"]]
    px, py = point(x, y)
    svg.append(f'<path d="M{px},{py-6}l6,6l-6,6l-6,-6Z" fill="{color}" stroke="white"/>')

labels = {"S": (0, 2.7), "C0": (9.8, .7), "H": (26.8, .5), "P": (20.6, 8.8),
          "A": (16.3, 15.0), "B": (0, 19.2), "W": (-7.1, 2.3), "ER": (42.5, 1.1),
          "ED": (47.2, -6.0), "G": (41.2, -12), "R": (33.5, -10),
          "O1": (19.2, -10.9), "O2": (25.8, -11.6), "O3": (26.05, 9.3)}
for label, (x, y) in labels.items():
    px, py = point(x, y)
    svg.append(f'<rect x="{px-19}" y="{py-15}" width="38" height="24" rx="4" fill="white" opacity=".9"/>')
    text(px, py+4, label, weight="bold", anchor="middle")
px, py = point(0,0)
svg.append(f'<circle cx="{px}" cy="{py}" r="7" fill="#263647" stroke="white" stroke-width="2"/>')
text(45, 49, "TASK-001 · MorgueRouteTest — фактическое пространство", 28, "bold")
text(45, 87, "Снимок Unity 24.10.2026 · 15 участков пола · 27 Сталкеров · 11 дверей, из них 6 карточных", 17)
legend = """УЧАСТКИ

S     Стартовый морг, фонарь
C0   Входной коридор
H    Распределительный зал
ER   Восточный подход
ED   Восточный спуск
G    Зелёная карта
R    Служебный возврат
P    Пистолет
A    Боезапас перед ареной
B    Арена, синяя карта
W    Западный возврат
O1  Линза, 2 врага
O2  Дробовик, 10 врагов
O3  Пустая боковая комната

ОБОЗНАЧЕНИЯ

Красные точки — враги
Ромбы — подбираемые предметы
Синие / зелёные линии — замки
Серые линии — обычные двери
Штрих — поясняющая трасса

ЧТО ПОКАЗАНО

Реальные позиции и размеры.
Трасса объясняет порядок;
это не запись прохождения
и не проверенный путь AI.

Вне схемы: будущий выход,
этажи и финал полного демо.
Их в этой сцене нет."""
for index, line in enumerate(legend.splitlines()):
    text(1215, 151 + index*22, line, 16, "bold" if line.isupper() else "normal")
text(45, 1035, "Источник: authored-scene.json. Позиции — в Unity units, север (+Y) сверху. Несохранённая сцена прочитана без сохранения.", 16)
text(45, 1064, "Сценарное знакомство, условие стартовой двери и меню разрабатываются отдельно. Их последующие изменения не входят в снимок.", 16)
svg.append('</svg>')
(OUT / "route-map.svg").write_text("\n".join(svg) + "\n", encoding="utf-8")
print(json.dumps({"floors": len(floors), "enemies": len(enemies), "doors": len(doors),
                  "groups": dict(enemy_groups), "access": summary["reachability"]}, ensure_ascii=False))
