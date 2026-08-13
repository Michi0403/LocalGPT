#!/usr/bin/env python3
"""Validates comprehensive and contextual XML documentation for maintained C# source."""
from __future__ import annotations
import argparse
from pathlib import Path
from xml_documentation import run
if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('root', type=Path)
    args=parser.parse_args()
    raise SystemExit(run(args.root, 'validate'))
