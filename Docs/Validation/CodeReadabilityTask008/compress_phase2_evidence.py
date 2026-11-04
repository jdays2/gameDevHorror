"""Losslessly archive only this task's four new native snapshots; verify before unlink."""
import gzip
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
NAMES = ("native.phase2.before.json", "native.phase2.after-diagnostics.json",
         "native.phase2.prebuild.json", "native.phase2.final.json")


def main():
    rows = []
    assert not (ROOT / "phase2-compression-manifest.json").exists()
    for name in NAMES:
        path = (ROOT / name).resolve()
        assert path.parent == ROOT.resolve() and path.name.startswith("native.phase2.")
        raw = path.read_bytes()
        compressed = gzip.compress(raw, mtime=0)
        assert gzip.decompress(compressed) == raw
        destination = path.with_suffix(path.suffix + ".gz")
        with destination.open("xb") as output:
            output.write(compressed)
        assert gzip.decompress(destination.read_bytes()) == raw
        rows.append({"source": name, "archive": destination.name,
                     "sourceBytes": len(raw), "archiveBytes": len(compressed),
                     "sourceSha256": hashlib.sha256(raw).hexdigest(),
                     "archiveSha256": hashlib.sha256(compressed).hexdigest()})
        path.unlink()
    with (ROOT / "phase2-compression-manifest.json").open("x", encoding="utf-8") as output:
        json.dump({"method": "gzip, mtime0, exact decompression verified before removing each raw owned snapshot", "files": rows}, output, indent=2)
    print(json.dumps({"losslessArchives": len(rows), "rawBytes": sum(row["sourceBytes"] for row in rows),
                      "archiveBytes": sum(row["archiveBytes"] for row in rows)}))


if __name__ == "__main__":
    main()
