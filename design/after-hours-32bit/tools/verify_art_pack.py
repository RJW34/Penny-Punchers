"""Read-only art validation; --refresh writes catalogs only inside this art pack.

Uses only the Python standard library. Never changes or draws raster images.
"""
from pathlib import Path
import argparse
import csv
from datetime import datetime, timezone
import hashlib
import json
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parents[1]
SELECTED = [f"fighters/{f}/{s}.png" for f in ("rook", "vale")
            for s in ("identity", "universal", "normals", "techniques", "supplemental")]
SELECTED += ["stages/foundry.png", "stages/grid.png", "stages/environment-atlas.png",
             "effects/combat-atlas.png"]
SELECTED += [f"ui/{s}.png" for s in ("title-backdrop", "hud-atlas", "interface-atlas",
              "lease-icons", "menu-designs", "typography", "branding-announcements", "system-designs")]

def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def png_info(path):
    data = path.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", f"Invalid PNG: {path}"
    offset, pixels, chunks, header = 8, bytearray(), [], None
    while offset < len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + length]
        assert len(payload) == length, f"Truncated chunk: {path}"
        crc = struct.unpack_from(">I", data, offset + 8 + length)[0]
        assert zlib.crc32(kind + payload) & 0xffffffff == crc, f"PNG CRC: {path} {kind}"
        chunks.append(kind.decode("ascii"))
        if kind == b"IHDR":
            header = struct.unpack(">IIBBBBB", payload)
        elif kind == b"IDAT":
            pixels.extend(payload)
        offset += length + 12
        if kind == b"IEND":
            break
    assert header and chunks[-1] == "IEND", f"Incomplete PNG: {path}"
    width, height, bits, color, compression, filtering, interlace = header
    assert bits == 8 and color in (2, 6) and interlace == 0, f"Unexpected format: {path}"
    channels = 4 if color == 6 else 3
    decoded = zlib.decompress(pixels)
    row_bytes = width * channels + 1
    assert len(decoded) == row_bytes * height, f"Incorrect PNG payload: {path}"
    assert all(decoded[y * row_bytes] <= 4 for y in range(height)), f"PNG filters: {path}"
    return {"width": width, "height": height, "format": "RGBA" if color == 6 else "RGB",
            "alpha_channel": color == 6, "bit_depth_per_channel": bits,
            "png_crc_and_payload_valid": True}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", action="store_true")
    args = parser.parse_args()
    records = []
    for relative in SELECTED:
        path = ROOT / relative
        assert path.is_file(), f"Missing selected art: {relative}"
        info = png_info(path)
        records.append({"path": relative, **info, "bytes": path.stat().st_size,
                        "sha256": sha(path), "source": "Original built-in image_gen generation/edit",
                        "author": "OpenAI ImageGen; art direction by Codex for this project",
                        "rights_note": "Generated for this project; no third-party artwork supplied as source. No separate font license or public-domain claim.",
                        "status": "selected_design_source_not_runtime_animation",
                        "runtime_referenced": False,
                        "prompt_source": (f"fighters/{relative.split('/')[1]}/README.md" if relative.startswith("fighters/") else "docs/main-prompts.json"),
                        "import_notes": "ASSET_NOTES.md; docs/SWAP_GUIDE.md; fighter manifest where applicable"})

    inventory = list(csv.DictReader((ROOT / "docs/VISUAL_COVERAGE.csv").open(encoding="utf-8-sig", newline="")))
    referenced = set()
    for row in inventory:
        for reference in row["reference_art"].split(";"):
            reference = reference.strip()
            if reference:
                assert (ROOT / reference).is_file(), f"Missing design reference: {reference}"
                referenced.add(reference)
    moves = read_json(ROOT / "docs/move-coverage.json")
    actual_moves = {(r["fighter"], r["move_id"]) for r in moves["moves"]}
    expected_moves = {(fighter, m["id"]) for fighter in ("rook", "vale")
                      for m in read_json(PROJECT / f"data/fighters/{fighter}.json")["moves"]}
    assert len(moves["moves"]) == len(actual_moves) == 98
    assert actual_moves == expected_moves, "Canonical move inventory drift"
    for relative, fingerprint in moves["canonical_source_sha256"].items():
        assert sha(PROJECT / relative).lower() == fingerprint.lower(), f"Canonical source changed: {relative}"
    baseline = read_json(ROOT / "docs/existing-files-baseline.json")
    changed = []
    for record in baseline:
        path = PROJECT / record["path"].replace("\\", "/")
        if not path.is_file() or sha(path).lower() != record["sha256"].lower():
            changed.append(record["path"])
    report = {"verified_at_utc": datetime.now(timezone.utc).isoformat(),
              "selected_pngs": len(records), "png_integrity_pass": True,
              "visual_inventory_rows": len(inventory), "unique_reference_files": len(referenced),
              "canonical_moves": len(actual_moves), "canonical_source_fingerprints_pass": True,
              "baseline_existing_files_checked": len(baseline),
              "existing_files_changed_since_baseline": changed,
              "scope": "Traditional-fighter design directory only. No runtime integration or game tests claimed.",
              "animation_complete": False,
              "limitations": "Fighter sheets are opaque design boards. Cutouts, pixel normalization, cel timelines and integration remain. See source manifests and ASSET_NOTES.md."}
    manifest = {"pack": "AFTER HOURS", "game": "Strike Ledger", "scope": "traditional fighter only",
                "selected_image_count": len(records), "generation_method": "built-in image_gen; no CLI fallback",
                "target_design_canvas": [640, 360], "source_sizes_are_not_export_sizes": True,
                "prototype_modified_by_art_pack": False, "assets": records}
    if args.refresh:
        (ROOT / "ASSET_MANIFEST.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        (ROOT / "VERIFICATION.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        with (ROOT / "ASSET_REGISTER.csv").open("w", encoding="utf-8", newline="") as handle:
            fields = ["path", "source", "author", "rights_note", "status", "width", "height", "format", "sha256", "prompt_source"]
            writer = csv.DictWriter(handle, fieldnames=fields, extrasaction="ignore")
            writer.writeheader()
            writer.writerows(records)
    else:
        existing = read_json(ROOT / "ASSET_MANIFEST.json")
        assert {r["path"]: r["sha256"] for r in existing["assets"]} == {r["path"]: r["sha256"] for r in records}, "Art changed since catalog generation"
    print(json.dumps(report, indent=2))

if __name__ == "__main__":
    main()
