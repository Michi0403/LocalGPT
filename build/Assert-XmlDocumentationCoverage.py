#!/usr/bin/env python3
"""Validates comprehensive, contextual XML documentation for maintained C# and Razor source."""
from __future__ import annotations
import argparse
from pathlib import Path
from xml_documentation import run as run_csharp
from razor_xml_documentation import run as run_razor

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('root', type=Path)
    args = parser.parse_args()
    csharp_exit = run_csharp(args.root, 'validate')
    razor_exit = run_razor(args.root, 'validate')
    raise SystemExit(csharp_exit or razor_exit)
