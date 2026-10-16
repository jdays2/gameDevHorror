"""Editable, spatial revision of MorgueDemo03. No Unity content is touched."""
from pathlib import Path
from collections import deque
import hashlib
import html
import json
import math
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont
OUT = Path(__file__).resolve().parent
W, H, GRID = (1800, 1440, 20)
INK, ROUTE, RETURN, SIGHT = ('#30373e', '#326c8a', '#a46b29', '#597c60')
FONT = 'C:/Windows/Fonts/segoeui.ttf'
BOLD = 'C:/Windows/Fonts/segoeuib.ttf'
if not Path(FONT).exists():
    FONT = 'DejaVuSans.ttf'
    BOLD = 'DejaVuSans-Bold.ttf'
pages = json.loads((OUT / 'plan.json').read_text(encoding='utf-8'))['pages']

def cells(rect):
    x, y, w, h = rect
    assert all((n % GRID == 0 for n in rect))
    return [(i, j) for i in range(x // GRID, (x + w) // GRID) for j in range(y // GRID, (y + h) // GRID)]

def edgekey(a, b):
    return tuple(sorted((tuple(a), tuple(b))))

def prepare(p):
    occupancy = {}
    lookup = {s['id']: s for s in p['spaces']}
    for s in p['spaces']:
        for c in cells(s['rect']):
            assert c not in occupancy, ('overlapping spaces', s['id'], c)
            occupancy[c] = s['id']
    blockers = {c for ob in p['obstacles'] for c in cells(ob['rect'])}
    assert blockers <= occupancy.keys(), 'Obstacle outside floor'
    openings = {}
    for d in p['doors']:
        x1, y1, x2, y2 = d['line']
        assert x1 == x2 or y1 == y2
        for v in range(y1, y2, GRID) if x1 == x2 else range(x1, x2, GRID):
            a, b = ((x1 // GRID - 1, v // GRID), (x1 // GRID, v // GRID)) if x1 == x2 else ((v // GRID, y1 // GRID - 1), (v // GRID, y1 // GRID))
            assert {occupancy.get(a), occupancy.get(b)} == {d['a'], d['b']}, ('invalid door', d['id'], a, b)
            openings[edgekey(a, b)] = d
    walls = {}
    for (x, y), id in occupancy.items():
        for dx, dy, line in [(0, -1, [x * GRID, y * GRID, (x + 1) * GRID, y * GRID]), (0, 1, [x * GRID, (y + 1) * GRID, (x + 1) * GRID, (y + 1) * GRID]), (-1, 0, [x * GRID, y * GRID, x * GRID, (y + 1) * GRID]), (1, 0, [(x + 1) * GRID, y * GRID, (x + 1) * GRID, (y + 1) * GRID])]:
            other = (x + dx, y + dy)
            oid = occupancy.get(other)
            if oid == id or (oid and lookup[oid].get('group', oid) == lookup[id].get('group', id)):
                continue
            key = edgekey((x, y), other)
            if key not in openings:
                walls[key] = line
    p['_occ'], p['_block'], p['_open'], p['_walls'] = (occupancy, blockers, openings, walls)

def point_cell(pt):
    return (int(pt[0] // GRID), int(pt[1] // GRID))

def find_path(p, start, end, unlock=False, without=()):
    start, end = (point_cell(start), point_cell(end))
    blocked = p['_block'] | {c for c, id in p['_occ'].items() if id in without}
    q = deque([start])
    previous = {start: None}
    while q:
        cur = q.popleft()
        if cur == end:
            path = []
            while cur is not None:
                path.append([cur[0] * GRID + GRID // 2, cur[1] * GRID + GRID // 2])
                cur = previous[cur]
            return path[::-1]
        for dx, dy in [(1, 0), (0, 1), (-1, 0), (0, -1)]:
            nxt = (cur[0] + dx, cur[1] + dy)
            key = edgekey(cur, nxt)
            if nxt in previous or nxt not in p['_occ'] or nxt in blocked or (key in p['_walls']):
                continue
            door = p['_open'].get(key)
            if door and (door['kind'] == 'secret' or (door['kind'] == 'shortcut' and (not unlock))):
                continue
            previous[nxt] = cur
            q.append(nxt)
    return None

def simplify(path):
    result = []
    for p in path:
        if len(result) >= 2 and (result[-1][0] - result[-2][0]) * (p[1] - result[-1][1]) == (result[-1][1] - result[-2][1]) * (p[0] - result[-1][0]):
            result[-1] = p
        else:
            result.append(p)
    return result

def segment_hit(a, b, line):
    x, y, x2, y2 = line
    dx, dy = (b[0] - a[0], b[1] - a[1])
    if x == x2 and dx:
        t = (x - a[0]) / dx
        v = a[1] + t * dy
        if 0 < t < 1 and min(y, y2) - 0.01 <= v <= max(y, y2) + 0.01:
            return (t, [x, v])
    if y == y2 and dy:
        t = (y - a[1]) / dy
        v = a[0] + t * dx
        if 0 < t < 1 and min(x, x2) - 0.01 <= v <= max(x, x2) + 0.01:
            return (t, [v, y])

def ray(p, a, b):
    lines = list(p['_walls'].values()) + [d['line'] for d in p['doors'] if d['kind'] != 'open']
    for ob in p['obstacles']:
        x, y, w, h = ob['rect']
        lines += [[x, y, x + w, y], [x + w, y, x + w, y + h], [x + w, y + h, x, y + h], [x, y + h, x, y]]
    hits = [hit for line in lines if (hit := segment_hit(a, b, line))]
    return min(hits, key=lambda v: v[0])[1] if hits else None

def audit():
    b, u = pages
    side_b = ['BO1', 'B00', 'B05', 'B06', 'B07', 'S1']
    side_u = ['UO1', 'U6', 'U7', 'S2', 'T1', 'T2', 'T3']
    start, end = ([350, 710], [1190, 1170])
    checks = {'basement_without_side_content': bool(find_path(b, start, end, without=side_b)), 'b02_mandatory_before_shortcut': find_path(b, start, end, without=['B02']) is None, 'shortcut_before_last_basement_encounter': bool(find_path(b, [1030, 830], [870, 1170])), 'upper_a_without_side_content': bool(find_path(u, [1190, 1170], [430, 310], without=side_u + ['U2', 'C12a', 'C12b'])), 'upper_b_without_side_content': bool(find_path(u, [1190, 1170], [430, 310], without=side_u + ['A'])), 'upper_observation_hub_mandatory': find_path(u, [1190, 1170], [830, 970], without=['U1']) is None, 'third_role_before_arena': find_path(u, [1190, 1170], [430, 310], without=['U4']) is None, 'floor_transition_same_coordinates': next((s['rect'] for s in b['spaces'] if s['id'] == 'Vminus')) == next((s['rect'] for s in u['spaces'] if s['id'] == 'Vzero')), 'no_optional_stalker_in_main_routes': True}
    closed = find_path(b, [1030, 830], [250, 1030])
    opened = find_path(b, [1030, 830], [250, 1030], unlock=True)
    checks['shortcut_shorter'] = len(opened) < len(closed)
    a = find_path(u, [1190, 970], [830, 970], without=['U2', 'C12a', 'C12b'])
    alt = find_path(u, [1190, 970], [830, 970], without=['A'])
    checks['b_physically_longer_than_a'] = len(alt) > len(a)
    for p in pages:
        side_ids = {s['id'] for s in p['spaces'] if s['kind'] in ['optional', 'secret']}
        for r in p['routes']:
            path = []
            for src, dst in zip(r['via'], r['via'][1:]):
                leg = find_path(p, src, dst, r.get('unlock', False))
                assert leg, ('no route', p['id'], r['id'], src, dst)
                path += leg if not path else leg[1:]
            r['points'] = simplify(path)
            r['grid_steps'] = len(path) - 1
            if r['kind'] == 'main':
                checks['no_optional_stalker_in_main_routes'] &= all((p['_occ'][point_cell(pt)] not in side_ids for pt in path))
        for peek in p['peeks']:
            checks[p['id'] + '_' + peek['id'] + '_clear_sightline'] = ray(p, peek['at'], peek['target']) is None
            if 'hidden' in peek:
                peek['occlusion'] = ray(p, peek['at'], peek['hidden'])
                checks[p['id'] + '_' + peek['id'] + '_far_target_occluded'] = peek['occlusion'] is not None
    assert all(checks.values()), checks
    return dict(checks=checks, shortcut_grid_steps=dict(closed=len(closed) - 1, open=len(opened) - 1), upper_choice_grid_steps=dict(A=len(a) - 1, B=len(alt) - 1), scope='Relative plan geometry only, 20 drawing units per grid cell. Not Unity units, player clearance, AI, combat, time, balance or FPS validation.')

class Canvas:

    def __init__(self, id):
        self.id = id
        self.svg = []
        self.shapes = []
        self.img = Image.new('RGB', (W, H), '#fafafa')
        self.d = ImageDraw.Draw(self.img)

    def add(self, kind, **kw):
        self.shapes.append(dict(kind=kind, **kw))

    def text(self, x, y, text, size=18, bold=False, color=INK, bg=None):
        outline = f' paint-order="stroke" stroke="{bg}" stroke-width="5" stroke-linejoin="round"' if bg else ''
        self.svg.append(f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{(700 if bold else 400)}" fill="{color}"{outline}>{html.escape(text)}</text>')
        self.d.text((x, y), text, font=ImageFont.truetype(BOLD if bold else FONT, size), fill=color, anchor='ls', stroke_width=2 if bg else 0, stroke_fill=bg)
        self.add('text', x=x, y=y, text=text, size=size, bold=bold, color=color, bg=bg)

    def line(self, pts, color=INK, width=3, dash=False, arrow=False):
        self.svg.append(f'''<polyline points="{' '.join((f'{x},{y}' for x, y in pts))}" fill="none" stroke="{color}" stroke-width="{width}"''' + (' stroke-dasharray="9 6"' if dash else '') + '/>')
        for a, b in zip(pts, pts[1:]):
            if dash:
                length = math.dist(a, b)
                if length == 0:
                    continue
                for dist in range(0, math.ceil(length), 15):
                    end = min(dist + 9, length)
                    p1 = tuple((a[i] + (b[i] - a[i]) * dist / length for i in [0, 1]))
                    p2 = tuple((a[i] + (b[i] - a[i]) * end / length for i in [0, 1]))
                    self.d.line([p1, p2], fill=color, width=width)
            else:
                self.d.line([tuple(a), tuple(b)], fill=color, width=width)
        self.add('line', points=pts, color=color, width=width, dash=dash, arrow=arrow)
        if arrow:
            a, b = pts[-2:]
            ang = math.atan2(b[1] - a[1], b[0] - a[0])
            self.line([[b[0] - 12 * math.cos(ang - 0.45), b[1] - 12 * math.sin(ang - 0.45)], b, [b[0] - 12 * math.cos(ang + 0.45), b[1] - 12 * math.sin(ang + 0.45)]], color, width)

    def rect(self, rect, fill, stroke=None):
        x, y, w, h = rect
        self.svg.append(f'''<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" stroke="{stroke or 'none'}"/>''')
        self.d.rectangle([x, y, x + w, y + h], fill=fill, outline=stroke)
        self.add('rect', rect=rect, fill=fill, stroke=stroke)

    def circle(self, at, text, color=INK, fill='#fafafa', radius=15):
        x, y = at
        self.svg.append(f'<circle cx="{x}" cy="{y}" r="{radius}" fill="{fill}" stroke="{color}" stroke-width="2"/>')
        self.d.ellipse([x - radius, y - radius, x + radius, y + radius], fill=fill, outline=color, width=2)
        self.add('ellipse', rect=[x - radius, y - radius, 2 * radius, 2 * radius], fill=fill, stroke=color)
        if text:
            self.text(x - len(text) * 4, y + 5, text, 14, True, color)

    def paragraph(self, x, y, text, width=340, size=18, bg=None):
        current = ''
        for word in text.split():
            s = (current + ' ' + word).strip()
            if current and self.d.textlength(s, font=ImageFont.truetype(FONT, size)) > width:
                self.text(x, y, current, size, bg=bg)
                y += 25
                current = word
            else:
                current = s
        if current:
            self.text(x, y, current, size, bg=bg)
            y += 25
        return y

def render(p, report):
    c = Canvas(p['id'])
    c.text(60, 55, p['title'], 34, True)
    c.text(60, 92, p['subtitle'], 22)
    c.text(60, 124, 'Ревизия 03 · пространственное предложение · без арта · относительные размеры', 18, color='#64707a')
    fills = {'room': '#ffffff', 'side': '#f4f5f5', 'corridor': '#e9edf0', 'stairs': '#dce4e9', 'optional': '#edf3f6', 'secret': '#f0eef4', 'arena': '#edf0f2', 'threat': '#ece8e4'}
    for s in p['spaces']:
        c.svg.append(f"""<g id="space-{s['id']}"><title>{html.escape(s['title'])}</title>""")
        c.rect(s['rect'], fills[s['kind']])
        c.svg.append('</g>')
    for r in p['routes']:
        color = RETURN if r['kind'] == 'return' else ROUTE if r['kind'] in ['main', 'alternative'] else '#7b8b96'
        c.svg.append(f"""<g id="route-{r['id']}">""")
        c.line(r['points'], color, 3, r['kind'] != 'main', True)
        c.svg.append('</g>')
    for ob in p['obstacles']:
        c.svg.append(f"""<g id="block-{ob['id']}">""")
        c.rect(ob['rect'], '#b5bdc3', INK)
        c.svg.append('</g>')
    for line in p['_walls'].values():
        c.line([[line[0], line[1]], [line[2], line[3]]], INK, 4)
    for d in p['doors']:
        x, y, x2, y2 = d['line']
        color = RETURN if d['kind'] == 'shortcut' else '#78688c' if d['kind'] == 'secret' else '#68747d'
        if d['kind'] != 'open':
            c.line([[x, y], [x2, y2]], color, 4, True)
        if x == x2:
            c.line([[x - 6, y], [x + 6, y]], color, 2)
            c.line([[x - 6, y2], [x + 6, y2]], color, 2)
        else:
            c.line([[x, y - 6], [x, y + 6]], color, 2)
            c.line([[x2, y - 6], [x2, y + 6]], color, 2)
    for s in p['spaces']:
        x, y = s['label']
        w = s['rect'][0] + s['rect'][2] - x - 10
        bg = fills[s['kind']]
        if s['id'] not in ['T1', 'T2', 'T3']:
            c.text(x, y, s['id'].replace('minus', ' −1').replace('zero', ' 0'), 13, True, '#68747d', bg)
            y += 21
        y = c.paragraph(x, y, s['title'], w, 17, bg)
        if s['detail']:
            c.paragraph(x, y, s['detail'], w, 15, bg)
    for m in p['markers']:
        color = '#714f4f' if m['kind'] == 'enemy' else INK
        c.circle(m['at'], m['text'], color, radius=17 if len(m['text']) > 2 else 14)
    for peek in p['peeks']:
        c.line([peek['at'], peek['target']], SIGHT, 2, True, True)
        if 'occlusion' in peek:
            c.line([peek['at'], peek['occlusion']], '#8a8e92', 2, True)
        x, y = peek['at']
        c.circle([x, y], None, SIGHT, radius=6)
        c.text(x - 28, y + 30, peek['id'], 16, True, SIGHT)
    if p['id'] == 'basement':
        c.text(925, 740, 'D1', 19, True, RETURN)
        c.text(845, 1020, '↓ ещё один участок', 17, color=ROUTE)
    else:
        c.text(100, 850, 'Победа → итоговая статистика', 18, True)
        for pts in [[[280, 190], [280, 280]], [[45, 540], [135, 540]], [[280, 790], [280, 685]]]:
            c.line(pts, '#8e6256', 3, False, True)
    c.line([[1360, 160], [1360, 1260]], '#bdc6cd', 1)
    y = 190
    for title, body in p['notes']:
        c.text(1390, y, title, 21, True)
        y += 30
        y = c.paragraph(1390, y, body, 350, 18) + 27
    if p['id'] == 'basement':
        v = report['shortcut_grid_steps']
        c.text(1390, 1200, f"D1: возврат {v['closed']} → {v['open']} шагов сетки", 17, True, RETURN)
    else:
        v = report['upper_choice_grid_steps']
        c.text(1390, 1200, f"U1 → U3: A {v['A']} / B {v['B']} шагов сетки", 17, True, ROUTE)
    c.line([[60, 1285], [1740, 1285]], '#bdc6cd', 1)
    c.text(60, 1320, 'Синий → основной путь; синий пунктир → обход. Охра → возврат после открытия D1. Зелёный → видимость из P0–P3.', 18)
    c.text(60, 1350, 'Серые блоки — крупные препятствия. I / II / III? — роли угроз, количество условно. W? / +Ф / +О — предложения выдачи и награды.', 18)
    c.text(60, 1380, 'Весь уровень тёмный. Повороты не сбрасывают агрессию. Размеры, свет, бой и длительность 30 / 50 минут требуют будущей игровой проверки.', 18)
    return c

def svg(body, height=H):
    return f'<?xml version="1.0" encoding="utf-8"?>\n<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{height}" viewBox="0 0 {W} {height}"><style>text{{font-family:"Segoe UI",Arial,sans-serif}}</style><rect width="{W}" height="{height}" fill="#fafafa"/>\n' + body + '\n</svg>\n'

def export_drawio(canvases):
    file = ET.Element('mxfile', host='app.diagrams.net', type='device')
    for p, c in zip(pages, canvases):
        dia = ET.SubElement(file, 'diagram', id=p['id'], name=p['title'])
        model = ET.SubElement(dia, 'mxGraphModel', grid='1', gridSize='20', page='1', pageScale='1', pageWidth=str(W), pageHeight=str(H), background='#fafafa')
        root = ET.SubElement(model, 'root')
        ET.SubElement(root, 'mxCell', id='0')
        ET.SubElement(root, 'mxCell', id='1', parent='0')
        for i, s in enumerate(c.shapes):
            kind = s['kind']
            style = 'fontFamily=Segoe UI;'
            attrs = dict(id=f'element-{i}', parent='1')
            value = ''
            if kind == 'line':
                pts = s['points']
                style += f"edgeStyle=none;endArrow=none;strokeColor={s['color']};strokeWidth={s['width']};"
                if s['dash']:
                    style += 'dashed=1;dashPattern=9 6;'
                attrs['edge'] = '1'
                cell = ET.SubElement(root, 'mxCell', style=style, **attrs)
                geo = ET.SubElement(cell, 'mxGeometry', relative='1', **{'as': 'geometry'})
                for point, name in [(pts[0], 'sourcePoint'), (pts[-1], 'targetPoint')]:
                    ET.SubElement(geo, 'mxPoint', x=str(point[0]), y=str(point[1]), **{'as': name})
                if len(pts) > 2:
                    arr = ET.SubElement(geo, 'Array', **{'as': 'points'})
                    for x, y in pts[1:-1]:
                        ET.SubElement(arr, 'mxPoint', x=str(x), y=str(y))
            else:
                if kind == 'text':
                    x, y = (s['x'], s['y'] - s['size'])
                    w = c.d.textlength(s['text'], font=ImageFont.truetype(BOLD if s['bold'] else FONT, s['size'])) + 8
                    h = s['size'] * 1.35
                    value = s['text']
                    style += f"text;html=0;strokeColor=none;fillColor={s.get('bg') or 'none'};align=left;verticalAlign=top;fontSize={s['size']};fontColor={s['color']};fontStyle={(1 if s['bold'] else 0)};spacing=0;"
                else:
                    x, y, w, h = s['rect']
                    style += f"shape={('ellipse' if kind == 'ellipse' else 'rectangle')};fillColor={s['fill']};strokeColor={s.get('stroke') or 'none'};"
                cell = ET.SubElement(root, 'mxCell', value=value, style=style, vertex='1', **attrs)
                ET.SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h), **{'as': 'geometry'})
    ET.indent(file, space='  ')
    ET.ElementTree(file).write(OUT / 'morgue-demo.drawio', encoding='utf-8', xml_declaration=True)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for p in pages:
        prepare(p)
    report = audit()
    canvases = [render(p, report) for p in pages]
    parts = []
    combined = Image.new('RGB', (W, H * 2), '#fafafa')
    for i, (p, c) in enumerate(zip(pages, canvases)):
        body = '\n'.join(c.svg)
        (OUT / (p['id'] + '.svg')).write_text(svg(body), encoding='utf-8')
        c.img.save(OUT / (p['id'] + '.png'))
        combined.paste(c.img, (0, i * H))
        parts.append(f"""<g id="floor-{p['id']}" transform="translate(0,{i * H})">{body}</g>""")
    combined.save(OUT / 'preview.png')
    (OUT / 'demo.svg').write_text(svg('\n'.join(parts), H * 2), encoding='utf-8')
    export_drawio(canvases)
    meta = dict(title='Emergency Shift — spatial revision 03', date='2026-10-16', status='Proposal; no Unity changes', drawing_units='Relative: grid 20. Not game units. V footprints aligned across floors.', implementation='No new enemy, weapon progression, scene or AI feature is implemented by this plan.', reference_adoption='Geometric principles from documented AS maps; functional/visibility principles are our adaptation of Subterrain/Teleglitch research.')
    clean = [{k: v for k, v in p.items() if not k.startswith('_')} for p in pages]
    (OUT / 'plan.json').write_text(json.dumps(dict(meta=meta, pages=clean), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (OUT / 'validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    files = ['plan.json', 'morgue-demo.drawio', 'basement.svg', 'upper.svg', 'demo.svg', 'basement.png', 'upper.png', 'preview.png', 'validation.json', 'export.py']
    provenance = dict(author='Project-authored spatial plan, native SVG/draw.io and Pillow from identical drawing primitives', date=meta['date'], source='plan.json + export.py', license='Project-authored schematic; no third-party artwork', status='Spatial proposal, unapproved, not integrated', files={name: hashlib.sha256((OUT / name).read_bytes()).hexdigest() for name in files})
    (OUT / 'provenance.json').write_text(json.dumps(provenance, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))
if __name__ == '__main__':
    main()
