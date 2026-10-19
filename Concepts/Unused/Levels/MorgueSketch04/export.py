"""Export this proposal only. Never regenerates earlier plans or Unity scenes."""
import argparse
from collections import deque
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
WIDTH, HEIGHT = 1160, 1010
BLACK, BLUE, GREEN, ORANGE = '#252c32', '#4657c7', '#238856', '#bb6523'
primitives = []


def box(x, y, w, h, fill='white', stroke=BLACK, width=3, ellipse=False):
    primitives.append(dict(kind='ellipse' if ellipse else 'rect', x=x, y=y,
                           w=w, h=h, fill=fill, stroke=stroke, width=width))


def line(x1, y1, x2, y2, color=BLACK, width=3):
    primitives.append(dict(kind='line', x=x1, y=y1, x2=x2, y2=y2,
                           stroke=color, width=width))


def label(x, y, value, size=18, color=BLACK, bold=False):
    primitives.append(dict(kind='text', x=x, y=y, text=value,
                           size=size, fill=color, bold=bold))


def point(x, y):
    return x * 1.8 + 40, y * 1.8 - 140


def room(x, y, w, h, fill='white'):
    px, py = point(x, y)
    box(px, py, w * 1.8, h * 1.8, fill=fill, width=4)


def note(x, y, value, size=18, color=BLACK, bold=False):
    px, py = point(x, y)
    label(px, py, value, size, color, bold)


def door(x, y, length, axis, access=None, number=None, leaf=True):
    px, py = point(x, y)
    span = length * 1.8
    color = {'blue': BLUE, 'green': GREEN}.get(access, BLACK)
    if axis == 'v':
        line(px, py, px, py + span, 'white', 9)
        if leaf:
            line(px, py + span, px + span * .58, py + span * .18, color, 5)
        if access:
            label(px + 15, py + 8, 'B' if access == 'blue' else 'G', 15, color, True)
    else:
        line(px, py, px + span, py, 'white', 9)
        if leaf:
            line(px, py, px + span * .85, py - span * .4, color, 5)
        if access:
            label(px + span * .65, py - 25, 'B' if access == 'blue' else 'G', 15, color, True)
    if number:
        bx, by = px - 32, py + 2
        box(bx, by, 28, 28, ORANGE, ORANGE, 1, True)
        label(bx + 8, by + 3, str(number), 18, 'white', True)


def compose():
    label(42, 23, 'ПЕРВЫЙ УРОВЕНЬ · ПРЕДЛОЖЕНИЕ 04', 27, bold=True)
    label(42, 62, 'По зарисовке владельца · 19.10.2026 · относительные размеры', 18)
    label(42, 91, 'B — синяя карта    G — зелёная карта    Чёрная створка — обычная дверь', 17)
    rooms = [
        (5, 145, 184, 127), (9, 272, 22, 106), (9, 363, 43, 15),
        (52, 311, 81, 73), (133, 329, 116, 37),
        (189, 187, 120, 35), (249, 222, 60, 76),
        (249, 298, 224, 105), (334, 242, 38, 56),
        (267, 403, 36, 60), (332, 403, 37, 67),
        (473, 320, 99, 45), (531, 365, 41, 88),
        (406, 403, 42, 84), (448, 453, 124, 63),
    ]
    for geometry in rooms:
        room(*geometry)
    # Erase the interior seam in the L-shaped western corridor.
    door(10, 363, 20, 'h', leaf=False)
    door(31, 364, 13, 'v', leaf=False)
    # Traversable seams inferred from the original schematic.
    door(11, 272, 19, 'h', leaf=False)
    door(269, 222, 28, 'h', leaf=False)
    # Existing ordinary doors and passage joins.
    door(133, 333, 29, 'v')
    door(249, 334, 26, 'v', leaf=False)
    door(473, 324, 30, 'v')
    door(536, 365, 30, 'h')
    door(536, 453, 30, 'h', leaf=False)
    # Retained card gates, including the western post-blue shortcut.
    door(52, 364, 14, 'v', 'blue')
    door(263, 298, 34, 'h', 'green')
    door(411, 403, 32, 'h', 'green')
    door(271, 403, 28, 'h', 'blue')
    door(336, 403, 29, 'h', 'blue')
    door(340, 298, 26, 'h', 'blue')
    # The only access-rule changes, numbered as in FIRST_LEVEL.md.
    door(189, 192, 28, 'v', number=1)
    door(448, 462, 25, 'v', number=2)
    # Suggested large obstacles; no decorative or combat placement implied.
    for geometry in [(45, 196, 45, 18), (109, 232, 46, 18),
                     (285, 338, 42, 18), (382, 342, 42, 18)]:
        room(*geometry, fill='#cdd2d3')
    note(20, 159, 'СИНЯЯ КАРТА', 19, BLUE, True)
    note(20, 180, 'B', 24, BLUE, True)
    note(66, 323, 'СТАРТ', 21, bold=True)
    note(66, 345, 'Морг', 18)
    note(223, 195, 'Патроны', 17)
    note(255, 244, 'Пистолет', 16)
    note(267, 317, 'Распределительный зал', 20, bold=True)
    note(342, 259, 'B3', 19, BLUE, True)
    note(273, 428, 'B1', 19, BLUE, True)
    note(338, 433, 'B2', 19, BLUE, True)
    note(508, 333, 'Обход', 17)
    note(464, 472, 'ЗЕЛЁНАЯ', 17, GREEN, True)
    note(464, 490, 'КАРТА  G', 17, GREEN, True)
    label(42, 815, '1  Вход к синей карте со стороны патронов — обычная дверь.', 20, ORANGE, True)
    label(42, 850, '2  Убрать повторный зелёный замок у комнаты карты.', 20, ORANGE, True)
    label(42, 885, 'Серое — предлагаемые препятствия. B1–B3 — боковые ветки по синей карте.', 17)
    label(42, 917, 'Геометрия слегка расширена для читаемости. Коллизии и бой ещё не проверены.', 17)
    label(42, 954, 'Будущий путь к выходу по B — за пределами схемы; новые локации не добавлены.', 18, bold=True)


def render():
    svg = ET.Element('svg', xmlns='http://www.w3.org/2000/svg',
                     width=str(WIDTH), height=str(HEIGHT), viewBox=f'0 0 {WIDTH} {HEIGHT}')
    ET.SubElement(svg, 'rect', width='100%', height='100%', fill='white')
    bitmap = Image.new('RGB', (WIDTH, HEIGHT), 'white')
    draw = ImageDraw.Draw(bitmap)
    mxfile = ET.Element('mxfile', host='app.diagrams.net')
    diagram = ET.SubElement(mxfile, 'diagram', name='Предложение 04', id='morgue-sketch-04')
    model = ET.SubElement(diagram, 'mxGraphModel', page='1', pageWidth=str(WIDTH), pageHeight=str(HEIGHT))
    graph = ET.SubElement(model, 'root')
    ET.SubElement(graph, 'mxCell', id='0')
    ET.SubElement(graph, 'mxCell', id='1', parent='0')
    for index, p in enumerate(primitives, 2):
        kind = p['kind']
        x, y = p['x'], p['y']
        if kind == 'text':
            font_path = 'C:/Windows/Fonts/arialbd.ttf' if p['bold'] else 'C:/Windows/Fonts/arial.ttf'
            font = ImageFont.truetype(font_path, p['size'])
            draw.text((x, y), p['text'], font=font, fill=p['fill'])
            element = ET.SubElement(svg, 'text', x=str(x), y=str(y + p['size']),
                                    fill=p['fill'], **{'font-family': 'Arial, sans-serif',
                                    'font-size': str(p['size']), 'font-weight': 'bold' if p['bold'] else 'normal'})
            element.text = p['text']
            style = f'text;html=0;align=left;verticalAlign=top;whiteSpace=wrap;fontFamily=Arial;fontSize={p["size"]};fontColor={p["fill"]};fontStyle={1 if p["bold"] else 0};'
            w, h = draw.textbbox((0, 0), p['text'], font=font)[2:]
            cell = ET.SubElement(graph, 'mxCell', id=str(index), parent='1', vertex='1', value=p['text'], style=style)
            ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w + 8), height=str(h + 10), **{'as': 'geometry'})
        elif kind == 'line':
            draw.line((x, y, p['x2'], p['y2']), fill=p['stroke'], width=p['width'])
            ET.SubElement(svg, 'line', x1=str(x), y1=str(y), x2=str(p['x2']), y2=str(p['y2']), stroke=p['stroke'], **{'stroke-width': str(p['width'])})
            cell = ET.SubElement(graph, 'mxCell', id=str(index), parent='1', edge='1', style=f'endArrow=none;startArrow=none;strokeColor={p["stroke"]};strokeWidth={p["width"]};')
            geom = ET.SubElement(cell, 'mxGeometry', relative='1', **{'as': 'geometry'})
            ET.SubElement(geom, 'mxPoint', x=str(x), y=str(y), **{'as': 'sourcePoint'})
            ET.SubElement(geom, 'mxPoint', x=str(p['x2']), y=str(p['y2']), **{'as': 'targetPoint'})
        else:
            coords = (x, y, x + p['w'], y + p['h'])
            painter = draw.ellipse if kind == 'ellipse' else draw.rectangle
            painter(coords, fill=p['fill'], outline=p['stroke'], width=p['width'])
            attrs = dict(fill=p['fill'], stroke=p['stroke'], **{'stroke-width': str(p['width'])})
            if kind == 'ellipse':
                ET.SubElement(svg, 'ellipse', cx=str(x + p['w'] / 2), cy=str(y + p['h'] / 2), rx=str(p['w'] / 2), ry=str(p['h'] / 2), **attrs)
            else:
                ET.SubElement(svg, 'rect', x=str(x), y=str(y), width=str(p['w']), height=str(p['h']), **attrs)
            style = f'{"ellipse" if kind == "ellipse" else "rounded=0"};fillColor={p["fill"]};strokeColor={p["stroke"]};strokeWidth={p["width"]};'
            cell = ET.SubElement(graph, 'mxCell', id=str(index), parent='1', vertex='1', style=style)
            ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(p['w']), height=str(p['h']), **{'as': 'geometry'})
    (ROOT / 'proposal.svg').write_bytes(ET.tostring(svg, encoding='utf-8', xml_declaration=True))
    (ROOT / 'proposal.drawio').write_bytes(ET.tostring(mxfile, encoding='utf-8', xml_declaration=True))
    bitmap.save(ROOT / 'proposal.png')


def explore(plan, proposed, excluded=(), forbidden_loot=()):
    start = ('S', frozenset())
    queue = deque([(start, ['S'])])
    seen = {start}
    witnesses = {}
    visited = set()
    while queue:
        (node, inventory), path = queue.popleft()
        visited.add(node)
        for item in inventory:
            witnesses.setdefault(item, path)
        for edge in plan['edges']:
            if node not in (edge['a'], edge['b']):
                continue
            target = edge['b'] if node == edge['a'] else edge['a']
            requirement = edge.get('proposedRequires', edge['requires']) if proposed else edge['requires']
            if target in excluded or requirement and requirement not in inventory:
                continue
            loot = plan['loot'].get(target)
            acquired = inventory | {loot} if loot and loot not in forbidden_loot else inventory
            state = (target, frozenset(acquired))
            if state not in seen:
                seen.add(state)
                queue.append((state, path + [target]))
    return dict(reachableNodes=sorted(visited), acquiredItems=sorted(witnesses), witnessPaths=witnesses)


def validate():
    plan = json.loads((ROOT / 'plan.json').read_text(encoding='utf-8'))
    original = explore(plan, False)
    proposed = explore(plan, True)
    optional_removed = explore(plan, True, excluded=('O1', 'O2', 'O3'))
    no_green = explore(plan, True, forbidden_loot=('green',))
    checks = {
        'original_blue_self_lock_detected': 'blue' not in original['acquiredItems'],
        'proposal_all_loot_reachable_from_empty_inventory': set(plan['loot'].values()) <= set(proposed['acquiredItems']),
        'proposal_all_local_nodes_reachable': set(plan['nodes']) == set(proposed['reachableNodes']),
        'blue_does_not_depend_on_optional_branches': 'blue' in optional_removed['acquiredItems'],
        'green_gate_still_precedes_weapon_and_blue': not {'pistol', 'ammo', 'blue'} & set(no_green['acquiredItems']),
        'no_exit_node_added_or_tested': not any('exit' in node.lower() for node in plan['nodes']),
    }
    assert all(checks.values()), checks
    report = dict(checks=checks, original=original, proposal=proposed,
                  optionalBranchesRemoved=optional_removed, withoutGreenPickup=no_green,
                  limits=plan['limits'])
    (ROOT / 'validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps(checks, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--replace', action='store_true', help='Explicitly overwrite this revision; never use after manual edits.')
    args = parser.parse_args()
    if not args.replace and any((ROOT / name).exists() for name in ('proposal.svg', 'proposal.drawio', 'proposal.png')):
        parser.error('Export already exists. Preserve manual changes; create a new revision or explicitly use --replace.')
    compose()
    render()
    validate()
