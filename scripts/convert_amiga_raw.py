from PIL import Image
import numpy as np

def convert_amiga_raw(filename, width=320, height=200, bitplanes=5):
    with open(filename, "rb") as f:
        data = np.frombuffer(f.read(), dtype=np.uint8)

    plane_size = height * width // 8
    img = np.zeros((height, width), dtype=np.uint8)

    for plane in range(bitplanes):
        for y in range(height):
            for x in range(0, width, 8):
                byte = data[plane * plane_size + y * (width // 8) + x // 8]
                for bit in range(8):
                    img[y, x + bit] |= ((byte >> (7 - bit)) & 1) << plane

    return Image.fromarray(img * (255 // (2 ** bitplanes - 1)))

img = convert_amiga_raw("masterblaster-1.0/font.raw")
img.show()
