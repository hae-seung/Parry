from pathlib import Path
import json, math
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Art/Characters/Player/Source/Hero-v7'
TMP=ROOT/'Tools/Art/References/Hero-Run-v7'
OUT.mkdir(exist_ok=True)
src=Image.open(TMP/'source.png').convert('RGBA').crop((0,0,440,510))
def piece(poly):
    mask=Image.new('L',src.size); ImageDraw.Draw(mask).polygon(poly,fill=255)
    a=np.array(src); a[:,:,3]=np.minimum(a[:,:,3],np.array(mask))
    return Image.fromarray(a)
torso=piece([(0,0),(440,0),(440,408),(337,408),(315,387),(306,375),(278,377),(251,381),(220,390),(198,404),(145,418),(100,417),(0,440)])
thigh=piece([(238,361),(271,363),(304,392),(324,414),(308,433),(283,423),(260,403),(233,387)])
shin=piece([(293,405),(317,410),(331,440),(346,461),(326,475),(305,457),(285,430)])
boot=piece([(315,451),(338,450),(348,459),(366,465),(376,480),(367,487),(310,484),(307,468)])
def place_segment(image,a,b,c,d,dark=False):
    # Similarity transform, retain painted armor texture; nearest pixel sampling.
    vx,vy=b[0]-a[0],b[1]-a[1]; wx,wy=d[0]-c[0],d[1]-c[1]
    den=vx*vx+vy*vy; u=(wx*vx+wy*vy)/den; v=(wy*vx-wx*vy)/den
    m=np.array([[u,-v],[v,u]]); inv=np.linalg.inv(m)
    t=np.array(c)-m@np.array(a); off=-inv@t
    out=image.transform((256,256),Image.Transform.AFFINE,(*inv[0],off[0],*inv[1],off[1]),Image.Resampling.NEAREST)
    if dark: out=ImageEnhance.Brightness(out).enhance(.65)
    return out
def limb(canvas,hip,knee,ankle,dark):
    canvas.alpha_composite(place_segment(thigh,(251,378),(302,420),hip,knee,dark))
    canvas.alpha_composite(place_segment(shin,(302,420),(330,463),knee,ankle,dark))
    canvas.alpha_composite(place_segment(boot,(330,463),(363,474),ankle,(ankle[0]+12,ankle[1]+4),dark))

# Explicit connected chains. Near/right hip is forward of the far/left hip.
# The near limb is painted last so its backward thigh crosses over the far limb.
poses=[
 (((155,200),(168,216)),((108,195),(98,181))),
 (((143,204),(135,218)),((132,194),(114,204))),
 (((122,202),(108,214)),((150,178),(140,201))),
 (((117,192),(106,179)),((153,194),(168,212))),
 (((116,195),(99,181)),((149,201),(163,216))),
 (((138,194),(119,204)),((133,204),(127,218))),
 (((159,178),(148,200)),((112,201),(98,214))),
 (((160,194),(175,212)),((107,192),(96,179))),
]
frames=[]
for i,(near,far) in enumerate(poses):
    bob=[0,2,0,-2,0,2,0,-2][i]
    frame=Image.new('RGBA',(256,256))
    limb(frame,(126,184+bob),*far,True)
    frame.alpha_composite(place_segment(torso,(251,378),(251,278),(132,182+bob),(132,144+bob)))
    limb(frame,(137,184+bob),*near,False)
    frames.append(frame)
sheet=Image.new('RGBA',(2048,256)); reviews=[]
for i,f in enumerate(frames):
    a=np.array(f)[:,:,3]
    assert not a[:16].any() and not a[-16:].any() and not a[:,:16].any() and not a[:,-16:].any()
    sheet.alpha_composite(f,(256*i,0))
    bg=Image.new('RGBA',(256,256),(37,42,52,255));bg.alpha_composite(f);reviews.append(bg.convert('RGB'))
assert len({f.tobytes() for f in frames})==8
sheet.save(OUT/'Hero_Run_8x256.png')
preview=ROOT/'Tools/Art/Previews';preview.mkdir(exist_ok=True)
reviews[0].save(preview/'Hero-Run-v7.gif',save_all=True,append_images=reviews[1:],duration=110,loop=0)
contact=Image.new('RGB',(1024,512),(37,42,52))
for i,f in enumerate(reviews):contact.paste(f,((i%4)*256,(i//4)*256))
contact.save(preview/'Hero-Run-v7-contact.png')
(OUT/'layout.json').write_text(json.dumps({'size':[2048,256],'frames':8,'cell':[256,256],'near_right_and_far_left_knee_ankle':poses,'minimum_clear_border':16},indent=2))
print('PASS: eight distinct poses; 256x256 cells; transparent borders; right knee behind hip on frames 3-5, left knee forward on frames 3-5.')

