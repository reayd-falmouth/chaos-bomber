"""
Proto-Bot — chibi yellow robot (rounded cube head, cylindrical torso, ball-joint limbs).

Run in Blender: Scripting workspace → Open this file → Run Script.
Or paste into MCP `execute_blender_code` when the Blender MCP addon is connected.

Clears only the `CHAR_PROTOBOT` collection and related materials (names prefixed).
Target: ~1.05 m tall, metric, +Y forward (face looks toward -Y; rotate root 180° for Unity if needed).
"""
from __future__ import annotations

import math

import bpy
import bmesh

COL_MAIN = "CHAR_PROTOBOT"
COL_EXPORT = "EXPORT_PROTOBOT"
PREFIX = "CHR_PrBot_"
MAT_PREFIX = "MAT_PrBot_"


def _remove_if_exists():
    main = bpy.data.collections.get(COL_MAIN)
    if main:

        def collect_objects(c: bpy.types.Collection, bucket: set[bpy.types.Object]) -> None:
            for o in c.objects:
                bucket.add(o)
            for ch in c.children:
                collect_objects(ch, bucket)

        to_delete: set[bpy.types.Object] = set()
        collect_objects(main, to_delete)
        for o in to_delete:
            mesh = o.data if o.type == "MESH" else None
            bpy.data.objects.remove(o, do_unlink=True)
            if mesh and mesh.users == 0:
                bpy.data.meshes.remove(mesh, do_unlink=True)

        def remove_child_collections(c: bpy.types.Collection) -> None:
            for ch in list(c.children):
                remove_child_collections(ch)
                bpy.data.collections.remove(ch)

        remove_child_collections(main)
        bpy.data.collections.remove(main)

    for mat in list(bpy.data.materials):
        if mat.name.startswith(MAT_PREFIX):
            bpy.data.materials.remove(mat, do_unlink=True)


def _ensure_collection(name: str, parent: bpy.types.Collection | None) -> bpy.types.Collection:
    if name in bpy.data.collections:
        return bpy.data.collections[name]
    coll = bpy.data.collections.new(name)
    if parent:
        parent.children.link(coll)
    else:
        bpy.context.scene.collection.children.link(coll)
    return coll


def _make_material(
    name: str,
    base_rgb: tuple[float, float, float],
    roughness: float = 0.38,
    metallic: float = 0.0,
    emission_strength: float = 0.0,
    emission_rgb: tuple[float, float, float] | None = None,
) -> bpy.types.Material:
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    out.location = (300, 0)
    principled = nt.nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (0, 0)
    principled.inputs["Base Color"].default_value = (*base_rgb, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    principled.inputs["Metallic"].default_value = metallic

    if emission_strength > 0.0:
        em = nt.nodes.new("ShaderNodeEmission")
        em.location = (0, -200)
        em.inputs["Color"].default_value = (*(emission_rgb or base_rgb), 1.0)
        em.inputs["Strength"].default_value = emission_strength
        mix = nt.nodes.new("ShaderNodeMixShader")
        mix.location = (150, 0)
        mix.inputs["Fac"].default_value = 0.65
        nt.links.new(principled.outputs["BSDF"], mix.inputs[1])
        nt.links.new(em.outputs["Emission"], mix.inputs[2])
        nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    else:
        nt.links.new(principled.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _mesh_box(name: str, hx: float, hy: float, hz: float) -> bpy.types.Mesh:
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= hx
        v.co.y *= hy
        v.co.z *= hz
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    return mesh


def _mesh_cylinder(name: str, radius: float, depth: float, verts: int) -> bpy.types.Mesh:
    bm = bmesh.new()
    # create_cone(r1=r2) — compatible with Blenders that lack bmesh.ops.create_cylinder
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=verts,
        radius1=radius,
        radius2=radius,
        depth=depth,
    )
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    return mesh


def _mesh_uvsphere(name: str, radius: float, u: int = 12, v: int = 8) -> bpy.types.Mesh:
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=radius)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    return mesh


def _obj(
    coll: bpy.types.Collection,
    name: str,
    mesh: bpy.types.Mesh,
    location: tuple[float, float, float],
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0),
    mat: bpy.types.Material | None = None,
) -> bpy.types.Object:
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    coll.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    if mat:
        obj.data.materials.append(mat)
    return obj


def _bevel_modifier(obj: bpy.types.Object, width: float, segments: int = 3) -> None:
    mod = obj.modifiers.new(name="Bevel_Round", type="BEVEL")
    mod.affect = "EDGES"
    mod.limit_method = "ANGLE"
    mod.angle_limit = 0.698  # ~40°
    mod.width = width
    mod.segments = segments
    mod.profile = 0.7


def build() -> None:
    _remove_if_exists()

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    char_coll = _ensure_collection(COL_MAIN, None)
    export_coll = _ensure_collection(COL_EXPORT, char_coll)

    yellow = _make_material(MAT_PREFIX + "Yellow", (0.92, 0.78, 0.12), roughness=0.32)
    purple = _make_material(MAT_PREFIX + "Purple", (0.42, 0.22, 0.55), roughness=0.35)
    mid = _make_material(MAT_PREFIX + "MidBand", (0.38, 0.35, 0.44), roughness=0.45)
    black = _make_material(MAT_PREFIX + "Face", (0.02, 0.02, 0.03), roughness=0.55)
    eye = _make_material(
        MAT_PREFIX + "EyeGlow",
        (0.05, 0.05, 0.02),
        roughness=0.2,
        emission_strength=6.0,
        emission_rgb=(1.0, 0.92, 0.25),
    )
    mouth = _make_material(
        MAT_PREFIX + "MouthGlow",
        (0.9, 0.8, 0.15),
        roughness=0.4,
        emission_strength=1.5,
        emission_rgb=(1.0, 0.9, 0.2),
    )

    root = bpy.data.objects.new(PREFIX + "Root", None)
    char_coll.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.15

    z_floor = 0.0
    # --- Legs (from ground up)
    foot_h = 0.07
    shin_h = 0.16
    thigh_h = 0.14
    hip_r = 0.065
    leg_x = 0.1

    z_foot = z_floor + foot_h / 2
    z_knee = z_foot + foot_h / 2 + shin_h / 2
    z_hip = z_knee + shin_h / 2 + thigh_h / 2

    def leg(side: float):
        lr = "L" if side < 0 else "R"
        foot = _mesh_box(f"FootMesh_{lr}", 0.09, 0.12, foot_h)
        shin = _mesh_cylinder(f"ShinMesh_{lr}", 0.055, shin_h, 14)
        thigh = _mesh_cylinder(f"ThighMesh_{lr}", 0.06, thigh_h, 14)
        hip = _mesh_uvsphere(f"HipMesh_{lr}", hip_r, 10, 8)
        x = side * leg_x
        o_hip = _obj(char_coll, f"Hip_{'L' if side < 0 else 'R'}", hip, (x, 0.0, z_hip), mat=yellow)
        o_thigh = _obj(char_coll, f"Thigh_{'L' if side < 0 else 'R'}", thigh, (x, 0.0, z_hip - thigh_h / 2 - hip_r * 0.3), mat=yellow)
        o_shin = _obj(char_coll, f"Shin_{'L' if side < 0 else 'R'}", shin, (x, 0.0, z_knee), mat=yellow)
        o_foot = _obj(char_coll, f"Foot_{'L' if side < 0 else 'R'}", foot, (x, 0.02, z_foot), mat=purple)
        for o in (o_hip, o_thigh, o_shin, o_foot):
            o.parent = root
        return z_hip + hip_r

    z_pelvis_base = max(leg(-1.0), leg(1.0))

    pelvis = _mesh_cylinder("PelvisMesh", 0.14, 0.1, 20)
    o_pelvis = _obj(char_coll, "Pelvis", pelvis, (0.0, 0.0, z_pelvis_base + 0.05), mat=yellow)
    o_pelvis.parent = root

    waist_h = 0.08
    torso_h = 0.22
    z_waist = z_pelvis_base + 0.05 + 0.05 + waist_h / 2
    z_torso = z_waist + waist_h / 2 + torso_h / 2

    waist = _mesh_cylinder("WaistMesh", 0.13, waist_h, 20)
    o_waist = _obj(char_coll, "Waist", waist, (0.0, 0.0, z_waist), mat=mid)
    o_waist.parent = root

    torso = _mesh_cylinder("TorsoMesh", 0.16, torso_h, 22)
    o_torso = _obj(char_coll, "Torso", torso, (0.0, 0.0, z_torso), mat=yellow)
    o_torso.parent = root

    shoulder_y = 0.06
    upper_h = 0.11
    fore_h = 0.1
    hand_r = 0.055
    z_shoulder = z_torso + torso_h / 2 - 0.02

    def arm(side: float):
        sx = side
        lr = "L" if side < 0 else "R"
        upper = _mesh_cylinder(f"UpArmMesh_{lr}", 0.055, upper_h, 14)
        fore = _mesh_cylinder(f"ForeMesh_{lr}", 0.05, fore_h, 14)
        joint1 = _mesh_uvsphere(f"ElbowMesh_{lr}", 0.055, 10, 8)
        joint0 = _mesh_uvsphere(f"ShoulderMesh_{lr}", 0.06, 10, 8)
        hand = _mesh_uvsphere(f"HandMesh_{lr}", hand_r, 10, 8)
        x = sx * 0.22
        o_s = _obj(char_coll, f"Shoulder_{'L' if side < 0 else 'R'}", joint0, (x, shoulder_y, z_shoulder), mat=yellow)
        o_u = _obj(
            char_coll,
            f"UpperArm_{'L' if side < 0 else 'R'}",
            upper,
            (x, shoulder_y + upper_h / 2 * 0.3, z_shoulder - upper_h / 2 * 0.95),
            (math.radians(15), 0.0, math.radians(12 * sx)),
            mat=yellow,
        )
        el_z = z_shoulder - upper_h - 0.02
        o_e = _obj(char_coll, f"Elbow_{'L' if side < 0 else 'R'}", joint1, (x * 1.05, shoulder_y + 0.04, el_z), mat=yellow)
        o_f = _obj(
            char_coll,
            f"ForeArm_{'L' if side < 0 else 'R'}",
            fore,
            (x * 1.12, shoulder_y + 0.05, el_z - fore_h / 2 - 0.02),
            (math.radians(8), 0.0, math.radians(8 * sx)),
            mat=yellow,
        )
        o_h = _obj(
            char_coll,
            f"Hand_{'L' if side < 0 else 'R'}",
            hand,
            (x * 1.18, shoulder_y + 0.05, el_z - fore_h - 0.05),
            mat=purple,
        )
        for o in (o_s, o_u, o_e, o_f, o_h):
            o.parent = root

    arm(-1.0)
    arm(1.0)

    # --- Head (rounded cube)
    head_size = 0.36
    head_z = z_torso + torso_h / 2 + head_size / 2 + 0.02
    head_mesh = _mesh_box("HeadMesh", head_size / 2, head_size / 2, head_size / 2)
    o_head = _obj(char_coll, "Head", head_mesh, (0.0, 0.0, head_z), mat=yellow)
    o_head.parent = root
    _bevel_modifier(o_head, width=0.055, segments=4)

    # Face recess (black panel on -Y)
    face_mesh = _mesh_box("FaceMesh", head_size * 0.42, 0.02, head_size * 0.38)
    o_face = _obj(
        char_coll,
        "FaceScreen",
        face_mesh,
        (0.0, -head_size / 2 - 0.001, 0.0),
        mat=black,
    )
    o_face.parent = o_head

    eye_w, eye_h, eye_d = 0.07, 0.05, 0.015
    ex = head_size * 0.16
    ez_local = head_size * 0.06
    for i, sx in enumerate((-1, 1)):
        em = _mesh_box(f"EyeMesh_{i}", eye_w / 2, eye_d / 2, eye_h / 2)
        o_eye = _obj(
            char_coll,
            f"Eye_{'L' if sx < 0 else 'R'}",
            em,
            (sx * ex, -head_size / 2 - 0.012, ez_local),
            mat=eye,
        )
        o_eye.parent = o_head

    mouth_mesh = _mesh_box("MouthMesh", head_size * 0.12, 0.008, 0.012)
    o_mouth = _obj(
        char_coll,
        "Mouth",
        mouth_mesh,
        (0.0, -head_size / 2 - 0.014, -head_size * 0.1),
        mat=mouth,
    )
    o_mouth.parent = o_head

    # Link export copies (instances) — same objects, also in EXPORT for visibility
    for o in list(char_coll.objects):
        if PREFIX in o.name and o.name not in {x.name for x in export_coll.objects}:
            export_coll.objects.link(o)

    bpy.context.view_layer.objects.active = o_head
    print("Proto-Bot built: collection", COL_MAIN, "| total height ~1.05 m | apply Bevel before export if needed.")


def main() -> None:
    print("build_proto_bot: starting…")
    build()
    print("build_proto_bot: done.")


# Blender Text Editor often sets __name__ to the text block name, not "__main__".
main()
