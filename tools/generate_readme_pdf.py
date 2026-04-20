#!/usr/bin/env python3
"""
Render README.md to a PDF (Markdown tables, headings, lists, code).

Dependencies:
  pip install markdown xhtml2pdf beautifulsoup4

Usage (from project root):
  python tools/generate_readme_pdf.py

Before rendering, injects REFERENCES.md into README.md between
<!-- REFERENCES_START --> and <!-- REFERENCES_END --> (see tools/readme_reference_sync.py).

The PDF omits the "## Android CI AAB builds" section (coursework PDF only; README.md is unchanged).

Output: DavidReay_IGO721_README.pdf next to README.md
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

import markdown
from xhtml2pdf import pisa


def _inject_table_colwidths(html: str) -> str:
    """xhtml2pdf often ignores colgroup; set width on each cell + colgroup for 3-col tables."""
    try:
        from bs4 import BeautifulSoup
    except ImportError:
        return html

    soup = BeautifulSoup(html, "html.parser")
    for table in soup.find_all("table"):
        first_row = None
        thead = table.find("thead")
        if thead:
            first_row = thead.find("tr")
        if not first_row:
            first_row = table.find("tr")
        if not first_row:
            continue
        headers = first_row.find_all("th") or first_row.find_all("td")
        n = len(headers)
        if n == 3:
            widths = ("16%", "30%", "54%")
        elif n == 2:
            widths = ("28%", "72%")
        else:
            continue
        colgroup = soup.new_tag("colgroup")
        for w in widths:
            col = soup.new_tag("col")
            col["width"] = w
            col["style"] = f"width:{w}"
            colgroup.append(col)
        table.insert(0, colgroup)
        # Per-cell width helps xhtml2pdf more than colgroup alone
        for tr in table.find_all("tr"):
            cells = tr.find_all(["th", "td"])
            if len(cells) != n:
                continue
            for cell, w in zip(cells, widths):
                prev = cell.get("style") or ""
                cell["style"] = (prev + f" width:{w}; max-width:{w};").strip()
    return str(soup)


def _strip_remote_images(md: str) -> str:
    """Remote badge/images break offline PDF generation; replace with alt text."""
    return re.sub(r"!\[([^\]]*)\]\([^)]+\)", r"*\1*", md)


def _strip_pdf_only_sections(md: str) -> str:
    """Remove README blocks that should not appear in the coursework PDF."""
    md = md.replace("\r\n", "\n")
    pattern = re.compile(
        r"\n## Android CI AAB builds\n.*?\n---\s*\n+(?=## Coursework deliverables)",
        re.DOTALL,
    )
    return pattern.sub("\n", md, count=1)


def _build_html_document(body_html: str) -> str:
    return f"""<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8"/>
<style>
  @page {{ size: A4; margin: 18mm; }}
  body {{
    font-family: Helvetica, Arial, sans-serif;
    font-size: 10pt;
    line-height: 1.35;
    color: #111;
  }}
  h1 {{ font-size: 16pt; margin: 0.6em 0 0.3em; border-bottom: 1px solid #ccc; }}
  h2 {{ font-size: 13pt; margin: 0.8em 0 0.35em; }}
  h3 {{ font-size: 11pt; margin: 0.7em 0 0.3em; }}
  h4 {{ font-size: 10pt; margin: 0.6em 0 0.2em; }}
  p {{ margin: 0.35em 0; }}
  ul, ol {{ margin: 0.35em 0 0.35em 1.2em; padding: 0; }}
  li {{ margin: 0.15em 0; }}
  code, pre {{
    font-family: Consolas, "Liberation Mono", Courier, monospace;
    font-size: 9pt;
  }}
  pre {{
    background: #f5f5f5;
    border: 1px solid #ddd;
    padding: 8px;
    overflow-wrap: anywhere;
  }}
  table {{
    border-collapse: collapse;
    table-layout: fixed;
    width: 100%;
    margin: 0.75em 0;
    font-size: 9pt;
    page-break-inside: auto;
  }}
  th, td {{
    border: 1px solid #444;
    padding: 4px 6px;
    vertical-align: top;
    word-wrap: break-word;
    overflow-wrap: break-word;
    box-sizing: border-box;
  }}
  th {{ background: #eee; }}
  blockquote {{
    margin: 0.4em 0 0.6em 0;
    padding: 0.35em 0.75em;
    border-left: 3px solid #ccc;
    color: #333;
    font-size: 9.5pt;
  }}
  hr {{ border: none; border-top: 1px solid #ccc; margin: 1em 0; }}
  a {{ color: #0645ad; }}
</style>
</head>
<body>
{body_html}
</body>
</html>
"""


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    readme = root / "README.md"
    out = root / "DavidReay_IGO721_README.pdf"

    if not readme.is_file():
        print(f"Missing {readme}", file=sys.stderr)
        return 1

    tools_dir = Path(__file__).resolve().parent
    if str(tools_dir) not in sys.path:
        sys.path.insert(0, str(tools_dir))
    try:
        from readme_reference_sync import sync_readme_references
    except ImportError as e:
        print(f"Could not import readme_reference_sync: {e}", file=sys.stderr)
        return 1
    try:
        if sync_readme_references(root):
            print("Synced REFERENCES.md into README.md")
    except Exception as e:
        print(f"Reference sync failed: {e}", file=sys.stderr)
        return 1

    raw = readme.read_text(encoding="utf-8")
    raw = _strip_pdf_only_sections(raw)
    raw = _strip_remote_images(raw)

    # nl2br can confuse xhtml2pdf table layout; keep hard line breaks out of tables
    body = markdown.markdown(
        raw,
        extensions=[
            "markdown.extensions.extra",
            "markdown.extensions.sane_lists",
        ],
    )
    body = _inject_table_colwidths(body)
    html = _build_html_document(body)

    with open(out, "w+b") as dest:
        status = pisa.CreatePDF(
            src=html,
            dest=dest,
            encoding="utf-8",
            link_callback=None,
        )
    if status.err:
        print(f"xhtml2pdf reported errors; check {out}", file=sys.stderr)
        return 1

    print(f"Wrote: {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
