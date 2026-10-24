import hashlib
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
staged = root / 'Builds/ValidationProject'
backup = root / 'Builds/EncounterBackups/20261021-UplinkDefense'
task = root / 'Builds/Encounters/20261021-UplinkDefense'
errors = []
protected = [
    'Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity',
    'Assets/_Project/Settings/CanThoCampusNavigation.asset',
    'Assets/_Project/Prefabs/Networking/RobotEnemy.prefab',
    'Assets/_Project/ScriptableObjects/Missions/CampusRecon.asset',
    'Assets/_Project/ScriptableObjects/Missions/RestoreUplink.asset',
    'Assets/_Project/Prefabs/Player/NetworkPlayer.prefab',
    'Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx',
    'Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json',
]
for path in protected:
    if (root / path).read_bytes() != (staged / path).read_bytes():
        errors.append('Protected asset differs: ' + path)
for item in json.loads((backup / 'BaselineHashes.json').read_text(encoding='utf-8-sig')):
    if hashlib.sha256((root / item['Path']).read_bytes()).hexdigest().upper() != item['Hash']:
        errors.append('Main asset changed during validation: ' + item['Path'])
menu = 'Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab'
if (staged / menu).read_bytes().replace(b'PROTOTYPE  /  BUILD 25', b'PROTOTYPE  /  BUILD 24') != (backup / menu).read_bytes():
    errors.append('Unexpected menu changes.')

room = 'Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab'
before = (backup / room).read_text(encoding='utf-8-sig')
after = (staged / room).read_text(encoding='utf-8-sig')
parts = re.split(r'(?m)^(--- !u!114 &\d+\n)', after)
new_component = parts[-2] + parts[-1]
component_id = re.search(r'&(\d+)', parts[-2]).group(1)
trimmed = after[:-len(new_component)].replace('  - component: {fileID: ' + component_id + '}\n', '')
if trimmed != before or 'LastProtocol.UplinkEncounter\n' not in new_component:
    errors.append('Room has changes beyond the encounter component.')

prefabs = 'Assets/_Project/Settings/MvpNetworkPrefabs.asset'
before_list = (backup / prefabs).read_text(encoding='utf-8-sig')
after_list = (staged / prefabs).read_text(encoding='utf-8-sig')
enemy_guid = re.search(r'(?m)^guid: (\w+)', (staged / 'Assets/_Project/Prefabs/Networking/UplinkPatrol.prefab.meta').read_text()).group(1)
suffix = after_list[len(before_list):] if after_list.startswith(before_list) else ''
if not suffix or suffix.count('  - Override: 0\n') != 1 or enemy_guid not in suffix or suffix.count('    Prefab: ') != 1:
    errors.append('Prefab registration changed existing entries.')
if (root / prefabs).read_bytes() != (backup / prefabs).read_bytes():
    errors.append('Main prefab list changed during validation.')
report = {'passed': not errors, 'protectedAssetsUnchanged': protected, 'newRoomComponents': 1,
          'newRegisteredPrefabs': 1, 'menuChange': 'Build label 24 -> 25 only', 'errors': errors}
(task / 'PreservationValidation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
if errors:
    raise SystemExit(1)
