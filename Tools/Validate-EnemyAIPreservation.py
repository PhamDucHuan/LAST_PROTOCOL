"""Exact guards for existing scene, art, navigation and network prefabs."""
import hashlib, json, pathlib, re
ROOT=pathlib.Path(__file__).resolve().parents[1]
TASK=ROOT/'Builds/EnemyAI/20261022-EnemyAI'
BACKUP=ROOT/'Builds/EnemyAIBackups/20261022-EnemyAI'
VP=ROOT/'Builds/ValidationProject'
ROOM='Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab'
MENU='Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab'
LIST='Assets/_Project/Settings/MvpNetworkPrefabs.asset'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def source(p): return p.read_text(encoding='utf-8-sig').replace('\r\n','\n')
def guid(p): return re.search(r'^guid: (.+)$',source(p),re.M)[1]
protected=[]
for row in json.loads((BACKUP/'BaselineHashes.json').read_text(encoding='utf-8-sig')):
    name=row['Path']
    assert sha(ROOT/name)==row['Hash'],f'Main changed before promotion: {name}'
    if name not in (ROOM,MENU,LIST):
        assert sha(VP/name)==row['Hash'],f'Protected asset changed: {name}'
        protected.append(name)
    assert guid(ROOT/(name+'.meta'))==guid(VP/(name+'.meta')),f'GUID changed: {name}'
room=source(VP/ROOM)
brain_guid=guid(ROOT/'Assets/_Project/Scripts/Gameplay/Enemy/EnemyDirector.cs.meta')
docs=re.split(r'(?=^--- !u!)',room,flags=re.M)
new=[d for d in docs if f'guid: {brain_guid},' in d]
assert len(new)==1
fid=re.search(r'^--- !u!114 &(\d+)',new[0])[1]
stripped=room.replace(new[0],'').replace(f'  - component: {{fileID: {fid}}}\n','')
stripped=re.sub(r'^  tacticalProfile: \{fileID: 11400000, guid: [a-f0-9]+, type: 2\}\n','',stripped,flags=re.M)
assert stripped==source(BACKUP/ROOM),'Unrelated Room content changed'
assert source(VP/MENU).replace('PROTOTYPE  /  BUILD 27','PROTOTYPE  /  BUILD 26')==source(BACKUP/MENU),'Menu changed beyond build label'
before=source(BACKUP/LIST);after=source(VP/LIST)
for role in ('Frontliner','Flanker','Suppressor','Support','Controller','Hunter'):
    asset=f'Assets/_Project/Prefabs/Networking/Tactical{role}.prefab'
    g=guid(VP/(asset+'.meta'))
    pattern=r'  - Override: 0\n    Prefab: \{fileID: \d+, guid: '+g+r', type: 3\}\n    SourcePrefabToOverride: \{fileID: 0\}\n    SourceHashToOverride: 0\n    OverridingTargetPrefab: \{fileID: 0\}\n'
    after,n=re.subn(pattern,'',after)
    assert n==1,f'Registry entry missing or duplicate: {role}'
assert after==before,'Existing registry entries changed'
(TASK/'PreservationValidation.json').write_text(json.dumps({'passed':True,'protectedAssets':protected,'room':'one Director + tactical encounter reference','menu':'26 to 27 only','registry':'six new entries'},indent=2))
print('ENEMY_AI_PRESERVATION_PASS')
