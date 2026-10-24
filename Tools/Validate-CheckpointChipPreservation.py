"""Guard the user's map and pre-existing assets before promotion."""
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
TASK = ROOT / 'Builds/Checkpoints/20261022-CheckpointChip'
BACKUP = ROOT / 'Builds/CheckpointBackups/20261022-CheckpointChip'
VP = ROOT / 'Builds/ValidationProject'
ROOM = 'Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab'
MENU = 'Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def source(path):
    return path.read_text(encoding='utf-8-sig').replace('\r\n', '\n')

rows = []
for old in json.loads((BACKUP / 'BaselineHashes.json').read_text(encoding='utf-8-sig')):
    name = old['Path']
    assert sha(ROOT / name) == old['Hash'], f'Main changed during validation: {name}'
    if name not in (ROOM, MENU):
        assert sha(VP / name) == old['Hash'], f'Protected asset changed: {name}'
    assert re.search(r'^guid: (.+)$', source(ROOT / (name + '.meta')), re.M)[1] == re.search(r'^guid: (.+)$', source(VP / (name + '.meta')), re.M)[1], name
    rows.append(name)

guid = re.search(r'^guid: (.+)$', source(ROOT / 'Assets/_Project/Scripts/Gameplay/Missions/NetworkChip.cs.meta'), re.M)[1]
room = source(VP / ROOM)
docs = re.split(r'(?=^--- !u!)', room, flags=re.M)
chip_docs = [doc for doc in docs if f'guid: {guid},' in doc]
assert len(chip_docs) == 1, 'Expected exactly one Room NetworkChip component'
file_id = re.search(r'^--- !u!114 &(\d+)', chip_docs[0])[1]
stripped = room.replace(chip_docs[0], '').replace(f'  - component: {{fileID: {file_id}}}\n', '')
assert stripped == source(BACKUP / ROOM), 'Unrelated Room data changed'
assert source(VP / MENU).replace('PROTOTYPE  /  BUILD 26', 'PROTOTYPE  /  BUILD 25') == source(BACKUP / MENU), 'Unrelated menu content changed'
report = {'passed': True, 'protectedAssets': rows, 'roomChange': 'one NetworkChip component', 'menuChange': '25 to 26 only'}
(TASK / 'PreservationValidation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('CHECKPOINT_CHIP_PRESERVATION_PASS')
