#!/usr/bin/env python3
"""Enriches XML documentation for maintained C# and Razor component declarations."""
from __future__ import annotations
import argparse
from pathlib import Path
from xml_documentation import run as run_csharp
from razor_xml_documentation import run as run_razor

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('root', type=Path)
    args = parser.parse_args()
    razor_exit = run_razor(args.root, 'enhance')
    csharp_exit = run_csharp(args.root, 'enhance')
    raise SystemExit(razor_exit or csharp_exit)
