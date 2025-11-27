#!/usr/bin/env python3
"""
Script to create a Windows .ico file from the MDI home-sound-out icon SVG.
This version creates a simple bitmap-based icon without requiring Cairo.
Requires: pip install pillow
"""

import sys
from io import BytesIO

try:
    from PIL import Image, ImageDraw
except ImportError:
    print("Error: Pillow not installed.")
    print("Please run: pip install pillow")
    sys.exit(1)

def create_house_with_sound_icon(size):
    """
    Create a house icon with sound waves based on the MDI home-sound-out design.
    
    Args:
        size: The size of the icon (width and height)
    
    Returns:
        PIL Image object
    """
    # Create a new RGBA image with white background for visibility
    img = Image.new('RGBA', (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(img)
    
    # Scale factor for drawing
    scale = size / 24
    
    # Color: dark gray/black with full opacity
    color = (40, 40, 40, 255)
    line_width = max(1, int(scale * 0.8))
    
    # Sound waves (arcs on the left side) - draw these first
    # Wave 1 (outermost)
    draw.arc([0.5 * scale, 1 * scale, 7.5 * scale, 7.5 * scale], 
             -10, 100, fill=color, width=line_width)
    
    # Wave 2 (middle)
    draw.arc([2 * scale, 2.5 * scale, 7 * scale, 7.5 * scale], 
             -10, 100, fill=color, width=line_width)
    
    # Wave 3 (innermost)
    draw.arc([4 * scale, 4.5 * scale, 7 * scale, 7.5 * scale], 
             -10, 100, fill=color, width=line_width)
    
    # House shape
    # Triangle roof (filled)
    roof_points = [
        (9 * scale, 2 * scale),    # Top
        (19 * scale, 12 * scale),  # Right
        (7 * scale, 12 * scale)    # Left
    ]
    draw.polygon(roof_points, fill=color, outline=color)
    
    # House body (rectangle)
    body_x1 = 7 * scale
    body_y1 = 12 * scale
    body_x2 = 19 * scale
    body_y2 = 22 * scale
    draw.rectangle([body_x1, body_y1, body_x2, body_y2], fill=color, outline=color)
    
    # Add some detail - window cutout for larger icons
    if size >= 32:
        window_margin = scale * 2
        window_size = scale * 3
        window_x1 = body_x1 + window_margin
        window_y1 = body_y1 + window_margin
        window_x2 = window_x1 + window_size
        window_y2 = window_y1 + window_size
        # Create a lighter window area
        draw.rectangle([window_x1, window_y1, window_x2, window_y2], 
                      fill=(200, 200, 200, 255), outline=color, width=max(1, line_width // 2))
    
    return img

def create_ico(output_path, sizes=[16, 24, 32, 48, 256]):
    """
    Create multi-resolution ICO file.
    
    Args:
        output_path: Path to save the .ico file
        sizes: List of icon sizes to include
    """
    print("Generating icon at multiple resolutions...")
    
    # Create all images first
    images_data = []
    for size in sizes:
        print(f"  Creating {size}x{size} icon...")
        img = create_house_with_sound_icon(size)
        images_data.append(img)
    
    # Save as ICO with multiple resolutions
    print(f"Saving ICO file to {output_path}...")
    
    # Save the first (smallest) image with all others appended
    images_data[0].save(
        output_path,
        format='ICO',
        append_images=images_data[1:],
        sizes=[(img.width, img.height) for img in images_data]
    )
    
    print(f"Successfully created {output_path}")
    print(f"Number of sizes: {len(sizes)}")
    print(f"Sizes included: {sizes}")

if __name__ == "__main__":
    output_file = "src/app.ico"
    png_file = "src/app.png"
    
    try:
        # Create the main 48x48 PNG icon for better quality
        print("Creating PNG icon...")
        png_icon = create_house_with_sound_icon(48)
        png_icon.save(png_file, 'PNG')
        print(f"PNG icon created: {png_file}")
        
        # Also create multi-resolution ICO
        create_ico(output_file)
        print(f"\nIcon files created successfully!")
        print(f"  - {output_file} (multi-resolution ICO)")
        print(f"  - {png_file} (48x48 PNG)")
    except Exception as e:
        print(f"Error creating icon: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
