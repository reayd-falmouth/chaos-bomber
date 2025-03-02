from PIL import Image, ImageOps
import numpy as np

# Configurable option to add padding or not
ADD_PADDING = False  # Set to False to remove padding

# Open the image in RGB mode (to handle black background)
img = Image.open("font.png").convert("RGB")

# Convert to numpy array
data = np.array(img)

# Define colors
background_color = np.array([0, 0, 0])  # Black background
font_color = np.array([255, 255, 255])  # White font
separator_color = (255, 255, 0)  # Yellow separator

# Create a mask: True where the pixel is white (font)
font_mask = np.all(data == font_color, axis=-1)

# Create new image data with black background
new_data = np.full_like(data, background_color)  # Start with all pixels black

# Set font pixels to white
new_data[font_mask] = [255, 255, 255]  # White font

# Convert back to an image
new_img = Image.fromarray(new_data, mode="RGB")

# Define glyph dimensions (8x7 pixels)
glyph_width = 8
glyph_height = new_img.height  # Default height without padding

# Apply padding if enabled
if ADD_PADDING:
    glyph_height += 10  # Increase height by 10 pixels (5 above, 5 below)
    new_width, new_height = new_img.size
    padded_img = Image.new("RGB", (new_width, glyph_height), (0, 0, 0))  # Black background
    padded_img.paste(new_img, (0, 5))  # Paste with 5px top padding
else:
    padded_img = new_img  # No padding applied

# Save the processed image
padded_img.save("font_processed.png")

# Calculate number of glyphs
num_glyphs = padded_img.width // glyph_width

separator_width = 3  # Width of yellow separators

# **Calculate the new width dynamically (removing trailing black pixels)**
new_width = 1 + num_glyphs * glyph_width + (num_glyphs * separator_width)  # Includes leading yellow column

# Create new image with a leading yellow column and separators
combined_img = Image.new("RGB", (new_width, glyph_height), (0, 0, 0))  # Black background

# **Draw the first yellow column (marking the start)**
for y in range(glyph_height):
    combined_img.putpixel((0, y), separator_color)  # First column is yellow

# Paste glyphs into the new image with separators
for i in range(num_glyphs):
    box = (i * glyph_width, 0, (i + 1) * glyph_width, glyph_height)
    glyph = padded_img.crop(box)

    # Calculate paste position considering the leading yellow column
    paste_x = 1 + i * (glyph_width + separator_width)  # Offset by 1 for the first yellow column
    combined_img.paste(glyph, (paste_x, 0))

    # Draw separator (skip after last glyph)
    if i < num_glyphs - 1:
        for x in range(paste_x + glyph_width, paste_x + glyph_width + separator_width):
            for y in range(glyph_height):
                combined_img.putpixel((x, y), separator_color)

# **Trim trailing black pixels dynamically**
bbox = combined_img.getbbox()  # Get bounding box of non-black content
trimmed_img = combined_img.crop(bbox) if bbox else combined_img  # Trim only if needed

# Save the final trimmed image
trimmed_img.save("imagefont.png")

print(f"Font processed with black background, white font, {'10px padding' if ADD_PADDING else 'no padding'}, 3px yellow separators, leading yellow column, and trimmed trailing black space.")
