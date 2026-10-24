"""Prepare a selective Git index payload from already-authored Unity scene deltas.

Never writes Assets. Keeps unrelated draft blocks and hand-tuned positions out of
the commit while leaving the full working scene untouched. This is patch staging,
not scene authoring; every task delta came from the native Editor operations.
"""
import hashlib
import json
import re
import subprocess
from pathlib import Path
from AuditSceneHierarchy import BLOCK, blocks

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "work/scene-grouping-task002"
OUT = ROOT / "Docs/Validation/SceneGroupingTask002"


def strip_name_parent(body):
    body = re.sub(r"m_TransformParent: \{fileID: -?\d+\}", "m_TransformParent: PARENT", body)
    return re.sub(r"(propertyPath: m_Name\n      value:) [^\n]+", r"\1 NAME", body)


def main():
    rows = []
    plans = json.loads((OUT / "authoring-plan.json").read_text(encoding="utf-8"))
    for plan in plans:
        path = Path(plan["scene"])
        before = blocks(WORK / "before" / path)
        current = blocks(ROOT / path)
        head_bytes = subprocess.check_output(["git", "show", "HEAD:" + path.as_posix()], cwd=ROOT)
        head_file = WORK / ("index-base-" + path.name)
        head_file.write_bytes(head_bytes)
        head = blocks(head_file)
        assert not before.keys()-current.keys(), path
        added = current.keys()-before.keys()
        assert not added & head.keys(), path
        changed = {key for key in before.keys() & current.keys() if before[key] != current[key]}
        replacements = {}
        merges = []
        for key in changed:
            assert key in head, (path, "task object absent from HEAD", key)
            if head[key] == before[key]:
                replacements[key] = current[key][1]
                continue
            # The four actors already had uncommitted authored world positions.
            # Apply only the two actual native changes to their committed blocks.
            assert before[key][0] == "1001" and strip_name_parent(before[key][1]) == strip_name_parent(current[key][1]), (path,key)
            result = head[key][1]
            for pattern in [r"m_TransformParent: \{fileID: -?\d+\}", r"propertyPath: m_Name\n      value: [^\n]+"]:
                old = re.search(pattern, before[key][1])[0]
                new = re.search(pattern, current[key][1])[0]
                assert result.count(old) == 1, (path,key,old)
                result = result.replace(old,new,1)
            replacements[key] = result
            merges.append(key)

        source = head_bytes.decode("utf-8-sig").replace("\r\n", "\n")
        matches = list(BLOCK.finditer(source))
        result = source[:matches[0].start()]
        for i,m in enumerate(matches):
            end = matches[i+1].start() if i+1<len(matches) else len(source)
            result += source[m.start():m.end()] + replacements.get(m[2],source[m.end():end])
        # Object block order is not semantic; add only the new neutral groups.
        for key in sorted(added,key=int):
            result += "--- !u!"+current[key][0]+" &"+key+current[key][1]
        payload = WORK / ("index-task-" + path.name)
        payload.write_text(result,encoding="utf-8",newline="\n")
        staged = blocks(payload)
        assert head.keys() <= staged.keys()
        assert all(staged[k] == v for k,v in head.items() if k not in changed)
        assert all(staged[k][0] in {"1","4"} for k in added)
        rows.append(dict(scene=path.as_posix(), originalHeadBlocks=len(head), ownChangedBlocks=sorted(changed),
                         ownAddedBlocks=sorted(added), actorsWithForeignPoseExcluded=merges,
                         headSHA256=hashlib.sha256(head_bytes).hexdigest(),
                         indexPayloadSHA256=hashlib.sha256(payload.read_bytes()).hexdigest(),
                         workingFileUntouched=True))
        print(path.stem,len(changed),"changed blocks,",len(added),"new blocks,",len(merges),"foreign poses excluded")
    (OUT/"commit-scope.json").write_text(json.dumps(rows,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")


if __name__ == "__main__":
    main()
