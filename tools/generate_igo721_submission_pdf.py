#!/usr/bin/env python3
"""
Generate IGO721 submission PDF (tables). Usage: python tools/generate_igo721_submission_pdf.py
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from fpdf import FPDF
from fpdf.enums import Align, TableBordersLayout, XPos, YPos

# Hyphens in the ?id= GUID can wrap in PDF tables; some viewers drop them on copy (breaking the link).
# Percent-encoding hyphens (%2D) keeps the string copy-safe; Panopto accepts this form.
PANOPTO_PITCH_VIDEO_URL = (
    "https://falmouth.cloud.panopto.eu/Panopto/Pages/Viewer.aspx?"
    "id=a3713fcb%2D2db8%2D4d8d%2D931e%2Db42a00eb0952"
)

OUTPUT_NAME = "DavidReay_IGO721_SubmissionNote.pdf"
STUDENT_DISPLAY_NAME = "David Reay (DR323090)"

GITHUB_SOURCE = "https://github.com/reayd-falmouth/MasterBlaster_FPS"
GOOGLE_DRIVE_FOLDER = (
    "https://drive.google.com/drive/folders/11xjCfhYJwO3qSf5NiCT2jXfPoi4jaVCt?usp=drive_link"
)

# Wide third column (same as Generative AI table); renders reliably in fpdf2 vs. tighter splits.
TABLE_3COL_WIDE_LAST = (0.18, 0.22, 0.60)


class SubmissionPDF(FPDF):
    def footer(self) -> None:
        self.set_y(-12)
        self.set_font("Helvetica", "I", 8)
        self.set_text_color(100, 100, 100)
        self.cell(0, 8, f"Page {self.page_no()}/{{nb}}", align="C")
        self.set_text_color(0, 0, 0)


def mc(pdf: FPDF, w: float, h: float, text: str) -> None:
    pdf.multi_cell(w, h, text, new_x=XPos.LMARGIN, new_y=YPos.NEXT)


def heading(pdf: FPDF, text: str, size: int = 14) -> None:
    pdf.ln(2)
    pdf.set_font("Helvetica", "B", size)
    mc(pdf, 0, 7, text)
    pdf.set_font("Helvetica", "", 10)


def data_table(
    pdf: FPDF,
    col_widths: tuple[float, ...],
    rows: list[list[str | dict[str, Any]]],
    *,
    line_height: float = 6,
    padding: float = 2.5,
    first_row_as_headings: bool = True,
    text_align: str | Align = "LEFT",
) -> None:
    """Draw a bordered table. Cells may be str or dicts passed to Row.cell() (e.g. link=)."""
    with pdf.table(
        col_widths=col_widths,
        line_height=line_height,
        padding=padding,
        text_align=text_align,
        first_row_as_headings=first_row_as_headings,
        borders_layout=TableBordersLayout.ALL,
        align=Align.L,
        width=pdf.epw,
    ) as table:
        for row in rows:
            table.row(row)


def load_manifest_packages(project_root: Path) -> list[tuple[str, str]]:
    manifest_path = project_root / "Packages" / "manifest.json"
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    deps = data.get("dependencies", {})
    out = [(k, v) for k, v in sorted(deps.items()) if not k.startswith("com.unity.modules.")]
    return out


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    out_path = root / OUTPUT_NAME
    packages = load_manifest_packages(root)

    pdf = SubmissionPDF()
    pdf.set_auto_page_break(auto=True, margin=16)
    pdf.alias_nb_pages()
    pdf.set_margins(18, 18, 18)
    pdf.add_page()

    # --- Cover ---
    pdf.set_font("Helvetica", "B", 20)
    mc(pdf, 0, 9, "IGO721 - Game Development")
    pdf.set_font("Helvetica", "", 12)
    pdf.ln(2)
    mc(pdf, 0, 6, "Indie Game Prototype (AR Artefact)")
    pdf.ln(2)
    pdf.set_font("Helvetica", "B", 14)
    mc(pdf, 0, 7, "MasterBlaster - Submission document")
    pdf.ln(4)

    pdf.set_font("Helvetica", "", 10)
    data_table(
        pdf,
        (0.34, 0.66),
        [
            ["Field", "Value"],
            ["Student", STUDENT_DISPLAY_NAME],
            ["Module", "IGO721 - Game Development"],
            ["Institution", "Falmouth University"],
            ["Project", "MasterBlaster (Unity FPS Microgame + hybrid arena/FPS)"],
        ],
    )

    # --- Part 1 ---
    pdf.add_page()
    heading(pdf, "Part 1 - Pitch video (40%)", 14)
    data_table(
        pdf,
        (0.28, 0.72),
        [
            ["Deliverable", "URL"],
            [
                "Panopto",
                {
                    # Visible text + PDF link annotation (same URI; avoids broken copy from hyphen wraps)
                    "text": PANOPTO_PITCH_VIDEO_URL,
                    "link": PANOPTO_PITCH_VIDEO_URL,
                },
            ],
        ],
        line_height=5,
    )

    # --- Part 2 ---
    pdf.add_page()
    heading(pdf, "Part 2 - Playable prototype and readme (60%)", 14)
    data_table(
        pdf,
        (0.32, 0.68),
        [
            ["Property", "Details"],
            ["Prototype", "MasterBlaster - Bomberman-style arena + FPS hybrid in one session"],
            ["Engine", "Unity 6000.3.9f1, URP 17.3.0"],
            ["Primary scene", "Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity"],
            ["Bindings", "Input System (arena/menu); legacy Input Manager (FPS) - see control tables"],
        ],
    )
    pdf.ln(3)
    heading(pdf, "Submission links", 12)
    pdf.set_font("Helvetica", "", 9)
    pdf.set_text_color(0, 0, 180)
    data_table(
        pdf,
        (0.30, 0.70),
        [
            ["Deliverable", "URL"],
            ["Google Drive folder (pitch video + prototype)", GOOGLE_DRIVE_FOLDER],
            ["Project source (GitHub)", GITHUB_SOURCE],
        ],
    )
    pdf.set_text_color(0, 0, 0)

    # --- Controls ---
    pdf.add_page()
    heading(pdf, "Readme - Control scheme", 14)
    pdf.set_font("Helvetica", "", 9)
    mc(
        pdf,
        0,
        4,
        "PlayerControls.inputactions (Input System) for menus and arena; ProjectSettings/InputManager.asset for FPS.",
    )
    pdf.ln(2)

    heading(pdf, "Menus", 11)
    data_table(
        pdf,
        (0.28, 0.36, 0.36),
        [
            ["Action", "Keyboard", "Gamepad"],
            ["Navigate", "Arrow keys or WASD", "Left stick or D-pad"],
            ["Confirm / advance", "Space", "South face button (A / Cross)"],
        ],
    )
    pdf.ln(2)

    heading(pdf, "Arena (Bomberman / top-down)", 11)
    data_table(
        pdf,
        (0.28, 0.36, 0.36),
        [
            ["Action", "Keyboard", "Gamepad"],
            ["Move", "Arrow keys or WASD", "Left stick or D-pad"],
            ["Place bomb", "Space", "South face button"],
            ["Switch arena to FPS", "Tab", "North face button"],
            ["Pause", "Esc or P", "Start"],
        ],
    )
    pdf.ln(2)

    heading(pdf, "FPS mode (legacy Input Manager)", 11)
    data_table(
        pdf,
        TABLE_3COL_WIDE_LAST,
        [
            ["Action", "Keyboard / mouse", "Notes"],
            ["Move", "WASD", "Horizontal and Vertical axes"],
            ["Look", "Mouse", "Mouse X and Mouse Y"],
            ["Fire", "Left mouse", ""],
            ["Aim", "Right mouse (hold)", ""],
            ["Sprint", "Left Shift", "Gamepad where configured"],
            ["Jump", "Space", "FPS only; arena uses Space for bomb"],
            ["Crouch", "C", ""],
            ["Reload", "R", ""],
            ["Weapons", "Q / E, mouse wheel", "See Input Manager"],
            ["Pause / menu", "Tab, P", "Gamepad: Input Manager"],
            ["UI Submit / Cancel", "Enter / Esc", ""],
        ],
        line_height=7,
        padding=3,
    )

    # --- Bugs ---
    pdf.add_page()
    heading(pdf, "Readme - Known bugs / issues", 14)
    data_table(
        pdf,
        (0.22, 0.78),
        [
            ["ID", "Description"],
            [
                "1",
                "Alternate normal level: disabling Normal Level does not fully apply alternate map settings "
                "(LoadAlternateLevelSettings in GameManager.cs is a stub).",
            ],
            [
                "2",
                "Credits / continue: multiple ContinueOnAnyInput instances may advance two steps on one keypress.",
            ],
            [
                "3",
                "Multiplayer: not completed - Netcode / multiplayer-related packages do not result in a working networked mode in this prototype.",
            ],
            [
                "4",
                "Title screen: asteroid particle effect is too large / overpowering.",
            ],
            [
                "5",
                "Load time: long delay after the countdown before gameplay starts.",
            ],
            [
                "6",
                "Optional packages (Netcode, ML-Agents, Multiplayer Services, Unity MCP) are not required for a local single-player build.",
            ],
        ],
    )

    # --- Third party: packages ---
    pdf.add_page()
    heading(pdf, "Third-party assets - Unity Package Manager", 13)
    pdf.set_font("Helvetica", "", 8)
    pkg_rows: list[list[str]] = [["Package", "Version / source"]]
    for name, ver in packages:
        pkg_rows.append([name, ver])
    data_table(
        pdf,
        (0.52, 0.48),
        pkg_rows,
        line_height=4.8,
        padding=1.8,
        first_row_as_headings=True,
    )
    pdf.set_font("Helvetica", "", 10)

    pdf.add_page()
    heading(pdf, "Third-party assets - Repository content", 13)
    data_table(
        pdf,
        (0.30, 0.70),
        [
            ["Asset / library", "Location / note"],
            [
                "Unity FPS Microgame",
                "Assets/App/FPS - see FPSMicrogame_README.txt, Third-PartyNotice.txt (fonts: Roboto, EmojiOne, LiberationSans, etc.)",
            ],
            [
                "NavMesh Components",
                "Assets/App/NavMeshComponents (LICENSE) and Assets/Scripts/NavMeshComponents",
            ],
        ],
    )
    pdf.ln(2)
    heading(pdf, "Assets/3rdParty (vendor, gitignored / CI)", 12)
    data_table(
        pdf,
        (0.22, 0.78),
        [
            ["Vendor folder", "Description"],
            ["DAVFX", "Realistic 6D Lighting Explosions (VFX)"],
            ["Feel", "More Mountains Feel (juice / feedback)"],
            ["ithappy", "Third-party art pack"],
            ["Nebula Skyboxes", "Skybox assets"],
            ["ParallelCascades", "Third-party assets"],
            ["PicaVoxel", "Voxel tools / content"],
            ["PlayFabSDK", "PlayFab SDK"],
            ["SpriteExporter", "Editor tooling"],
            ["TextMesh Pro", "TMP resources (may overlap package)"],
            ["Universal Sound FX", "Audio library"],
            ["VolFx", "Volume / post-style effects"],
        ],
    )

    # --- AI ---
    pdf.add_page()
    heading(pdf, "Generative AI disclosure", 14)
    data_table(
        pdf,
        TABLE_3COL_WIDE_LAST,
        [
            ["Tool", "Purpose", "What was incorporated"],
            [
                "Cursor",
                "Coding assistance",
                "AI-assisted C#/Unity suggestions (generation, edits, refactors, debugging); reviewed and integrated where appropriate.",
            ],
            [
                "Google Gemini",
                "Concept art",
                "Generative concept imagery for visual development; selected or adapted as reference for art direction.",
            ],
        ],
    )

    pdf.output(str(out_path))
    print(f"Wrote: {out_path}")


if __name__ == "__main__":
    main()
