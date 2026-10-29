#!/usr/bin/env python3
"""Adds a published release to the change log (src/web/CHANGELOG.md).

Reads the release from environment variables, because release notes are untrusted text and must never be
interpolated into a shell command:

    RELEASE_TAG   the release tag, for example v1.2.19 or 1.2.19
    RELEASE_DATE  the release date, yyyy-MM-dd
    RELEASE_BODY  the release notes (Markdown), may be empty
    CHANGELOG     path to the change log, defaults to src/web/CHANGELOG.md

The entry format is described in the header comment of CHANGELOG.md and read by Notary.Changelog.ChangelogParser.
Prints "added" or "unchanged" so the workflow can tell whether there is anything to commit.
"""
import os
import re
import sys

ENTRY_HEADING = re.compile(r"^## (v\d+\.\d+\.\d+) - \d{4}-\d{2}-\d{2}\s*$")
VERSION = re.compile(r"^v\d+\.\d+\.\d+$")
DATE = re.compile(r"^\d{4}-\d{2}-\d{2}$")


def clean(body: str) -> str:
    lines = []
    for line in body.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        if re.match(r"^\s*\*\*Full [Cc]hangelog\*\*", line):
            continue
        if re.match(r"^# v\d", line):
            continue
        line = re.sub(r"\s+by @[\w-]+ in https://github\.com/[^/\s]+/[^/\s]+/pull/(\d+)", r" (#\1)", line)
        heading = re.match(r"^\s{0,3}(#{1,6})\s+(.*)$", line)
        if heading:
            # Level 4 or deeper, so a release note can never start an entry of its own
            level = max(len(heading.group(1)), 4)
            line = "#" * level + " " + heading.group(2)
        lines.append(line)
    text = re.sub(r"\n{3,}", "\n\n", "\n".join(lines)).strip()
    return text if text else "_No release notes._"


def add_entry(text: str, version: str, date: str, notes: str) -> str | None:
    lines = text.replace("\r\n", "\n").split("\n")
    insert_at = None
    for index, line in enumerate(lines):
        match = ENTRY_HEADING.match(line)
        if match:
            if match.group(1) == version:
                return None
            if insert_at is None:
                insert_at = index
    entry = [f"## {version} - {date}", "", notes, ""]
    if insert_at is None:
        while lines and lines[-1] == "":
            lines.pop()
        lines += [""] + entry
    else:
        lines[insert_at:insert_at] = entry
    return "\n".join(lines).rstrip("\n") + "\n"


def main() -> int:
    tag = os.environ.get("RELEASE_TAG", "").strip()
    date = os.environ.get("RELEASE_DATE", "").strip()
    body = os.environ.get("RELEASE_BODY", "")
    path = os.environ.get("CHANGELOG", "src/web/CHANGELOG.md")

    version = tag if tag.startswith("v") else "v" + tag
    if not VERSION.match(version):
        print(f"Release tag '{tag}' is not a version such as v1.2.19.", file=sys.stderr)
        return 1
    if not DATE.match(date):
        print(f"Release date '{date}' is not yyyy-MM-dd.", file=sys.stderr)
        return 1

    with open(path, encoding="utf-8", newline="") as file:
        original = file.read()

    updated = add_entry(original, version, date, clean(body))
    if updated is None:
        print("unchanged")
        return 0

    with open(path, "w", encoding="utf-8", newline="\n") as file:
        file.write(updated)
    print("added")
    return 0


if __name__ == "__main__":
    sys.exit(main())
