"""Dump GameObjects from a Unity scene: name, parent path, position, scale, sprite file, sorting order, notable fields."""
import re, sys, os, glob
scene, root = sys.argv[1], sys.argv[2]
guid2file = {}
for m in glob.glob(root + '/Assets/**/*.meta', recursive=True):
    g = re.search(r'guid: (\w+)', open(m, errors='ignore').read())
    if g: guid2file[g.group(1)] = os.path.relpath(m[:-5], root)
txt = open(scene, errors='ignore').read()
docs = re.split(r'^--- !u!(\d+) &(-?\d+).*$', txt, flags=re.M)
objs = {}
for i in range(1, len(docs), 3):
    objs[docs[i + 1]] = (docs[i], docs[i + 2])
go_name, comp_go, tr = {}, {}, {}
for fid, (cls, body) in objs.items():
    if cls == '1':
        mm = re.search(r'm_Name: (.*)', body); go_name[fid] = mm.group(1) if mm else '(prefab)'
    g = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body)
    if g: comp_go.setdefault(g.group(1), []).append((cls, fid, body))
    if cls in ('4', '224'):
        tr[fid] = body
def num(body, key):
    m = re.search(key + r': \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)', body)
    return tuple(round(float(v), 3) for v in m.groups()) if m else None
tr_go = {}
for fid, body in tr.items():
    mm = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', body); tr_go[fid] = mm.group(1) if mm else '0'
def path(tfid):
    parts = []
    while tfid and tfid != '0' and tfid in tr:
        parts.append(go_name.get(tr_go[tfid], '?'))
        mm = re.search(r'm_Father: \{fileID: (-?\d+)\}', tr[tfid]); tfid = mm.group(1) if mm else '0'
    return '/'.join(reversed(parts))
for tfid, body in tr.items():
    if objs[tfid][0] != '4': continue
    g = tr_go[tfid]
    line = f"{path(tfid):60s} pos={num(body,'m_LocalPosition')} scale={num(body,'m_LocalScale')}"
    act = re.search(r"m_IsActive: (\d)", objs[g][1]) if g in objs else None
    if act and act.group(1) == '0': line += ' INACTIVE'
    for cls, fid, cb in comp_go.get(g, []):
        if cls == '212':
            s = re.search(r'm_Sprite: \{fileID: (-?\d+), guid: (\w+)', cb)
            so = re.search(r'm_SortingOrder: (-?\d+)', cb)
            col = re.search(r'm_Color: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)', cb)
            dm = re.search(r'm_DrawMode: (\d)', cb); sz = re.search(r'm_Size: \{x: ([\d.]+), y: ([\d.]+)', cb)
            line += f" SPR={guid2file.get(s.group(2),'?') if s else None} order={so.group(1) if so else ''} col={col.groups() if col else ''} draw={dm.group(1) if dm else ''} size={sz.groups() if sz else ''}"
        elif cls == '114':
            sc = re.search(r'm_Script: \{fileID: \d+, guid: (\w+)', cb)
            name = os.path.basename(guid2file.get(sc.group(1), '?')) if sc else '?'
            if name.endswith('.cs') or 'Water' in name or 'Parallax' in name:
                fields = re.findall(r'^  (parallaxFactor|waveAmplitude|waveSpeed|numSprings|spacing|waveStep|smoothTime): (.*)$', cb, re.M)
                line += f" [{name} {fields}]"
        elif cls == '20':
            sz = re.search(r'orthographic size: ([\d.]+)', cb)
            tt = re.search(r'm_TargetTexture: \{fileID: (-?\d+), guid: (\w+)', cb)
            line += f" CAMERA ortho={sz.group(1) if sz else ''} target={guid2file.get(tt.group(2)) if tt else None}"
        elif cls == '23':
            mats = re.findall(r'guid: (\w+)', cb)
            so = re.search(r'm_SortingOrder: (-?\d+)', cb)
            line += f" MESH mats={[guid2file.get(m) for m in mats]} order={so.group(1) if so else ''}"
    print(line)
