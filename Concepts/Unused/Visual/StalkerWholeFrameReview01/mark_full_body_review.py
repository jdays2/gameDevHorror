"""Record actual completed page inspection, not inferred alpha acceptance."""
from pathlib import Path
import argparse
import json


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--rows",required=True)
    parser.add_argument("--backgrounds",required=True)
    parser.add_argument("--note",required=True)
    args=parser.parse_args()
    rows={int(x) for x in args.rows.split(",")}
    backgrounds=set(args.backgrounds.split(","))
    path=Path(__file__).resolve().parent/"FullBodyFinal04-review-coverage.json"
    data=json.loads(path.read_text())
    marked=[]
    for page in data["pages"]:
        row=int(Path(page["path"]).name.split("-")[1][3:])
        if row in rows and page["background"] in backgrounds:
            page["reviewed"]=True
            page["reviewNote"]=args.note
            marked.append(Path(page["path"]).name)
    data["status"]="FULL PAGE REVIEW COMPLETE, final synthesis pending" if all(p["reviewed"] for p in data["pages"]) else "VISUAL REVIEW IN PROGRESS"
    path.write_text(json.dumps(data,indent=2))
    print(json.dumps({"newlyRecordedPages":len(marked),"reviewedPages":sum(p["reviewed"] for p in data["pages"]),"totalPages":len(data["pages"])}))


if __name__=="__main__":
    main()
