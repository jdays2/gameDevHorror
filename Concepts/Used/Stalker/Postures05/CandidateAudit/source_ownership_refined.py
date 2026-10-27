"""Study02 source ownership: preserve the original underbelly alpha edge.

V1's centre-bottom polygon crossed real opaque skin at y239..243. This trace
goes beyond the measured native alpha edge (centre ends at y244..248). No
alpha feather or fill is added: original body RGBA is assigned exactly once.
"""
from PIL import Image, ImageDraw
import numpy as np
from native_uv_painter import _decode, ORIGIN
from source_ownership_study import PROJECT

BODY_OWNER = [(185,123),(193,125),(198,132),(200,143),(205,151),
    (214,167),(223,183),(232,201),(242,217),(249,224),(255,231),
    (257,238),(257,247),(249,252),(235,254),(221,255),(213,255),
    (207,252),(200,250),(190,247),(180,247),(170,250),(163,253),
    (151,254),(138,253),(126,251),(118,248),(113,242),(112,235),
    (116,227),(125,219),(136,200),(146,181),(156,164),(166,148),
    (174,137),(177,129)]


def owned_materials(painter):
    path=PROJECT/'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png'
    with Image.open(path) as image:
        native=_decode(image.convert('RGBA').crop((30,84,388,490)))
    mask=Image.new('L',(358,406))
    ImageDraw.Draw(mask).polygon(BODY_OWNER,fill=255)
    owner=np.asarray(mask)>0
    sources=tuple(source*(~owner)[...,None] for source in painter.sources)
    for source in sources:
        source.flags.writeable=False
    torso=np.zeros((576,576,4),dtype='float32')
    x,y=(ORIGIN-np.asarray(painter.anatomy['pivot'])).astype(int)
    torso[y:y+406,x:x+358]=native*owner[...,None]
    return sources,torso,owner,native
