#!/usr/bin/env python3
"""Generate roster unit pictures with a local image model through the Draw Things HTTP API.

Draw Things (Mac App Store) must be open with its API server on (Settings > Advanced >
API Server: HTTP, port 7860). Prompts live in units.json next to this script.

    python3 tools/roster/generate.py --model qwen --keys unit_de_rifle,unit_su_rifle --seeds 3
    python3 tools/roster/generate.py --model zimage --keys all --seeds 2

Candidates go to ArtSource/roster/candidates/<key>/<model>_<seed>.png, with the exact
request beside each as .json; existing ones are skipped. Pick the best with process.py.
"""
import argparse
import base64
import json
import os
import sys
import time
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "ArtSource", "roster", "candidates")
API = "http://127.0.0.1:7860/sdapi/v1/txt2img"

# Per-model settings. Z Image Turbo is distilled: 8 steps, no guidance. Qwen Image needs real
# guidance and more steps; it is slow (minutes per image on an M4 Pro) but follows the
# man-by-man prompts much better.
MODELS = {
    "zimage": {"model": "z_image_turbo_1.0_i8x.ckpt", "steps": 8, "guidance_scale": 1.0,
               "sampler": "UniPC Trailing", "shift": 3.0},
    "qwen": {"model": "qwen_image_2512_q6p.ckpt", "steps": 30, "guidance_scale": 4.0,
             "sampler": "UniPC Trailing", "shift": 2.0},
    # The non-distilled Z Image: more varied, and it honours the negative prompt (units.json "negative").
    # Draw Things' recommended settings for it (2026-11-26).
    "zbase": {"model": "z_image_1.0_i8x.ckpt", "steps": 30, "guidance_scale": 4.0,
              "sampler": "UniPC Trailing", "shift": 1.0, "resolution_dependent_shift": True},
}

# Models that use classifier-free guidance, so a negative prompt has an effect.
USES_NEGATIVE = {"qwen", "zbase"}


def prompt_for(unit, data):
    parts = [unit["prompt"]]
    if unit["style"] == "men":
        parts.append(data["uniforms"][unit["side"]])
    parts.append(data["styles"][unit["style"]])
    return " ".join(parts)


def generate(body, tries=2):
    """The image, or None if Draw Things returned none (it sometimes drops a request) twice."""
    for _ in range(tries):
        req = urllib.request.Request(API, data=json.dumps(body).encode(), headers={"Content-Type": "application/json"})
        with urllib.request.urlopen(req, timeout=3600) as r:
            images = json.load(r).get("images") or []
        if images:
            return base64.b64decode(images[0])
        time.sleep(5)
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", choices=sorted(MODELS), default="qwen")
    ap.add_argument("--keys", default="all", help="comma-separated unit keys, or 'all'")
    ap.add_argument("--seeds", type=int, default=2, help="candidates per unit")
    ap.add_argument("--seed", type=int, default=1000, help="first seed")
    ap.add_argument("--width", type=int, default=1024)
    ap.add_argument("--height", type=int, default=768)
    args = ap.parse_args()

    data = json.load(open(os.path.join(HERE, "units.json")))
    units = data["units"] if args.keys == "all" else [u for u in data["units"] if u["key"] in args.keys.split(",")]
    if not units:
        sys.exit("no such unit keys: " + args.keys)

    preset = MODELS[args.model]
    for unit in units:
        folder = os.path.join(OUT, unit["key"])
        os.makedirs(folder, exist_ok=True)
        for seed in range(args.seed, args.seed + args.seeds):
            path = os.path.join(folder, f"{args.model}_{seed}.png")
            if os.path.exists(path):
                continue
            negative = data.get("negative", "") if args.model in USES_NEGATIVE else ""
            body = dict(preset, prompt=prompt_for(unit, data), negative_prompt=negative, seed=seed,
                        width=args.width, height=args.height, batch_size=1)
            started = time.time()
            image = generate(body)
            if image is None:
                print(f"{os.path.relpath(path, ROOT)}  no image returned, skipped", flush=True)
                continue
            with open(path, "wb") as f:
                f.write(image)
            with open(path[:-4] + ".json", "w") as f:
                json.dump(body, f, indent=1)
            print(f"{os.path.relpath(path, ROOT)}  {time.time() - started:.0f}s", flush=True)


if __name__ == "__main__":
    main()
