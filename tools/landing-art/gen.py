#!/usr/bin/env python3
"""Genera el set de arte del landing con un checkpoint de ComfyUI.

Uso:
    python tools/landing-art/gen.py hassakuSD15_v13.safetensors --profile sd15
    python tools/landing-art/gen.py NetaYumev35_pretrained_all_in_one.safetensors --profile sdxl

Salida:
    art-candidates/<modelo>/<asset>.png   (originales, gitignored)
    site-assets/landing/<asset>.webp      (lo que usa index.html)

Requiere ComfyUI escuchando en COMFYUI_URL (default http://127.0.0.1:8188)
y Pillow instalado.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path

try:
    from PIL import Image
except ImportError:  # pragma: no cover
    sys.exit("Falta Pillow: pip install pillow")

HOST = os.environ.get("COMFYUI_URL", "http://127.0.0.1:8188").rstrip("/")
REPO = Path(__file__).resolve().parents[2]
CANDIDATES = REPO / "art-candidates"
PUBLISHED = REPO / "site-assets" / "landing"
COMFY_OUTPUT = Path(os.environ.get("COMFY_OUTPUT", r"D:\Web\automation\comfyui\output"))

NEGATIVE = (
    "lowres, worst quality, low quality, blurry, jpeg artifacts, text, watermark, "
    "signature, logo, username, extra digits, fewer digits, bad hands, bad anatomy, "
    "cropped, out of frame, monochrome"
)

# id, ratio, big, prompt, alt
ASSETS = [
    ("hero-keyart", "16x9", True,
     "wide cinematic anime illustration of a cozy cafe interior at golden hour, wooden counter with espresso machine, "
     "hanging paper lamps, plants, steam in the air, a cheerful barista girl in a cream and rose apron behind the counter, "
     "shelves of cups, warm bokeh, composition leaves calm empty space on the lower left for text overlay",
     "Interior de cafetería anime con una barista al atardecer"),
    ("news-01", "16x9", False,
     "flat lay anime illustration of an autumn cafe menu on a wooden counter, pumpkin spice latte, cinnamon sticks, "
     "small pumpkin, whipped cream, warm orange and cream tones, soft window light",
     "Menú de otoño con latte de calabaza sobre la barra"),
    ("news-02", "16x9", False,
     "anime portrait of a calm night-shift barista girl with long dark plum hair and violet eyes, black apron with rose accents, "
     "standing in a dim cafe lit by moonlight and one warm lamp, soft rim light, detailed eyes",
     "Camille, la barista nocturna, en la cafetería a media luz"),
    ("news-03", "16x9", False,
     "close-up anime illustration of a match-3 puzzle board with cafe themed tiles, coffee bean, croissant, strawberry, teacup, "
     "glowing chain combo with sparkles, warm palette",
     "Tablero de combos con fichas de café"),
    ("featured-event", "16x9", True,
     "anime illustration of a cozy cafe open at midnight during a festival, string lights, warm glowing windows, "
     "deep blue night sky, sleepy customers inside, wooden storefront, atmospheric",
     "Cafetería abierta de noche durante el festival"),
    ("cast-aiko", "3x4", True,
     "full body anime character sheet of a cheerful barista girl, short caramel brown hair, bright amber eyes, "
     "cream apron with rose trim over a white shirt, holding an espresso cup, energetic pose, plain warm cream background, clean lineart",
     "Aiko, barista del turno mañana"),
    ("cast-camille", "3x4", True,
     "full body anime character sheet of an elegant night-shift barista girl, long dark plum hair, calm violet eyes, "
     "black apron with rose accents, holding a serving tray, composed pose, plain warm cream background, clean lineart",
     "Camille, barista del turno noche"),
    ("cast-yuzu", "3x4", True,
     "full body anime character sheet of a cheerful clumsy barista girl, green tinted bob hair, big bright eyes, "
     "matcha green apron, holding a bamboo whisk, hopeful pose, plain warm cream background, clean lineart",
     "Yuzu, barista del turno tarde"),
    ("screenshot-01", "16x9", False,
     "anime game screenshot mockup of a cozy cafe puzzle board, rows of cafe themed tiles mid cascade, order tickets panel on the side, "
     "warm cream and rose UI frames, isometric board",
     "Tablero en juego con cascada de fichas"),
    ("screenshot-02", "16x9", False,
     "anime game screenshot mockup of a decorated cafe room, tables, plants, hanging lamps, a barista serving a customer, "
     "warm cozy interior, 2D game art",
     "Cafetería decorada con clientes"),
    ("screenshot-03", "16x9", False,
     "anime game screenshot mockup of a character selection screen, three round portrait slots, skill icons, "
     "soft rose UI panels, warm cream background",
     "Pantalla de selección de party"),
    ("app-icon", "1x1", False,
     "app icon design, coffee cup with heart shaped latte art and a small rose ribbon, bold clean silhouette, "
     "cream background, flat vector anime style, centered",
     "Icono de la app WaifuCafe"),
    # og-image se deriva del hero (recorte 1200x630), no se genera aparte
]

PROFILES = {
    "sd15": {"steps": 28, "cfg": 7.0, "sampler": "dpmpp_2m", "scheduler": "karras"},
    "sdxl": {"steps": 30, "cfg": 5.5, "sampler": "dpmpp_2m", "scheduler": "karras"},
}


def base_size(ratio: str, big: bool, profile: str) -> tuple[int, int]:
    if profile == "sd15":
        table = {"16x9": (768, 432), "3x4": (640, 832), "1x1": (512, 512)}
    else:
        table = {"16x9": (1024, 576), "3x4": (768, 1024), "1x1": (768, 768)}
    return table[ratio]


def hires_size(ratio: str, profile: str) -> tuple[int, int]:
    if ratio == "16x9":
        return (1536, 864)
    if ratio == "3x4":
        return (960, 1280) if profile == "sdxl" else (960, 1248)
    return (1024, 1024)


def post(path: str, payload: dict) -> dict:
    req = urllib.request.Request(
        HOST + path,
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)


def get(path: str) -> dict:
    with urllib.request.urlopen(HOST + path, timeout=30) as r:
        return json.load(r)


def build(asset_id: str, ratio: str, big: bool, prompt: str, ckpt: str, profile: str,
          seed: int, loras: list[tuple[str, float, float]]) -> dict:
    p = PROFILES[profile]
    w, h = base_size(ratio, big, profile)

    wf: dict = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": ckpt}},
        "2": {"class_type": "EmptyLatentImage", "inputs": {"width": w, "height": h, "batch_size": 1}},
    }

    model, clip = ["1", 0], ["1", 1]
    for i, (name, sm, sc) in enumerate(loras):
        node = f"lora{i}"
        wf[node] = {"class_type": "LoraLoader",
                    "inputs": {"lora_name": name, "strength_model": sm, "strength_clip": sc,
                               "model": model, "clip": clip}}
        model, clip = [node, 0], [node, 1]

    wf["3"] = {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": clip}}
    wf["4"] = {"class_type": "CLIPTextEncode", "inputs": {"text": NEGATIVE, "clip": clip}}
    wf["5"] = {"class_type": "KSampler",
               "inputs": {"seed": seed, "steps": p["steps"], "cfg": p["cfg"],
                          "sampler_name": p["sampler"], "scheduler": p["scheduler"], "denoise": 1.0,
                          "model": model, "positive": ["3", 0], "negative": ["4", 0],
                          "latent_image": ["2", 0]}}

    last = ["5", 0]
    if big:
        uw, uh = hires_size(ratio, profile)
        wf["6"] = {"class_type": "LatentUpscale",
                   "inputs": {"upscale_method": "bislerp", "width": uw, "height": uh,
                              "crop": "disabled", "samples": last}}
        wf["7"] = {"class_type": "KSampler",
                   "inputs": {"seed": seed + 1, "steps": p["steps"], "cfg": p["cfg"],
                              "sampler_name": p["sampler"], "scheduler": p["scheduler"], "denoise": 0.45,
                              "model": model, "positive": ["3", 0], "negative": ["4", 0],
                              "latent_image": ["6", 0]}}
        last = ["7", 0]

    wf["8"] = {"class_type": "VAEDecode", "inputs": {"samples": last, "vae": ["1", 2]}}
    wf["9"] = {"class_type": "SaveImage",
               "inputs": {"filename_prefix": f"waifucafe/{asset_id}", "images": ["8", 0]}}
    return wf


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("checkpoint")
    ap.add_argument("--profile", choices=PROFILES, default="sd15")
    ap.add_argument("--loras", default="", help='nombre:model:clip,otro:0.7:0.7')
    ap.add_argument("--only", default="", help="ids separados por coma")
    args = ap.parse_args()

    loras = []
    for spec in filter(None, (s.strip() for s in args.loras.split(","))):
        name, sm, sc = spec.split(":")
        loras.append((name, float(sm), float(sc)))

    only = {s.strip() for s in args.only.split(",") if s.strip()}
    chosen = [a for a in ASSETS if not only or a[0] in only]
    model_dir = CANDIDATES / Path(args.checkpoint).stem
    model_dir.mkdir(parents=True, exist_ok=True)
    PUBLISHED.mkdir(parents=True, exist_ok=True)

    print(f"modelo: {args.checkpoint}  perfil: {args.profile}  assets: {len(chosen)}  loras: {len(loras)}")
    pending: dict[str, tuple[str, str]] = {}
    for i, (asset_id, ratio, big, prompt, alt) in enumerate(chosen):
        wf = build(asset_id, ratio, big, prompt, args.checkpoint, args.profile,
                   seed=777000 + i * 13, loras=loras)
        pid = post("/prompt", {"prompt": wf, "client_id": str(uuid.uuid4())})["prompt_id"]
        pending[pid] = (asset_id, alt)
        print(f"  encolado {asset_id:15} {pid}")

    started = time.time()
    done: dict[str, dict] = {}
    while pending:
        time.sleep(3)
        for pid in list(pending):
            try:
                hist = get(f"/history/{pid}")
            except urllib.error.URLError:
                continue
            if pid not in hist:
                continue
            entry = hist[pid]
            asset_id, _alt = pending.pop(pid)
            status = entry.get("status", {}).get("status_str")
            if status != "success":
                print(f"  FALLO {asset_id}: {json.dumps(entry.get('status'))[:400]}")
                continue
            imgs = [im for node in entry.get("outputs", {}).values() for im in node.get("images", [])]
            if not imgs:
                print(f"  FALLO {asset_id}: sin imágenes")
                continue
            src = COMFY_OUTPUT / imgs[0].get("subfolder", "") / imgs[0]["filename"]
            dst = model_dir / f"{asset_id}.png"
            shutil.copy2(src, dst)
            done[asset_id] = {"png": dst, "alt": _alt}
            print(f"  ok {asset_id:15} {dst.name}  ({int(time.time() - started)}s)")
        if time.time() - started > 1800:
            print("timeout total, corto")
            break

    # publicar webp con nombres canónicos
    for asset_id, info in done.items():
        ratio = next(a[1] for a in ASSETS if a[0] == asset_id)
        img = Image.open(info["png"]).convert("RGB")
        if asset_id == "app-icon":
            img.save(PUBLISHED / "app-icon.png", optimize=True)
        img.save(PUBLISHED / f"{asset_id}.webp", "WEBP", quality=86, method=6)
        print(f"  publicado {asset_id}.webp {img.size[0]}x{img.size[1]}")

    hero = done.get("hero-keyart")
    if hero:
        img = Image.open(hero["png"]).convert("RGB")
        w, h = img.size
        target = 1200 / 630
        if w / h > target:
            nw = int(h * target)
            img = img.crop(((w - nw) // 2, 0, (w + nw) // 2, h))
        else:
            nh = int(w / target)
            img = img.crop((0, (h - nh) // 2, w, (h + nh) // 2))
        img = img.resize((1200, 630), Image.LANCZOS)
        img.save(PUBLISHED / "og-image.webp", "WEBP", quality=86, method=6)
        print("  publicado og-image.webp 1200x630")

    print(f"\nlisto: {len(done)}/{len(chosen)} en {int(time.time() - started)}s")
    return 0 if len(done) == len(chosen) else 1


if __name__ == "__main__":
    sys.exit(main())
