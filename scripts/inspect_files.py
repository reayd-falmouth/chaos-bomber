#!/usr/bin/env python3
import os
import sys
import re

def hexdump(data, length=64):
    # Return a string with the first 'length' bytes in hex format.
    return " ".join("{:02x}".format(b) for b in data[:length])

def inspect_file(filepath, patterns):
    print("File:", filepath)
    # Try to read the file as text.
    try:
        with open(filepath, "r", encoding="latin-1", errors="ignore") as f:
            content = f.read()
    except Exception as e:
        print("  [ERROR reading text]:", e)
        content = None

    if content:
        for key, pattern in patterns.items():
            matches = pattern.findall(content)
            if matches:
                # Only show up to the first 10 matches for brevity.
                print(f"  Pattern '{key}' found ({len(matches)} occurrences): {matches[:10]}")
    else:
        print("  Could not read as text.")

    # Always print a hexdump of the first 64 bytes.
    try:
        with open(filepath, "rb") as f:
            data = f.read(64)
            print("  Hexdump (first 64 bytes):", hexdump(data))
    except Exception as e:
        print("  [ERROR reading binary]:", e)
    print("-" * 60)

def main(directory):
    # Define regex patterns to search for.
    patterns = {
        "dc.w": re.compile(r'\bdc\.w\b', re.IGNORECASE),
        "COLOR": re.compile(r'\bcolor', re.IGNORECASE),
        "copper": re.compile(r'copper', re.IGNORECASE),
        "hex": re.compile(r'[\$0x][0-9a-fA-F]+')
    }

    if not os.path.isdir(directory):
        print(f"Error: {directory} is not a valid directory.")
        sys.exit(1)

    # Walk through all files in the directory.
    for entry in os.listdir(directory):
        full_path = os.path.join(directory, entry)
        if os.path.isfile(full_path):
            inspect_file(full_path, patterns)

if __name__ == "__main__":
    # Usage: python inspect_files.py <directory>
    directory = sys.argv[1] if len(sys.argv) > 1 else "."
    main(directory)
