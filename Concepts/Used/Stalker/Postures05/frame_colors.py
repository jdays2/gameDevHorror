"""Native straight RGBA input, linear premultiplied work, straight PNG output."""
import numpy as np
from PIL import Image


def decode(image):
    rgba = np.asarray(image).astype(np.float32) / 255
    rgb = rgba[..., :3]
    linear = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
    return np.concatenate([linear * rgba[..., 3:4], rgba[..., 3:4]], axis=-1)


def over(top, bottom):
    return top + bottom * (1 - top[..., 3:4])


def encode(rgba, green=False):
    alpha = rgba[..., 3:4]
    rgb = rgba[..., :3]
    if green:
        rgb = rgb + np.array([0, 1, 0], dtype=np.float32) * (1 - alpha)
    else:
        rgb = np.divide(rgb, alpha, out=np.zeros_like(rgb), where=alpha > 1e-8)
    rgb = np.where(rgb <= .0031308, rgb * 12.92,
                   1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
    if not green:
        rgb = np.concatenate([rgb, alpha], axis=-1)
    return Image.fromarray(np.rint(np.clip(rgb, 0, 1) * 255).astype('uint8'))
