import numpy as np
from PIL import Image

def extract_amiga_font(
    filename,
    char_width=8,
    char_height=8,
    num_chars=52,
    reverse_bits=False,
    skip_footer=True
):
    """
    Reads a raw font file and arranges it into a grid.
    reverse_bits=True will interpret bit 0 as the leftmost pixel
    skip_footer=True will remove extra trailing bytes instead of a header
    """

    with open(filename, "rb") as f:
        file_data = f.read()

    # We expect exactly num_chars * char_height bytes for a 1-plane font.
    expected_length = num_chars * char_height
    data_length = len(file_data)

    # If there's a mismatch, decide whether to skip from the end or the start.
    if data_length != expected_length:
        diff = data_length - expected_length
        if diff > 0:
            if skip_footer:
                # Remove diff bytes from the END
                file_data = file_data[:-diff]
            else:
                # Remove diff bytes from the START
                file_data = file_data[diff:]
        elif diff < 0:
            raise ValueError(
                f"File has fewer bytes ({data_length}) than expected ({expected_length})."
            )

    # Convert to numpy for easy indexing
    font_data = np.frombuffer(file_data, dtype=np.uint8)

    # Arrange characters in a grid
    cols = min(num_chars, 13)
    rows = (num_chars + cols - 1) // cols
    image_width = cols * char_width
    image_height = rows * char_height

    # Create an array for an 8-bit grayscale image (0 = white, 255 = black)
    img = np.full((image_height, image_width), 255, dtype=np.uint8)

    for char_idx in range(num_chars):
        x0 = (char_idx % cols) * char_width
        y0 = (char_idx // cols) * char_height

        for row in range(char_height):
            font_byte = font_data[char_idx * char_height + row]

            # For each bit in font_byte, decide how to map it to x-pixels
            for b in range(char_width):
                # If reverse_bits=False, bit 7 is leftmost
                # If reverse_bits=True, bit 0 is leftmost
                if reverse_bits:
                    bit_mask = 1 << b
                else:
                    bit_mask = 1 << (7 - b)

                if font_byte & bit_mask:
                    # Black pixel
                    x = x0 + b
                    y = y0 + row
                    img[y, x] = 0  # black

    return Image.fromarray(img, mode="L")

# Try extracting with various toggles:
# 1) The "normal" approach (skip_footer=True, reverse_bits=False)
font_img_1 = extract_amiga_font(
    "masterblaster-1.0/font.raw",
    char_width=7,
    char_height=8,
    num_chars=52,
    reverse_bits=False,
    skip_footer=False
)
font_img_1.save("font_extracted_normal.png")

# 2) Reverse bits
font_img_2 = extract_amiga_font(
    "masterblaster-1.0/font.raw",
    char_width=7,
    char_height=8,
    num_chars=52,
    reverse_bits=True,
    skip_footer=False
)
font_img_2.save("font_extracted_reversed.png")
font_img_2.show()
