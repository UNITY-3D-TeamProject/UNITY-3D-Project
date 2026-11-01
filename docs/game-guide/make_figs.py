import sys
from PIL import Image, ImageDraw, ImageFont

SRC = sys.argv[1]  # docs/game-guide/images
OUT = sys.argv[2]  # docs/game-guide/figs

BADGE_FILL = (255, 196, 0)
BADGE_TEXT = (10, 14, 30)
LINE = (255, 196, 0)
FONT = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 30)


def badge(draw, num, pos, target=None, r=24):
    x, y = pos
    if target:
        draw.line([pos, target], fill=LINE, width=4)
        tx, ty = target
        draw.ellipse([tx - 7, ty - 7, tx + 7, ty + 7], fill=LINE, outline=(0, 0, 0), width=2)
    draw.ellipse([x - r, y - r, x + r, y + r], fill=BADGE_FILL, outline=(0, 0, 0), width=3)
    draw.text((x, y), str(num), fill=BADGE_TEXT, font=FONT, anchor="mm")


def save(im, name, width=None, quality=86):
    if width and im.width > width:
        im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    im.convert("RGB").save(f"{OUT}/{name}", quality=quality, optimize=True)


# 그대로 쓰는 화면
save(Image.open(f"{SRC}/06_world.png"), "cover.jpg", 1600)
save(Image.open(f"{SRC}/01_title.png"), "title.jpg", 1280)
save(Image.open(f"{SRC}/02_mainmenu.png"), "mainmenu.jpg", 1600)
save(Image.open(f"{SRC}/04_gameplay_a.png"), "gameplay.jpg", 1600)
save(Image.open(f"{SRC}/03_tutorial_intro.png"), "intro.jpg", 1600)

# HUD 전체: 1 HP, 조준점 묶음 영역 점선 상자, 6 목표 안내
hud = Image.open(f"{SRC}/05_charge.png").convert("RGB")
d = ImageDraw.Draw(hud)
box = (820, 420, 1220, 680)
for i in range(box[0], box[2], 24):
    d.line([(i, box[1]), (min(i + 12, box[2]), box[1])], fill=LINE, width=4)
    d.line([(i, box[3]), (min(i + 12, box[2]), box[3])], fill=LINE, width=4)
for j in range(box[1], box[3], 24):
    d.line([(box[0], j), (box[0], min(j + 12, box[3]))], fill=LINE, width=4)
    d.line([(box[2], j), (box[2], min(j + 12, box[3]))], fill=LINE, width=4)
d.text((box[0] + 4, box[1] - 12), "2-5", fill=LINE, font=FONT, anchor="lb")
badge(d, 1, (620, 900), (548, 960), r=30)
save(hud, "hud_overview.jpg", 1600)

# 조준점 확대: 2 배터리, 3 과열, 4 차지샷, 5 조준점
zoom = Image.open(f"{SRC}/05_charge.png").convert("RGB").crop(box).resize((800, 520), Image.LANCZOS)
d = ImageDraw.Draw(zoom)
badge(d, 3, (60, 70), (150, 170))
badge(d, 2, (70, 455), (205, 345))
badge(d, 5, (420, 60), (288, 228))
badge(d, 4, (720, 470), (640, 405))
save(zoom, "hud_zoom.jpg")

# 목표 안내 창: 6
guide = Image.open(f"{SRC}/07_guide.png").convert("RGB").crop((580, 815, 1260, 1030))
d = ImageDraw.Draw(guide)
badge(d, 6, (34, 34))
save(guide, "hud_guide.jpg")
print("done")
