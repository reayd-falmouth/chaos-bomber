"""Expand a Unity .unitypackage (tar) into Assets/3rdParty/Vendor/PlayFabPartySDK."""
from __future__ import annotations

import os
import shutil
import sys
import tarfile
from pathlib import Path


def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    pkg = repo / "_tmp_playfab_party" / "playfab-party.1.10.5.0-main.0-3.18.2025.unitypackage"
    if not pkg.is_file():
        print("unitypackage not found:", pkg, file=sys.stderr)
        return 1
    extract_dir = repo / "_tmp_playfab_party_extract"
    extract_dir.mkdir(parents=True, exist_ok=True)
    with tarfile.open(pkg, "r:*") as tf:
        tf.extractall(extract_dir)

    dest_vendor = repo / "Assets" / "Plugins" / "PlayFabPartySDK"
    dest_vendor.mkdir(parents=True, exist_ok=True)

    n = 0
    for d in extract_dir.iterdir():
        if not d.is_dir() or len(d.name) != 32:
            continue
        path_file = d / "pathname"
        asset_file = d / "asset"
        if not path_file.is_file() or not asset_file.is_file():
            continue
        rel = path_file.read_text(encoding="utf-8", errors="replace").strip().replace("\\", "/")
        if not rel.startswith("Assets/"):
            continue
        # Map PlayFab Party assets under vendor folder
        if rel.startswith("Assets/PlayFabPartySDK/"):
            out_rel = rel[len("Assets/PlayFabPartySDK/") :]
        elif rel.startswith("Assets/PlayFabSDK/"):
            # Package may bundle overlapping PlayFab core; skip if would duplicate Vendor/PlayFabSDK
            out_rel = "_bundled_playfab_sdk/" + rel[len("Assets/PlayFabSDK/") :]
        else:
            out_rel = rel[len("Assets/") :]

        out_path = dest_vendor / out_rel
        out_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(asset_file, out_path)
        n += 1
    print(f"Materialized {n} files to {dest_vendor}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
