#!/usr/bin/env python3
"""Verify the Release AAB contains the web UI, its CSS dependencies, and AI assets."""

import argparse
from html.parser import HTMLParser
from pathlib import Path
import posixpath
import re
import sys
from urllib.parse import unquote, urlsplit
from zipfile import BadZipFile, ZipFile


class HostReferences(HTMLParser):
    def __init__(self):
        super().__init__()
        self.urls = []

    def handle_starttag(self, tag, attributes):
        attributes = dict(attributes)
        if tag == "script" and attributes.get("src"):
            self.urls.append(attributes["src"])
        if tag == "link" and "stylesheet" in attributes.get("rel", "").split():
            self.urls.append(attributes.get("href", ""))


CSS_URLS = re.compile(
    r'''url\(\s*(?:"([^"]*)"|'([^']*)'|([^\s)]+))\s*\)|@import\s+["']([^"']+)["']''',
    re.IGNORECASE,
)
WEB_ROOT = "base/assets/wwwroot/"
REQUIRED = [
    "base/assets/onnx/arcfaceresnet100-11-int8.onnx",
    "base/assets/onnx/scrfd_2.5g_kps.onnx",
    "base/assets/onnx/open_closed_eye.onnx",
    WEB_ROOT + "media/portraits/Barack_Obama_01.jpg",
    WEB_ROOT + "media/groups/Obama_and_Biden.jpg",
]


def web_path(url, parent="index.html"):
    parts = urlsplit(url.strip())
    if parts.scheme or parts.netloc or not parts.path:
        return None  # External URLs, data URLs, and fragment-only references.
    return posixpath.normpath(posixpath.join("/", posixpath.dirname(parent), unquote(parts.path))).lstrip("/")


def verify(bundle):
    errors, checked = [], set()
    with ZipFile(bundle) as archive:
        entries = {entry.filename: entry for entry in archive.infolist()}

        def require(path, owner):
            if path in checked:
                return path in entries and entries[path].file_size > 0
            checked.add(path)
            if path not in entries or entries[path].file_size == 0:
                errors.append(f"Missing or empty: {path} (required by {owner})")
                return False
            return True

        for path in REQUIRED:
            require(path, "face recognition / example gallery")
        host = HostReferences()
        source = Path(__file__).resolve().parents[1] / "src/BlazorFace.Maui/wwwroot/index.html"
        host.feed(source.read_text(encoding="utf-8"))
        if require(WEB_ROOT + "index.html", "BlazorWebView host"):
            host.feed(archive.read(WEB_ROOT + "index.html").decode("utf-8"))
        pending = [(url, "index.html") for url in host.urls]
        parsed_css = set()
        while pending:
            url, parent = pending.pop()
            path = web_path(url, parent)
            if path is None or not require(WEB_ROOT + path, parent):
                continue
            if path.lower().endswith(".css") and path not in parsed_css:
                parsed_css.add(path)
                css = archive.read(WEB_ROOT + path).decode("utf-8-sig")
                css = re.sub(r"/\*.*?\*/", "", css, flags=re.DOTALL)
                pending.extend((next(value for value in match if value), path)
                               for match in CSS_URLS.findall(css) if any(match))
    if errors:
        print("Android asset verification failed:\n" + "\n".join(sorted(errors)), file=sys.stderr)
        return 1
    print(f"Verified {len(checked)} packaged assets, including {len(parsed_css)} CSS files and their dependencies.")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bundle", type=Path, help="Path to the Release .aab (signed or unsigned)")
    try:
        sys.exit(verify(parser.parse_args().bundle))
    except (OSError, BadZipFile, UnicodeError) as error:
        sys.exit(f"Cannot verify Android assets: {error}")
