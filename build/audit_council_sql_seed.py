#!/usr/bin/env python3
"""Validate the packaged LocalGPT Council knowledge repair seed as executable, idempotent SQLite."""
from __future__ import annotations

from pathlib import Path
import hashlib
import re
import sqlite3
import sys

ROOT = Path(__file__).resolve().parents[1]
SEED = ROOT / "docs" / "COUNCIL_KNOWLEDGE_SEED.sql"


def fail(message: str) -> None:
    print(f"Council knowledge SQL seed validation failed: {message}")
    raise SystemExit(1)


if not SEED.is_file():
    fail(f"missing {SEED.relative_to(ROOT).as_posix()}")

sql = SEED.read_text(encoding="utf-8-sig", errors="strict")
insert_marker = 'INSERT OR IGNORE INTO "CouncilKnowledgeEntries"'
if sql.count(insert_marker) < 1:
    fail("the file contains no executable idempotent CouncilKnowledgeEntries inserts")

for forbidden in ("UPDATE", "DELETE", "DROP", "ALTER", "REPLACE"):
    if re.search(rf"(?im)^\s*{forbidden}\b", sql):
        fail(f"the maintained seed contains forbidden {forbidden} SQL")

required_columns = (
    "VerificationStatus",
    "ReviewStatus",
    "LastVerifiedAtUtc",
    "StalenessReason",
    "StalenessDetectedBy",
    "SourceHash",
)
for column in required_columns:
    if f'"{column}"' not in sql:
        fail(f"the current-schema column {column} is absent from the seed")

# CouncilKnowledgeEntries is created by the current Initial migration and is not altered by
# later migrations. Mirror that table here so source validation catches missing NOT NULL fields
# without requiring a running LocalGPT instance or mutating a user's database.
connection = sqlite3.connect(":memory:")
try:
    connection.executescript(
        '''
        CREATE TABLE "CouncilKnowledgeEntries" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "CreatedAtUtc" TEXT NOT NULL,
            "UpdatedAtUtc" TEXT NOT NULL,
            "Topic" TEXT NOT NULL,
            "Scope" TEXT NOT NULL,
            "Content" TEXT NOT NULL,
            "Source" TEXT NOT NULL,
            "HelpfulSources" TEXT NOT NULL,
            "Tags" TEXT NOT NULL,
            "Confidence" INTEGER NOT NULL,
            "VerificationStatus" TEXT NOT NULL,
            "ReviewStatus" TEXT NOT NULL,
            "ExpiresAtUtc" TEXT NULL,
            "LastVerifiedAtUtc" TEXT NULL,
            "LastUsedAtUtc" TEXT NULL,
            "SupersededByKnowledgeId" TEXT NULL,
            "StalenessReason" TEXT NOT NULL,
            "StalenessDetectedAtUtc" TEXT NULL,
            "StalenessDetectedBy" TEXT NOT NULL,
            "SourceHash" TEXT NOT NULL,
            "SourceDateUtc" TEXT NULL,
            "IsUserApproved" INTEGER NOT NULL,
            "IsPinned" INTEGER NOT NULL,
            "IsArchived" INTEGER NOT NULL
        );
        '''
    )
    connection.executescript(sql)
    rows = connection.execute(
        'SELECT "Id", "Topic", "Scope", "Content", "Source", "HelpfulSources", "SourceHash" '
        'FROM "CouncilKnowledgeEntries" ORDER BY rowid'
    ).fetchall()
    if len(rows) != 60:
        fail(f"expected the 60 supplied historical rows, but the script inserted {len(rows)}")

    for row_id, topic, scope, content, source, helpful_sources, source_hash in rows:
        expected_hash = hashlib.sha256(
            f"{topic}\n{scope}\n{source}\n{helpful_sources}\n{content}".encode("utf-8")
        ).hexdigest().upper()
        if source_hash != expected_hash:
            fail(f"SourceHash does not match LocalGPT's knowledge hash contract for {row_id}")

    connection.executescript(sql)
    second_count = connection.execute('SELECT COUNT(*) FROM "CouncilKnowledgeEntries"').fetchone()[0]
    if second_count != len(rows):
        fail("a second execution changed the row count; the repair seed is not idempotent")
finally:
    connection.close()

print("Council knowledge SQL seed validation passed: 60 executable current-schema INSERT OR IGNORE rows; deterministic source hashes and second-run idempotency verified.")
