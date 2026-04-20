"""Inject REFERENCES.md into README.md between HTML comment markers."""

from __future__ import annotations

from pathlib import Path

REF_START = "<!-- REFERENCES_START -->"
REF_END = "<!-- REFERENCES_END -->"


def sync_readme_references(root: Path) -> bool:
    """
    Replace the region between REF_START and REF_END (exclusive of the markers)
    with the contents of REFERENCES.md (normalized trailing newline).

    Returns True if README.md was modified on disk.
    Raises FileNotFoundError, ValueError on invalid inputs.
    """
    ref_path = root / "REFERENCES.md"
    readme_path = root / "README.md"

    if not ref_path.is_file():
        raise FileNotFoundError(f"Missing bibliography file: {ref_path}")
    if not readme_path.is_file():
        raise FileNotFoundError(f"Missing README: {readme_path}")

    refs = ref_path.read_text(encoding="utf-8").replace("\r\n", "\n").rstrip() + "\n"
    readme = readme_path.read_text(encoding="utf-8").replace("\r\n", "\n")

    if REF_START not in readme:
        raise ValueError(f"README.md must contain {REF_START!r}")
    if REF_END not in readme:
        raise ValueError(f"README.md must contain {REF_END!r}")

    i0 = readme.index(REF_START) + len(REF_START)
    i1 = readme.index(REF_END)
    if i1 < i0:
        raise ValueError("REFERENCE markers are out of order in README.md")

    new_readme = readme[:i0] + "\n" + refs + readme[i1:]
    if new_readme == readme:
        return False

    readme_path.write_text(new_readme, encoding="utf-8", newline="\n")
    return True
