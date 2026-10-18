"""Recreate the shared white UI primitive; current icons use HUD03 SVG sources."""
from pathlib import Path
from PIL import Image

output = Path(__file__).resolve().parents[2] / 'Assets/EmergencyShift/Art/UI/GameplayHud02'
output.mkdir(parents=True, exist_ok=True)
Image.new('RGBA', (4, 4), 'white').save(output / 'white.png')
print(output)
