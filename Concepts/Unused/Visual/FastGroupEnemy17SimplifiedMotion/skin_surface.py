"""Local elastic surface fitted to source alpha, with rigid foot constraints.

The graph follows painted skin connectivity, including the spaces between toes.
Unrelated close surfaces cannot exchange weights across a transparent gap.
"""
import math
from collections import deque
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


class SkinSurface:
    def __init__(self, rig, step=5):
        self.rig = rig
        self.step = step
        self.feet = {}
        self.foot_masks = {}
        self.cut_lines = {}
        self.body_image = self.split_feet()
        self.art = np.asarray(self.body_image).astype(np.float32)/255.
        self.art[..., :3] *= self.art[..., 3:4]
        values = np.arange(0, 1255, step, dtype=float)
        yy, xx = np.meshgrid(values, values, indexing='ij')
        self.shape = xx.shape
        grid = np.stack([xx, yy], axis=-1)
        cells = []
        alpha = self.art[..., 3]
        for y in range(len(values)-1):
            for x in range(len(values)-1):
                px, py = int(values[x]), int(values[y])
                if np.any(alpha[py:min(1254, py+step+1), px:min(1254, px+step+1)]>.005):
                    a = y*len(values)+x
                    cells.append([a, a+1, a+len(values)+1, a+len(values)])
        self.cells = np.asarray(cells, dtype=int)
        used = np.unique(self.cells)
        lookup = np.full(grid.shape[0]*grid.shape[1], -1, dtype=int)
        lookup[used] = np.arange(len(used))
        self.cells = lookup[self.cells]
        self.source = grid.reshape(-1, 2)[used]
        triangles = np.concatenate([self.cells[:, [0, 1, 2]], self.cells[:, [0, 2, 3]]])
        edges = np.concatenate([triangles[:, [0, 1]], triangles[:, [1, 2]], triangles[:, [0, 2]]])
        edges.sort(axis=1)
        self.edges = np.unique(edges, axis=0)
        self.i, self.j = self.edges.T
        self.e = self.source[self.i]-self.source[self.j]
        self.weights = step*step/np.sum(self.e*self.e, axis=1)
        # Topology follows the visible skin. Coarse boundary cells may meet at
        # a vertex across an empty gap; those edges must not carry tissue force.
        samples = []
        for fraction in [.25, .5, .75]:
            p = self.source[self.i]*fraction+self.source[self.j]*(1.-fraction)
            px = np.clip(np.rint(p[:, 0]).astype(int), 0, 1253)
            py = np.clip(np.rint(p[:, 1]).astype(int), 0, 1253)
            samples.append(alpha[py, px])
        connectivity = np.maximum(np.min(samples, axis=0), .0001)
        self.weights *= connectivity
        self.degree = self.scatter(self.weights, self.weights)
        self.owners, self.parts = self.classify()
        px = np.clip(np.rint(self.source[:, 0]).astype(int), 0, 1253)
        py = np.clip(np.rint(self.source[:, 1]).astype(int), 0, 1253)
        # Boundary vertices in transparent air are not trunk attachment pins.
        # They follow the neighbouring surface; pinning them creates a jagged
        # tear where the moving skin meets an otherwise empty mesh cell.
        rim = (self.art[py, px, 3]<.9) & (self.parts == 'body')
        neck = (self.source[:, 0]>365.) & (self.source[:, 0]<655.) & (self.source[:, 1]>545.) & (self.source[:, 1]<785.)
        self.parts[rim & ~neck] = 'free'
        self.free = np.flatnonzero(self.parts == 'free')
        self.fixed = np.flatnonzero(self.parts != 'free')
        free_lookup = np.full(len(self.source), -1, dtype=int)
        free_lookup[self.free] = np.arange(len(self.free))
        self.fi, self.fj = free_lookup[self.i], free_lookup[self.j]
        both = (self.fi>=0) & (self.fj>=0)
        self.ff_i, self.ff_j, self.ff_w = self.fi[both], self.fj[both], self.weights[both]
        self.diagonal = self.degree[self.free]
        one_i = (self.fi>=0) & (self.fj<0)
        one_j = (self.fj>=0) & (self.fi<0)
        self.pin_free = np.r_[self.fi[one_i], self.fj[one_j]]
        self.pin_fixed = np.r_[self.j[one_i], self.i[one_j]]
        self.pin_weight = np.r_[self.weights[one_i], self.weights[one_j]]
        self.triangles = triangles
        centroids = np.rint(self.source[triangles].mean(axis=1)).astype(int)
        centroids = np.clip(centroids, 0, 1253)
        self.visible_triangles = triangles
        self.is_free = np.zeros(len(self.source), dtype=bool)
        self.is_free[self.free] = True

    def split_feet(self):
        art = self.rig.ART
        alpha = np.asarray(art.getchannel('A')).astype(float)/255.
        yy, xx = np.mgrid[:1254, :1254]
        body_alpha = alpha.copy()
        for ident in ['FU', 'FL', 'RU', 'RL']:
            upper = ident[1] == 'U'
            if ident[0] == 'F':
                foot = (xx<374) & ((yy<600) if upper else (yy>730))
                remove = (xx<359) & ((yy<600) if upper else (yy>730))
            else:
                polygon = [(880, 400), (1254, 400), (1254, 585), (1155, 548), (1105, 527), (1070, 537), (1005, 560), (905, 547), (880, 545)]
                line = np.array([[1105., 527.], [1155., 548.]])
                if not upper:
                    # The selected painting is asymmetric in pose. Mirroring
                    # its upper-foot matte captures a narrow strip of lower
                    # shank; this is a source-layer cut, not a new bone pose.
                    polygon = [(880, 930), (1254, 930), (1254, 750), (1210, 750), (1145, 800), (1100, 821), (1070, 815), (1005, 780), (910, 792), (880, 805)]
                    line = np.array([[1100., 821.], [1145., 800.]])
                self.cut_lines[ident] = line
                mask = Image.new('L', (1254, 1254))
                ImageDraw.Draw(mask).polygon(polygon, fill=255)
                candidate = (np.asarray(mask)>0) & (alpha>.003)
                seed = (1080, 488 if upper else 842)
                component = np.zeros_like(candidate)
                queue = deque([seed])
                component[seed[1], seed[0]] = True
                while queue:
                    x, y = queue.popleft()
                    for nx, ny in [(x-1, y), (x+1, y), (x, y-1), (x, y+1)]:
                        if 0<=nx<1254 and 0<=ny<1254 and candidate[ny, nx] and not component[ny, nx]:
                            component[ny, nx] = True
                            queue.append((nx, ny))
                foot = np.asarray(Image.fromarray(component.astype('uint8')*255).filter(ImageFilter.MaxFilter(3)))>0
                foot &= np.asarray(mask)>0
                # The painting's flank reaches farther left than the toes. Its
                # thin contour must stay with the trunk, not with the foot.
                foot &= xx>=910
                a, b = line
                vector = b-a
                q = np.clip(((xx-a[0])*vector[0]+(yy-a[1])*vector[1])/np.dot(vector, vector), 0., 1.)
                distance = np.sqrt((xx-a[0]-q*vector[0])**2+(yy-a[1]-q*vector[1])**2)
                remove = foot & (distance>12.)
            piece = np.asarray(art).copy()
            piece[..., 3] = np.uint8(alpha*foot*255.)
            piece[piece[..., 3] == 0, :3] = 0
            self.feet[ident] = Image.fromarray(piece)
            self.foot_masks[ident] = foot
            self.feet[ident].save(self.rig.ROOT/f'layer-foot-{ident}.png')
            body_alpha[remove] = 0.
        body = np.asarray(art).copy()
        body[..., 3] = np.uint8(body_alpha*255.)
        body[body[..., 3] == 0, :3] = 0
        result = Image.fromarray(body)
        result.save(self.rig.ROOT/'layer-continuous-skin.png')
        return result

    def scatter(self, at_i, at_j):
        size = len(self.source)
        if at_i.ndim == 1:
            return np.bincount(self.i, at_i, minlength=size)+np.bincount(self.j, at_j, minlength=size)
        return np.stack([np.bincount(self.i, at_i[:, k], minlength=size)+np.bincount(self.j, at_j[:, k], minlength=size) for k in range(at_i.shape[1])], axis=-1)

    def classify(self):
        x, y = self.source.T
        owners = np.full(len(x), 'B', dtype='U2')
        parts = np.full(len(x), 'body', dtype='U7')
        for side, outward in [('U', 665.-y), ('L', y-665.)]:
            front = (x<750.) & ((outward>140.) | ((x<365.) & (outward>100.)))
            owners[front] = 'F'+side
            parts[front] = 'free'
            hand = front & (x<385.)
            forearm = front & (x>=410.) & (x<530.)
            upper = front & (x>635.) & (outward>215.)
            parts[hand] = 'hand'
            parts[forearm] = 'forearm'
            parts[upper] = 'upper'
            # Include every rear digit, especially the low lateral claw. Skin
            # connectivity, rather than a y-only mask, owns the ankle bend.
            foot_polygon = [(905, 415), (1135, 415), (1250, 560), (1210, 575), (1148, 550), (1110, 528), (1085, 528), (1050, 548), (1005, 552), (905, 540)]
            ankle_polygon = [(1010, 550), (1135, 515), (1170, 550), (1155, 625), (1090, 625), (1020, 595)]
            if side == 'L':
                foot_polygon = [(a, 1330-b) for a, b in foot_polygon]
                ankle_polygon = [(a, 1330-b) for a, b in ankle_polygon]
            def inside(polygon, dilate=0):
                mask = Image.new('L', (1254, 1254))
                ImageDraw.Draw(mask).polygon(polygon, fill=255)
                if dilate:
                    mask = mask.filter(ImageFilter.MaxFilter(dilate*2+1))
                return np.asarray(mask)[np.clip(y.astype(int), 0, 1253), np.clip(x.astype(int), 0, 1253)]>0
            mask = self.foot_masks['R'+side]
            px = np.clip(x.astype(int), 0, 1253)
            py = np.clip(y.astype(int), 0, 1253)
            rear_hand = mask[py, px]
            surround = np.asarray(Image.fromarray(mask.astype('uint8')*255).filter(ImageFilter.MaxFilter(41)))>0
            rear = inside(ankle_polygon) | surround[py, px]
            owners[rear] = 'R'+side
            parts[rear] = 'free'
            parts[rear_hand] = 'hand'
            flank = rear & ~rear_hand & (x<1020.)
            parts[flank] = 'body'
            hip = self.rig.project(self.rig.REST['R'+side][0])
            root = np.sum((self.source-hip)**2, axis=1)<12.**2
            parts[root] = 'body'
            line = self.cut_lines['R'+side]
            a, b = line
            vector = b-a
            q = np.clip(np.sum((self.source-a)*vector, axis=1)/np.dot(vector, vector), 0., 1.)
            distance = np.linalg.norm(self.source-a-q[:, None]*vector, axis=1)
            join = distance<17.
            owners[join] = 'R'+side
            parts[join] = 'hand'
        return owners, parts

    def constraints(self, pose):
        r = self.rig
        motion = np.asarray(pose['rootOffset3D'][:2])*375.
        target = self.source+motion
        for ident, rest3 in r.REST.items():
            rest = r.project(rest3)
            posed = np.asarray(pose['limbs'][ident]['projected'])
            owned = self.owners == ident
            hand = owned & (self.parts == 'hand')
            target[hand] = self.source[hand]+posed[2]-rest[2]
            if ident[0] == 'F':
                for j, part in [(0, 'upper'), (1, 'forearm')]:
                    selected = owned & (self.parts == part)
                    target[selected] = r.segment_map(self.source[selected], *rest[j:j+2], *posed[j:j+2])
        return target

    def matvec(self, values):
        output = self.diagonal[:, None]*values
        for axis in range(2):
            output[:, axis] -= np.bincount(self.ff_i, self.ff_w*values[self.ff_j, axis], minlength=len(values))
            output[:, axis] -= np.bincount(self.ff_j, self.ff_w*values[self.ff_i, axis], minlength=len(values))
        return output

    def cg(self, rhs, initial):
        x = initial.copy()
        residual = rhs-self.matvec(x)
        z = residual/self.diagonal[:, None]
        direction = z.copy()
        rz = np.sum(residual*z, axis=0)
        for _ in range(160):
            product = self.matvec(direction)
            amount = rz/np.maximum(np.sum(direction*product, axis=0), 1e-25)
            x += amount*direction
            residual -= amount*product
            if np.max(np.linalg.norm(residual, axis=1))<2e-5:
                break
            z = residual/self.diagonal[:, None]
            next_rz = np.sum(residual*z, axis=0)
            direction = z+next_rz/np.maximum(rz, 1e-25)*direction
            rz = next_rz
        return x

    def deform(self, pose):
        target = self.constraints(pose)
        q = self.source.copy()
        # Small constraint increments avoid trapping a joint in a folded state
        # after a large one-step displacement from the neutral painting.
        for amount in np.linspace(1./6., 1., 6):
            partial = self.source+(target-self.source)*amount
            q[self.fixed] = partial[self.fixed]
            pins = np.stack([np.bincount(self.pin_free, self.pin_weight*partial[self.pin_fixed, k], minlength=len(self.free)) for k in range(2)], axis=-1)
            for iteration in range(18):
                delta = q[self.i]-q[self.j]
                cosine = np.sum(delta*self.e, axis=1)*self.weights
                sine = (delta[:, 1]*self.e[:, 0]-delta[:, 0]*self.e[:, 1])*self.weights
                c = self.scatter(cosine, cosine)
                s = self.scatter(sine, sine)
                norm = np.maximum(np.sqrt(c*c+s*s), 1e-12)
                c /= norm
                s /= norm
                ec = .5*(c[self.i]+c[self.j])
                es = .5*(s[self.i]+s[self.j])
                rotated = np.stack([ec*self.e[:, 0]-es*self.e[:, 1], es*self.e[:, 0]+ec*self.e[:, 1]], axis=-1)*self.weights[:, None]
                rhs = self.scatter(rotated, -rotated)[self.free]+pins
                updated = self.cg(rhs, q[self.free])
                change = np.max(np.linalg.norm(updated-q[self.free], axis=1))
                q[self.free] = updated
                if change<.001:
                    break
        return self.prevent_folds(q)

    def prevent_folds(self, q):
        # A source-surface triangle may compress but may not turn inside out.
        # Project only movable joint vertices onto this local area constraint;
        # body, feet, longitudinal segment pins and the neck remain untouched.
        triangles = self.visible_triangles
        for iteration in range(180):
            t = q[triangles]
            u, v = t[:, 1]-t[:, 0], t[:, 2]-t[:, 0]
            area = u[:, 0]*v[:, 1]-u[:, 1]*v[:, 0]
            bad = np.flatnonzero(area<self.step*self.step*.12)
            if len(bad) == 0:
                break
            for k in bad:
                indices = triangles[k]
                a, b, c = q[indices]
                actual = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
                gradient = np.array([[b[1]-c[1], c[0]-b[0]], [c[1]-a[1], a[0]-c[0]], [a[1]-b[1], b[0]-a[0]]])
                gradient *= self.is_free[indices, None]
                denominator = np.sum(gradient*gradient)
                if denominator>1e-10:
                    move = gradient*(self.step*self.step*.125-actual)/denominator
                    q[indices] += np.clip(move, -3., 3.)
        return q

    def raster(self, q):
        r = self.rig
        dst = r.screen(q)
        uv = np.full((r.W, r.W, 2), -1., dtype=np.float32)
        coverage = np.zeros((r.W, r.W), dtype=np.float32)
        minimum = 10.
        flipped = 0
        flipped_skin = 0
        minimum_skin = 10.
        for indices in self.triangles:
            source = self.source[indices]
            triangle = dst[indices]
            a, b, c = triangle
            determinant = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
            ratio = determinant/(r.SCALE*r.SCALE*self.step*self.step)
            minimum = min(minimum, ratio)
            center = np.rint(source.mean(axis=0)).astype(int)
            center = np.clip(center, 0, 1253)
            solid = self.art[center[1], center[0], 3]>.5
            if solid:
                minimum_skin = min(minimum_skin, ratio)
            if ratio<=0:
                flipped += 1
                if solid:
                    flipped_skin += 1
            if abs(determinant)<1e-5:
                continue
            lo = np.maximum(0, np.floor(triangle.min(axis=0)).astype(int))
            hi = np.minimum(r.W-1, np.ceil(triangle.max(axis=0)).astype(int))
            if np.any(hi<lo):
                continue
            yy, xx = np.mgrid[lo[1]:hi[1]+1, lo[0]:hi[0]+1].astype(np.float32)
            u = ((xx-a[0])*(c[1]-a[1])-(yy-a[1])*(c[0]-a[0]))/determinant
            v = ((b[0]-a[0])*(yy-a[1])-(b[1]-a[1])*(xx-a[0]))/determinant
            inside = (u>=-.0001) & (v>=-.0001) & (u+v<=1.0001)
            coords = source[0]+u[..., None]*(source[1]-source[0])+v[..., None]*(source[2]-source[0])
            # Transparent triangles cannot erase a neighboring visible surface.
            ax = np.clip(np.floor(coords[..., 0]).astype(int), 0, 1252)
            ay = np.clip(np.floor(coords[..., 1]).astype(int), 0, 1252)
            fx = np.clip(coords[..., 0]-ax, 0., 1.)
            fy = np.clip(coords[..., 1]-ay, 0., 1.)
            alpha = (self.art[ay, ax, 3]*(1.-fx)+self.art[ay, ax+1, 3]*fx)*(1.-fy)+(self.art[ay+1, ax, 3]*(1.-fx)+self.art[ay+1, ax+1, 3]*fx)*fy
            covered = coverage[lo[1]:hi[1]+1, lo[0]:hi[0]+1]
            # A transparent edge fragment cannot replace a solid skin fragment
            # at a projected fold. Test exactly the same texels as the sampler.
            inside &= (alpha>.001) & (alpha>=covered)
            tile = uv[lo[1]:hi[1]+1, lo[0]:hi[0]+1]
            tile[inside] = coords[inside]
            covered[inside] = alpha[inside]
        x, y = uv[..., 0], uv[..., 1]
        ix = np.clip(np.floor(x).astype(int), 0, 1252)
        iy = np.clip(np.floor(y).astype(int), 0, 1252)
        fx = np.clip(x-ix, 0., 1.)[..., None]
        fy = np.clip(y-iy, 0., 1.)[..., None]
        color = (self.art[iy, ix]*(1.-fx)+self.art[iy, ix+1]*fx)*(1.-fy)+(self.art[iy+1, ix]*(1.-fx)+self.art[iy+1, ix+1]*fx)*fy
        color *= (x>=0.)[..., None]
        color[..., :3] /= np.maximum(color[..., 3:4], 1e-6)
        return Image.fromarray(np.uint8(np.clip(color, 0., 1.)*255.)), {'minTriangleAreaRatio': float(minimum), 'flippedTriangles': flipped, 'minSolidTriangleAreaRatio': float(minimum_skin), 'flippedSolidTriangles': flipped_skin, 'freeSkinVertices': len(self.free), 'rigidVertices': len(self.fixed)}

    def render(self, pose):
        image, stats = self.raster(self.deform(pose))
        r = self.rig
        for ident, piece in self.feet.items():
            rest = r.project(r.REST[ident][2])
            posed = np.asarray(pose['limbs'][ident]['projected'][2])
            displacement = r.OFFSET+(posed-rest)*r.SCALE
            affine = (1./r.SCALE, 0., -displacement[0]/r.SCALE, 0., 1./r.SCALE, -displacement[1]/r.SCALE)
            image.alpha_composite(piece.transform((r.W, r.W), Image.Transform.AFFINE, affine, Image.Resampling.BICUBIC))
        return image, stats
