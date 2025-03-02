import PIL.Image
from PIL import Image

def palette_regdump2palette(text):
    """
    Converts a WinUAE custom register dump (e command) to a palette.
    Expects lines starting with "COLOR" followed by two tokens:
    e.g. "COLOR00 0x0f0" or similar.
    """
    toks = iter(text.split())
    rval = dict()
    try:
        while True:
            v = next(toks)
            if v.startswith("COLOR"):
                # Extract index from the last two characters (e.g., "COLOR00")
                index = int(v[-2:])
                # Next token is the hex color value
                v = int(next(toks), 16)
                # Convert from Amiga RGB4 (0x0RGB) to 8-bit per channel RGB.
                rval[index] = ((v & 0xF00) >> 4, (v & 0xF0), (v & 0xF) << 4)
    except StopIteration:
        pass
    # Return palette sorted by index.
    return [rval[k] for k in sorted(rval.keys())]

def extract_palette_from_info_regdump(filename, num_colors=16):
    with open(filename, "r", encoding="latin-1", errors="ignore") as f:
        text = f.read()
    palette = palette_regdump2palette(text)
    if len(palette) > num_colors:
        palette = palette[:num_colors]
    return palette

def palette_to_image(palette, output=None):
    sqs = 16  # swatch size
    width = sqs * len(palette)
    height = sqs
    img = Image.new('RGB', (width, height))
    x = 0
    for rgb in palette:
        for i in range(sqs):
            for j in range(sqs):
                img.putpixel((x + i, j), rgb)
        x += sqs
    if output:
        img.save(output)
    return img

if __name__ == "__main__":
    # Try using the regdump-style extraction on the guide.info file.
    info_filename = "masterblaster-1.0/main"
    palette = extract_palette_from_info_regdump(info_filename, num_colors=16)
    if palette:
        print("Extracted palette using regdump method:")
        for i, col in enumerate(palette):
            print(f"Color {i}: {col}")
        img = palette_to_image(palette, output="extracted_palette_regdump.png")
        img.show()
    else:
        print("No palette data found using regdump method.")
