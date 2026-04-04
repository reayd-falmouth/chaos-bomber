"""
Render the current scene to a PNG (Eevee preferred, then Cycles).

Run in Blender: Text Editor → Open this file → Run Script.

If "nothing happens": Window → Toggle System Console (Windows) to see prints/errors.

Output folder:
  - If the .blend is saved: <folder containing the .blend>/renders/
  - Else: this script's directory/renders/ (or edit FALLBACK_RENDER_DIR below)

MCP:
    _ns = {"bpy": bpy, "__name__": "__main__"}
    exec(open(r"...\\export_render.py", encoding="utf-8").read(), _ns)
"""
from __future__ import annotations

import os
import traceback

import bpy
from mathutils import Vector

# Used only when the .blend has never been saved and __file__ is missing (internal text block)
FALLBACK_RENDER_DIR = r"d:\Users\david\StonesAndDice_Unity_Projects\unity-fps-microgame\Blender\renders"

_DEFAULT_NAME = "proto_bot_render.png"


def _output_dir() -> str:
    if bpy.data.filepath:
        blend_dir = bpy.path.abspath("//")
        return os.path.normpath(os.path.join(blend_dir, "renders"))
    try:
        here = os.path.dirname(os.path.abspath(__file__))
        return os.path.normpath(os.path.join(here, "renders"))
    except NameError:
        return os.path.normpath(FALLBACK_RENDER_DIR)


def _render_write_still() -> None:
    """Avoid context poll() failures when running from the Text Editor."""
    scene = bpy.context.scene
    window = bpy.context.window
    if window is not None and hasattr(bpy.context, "temp_override"):
        with bpy.context.temp_override(window=window, scene=scene):
            bpy.ops.render.render(write_still=True)
    else:
        bpy.ops.render.render(write_still=True)


def export_render(
    filename: str = _DEFAULT_NAME,
    res_x: int = 1600,
    res_y: int = 1200,
    camera_object_name: str | None = None,
    target_object_name: str | None = None,
) -> str:
    out_dir = _output_dir()
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, filename)

    scene = bpy.context.scene
    cam = scene.camera
    if not cam and camera_object_name:
        cam = bpy.data.objects.get(camera_object_name)
        scene.camera = cam
    if not cam:
        cam = bpy.data.objects.get("Camera")
        scene.camera = cam

    # Aim at Proto-Bot root/head if present
    tgt_name = target_object_name
    if not tgt_name:
        for candidate in ("CHR_PrBot_Root", "CHR_PrBot_Head", "CHR_PrBot_Torso"):
            if bpy.data.objects.get(candidate):
                tgt_name = candidate
                break
    target = bpy.data.objects.get(tgt_name) if tgt_name else None

    if cam and target:
        t = target.matrix_world.translation
        cam.location = Vector((t.x + 1.8, t.y - 2.2, t.z + 0.65))
        direction = t - cam.location
        cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()

    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH", "CYCLES"):
        if hasattr(bpy.types, eng):
            scene.render.engine = eng
            break

    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.resolution_percentage = 100
    scene.render.filepath = out_path
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    if not bpy.ops.render.render.poll():
        print("WARNING: render.poll() is False; attempting render anyway…")
    _render_write_still()
    print("Saved render:", out_path)

    try:
        bpy.ops.wm.path_open(filepath=out_dir)
    except Exception:
        pass

    return out_path


def main() -> None:
    print("export_render: starting…")
    print("  output dir:", _output_dir())
    try:
        path = export_render()
        print("export_render: done ->", path)
    except Exception as e:
        print("export_render FAILED:", e)
        traceback.print_exc()


# Blender Text Editor often sets __name__ to the text datablock name (e.g. "Text"),
# not "__main__", so a guarded block never runs. This file is run-only.
main()
