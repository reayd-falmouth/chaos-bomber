#!/usr/bin/env python3
"""
Inject REFERENCES.md into README.md between <!-- REFERENCES_START --> and <!-- REFERENCES_END -->.

Usage (from project root):
  python tools/sync_readme_references.py
"""

from __future__ import annotations

import sys
from pathlib import Path

from readme_reference_sync import sync_readme_references


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        changed = sync_readme_references(root)
    except Exception as e:
        print(e, file=sys.stderr)
        return 1
    print("README.md updated from REFERENCES.md." if changed else "README.md already matched REFERENCES.md.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
