"""User-authorized deterministic packing of generated artwork; no Unity asset binding."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/Art/Characters/Player/Source/Hero-Spritesheet-v3-DRAFT.png'
OUT = ROOT / 'Assets/Art/Characters/Player/Source/Hero-v4'
OUT.mkdir(exist_ok=True)
im = Image.open(SOURCE).convert('RGBA')
arr = np.array(im)
# Discard near-transparent generation noise, preserve visible edge alpha.
arr[arr[:,:,3] < 32] = 0
im = Image.fromarray(arr)
ys = [0,175,315,495,637,800,987,1135,1254]
xs = [0,160,320,480,650,790,950,1110,1254]
names = ['Idle','Run','JumpFall','Attack1','Attack2','Attack3','Parry','DodgeRoll']
frames = []
report = []

# Hand-separate the protruding sword from the adjacent cyan effect.
right = Image.new('L', im.size)
d = ImageDraw.Draw(right)
d.polygon([(652,535),(705,533),(714,513),(770,510),(787,628),
           (654,632),(649,612),(663,589),(678,574),(700,564),
           (680,558),(650,553)], fill=255)

for row in range(8):
    row_frames = []
    for col in range(8):
        bounds = (xs[col],ys[row],xs[col+1],ys[row+1])
        if row == 3 and col in (3,4):
            region = np.array(im)
            mask = np.array(right)>0
            if col == 3:
                region[mask] = 0
                bounds = (480,495,674,637)
            else:
                region[~mask] = 0
                bounds = (645,495,790,637)
            sprite = Image.fromarray(region).crop(bounds)
        else:
            sprite = im.crop(bounds)
        bbox = sprite.getbbox()
        sprite = sprite.crop(bbox)
        # Retain original pixel size across motions, no per-frame scaling.
        assert sprite.width <= 224 and sprite.height <= 224
        canvas = Image.new('RGBA',(256,256))
        x = (256-sprite.width)//2
        y = 224-sprite.height
        canvas.alpha_composite(sprite,(x,y))
        row_frames.append(canvas)
    frames.append(row_frames)

# Pixel-level breathing: upper body moves; feet/lower legs remain identical.
# Nearest-row remapping only in the knee/hip connection, never global scaling.
base = np.array(frames[0][0])
for index, rise in enumerate([0,2,4,6,4,2,0,-2]):
    dst = np.zeros_like(base)
    for y in range(256):
        weight = max(0.0,min(1.0,(214-y)/24))
        source_y = int(round(y + rise*weight))
        if 0 <= source_y < 256:
            dst[y] = base[source_y]
    frames[0][index] = Image.fromarray(dst)

sheet = Image.new('RGBA',(2048,2048))
for row,name in enumerate(names):
    strip = Image.new('RGBA',(2048,256))
    for col,frame in enumerate(frames[row]):
        alpha = np.array(frame)[:,:,3]
        assert not alpha[:16].any() and not alpha[-16:].any()
        assert not alpha[:,:16].any() and not alpha[:,-16:].any()
        assert alpha.any()
        strip.alpha_composite(frame,(col*256,0))
        sheet.alpha_composite(frame,(col*256,row*256))
        report.append({'animation':name,'frame':col+1,'rect':[col*256,row*256,256,256],
                       'visible_bounds':frame.getbbox()})
    strip.save(OUT/f'Hero_{name}_8x256.png')
sheet.save(OUT/'Hero_Spritesheet_256x256_8x8.png')
assert len({f.tobytes() for f in frames[0]}) >= 5
assert all(np.array(f)[214:].tobytes()==base[214:].tobytes() for f in frames[0])
(OUT/'layout.json').write_text(json.dumps({'size':[2048,2048], 'cell':[256,256],
    'columns':8,'rows':names,'minimum_transparent_border':16,'frames':report},indent=2))
# Review artifacts are kept outside Assets.
preview = Image.new('RGB',(2048,2048),(40,44,54))
preview.paste(sheet,mask=sheet.getchannel('A'))
draw = ImageDraw.Draw(preview)
for p in range(0,2049,256):
    draw.line((p,0,p,2048),fill=(85,95,110))
    draw.line((0,p,2048,p),fill=(85,95,110))
preview.resize((1024,1024)).save(ROOT/'Temp/hero-v4-grid-preview.png')
frames[0][0].save(ROOT/'Temp/hero-v4-idle-preview.gif',save_all=True,
    append_images=frames[0][1:],duration=140,loop=0,disposal=2)
print('PASS: 2048x2048 RGBA; 64 cells; eight frames per animation; >=16px transparent borders; Idle feet fixed.')
