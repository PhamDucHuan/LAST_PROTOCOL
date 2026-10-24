"""Compare authored scene documents with the pre-uplink backup, without reserializing YAML."""
import difflib
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TASK = ROOT / "Builds/Missions/20261021-RestoreUplink"
BACKUP = ROOT / "Builds/MissionBackups/20261021-RestoreUplink"
STAGED = ROOT / "Builds/ValidationProject"
SCENE = Path("Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity")


def documents(path):
    text = path.read_text(encoding="utf-8-sig")
    parts = re.split(r"(?m)^(--- !u!\d+ &-?\d+.*\n)", text)
    return {re.search(r"&(-?\d+)", parts[i]).group(1): parts[i] + parts[i + 1] for i in range(1, len(parts), 2)}


old, new = documents(BACKUP / SCENE), documents(STAGED / SCENE)
missing = sorted(set(old) - set(new))
changes = []
errors = []
for key, before in old.items():
    after = new.get(key, "")
    if before == after:
        continue
    allowed = False
    if "SceneRoots:" in before:
        before_roots = re.findall(r"- \{fileID: (-?\d+)\}", before)
        after_roots = re.findall(r"- \{fileID: (-?\d+)\}", after)
        allowed = after_roots[:len(before_roots)] == before_roots and len(after_roots) == len(before_roots) + 4
    elif "stationId: CTU_ADMIN_DATA_01\n" in before:
        normalized = re.sub(r"(?m)^  (displayName|completionMessage):.*\n", "", after)
        allowed = normalized == before
    changes.append({"id": key, "allowed": allowed, "diff": "".join(difflib.unified_diff(before.splitlines(True), after.splitlines(True)))})
    if not allowed:
        errors.append("Unexpected existing scene document change: " + key)
for relative in ["Assets/_Project/Prefabs/Player/NetworkPlayer.prefab", "Assets/_Project/ScriptableObjects/Missions/CampusRecon.asset"]:
    # Player was not changed by this task; Recon schema0 is intentionally kept byte-for-byte.
    if (ROOT / relative).read_bytes() != (STAGED / relative).read_bytes():
        errors.append("Unrelated asset changed: " + relative)
menu = Path("Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab")
if (STAGED / menu).read_bytes().replace(b"PROTOTYPE  /  BUILD 24", b"PROTOTYPE  /  BUILD 23") != (BACKUP / menu).read_bytes():
    errors.append("Menu has changes beyond protocol label.")
for item in json.loads((BACKUP / "BaselineHashes.json").read_text(encoding="utf-8-sig")):
    digest = hashlib.sha256((ROOT / item["Path"]).read_bytes()).hexdigest().upper()
    if digest != item["Hash"]:
        errors.append("Main asset changed during validation: " + item["Path"])
report = {"passed": not missing and not errors, "beforeDocuments": len(old), "addedDocuments": len(set(new)-set(old)),
          "unchangedDocuments": sum(new.get(k) == v for k, v in old.items()), "missing": missing, "changes": changes, "errors": errors}
(TASK / "PreservationValidation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps({k:v for k,v in report.items() if k != "changes"}, ensure_ascii=False))
if not report["passed"]:
    raise SystemExit(1)
