from pathlib import Path
import json, math
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Art/Characters/Player/Source/Hero-v5'
TMP=ROOT/'Temp/hero-v5'
OUT.mkdir(exist_ok=True)
src=Image.open(TMP/'run-source.png').convert('RGBA').crop((0,0,440,510))
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
poses=[(((146,200),(157,216)),((111,194),(103,212))),
       (((138,207),(131,218)),((115,198),(108,183))),
       (((117,202),(109,217)),((145,194),(133,205))),
       (((112,194),(102,180)),((150,195),(158,209)))]
run=[]
for i in range(8):
    near,far=poses[i%4]
    if i>=4: near,far=far,near
    bob=[0,2,0,-2][i%4]; hip=(132,181+bob)
    frame=Image.new('RGBA',(256,256))
    limb(frame,hip,*far,True)
    limb(frame,hip,*near,False)
    frame.alpha_composite(place_segment(torso,(251,378),(251,278),hip,(hip[0],hip[1]-38)))
    run.append(frame)

death_src=Image.open(TMP/'death-source.png').convert('RGBA')
# Actual generated rows share ground baselines at approximately 480 and 835.
death=[]
for i in range(8):
    col=i%4; row=i//4
    crop=death_src.crop((round(col*death_src.width/4),0 if row==0 else 530,
                          round((col+1)*death_src.width/4),530 if row==0 else death_src.height))
    a=np.array(crop); a[a[:,:,3]<32]=0; crop=Image.fromarray(a)
    crop=crop.crop(crop.getbbox())
    crop=crop.resize((round(crop.width*.30),round(crop.height*.30)),Image.Resampling.NEAREST)
    frame=Image.new('RGBA',(256,256)); frame.alpha_composite(crop,((256-crop.width)//2,224-crop.height))
    death.append(frame)

report={}
for name,frames in [('Run',run),('Death',death)]:
    strip=Image.new('RGBA',(2048,256))
    for i,f in enumerate(frames):
        a=np.array(f)[:,:,3]
        assert a.any() and not a[:16].any() and not a[-16:].any() and not a[:,:16].any() and not a[:,-16:].any()
        strip.alpha_composite(f,(i*256,0))
    assert len(set(f.tobytes() for f in frames))==8
    strip.save(OUT/f'Hero_{name}_8x256.png')
    report[name]={'size':[2048,256],'frames':8,'cell':[256,256],'bounds':[f.getbbox() for f in frames]}
    preview=[]
    for f in frames:
        bg=Image.new('RGBA',(256,256),(37,42,52,255));bg.alpha_composite(f);preview.append(bg.convert('RGB'))
    preview[0].save(TMP/f'{name}.gif',save_all=True,append_images=preview[1:],duration=100 if name=='Run' else 160,loop=0)
    contact=Image.new('RGB',(1024,512),(37,42,52))
    for i,f in enumerate(preview): contact.paste(f,((i%4)*256,(i//4)*256))
    contact.save(TMP/f'{name}-review.png')
(OUT/'validation.json').write_text(json.dumps(report,indent=2))
print('PASS: two RGBA 2048x256 strips, 8 unique 256x256 frames each, 16px transparent boundaries.')
