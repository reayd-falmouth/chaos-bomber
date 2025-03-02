import argparse
import os
import shutil
import zipfile
import requests
import subprocess

# Constants
DEFAULT_VERSION = "11.5"
DEFAULT_ARCH = "win64"
GAME_NAME = "chaosbomber"
BUILD_DIR = "build"
MACOS_APP_NAME = f"{GAME_NAME}.app"

LOVE_DOWNLOAD_URLS = {
    "windows": "https://github.com/love2d/love/releases/download/{version}/love-{version}-{arch}.zip",
    "macos": "https://github.com/love2d/love/releases/download/{version}/love-{version}-macos.zip",
}


def download_love(version, arch, platform):
    """Download and extract the specified LÖVE version for Windows or macOS."""
    url = LOVE_DOWNLOAD_URLS[platform].format(version=version, arch=arch)
    love_zip_path = os.path.join(BUILD_DIR, f"love-{platform}.zip")
    love_extract_path = os.path.join(BUILD_DIR, f"love-{platform}")

    print(f"Downloading LÖVE {version} for {platform}...")

    os.makedirs(BUILD_DIR, exist_ok=True)

    response = requests.get(url, stream=True)
    if response.status_code == 200:
        with open(love_zip_path, "wb") as f:
            shutil.copyfileobj(response.raw, f)
    else:
        raise Exception(f"Failed to download LÖVE from {url}")

    with zipfile.ZipFile(love_zip_path, "r") as zip_ref:
        zip_ref.extractall(BUILD_DIR)

    extracted_folder = os.path.join(BUILD_DIR,
                                    f"love-{version}-{arch}" if platform == "windows" else f"love-{version}-macos")
    if os.path.exists(extracted_folder):
        shutil.move(extracted_folder, love_extract_path)

    print(f"LÖVE for {platform} downloaded and extracted successfully.")


def create_love_archive(source_dir):
    """Create a .love archive from the game source directory."""
    love_path = os.path.join(BUILD_DIR, f"{GAME_NAME}.love")

    print(f"Creating {GAME_NAME}.love archive...")

    with zipfile.ZipFile(love_path, "w", zipfile.ZIP_DEFLATED) as love_zip:
        for root, _, files in os.walk(source_dir):
            for file in files:
                file_path = os.path.join(root, file)
                archive_name = os.path.relpath(file_path, source_dir)
                love_zip.write(file_path, archive_name)

    print(f"{GAME_NAME}.love created successfully.")
    return love_path


def create_windows_executable(love_path, version, arch):
    """Combine love.exe with the .love file to create a Windows executable."""
    love_exe = os.path.join(BUILD_DIR, "love-windows", "love.exe")
    game_exe = os.path.join(BUILD_DIR, f"{GAME_NAME}.exe")

    print("Creating Windows executable...")

    with open(love_exe, "rb") as exe, open(love_path, "rb") as love, open(game_exe, "wb") as output:
        output.write(exe.read())
        output.write(love.read())

    print("Windows executable created successfully.")
    return game_exe


def package_windows_zip(version, arch):
    """Package everything into a ZIP archive for Windows distribution."""
    dist_dir = os.path.join(BUILD_DIR, f"{GAME_NAME}-windows")
    os.makedirs(dist_dir, exist_ok=True)

    shutil.move(os.path.join(BUILD_DIR, f"{GAME_NAME}.exe"), dist_dir)
    shutil.copy(os.path.join(BUILD_DIR, "love-windows", "license.txt"), dist_dir)

    for file in os.listdir(os.path.join(BUILD_DIR, "love-windows")):
        if file.endswith(".dll"):
            shutil.copy(os.path.join(BUILD_DIR, "love-windows", file), dist_dir)

    zip_name = f"{GAME_NAME}-{version}-{arch}.zip"
    zip_path = os.path.join(BUILD_DIR, zip_name)

    print(f"Creating final ZIP archive: {zip_name}...")

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(dist_dir):
            for file in files:
                file_path = os.path.join(root, file)
                archive_name = os.path.relpath(file_path, BUILD_DIR)
                zipf.write(file_path, archive_name)

    print(f"ZIP archive created: {zip_path}")
    return zip_path


def create_macos_app(love_path):
    """Create a macOS .app bundle with the .love file inside."""
    love_macos_dir = os.path.join(BUILD_DIR, "love-macos")
    app_dir = os.path.join(BUILD_DIR, MACOS_APP_NAME)

    print("Creating macOS application bundle...")

    if os.path.exists(app_dir):
        shutil.rmtree(app_dir)

    shutil.copytree(os.path.join(love_macos_dir, "love.app"), app_dir)
    shutil.copy(love_path, os.path.join(app_dir, "Contents", "Resources", f"{GAME_NAME}.love"))

    # Update Info.plist
    info_plist_path = os.path.join(app_dir, "Contents", "Info.plist")
    subprocess.run([
        "plutil", "-replace", "CFBundleName", "-string", GAME_NAME, info_plist_path
    ])
    subprocess.run([
        "plutil", "-replace", "CFBundleIdentifier", "-string", f"com.yourdomain.{GAME_NAME}", info_plist_path
    ])
    subprocess.run([
        "plutil", "-replace", "CFBundleExecutable", "-string", "love", info_plist_path
    ])

    print("macOS application bundle created successfully.")
    return app_dir


def package_macos_zip(version):
    """Package everything into a ZIP archive for macOS distribution."""
    zip_name = f"{GAME_NAME}-{version}-macos.zip"
    zip_path = os.path.join(BUILD_DIR, zip_name)

    print(f"Creating final ZIP archive: {zip_name}...")

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(os.path.join(BUILD_DIR, MACOS_APP_NAME)):
            for file in files:
                file_path = os.path.join(root, file)
                archive_name = os.path.relpath(file_path, BUILD_DIR)
                zipf.write(file_path, archive_name)

    print(f"ZIP archive created: {zip_path}")
    return zip_path


def main():
    parser = argparse.ArgumentParser(description="Build a distributable for a LÖVE game (Windows and macOS).")
    parser.add_argument("--version", type=str, default=DEFAULT_VERSION, help="LÖVE version (default: 11.5)")
    parser.add_argument("--arch", type=str, default=DEFAULT_ARCH, choices=["win32", "win64"],
                        help="Architecture (default: win64)")
    parser.add_argument("--platform", type=str, choices=["windows", "macos", "both"], default="both",
                        help="Target platform (default: both)")
    parser.add_argument("--source", type=str, default="src/chaos_bomber", help="Path to the game source directory")

    args = parser.parse_args()

    love_path = create_love_archive(args.source)

    if args.platform in ["windows", "both"]:
        download_love(args.version, args.arch, "windows")
        create_windows_executable(love_path, args.version, args.arch)
        package_windows_zip(args.version, args.arch)

    if args.platform in ["macos", "both"]:
        download_love(args.version, args.arch, "macos")
        create_macos_app(love_path)
        package_macos_zip(args.version)


if __name__ == "__main__":
    main()
