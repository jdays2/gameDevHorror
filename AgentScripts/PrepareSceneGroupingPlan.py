"""Compile TASK-002's reviewed ownership decisions into an explicit native-ID plan.

Reads the current native-before audit, writes a review manifest only. No Unity
assets are written. Zone decisions follow the actual authored stations and their
builders; coordinates below distinguish already-reviewed instances/segments.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BEFORE = ROOT / "work/scene-grouping-task002/native-before"
OUT = ROOT / "Docs/Validation/SceneGroupingTask002"
PLANS = []


class Plan:
    def __init__(self, scene):
        self.data = json.loads((BEFORE / (scene + ".json")).read_text(encoding="utf-8"))
        self.units = self.data["units"]
        self.groups = []
        self.moves = []

    def one(self, predicate):
        values = [u for u in self.units if predicate(u)]
        assert len(values) == 1, values
        return values[0]

    def group(self, key, parent, name):
        self.groups.append(dict(key=key, parent=parent, name=name))
        return key

    def move(self, unit, parent=None, name=None):
        self.moves.append(dict(id=unit["id"], before=unit["path"], parent=parent, name=name))

    def finish(self):
        PLANS.append(dict(scene=self.data["scene"], groups=self.groups, moves=self.moves))


# Two review rooms are intentionally comparable, not production room prefabs.
# Labels and local fills belong to their named specimen area. Boundary walls and
# corridor arrows are shared, so neither half accidentally owns the other half.
for scene in ["EnvironmentPropsReview", "EnvironmentPropsDarkReview"]:
    p = Plan(scene)
    room = p.one(lambda u: u["name"].startswith("Environment review"))
    for key, name in [("warehouse", "Warehouse"), ("lab", "Laboratory"),
                      ("geometry", "SharedGeometry"), ("way", "SharedWayfinding")]:
        p.group(key, room["id"], name)
    for u in p.units:
        if u["parent"] != room["id"]:
            continue
        n = u["name"]
        if n == "Wall":
            x, y, _ = u["position"]
            p.move(u, "geometry", "Perimeter_" + ("North" if y == 5 else "South" if y == -5 else "West" if x == -8 else "East"))
        elif n == "arrow-forward":
            p.move(u, "way", "Arrow_WarehouseExit" if u["position"][0] < 0 else "Arrow_LaboratoryEntry")
        else:
            # The existing authoring contract has warehouse props/labels on the
            # left and laboratory props/labels on the right; all are direct units.
            zone = "warehouse" if u["position"][0] < 0 else "lab"
            name = None
            if n == "warehouse-drum":
                name = "WarehouseDrum_Front" if u["position"][1] == 1 else "WarehouseDrum_Rear"
            elif n.startswith("Local ambient"):
                name = "WarehouseFill_NotDefensive" if zone == "warehouse" else "LaboratoryFill_NotDefensive"
            p.move(u, zone, name)
    p.finish()

# Furniture study's controller keeps every original serialized reference. Sample
# and matching light become siblings within one selectable comparison station.
p = Plan("FurnitureLightStudy")
room = p.one(lambda u: u["name"].startswith("Furniture light study"))
p.group("geometry", room["id"], "Geometry")
stations = {-5: ("locker", "S01_Locker"), 0: ("desk", "S02_Workbench"), 5: ("stretcher", "S03_Stretcher")}
for key, name in stations.values():
    p.group(key, room["id"], name)
for u in p.units:
    if u["parent"] != room["id"]:
        continue
    if u["name"] == "Wall":
        x, y, _ = u["position"]
        side = "North" if y == 5.5 else "South" if y == -5.5 else "West" if x == -10 else "East"
        p.move(u, "geometry", "Perimeter_" + side)
    elif u["name"] == "Matte tiled floor":
        p.move(u, "geometry")
    else:
        p.move(u, stations[u["position"][0]][0], "CeilingLight_NotDefensive" if u["name"] == "Ceiling comparison light" else None)
p.finish()

# These scenes already have meaningful warehouse/lab/medical and narrative
# assemblies. Make their cross-room boundaries and wayfinding explicit; preserve
# every existing functional/nested-prefab assembly intact.
for scene in ["EnvironmentLibraryReview", "EnvironmentNarrativeStressReview"]:
    p = Plan(scene)
    room = p.one(lambda u: u["name"].startswith("Library review") or u["name"].startswith("Service block"))
    p.group("geometry", room["id"], "SharedGeometry")
    p.group("way", room["id"], "SharedWayfinding")
    walls = [u for u in p.units if u["parent"] == room["id"] and u["name"] == "Wall"]
    for i, u in enumerate(walls, 1):
        # Names identify source geometry roles; no walls are moved in world space.
        names = ["Perimeter_North", "Perimeter_South", "Perimeter_West", "Perimeter_East",
                 "WarehouseLaboratory_Upper", "WarehouseLaboratory_Lower",
                 "LaboratoryMedical_Upper", "LaboratoryMedical_Lower",
                 "ReceivingPartition_Upper", "ReceivingPartition_Side"]
        p.move(u, "geometry", names[i - 1])
    arrows = sorted([u for u in p.units if u["parent"] == room["id"] and u["name"] == "arrow-forward"], key=lambda u: u["position"][0])
    for name, u in zip(["Arrow_WarehouseExit", "Arrow_LaboratoryEntry", "Arrow_LaboratoryExit", "Arrow_MedicalEntry"], arrows):
        p.move(u, "way", name)
    if scene == "EnvironmentNarrativeStressReview":
        warehouse = p.one(lambda u: u["name"] == "01 · Receiving and dispatch")
        p.group("loading", warehouse["id"], "ReceivingLoadingLanes")
        stripes = [u for u in p.units if u["parent"] == room["id"] and u["name"] in ["thin-straight", "thick-end"]]
        for i, u in enumerate(sorted(stripes, key=lambda u: (u["position"][0], u["position"][1])), 1):
            p.move(u, "loading", ("LaneEnd_" if u["name"] == "thick-end" else "LaneStripe_") + f"{i:02}")
    p.finish()

# Three real line compositions: arrow sequence, thin junction, closed rectangle.
p = Plan("FloorMarkingsReview")
root = p.one(lambda u: u["name"] == "Compositions from shared sprites")
for key, name in [("arrows", "ArrowSequence"), ("junction", "ThinLineJunction"), ("rectangle", "ClosedRectangle")]:
    p.group(key, root["id"], name)
counts = {}
for u in p.units:
    if u["parent"] != root["id"]:
        continue
    zone = "arrows" if u["position"][1] == 6.5 else "junction" if u["position"][1] > 0 else "rectangle"
    counts[(zone, u["name"])] = counts.get((zone, u["name"]), 0) + 1
    p.move(u, zone, u["name"] + "_" + str(counts[(zone, u["name"])]) )
p.finish()

# Test notes are not gameplay actors. Both existing Sleeper notes are preserved;
# their pre-existing overlap is reported separately rather than changing layout.
p = Plan("MechanicsSandbox")
notes = p.one(lambda u: u["path"] == "10_World/R03_WeaponsRange/Notes")
for u in p.units:
    if u["name"] == "Sleeper sign":
        name = "Sleeper_BehaviorNote" if u["id"].endswith("-1061168066-0") else "Sleeper_TargetNote"
        p.move(u, notes["id"], name)
    elif u["name"] == "Firing lane":
        p.move(u, name={45: "FiringLane_2HP", 50: "FiringLane_6HP", 55: "FiringLane_20HP"}[u["position"][0]])
    elif u["name"].startswith("Test illumination"):
        name = "StalkerTestFill_NotDefensive" if u["position"][0] == 10 else "AccessTestLowerFill_NotDefensive" if u["position"][1] == 5 else "AccessTestUpperFill_NotDefensive"
        p.move(u, name=name)
p.finish()

p = Plan("BasementPrototype")
for zone in ["R01_Laboratory", "C01_Corridor"]:
    actors = [u for u in p.units if u["name"].startswith("Stalker — six legs") and ("/" + zone + "/") in u["path"]]
    for i, u in enumerate(sorted(actors, key=lambda u: (u["position"][0], u["position"][1])), 1):
        p.move(u, name="Stalker_" + ("Laboratory" if zone == "R01_Laboratory" else "Corridor") + f"_{i:02}")
for u in p.units:
    if u["name"] == "ShadowStalker" and "Cover encounter" in u["path"]:
        p.move(u, name="Stalker_CoverProbe_Laboratory")
p.finish()

p = Plan("MorgueRouteTest")
for u in p.units:
    if u["name"] == "Shotgun reserve - 12 shells":
        p.move(u, name="ShotgunReserve_Left_12Shells" if u["position"][0] < 25 else "ShotgunReserve_Right_12Shells")
p.finish()

OUT.mkdir(parents=True, exist_ok=True)
(OUT / "authoring-plan.json").write_text(json.dumps(PLANS, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
for p in PLANS:
    print(Path(p["scene"]).stem, len(p["groups"]), "groups", len(p["moves"]), "reviewed units")
