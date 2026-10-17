"""Shared offline assembly of the existing eye art; does not edit live atlases."""
import numpy as np
from PIL import Image, ImageDraw


def pale_eye(canvas, center, radius):
    data = np.asarray(canvas).copy()
    yy, xx = np.indices(data.shape[:2])
    distance = ((xx - center[0]) / radius[0]) ** 2 + ((yy - center[1]) / radius[1]) ** 2
    weight = np.clip((1 - distance) / .08, 0, 1)[:, :, None]
    weight *= (data[:, :, 3:4] > 0)
    rgb = data[:, :, :3].astype(np.float32)
    luminance = rgb[:, :, 0] * .24 + rgb[:, :, 1] * .64 + rgb[:, :, 2] * .12
    # Almost white sclera, retaining the actual radial fibres and fine veins.
    light = (112 + luminance * .55)[:, :, None]
    target = light * np.array([1, .995, .98]) + (rgb - luminance[:, :, None]) * .10
    data[:, :, :3] = np.rint(rgb * (1 - weight) + target * weight).clip(0, 255).astype(np.uint8)
    return Image.fromarray(data)


def pupil_layer(centre, radius):
    pupil = Image.new("RGBA", (560 * 4, 560 * 4))
    drawing = ImageDraw.Draw(pupil)
    drawing.ellipse((round((centre[0] - radius[0]) * 4), round((centre[1] - radius[1]) * 4), round((centre[0] + radius[0]) * 4), round((centre[1] + radius[1]) * 4)), fill=(6, 4, 6, 255))
    return pupil.resize((560, 560), Image.Resampling.LANCZOS)


def dead_eye_organ(image):
    """Assemble a fixed, slightly rolled eye without changing the live atlas.

    Warp only the exposed fibres, like the runtime gaze shader. The fleshy
    membrane stays registered; the pupil is foreshortened and clipped by it.
    """
    organ = image.crop((32, 44, 592, 604))
    backing = Image.new("RGBA", organ.size)
    drawing = ImageDraw.Draw(backing)
    drawing.ellipse((164, 173, 396, 405), fill=(214, 194, 191, 255))
    organ = Image.alpha_composite(backing, organ)
    # Preserve the previous organ's exact alpha, including nearly opaque
    # source pixels that the old centered pupil covered.
    original_alpha = Image.alpha_composite(organ, pupil_layer((280, 289), (75.4, 75.4))).getchannel("A")
    data = np.asarray(organ).copy()
    yy, xx = np.indices(data.shape[:2], dtype=np.float32)
    distance = np.sqrt(((xx - 280) / 154.28) ** 2 + ((yy - 289) / 146.16) ** 2)
    # Image coordinates: up and left, with a small part behind the upper lid.
    gaze = (-30, -82)
    # A disk-preserving map prevents a large rolled gaze from pulling the
    # lower membrane into the eye. Only the inner ocular surface is sampled.
    position = (xx - 280) / 154.28 + 1j * (yy - 289) / 146.16
    offset = gaze[0] / 154.28 + 1j * gaze[1] / 146.16
    source = (position - offset) / (1 - np.conjugate(offset) * position)
    source_x = np.clip(280 + source.real * 154.28, 0, organ.width - 1)
    source_y = np.clip(289 + source.imag * 146.16, 0, organ.height - 1)
    x0 = source_x.astype(int)
    y0 = source_y.astype(int)
    x1 = np.minimum(x0 + 1, organ.width - 1)
    y1 = np.minimum(y0 + 1, organ.height - 1)
    fx = (source_x - x0)[:, :, None]
    fy = (source_y - y0)[:, :, None]
    rgb = data[:, :, :3].astype(np.float32)
    shifted = (rgb[y0, x0] * (1 - fx) + rgb[y0, x1] * fx) * (1 - fy)
    shifted += (rgb[y1, x0] * (1 - fx) + rgb[y1, x1] * fx) * fy
    transition = np.clip((distance - .91) / .09, 0, 1)
    aperture = 1 - transition * transition * (3 - 2 * transition)
    data[:, :, :3] = np.rint(rgb * (1 - aperture[:, :, None]) + shifted * aperture[:, :, None]).clip(0, 255).astype(np.uint8)
    organ = Image.fromarray(data)
    centre = (280 + gaze[0], 289 + gaze[1])
    radius = (75.4 * .94, 75.4 * .72)
    pupil = pupil_layer(centre, radius)
    pupil_data = np.asarray(pupil).copy()
    pupil_data[:, :, 3] = np.rint(pupil_data[:, :, 3] * aperture).astype(np.uint8)
    organ = Image.alpha_composite(organ, Image.fromarray(pupil_data))
    organ.putalpha(original_alpha)
    return organ


