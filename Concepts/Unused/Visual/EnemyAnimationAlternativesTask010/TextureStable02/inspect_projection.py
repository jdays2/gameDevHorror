"""Read-only Blender inspection of final saved volume / UV / projected folds."""
from pathlib import Path
import json,hashlib
import bpy,numpy as np
ROOT=Path(__file__).resolve().parent
VALID=ROOT.parents[4]/'Docs/Validation/EnemyAnimationAlternativesTask010/TextureStable02'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'fixed-material-volume.blend'))
obj=bpy.data.objects['Stalker_FixedMaterialVolume_TEST_NOT_PRODUCTION'];mesh=obj.data
image=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
pixels=np.empty(640*640*4,dtype=np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(640,640,4)[::-1]
rest=np.load(ROOT/'rest-surface.npz');n=len(rest['vertices'])//2
top=rest['faces'][np.all(rest['faces']<n,axis=1)]
tris=np.concatenate([top[:,[0,1,2]],top[:,[0,2,3]]]);uv=rest['uv'];events=[]
for i in range(24):
    bpy.context.scene.frame_set(i+1)
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());em=ev.to_mesh()
    verts=np.empty(len(em.vertices)*3,dtype=np.float32);em.vertices.foreach_get('co',verts);verts=verts.reshape(-1,3)
    a=verts[tris[:,1],:2]-verts[tris[:,0],:2];b=verts[tris[:,2],:2]-verts[tris[:,0],:2]
    area=a[:,0]*b[:,1]-a[:,1]*b[:,0]
    bad=tris[area<=0]
    samples=[]
    for t in bad:
        coords=uv[t];center=coords.mean(axis=0)
        samplesUV=np.vstack([coords,center]);samplesXY=np.c_[samplesUV[:,0]*640,(1-samplesUV[:,1])*640]
        coords2=np.clip(np.rint(samplesXY).astype(int),0,639)
        samples.append({'restPixelCenter':[float(center[0]*640),float((1-center[1])*640)],
            'maxSourceAlphaAtVerticesAndCenter':float(pixels[coords2[:,1],coords2[:,0],3].max()),
            'posedXYCenter':(verts[t,:2].mean(axis=0)*[224,-224]+[320,320]).tolist()})
    events.append({'frame':i,'nonpositiveTrianglesBothHalves':len(bad),'minTriangleArea':float(area.min()),
        'minVertexHeight':float(verts[:,2].min()),'faces':samples})
    ev.to_mesh_clear()
packedSHA=hashlib.sha256(bytes(image.packed_file.data)).hexdigest()
report={'masterFileSHA':hashlib.sha256((ROOT/'master.png').read_bytes()).hexdigest(),'packedImageSHA':packedSHA,
    'packedTextureExactFile':packedSHA==hashlib.sha256((ROOT/'master.png').read_bytes()).hexdigest(),
    'textureImageNodes':sum(n.type=='TEX_IMAGE' for n in obj.data.materials[0].node_tree.nodes),
    'UVLayerCount':len(mesh.uv_layers),'frameEvents':events,
    'note':'Closed3D volume can project backfaces near silhouette. Samples diagnostic only: source low alpha does not by itself prove no neighbouring visible defect. Actual all-frame RGBA reviewed separately.'}
(VALID/'projection-focus.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
(VALID/'render-geometry-check.json').write_text(json.dumps({'blender':bpy.app.version_string,
    'inspection':'Final saved V2 blend; both triangles of every rest-top quad, all24 keyed poses',
    'frames':[{'frame':f['frame'],'topTrianglesNonpositiveProjectedArea':f['nonpositiveTrianglesBothHalves'],
        'minTopTriangleArea':f['minTriangleArea'],'minVertexHeight':f['minVertexHeight']} for f in events],
    'note':'Projection orientation is diagnostic for closed3D volume, not automatically a visible hole. Surface contact remains unvalidated.'},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'textureExact':report['packedTextureExactFile'],'maxBothHalves':max(f['nonpositiveTrianglesBothHalves'] for f in events),
    'maxSourceAlphaOnNonpositiveSamples':max(p['maxSourceAlphaAtVerticesAndCenter'] for f in events for p in f['faces'])},indent=2))
