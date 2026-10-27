"""Pack newly drawn alternating-leg run poses; no skeletal reconstruction."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[2]
src=Image.open(root/'Tools/Art/References/Hero-Run-v6/source.png').convert('RGBA')
out=root/'Assets/Art/Characters/Player/Source/Hero-v6'
frames=[]
for i in range(8):
    c,r=i%4,i//4
    cell=src.crop((round(c*src.width/4),round(r*src.height/2),round((c+1)*src.width/4),round((r+1)*src.height/2)))
    # Keep generated alpha; use the same scale for every pose.
    bbox=cell.getbbox()
    assert bbox and bbox[0]>0 and bbox[1]>0 and bbox[2]<cell.width and bbox[3]<cell.height
    sprite=cell.crop(bbox)
    sprite=sprite.resize((round(sprite.width*.32),round(sprite.height*.32)),Image.Resampling.NEAREST)
    frame=Image.new('RGBA',(256,256))
    frame.alpha_composite(sprite,((256-sprite.width)//2,224-sprite.height))
    a=np.array(frame)[:,:,3]
    assert not a[:16].any() and not a[-16:].any() and not a[:,:16].any() and not a[:,-16:].any()
    frames.append(frame)
assert len(set(f.tobytes() for f in frames))==8
sheet=Image.new('RGBA',(2048,256))
preview=[]
for i,f in enumerate(frames):
    sheet.alpha_composite(f,(i*256,0))
    bg=Image.new('RGBA',(256,256),(37,42,52,255));bg.alpha_composite(f);preview.append(bg.convert('RGB'))
sheet.save(out/'Hero_Run_8x256.png')
preview[0].save(root/'Tools/Art/Previews/Hero-Run-v6.gif',save_all=True,append_images=preview[1:],duration=100,loop=0)
contact=Image.new('RGB',(1024,512),(37,42,52))
for i,f in enumerate(preview):contact.paste(f,((i%4)*256,(i//4)*256))
contact.save(root/'Tools/Art/Previews/Hero-Run-v6-contact.png')
(out/'layout.json').write_text(json.dumps({'size':[2048,256],'frame_size':[256,256],'frames':8,'order':'left to right','transparent_margin_minimum':16,'bounds':[f.getbbox() for f in frames]},indent=2))
print('PASS: RGBA 2048x256, 8 distinct 256x256 frames, source cells isolated, >=16px transparent borders.')
