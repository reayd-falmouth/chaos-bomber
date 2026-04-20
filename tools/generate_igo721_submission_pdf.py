#!/usr/bin/env python3
"""
Generate IGO721 submission PDF (tables). Usage: python tools/generate_igo721_submission_pdf.py

Control scheme, known bugs / issues, third-party assets, and Generative AI disclosure are omitted here;
see README.md and DavidReay_IGO721_README.pdf for those.
"""

from __future__ import annotations

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


class SubmissionPDF(FPDF):
    def footer(self) -> None:
        self.set_y(-12)
        self.set_font("Helvetica", "I", 8)
        self.set_text_color(100, 100, 100)
        self.cell(0, 8, f"Page {self.page_no()}/{{nb}}", align="C")
        self.set_text_color(0, 0, 0)


def mc(pdf: FPDF, w: float, h: float, text: str) -> None:
    pdf.multi_cell(w, h, text, new_x=XPos.LMARGIN, new_y=YPos.NEXT)


def heading(pdf: FPDF, text: str, size: int = 14, gap: float = 1.0) -> None:
    pdf.ln(gap)
    pdf.set_font("Helvetica", "B", size)
    mc(pdf, 0, 6, text)
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


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    out_path = root / OUTPUT_NAME

    pdf = SubmissionPDF()
    pdf.set_auto_page_break(auto=True, margin=14)
    pdf.alias_nb_pages()
    pdf.set_margins(14, 14, 14)
    pdf.add_page()

    # --- Title + all sections on minimal pages (no forced page breaks) ---
    pdf.set_font("Helvetica", "B", 16)
    mc(pdf, 0, 7, "IGO721 - Game Development")
    pdf.set_font("Helvetica", "", 10)
    pdf.ln(0.5)
    mc(pdf, 0, 5, "Indie Game Prototype (AR Artefact) - MasterBlaster submission document")
    pdf.ln(2)

    pdf.set_font("Helvetica", "", 9)
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
        line_height=4.5,
        padding=1.5,
    )

    heading(pdf, "Part 1 - Pitch video (40%)", 12, gap=1.5)
    pdf.set_font("Helvetica", "", 8)
    data_table(
        pdf,
        (0.22, 0.78),
        [
            ["Deliverable", "URL"],
            [
                "Panopto",
                {
                    "text": PANOPTO_PITCH_VIDEO_URL,
                    "link": PANOPTO_PITCH_VIDEO_URL,
                },
            ],
        ],
        line_height=4,
        padding=1.5,
    )

    heading(pdf, "Part 2 - Playable prototype and readme (60%)", 12, gap=1.5)
    pdf.set_font("Helvetica", "", 8)
    data_table(
        pdf,
        (0.28, 0.72),
        [
            ["Property", "Details"],
            ["Prototype", "MasterBlaster - Bomberman-style arena + FPS hybrid in one session"],
            ["Engine", "Unity 6000.3.9f1, URP 17.3.0"],
            ["Primary scene", "Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity"],
            [
                "Bindings",
                "Input System (arena/menu); legacy Input Manager (FPS) - full tables in README.md / DavidReay_IGO721_README.pdf",
            ],
            [
                "Known bugs / issues",
                "See README.md / DavidReay_IGO721_README.pdf",
            ],
        ],
        line_height=4,
        padding=1.5,
    )

    heading(pdf, "Submission links", 11, gap=1.5)
    pdf.set_font("Helvetica", "", 8)
    pdf.set_text_color(0, 0, 180)
    data_table(
        pdf,
        (0.26, 0.74),
        [
            ["Deliverable", "URL"],
            ["Google Drive folder (pitch video + prototype)", GOOGLE_DRIVE_FOLDER],
            ["Project source (GitHub)", GITHUB_SOURCE],
            [
                "README coursework PDF",
                "DavidReay_IGO721_README.pdf",
            ],
        ],
        line_height=4,
        padding=1.5,
    )
    pdf.set_text_color(0, 0, 0)

    pdf.output(str(out_path))
    print(f"Wrote: {out_path}")


if __name__ == "__main__":
    main()
