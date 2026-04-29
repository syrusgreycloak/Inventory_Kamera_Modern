"""
Parse an InventoryKamera debug log and emit a CSV of material scan results.

Usage:
    python parse_materials_log.py [log_path] [out_csv]

Defaults:
    log_path = InventoryKamera/bin/Debug/net8.0-windows/logging/InventoryKamera.debug.log
    out_csv  = same dir, materials_<timestamp>.csv

Columns:
    page             — page index from "Scanning material page N" lines
    name             — material name from "Found X" / "Final: X : N" / "Found (LastPage) X"
    parsed_count     — the integer count from "Final: X : N"
    ocr_attempts     — pipe-separated raw OCR strings from "Scanned: X -> ..." lines
    failed_to_parse  — 1 if "Failed to parse quantity for X" error was logged

Open the CSV in Excel/Google Sheets and add a "truth" column with the actual
in-game count, then use:
    correct = IF(parsed_count = truth, 1, 0)
"""
import csv, re, sys, os
from datetime import datetime

LOG_DEFAULT = r"C:\Users\karlp\RiderProjects\Inventory_Kamera\InventoryKamera\bin\Debug\net8.0-windows\logging\InventoryKamera.debug.log"

found_re   = re.compile(r"\|MaterialScraper\|Found(?:\s+\(LastPage\))?\s+(.+?)\s*$")
final_re   = re.compile(r"\|MaterialScraper\|Final(?:\s+\(LastPage\))?:\s*(.+?)\s*:\s*(-?\d+)")
scan_re    = re.compile(r"\|MaterialScraper\|Scanned:\s*(.*?)\s*->\s*Regex:")
page_re    = re.compile(r"\|MaterialScraper\|Scanning material page\s+(\d+)")
failed_re  = re.compile(r"\|UserInterface\|Failed to parse quantity for\s+(.+?)\s*$")

def parse(log_path):
    rows = []
    page = 0
    current = None  # {"name":..., "attempts":[], "count":None, "failed":0, "page":...}
    with open(log_path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            m = page_re.search(line)
            if m:
                page = int(m.group(1)); continue
            m = found_re.search(line)
            if m:
                if current and current["name"]:
                    rows.append(current)
                name = m.group(1).strip()
                # Strip user-added annotations like ❌/✅/comments
                name = re.sub(r"[❌✅❗].*$", "", name).strip()
                current = {"page": page, "name": name, "attempts": [], "count": None, "failed": 0}
                continue
            m = scan_re.search(line)
            if m and current:
                current["attempts"].append(m.group(1))
                continue
            m = final_re.search(line)
            if m and current:
                # Final line confirms count; the name from Found should match
                try:
                    current["count"] = int(m.group(2))
                except ValueError:
                    pass
                continue
            m = failed_re.search(line)
            if m and current and current["name"] and m.group(1).strip() == current["name"]:
                current["failed"] = 1
    if current and current["name"]:
        rows.append(current)
    return rows

def main():
    log = sys.argv[1] if len(sys.argv) > 1 else LOG_DEFAULT
    if not os.path.exists(log):
        print(f"Log not found: {log}", file=sys.stderr); sys.exit(1)
    if len(sys.argv) > 2:
        out = sys.argv[2]
    else:
        ts = datetime.now().strftime("%Y%m%d_%H%M%S")
        out = os.path.join(os.path.dirname(log) or ".", f"materials_parsed_{ts}.csv")

    rows = parse(log)
    with open(out, "w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["page", "name", "parsed_count", "ocr_attempts", "failed_to_parse"])
        for r in rows:
            w.writerow([
                r["page"],
                r["name"],
                r["count"] if r["count"] is not None else "",
                " | ".join(r["attempts"]),
                r["failed"],
            ])
    print(f"Wrote {len(rows)} rows to {out}")

if __name__ == "__main__":
    main()
