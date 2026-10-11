#!/usr/bin/env python3
"""Source-only regression audit for the restartable documentation PDF merge contract."""

from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


root = Path(__file__).resolve().parents[1]
errors = []


def read(path):
    return (root / path).read_text(encoding="utf-8-sig")


script = read("build/Build-Documentation.ps1")
packaging = read("src/LocalGPT.ReleasePackaging/Program.cs")
documentation_function = script.split("function Invoke-LocalGptChunkedBrowserPdf {", 1)[-1].split(
    "function Resolve-LocalGptHomebrew {", 1
)[0]
pdf_merge = packaging.split("private static void MergePdf(", 1)[-1].split(
    "private static void OptimizePdf(", 1
)[0]

for marker in (
    "$chunkPdfs.Add($frontMatterPdf)",
    "$chunkPdfs.Add($chunkPdf)",
    "[IO.File]::WriteAllLines($mergeInputListPath, [string[]]$chunkPdfs.ToArray(), [Text.UTF8Encoding]::new($false))",
    "$mergeArguments = @('pdf-merge', '--output', $PdfPath, '--input-list', $mergeInputListPath)",
    "@(& $PackagingTool @mergeArguments 2>&1)",
    "finally { Remove-LocalGptTemporaryPath -Path $chunkRoot }",
    "Durable chunks were retained for the next attempt.",
):
    if marker not in documentation_function:
        errors.append(f"Documentation chunk merge missing: {marker}")

if re.search(r"\$mergeArguments\.Add\('--input'", documentation_function):
    errors.append("PDF merge still expands every absolute chunk path onto the Windows command line")

for marker in (
    'var directInputs = Multi(values, "input");',
    'var inputLists = Multi(values, "input-list");',
    'File.ReadAllLines(Path.GetFullPath(inputLists[0]), Encoding.UTF8)',
    'directInputs.Count > 0 && inputLists.Count > 0',
    'inputPaths.Any(string.IsNullOrWhiteSpace)',
    'var inputs = inputPaths.Select(Path.GetFullPath).ToArray();',
    'using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import);',
    'if (!LooksLikePdf(temporary))',
    'CommitTemporaryFile(temporary, output);',
):
    if marker not in pdf_merge:
        errors.append(f"Release PDF merger missing: {marker}")

version_file = ET.parse(root / "src/LocalGPT/LocalGPT.csproj")
version = version_file.findtext("./PropertyGroup/Version")
if not version or not re.fullmatch(r"\d+\.[0-9]\.[0-9]", version):
    errors.append(f"Invalid single-digit minor/patch version: {version}")
elif tuple(map(int, version.split("."))) < (5, 5, 0):
    errors.append(f"Documentation merge repair requires LocalGPT 5.5.0 or later, found {version}")

if errors:
    print("LocalGPT documentation PDF merge source audit failed:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("LocalGPT documentation PDF merge source contract passed: ordered UTF-8 manifest, bounded invocation, existing PDF validation and durable recovery preserved.")
