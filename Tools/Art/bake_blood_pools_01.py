"""Bake the procedural studies to straight-alpha color sprites, without the floor."""
import argparse
import importlib.util
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


PROJECT = Path(__file__).resolve().parents[2]
SOURCE = PROJECT / "Concepts/Unused/Visual/ProceduralBlood01/render_samples.py"
DESTINATION = PROJECT / "Assets/EmergencyShift/Art/Effects/BloodPools01"


def material(density, style, source, opacity_density):
    rng = np.random.default_rng(144)
    size = density.shape[0]
    grain = source.fbm(rng, size, 95)
    veins = source.fbm(rng, size, 19)
    opacity = 1 - np.exp(-density * opacity_density)
    thick = 1 - np.exp(-density * .57)
    thin = np.array([.47, .015, .030])
    deep = np.array([.125, .004, .012])
    blood = thin * (1 - thick[..., None]) + deep * thick[..., None]
    blood *= (.75 + .38 * veins + .12 * grain)[..., None]
    clot = source.smoothstep(.53, .78, source.fbm(rng, size, 48))
    clot_strength = [.13, .13, .42][style]
    blood *= (1 - clot * clot_strength * thick)[..., None]
    roughness = [.02, .06, .12][style]
    height = (1 - np.exp(-density * 1.15)) * (1 - roughness + roughness * veins)
    height += clot * thick * .035
    height8 = Image.fromarray(np.uint8(np.clip(height, 0, 1) * 255))
    height = np.asarray(height8.filter(ImageFilter.GaussianBlur(1.5)), dtype=np.float32) / 255
    gy, gx = np.gradient(height, 2.8 / size)
    normal = np.stack([-gx * .23, -gy * .23, np.ones_like(gx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    half_light = np.array([-.24, -.30, 1.5])
    half_light /= np.linalg.norm(half_light)
    specular = np.maximum(0, normal @ half_light) ** 112
    specular *= source.smoothstep(.33, 1.25, density)
    specular *= .3 + .7 * source.smoothstep(.35, .63, veins)
    strength = [.40, .40, .23][style]
    blood += specular[..., None] * np.array([.66, .46, .43]) * strength
    return np.concatenate([np.clip(blood, 0, 1), opacity[..., None]], axis=-1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--opacity-density", type=float, default=1.75)
    args = parser.parse_args()
    if args.opacity_density <= 0:
        raise ValueError("Opacity density must be positive")
    specification = importlib.util.spec_from_file_location("blood_study", SOURCE)
    source = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(source)
    DESTINATION.mkdir(parents=True, exist_ok=True)
    coordinates = np.linspace(-1.4, 1.4, source.SIZE, dtype=np.float32)
    x, y = np.meshgrid(coordinates, coordinates)
    outputs = []
    for style, name in enumerate(["Fresh", "Ejected", "Clotted"]):
        density = source.make_pool(x / .82, y / .82, 47 + style * 1103, style)
        rgba = np.uint8(material(density, style, source, args.opacity_density) * 255)
        path = DESTINATION / (name + ".png")
        Image.fromarray(rgba).save(path)
        outputs.append({"name": name, "seed": 47 + style * 1103, "style": style, "size": source.SIZE})
    for index in range(6):
        size = 96
        rng = np.random.default_rng(111 + index)
        values = np.linspace(-1, 1, size, dtype=np.float32)
        x, y = np.meshgrid(values, values)
        radius = np.hypot(x * 1.1, y * 1.2)
        radius += (source.fbm(rng, size, 6) - .5) * .35
        density = (1 - source.smoothstep(.6, .72, radius)) * (1.2 + .5 * source.fbm(rng, size, 10))
        alpha = 1 - np.exp(-density * 1.6)
        rgb = np.zeros((size, size, 3), dtype=np.float32)
        rgb[:] = [.29, .008, .02]
        rgb *= (.8 + .25 * source.fbm(rng, size, 12))[..., None]
        rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
        Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255)).save(DESTINATION / f"Drop{index + 1}.png")
    print(json.dumps({"outputs": outputs, "drops": 6, "opacityDensity": args.opacity_density, "runtimeGeneration": False}))


if __name__ == "__main__":
    main()
