"""Convert .aseprite files to PNG (no dependencies).

Usage: python3 -I ase2png.py <src.aseprite> <out_prefix>
Writes <out_prefix>.png for single-frame files, <out_prefix>_<n>.png per frame otherwise,
and prints "w h frames durations" so callers can record animation timing.
Visible layers are flattened with normal blending (layer + cel opacity).
"""
import struct, sys, zlib

def read_ase(path):
    data = open(path, 'rb').read()
    (fsize, magic, nframes, W, H, depth, flags) = struct.unpack_from('<IHHHHHI', data, 0)
    assert magic == 0xA5E0, 'not an aseprite file'
    trans_idx = data[28]
    layers, frames = [], []
    palette = [(0, 0, 0, 0)] * 256
    off = 128
    for _ in range(nframes):
        fbytes, fmagic, old_chunks, dur = struct.unpack_from('<IHHH', data, off)
        new_chunks = struct.unpack_from('<I', data, off + 12)[0]
        nchunks = new_chunks if new_chunks else old_chunks
        p = off + 16
        cels = []
        for _ in range(nchunks):
            csize, ctype = struct.unpack_from('<IH', data, p)
            c = p + 6
            if ctype == 0x2004:
                lflags, ltype, child, _, _, blend, opacity = struct.unpack_from('<HHHHHHB', data, c)
                nlen = struct.unpack_from('<H', data, c + 16)[0]
                name = data[c + 18:c + 18 + nlen].decode('utf8', 'replace')
                layers.append(dict(flags=lflags, type=ltype, child=child, blend=blend,
                                   opacity=opacity if flags & 1 else 255, name=name))
            elif ctype == 0x2005:
                li, x, y, op, ctyp = struct.unpack_from('<HhhBH', data, c)
                d = c + 16
                if ctyp in (0, 2):
                    w, h = struct.unpack_from('<HH', data, d)
                    raw = data[d + 4:p + csize]
                    if ctyp == 2: raw = zlib.decompress(raw)
                    cels.append(dict(layer=li, x=x, y=y, op=op, w=w, h=h, px=raw))
                elif ctyp == 1:
                    link = struct.unpack_from('<H', data, d)[0]
                    cels.append(dict(layer=li, x=x, y=y, op=op, link=link))
            elif ctype == 0x2019:
                n, first, last = struct.unpack_from('<III', data, c)
                q = c + 20
                for i in range(first, last + 1):
                    eflags, r, g, b, a = struct.unpack_from('<HBBBB', data, q)
                    q += 6
                    if eflags & 1:
                        q += 2 + struct.unpack_from('<H', data, q)[0]
                    palette[i] = (r, g, b, a)
            elif ctype in (0x0004, 0x0011) and not any(palette):
                pass
            p += csize
        frames.append(dict(dur=dur, cels=cels))
        off += fbytes
    # resolve linked cels
    for f in frames:
        for i, cel in enumerate(f['cels']):
            if 'link' in cel:
                src = next((c for c in frames[cel['link']]['cels'] if c['layer'] == cel['layer'] and 'px' in c), None)
                if src: f['cels'][i] = dict(src, op=cel['op'])
    return W, H, depth, trans_idx, palette, layers, frames

def visible_layers(layers):
    """A layer is visible only if it and every parent group are visible."""
    vis, stack = [], []
    for L in layers:
        while len(stack) > L['child']: stack.pop()
        parent_vis = all(stack) if stack else True
        me = bool(L['flags'] & 1) and parent_vis
        vis.append(me and L['type'] != 1 and not (L['flags'] & 64))  # 64 = reference layer
        stack.append(bool(L['flags'] & 1))
    return vis

SKIP_FILL = False

def is_fill(cel, W, H, depth):
    """True for a background layer: one opaque colour covering most of the canvas."""
    if cel['w'] * cel['h'] < 0.9 * W * H: return False
    bpp = depth // 8
    px = cel['px']
    first = px[:bpp]
    return all(px[i:i + bpp] == first for i in range(0, len(px), bpp * 7))

def clear_border_fill(img, W, H):
    """Make the background transparent: flood-fill from the edges over the corner colour."""
    img = bytearray(img)
    c = img[0:4]
    if c[3] == 0: return bytes(img)
    seen = bytearray(W * H)
    stack = [(x, y) for x in range(W) for y in (0, H - 1)] + [(x, y) for y in range(H) for x in (0, W - 1)]
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= W or y >= H or seen[y * W + x]: continue
        seen[y * W + x] = 1
        o = (y * W + x) * 4
        if img[o:o + 4] != c: continue
        img[o:o + 4] = b'\0\0\0\0'
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return bytes(img)

def flatten(W, H, depth, trans_idx, palette, layers, frame):
    out = bytearray(W * H * 4)
    vis = visible_layers(layers)
    bpp = depth // 8
    for cel in sorted(frame['cels'], key=lambda c: c['layer']):
        if 'px' not in cel or cel['layer'] >= len(layers) or not vis[cel['layer']]: continue
        lop = layers[cel['layer']]['opacity'] * cel['op'] / (255 * 255)
        px, w, h = cel['px'], cel['w'], cel['h']
        for yy in range(h):
            ty = cel['y'] + yy
            if ty < 0 or ty >= H: continue
            for xx in range(w):
                tx = cel['x'] + xx
                if tx < 0 or tx >= W: continue
                i = (yy * w + xx) * bpp
                if depth == 32: r, g, b, a = px[i:i + 4]
                elif depth == 16: r = g = b = px[i]; a = px[i + 1]
                else:
                    idx = px[i]
                    if idx == trans_idx: continue
                    r, g, b, a = palette[idx]
                a = a * lop
                if a <= 0: continue
                o = (ty * W + tx) * 4
                da = out[o + 3] / 255
                sa = a / 255
                oa = sa + da * (1 - sa)
                if oa <= 0: continue
                for k, s in enumerate((r, g, b)):
                    out[o + k] = int(round((s * sa + out[o + k] * da * (1 - sa)) / oa))
                out[o + 3] = int(round(oa * 255))
    return bytes(out)

def write_png(path, W, H, rgba):
    rows = b''.join(b'\x00' + rgba[y * W * 4:(y + 1) * W * 4] for y in range(H))
    def chunk(t, d):
        c = struct.pack('>I', len(d)) + t + d
        return c + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 6, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(rows, 9)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)

if __name__ == '__main__':
    src, prefix = sys.argv[1], sys.argv[2]
    SKIP_FILL = '--no-bg' in sys.argv
    W, H, depth, ti, pal, layers, frames = read_ase(src)
    for n, f in enumerate(frames):
        img = flatten(W, H, depth, ti, pal, layers, f)
        if SKIP_FILL: img = clear_border_fill(img, W, H)
        write_png(prefix + ('.png' if len(frames) == 1 else f'_{n}.png'), W, H, img)
    print(W, H, len(frames), ','.join(str(f['dur']) for f in frames), '|', ';'.join(L['name'] for L in layers))
