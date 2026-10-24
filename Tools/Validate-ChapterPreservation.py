import hashlib,json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[1]
TASK=ROOT/'Builds/Pacing/20261022-Map1';BACKUP=ROOT/'Builds/PacingBackups/20261022-Map1';VP=ROOT/'Builds/ValidationProject'
ROOM='Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab';MENU='Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab';LIST='Assets/_Project/Settings/MvpNetworkPrefabs.asset'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def src(p):return p.read_text(encoding='utf-8-sig').replace('\r\n','\n')
def guid(p):return re.search(r'^guid: (.+)$',src(p),re.M)[1]
protected=[]
for row in json.loads((BACKUP/'BaselineHashes.json').read_text(encoding='utf-8-sig')):
    p=row['Path'];assert sha(ROOT/p)==row['Hash'],f'Main changed: {p}'
    if p not in (ROOM,MENU,LIST):assert sha(VP/p)==row['Hash'],f'Protected asset changed: {p}';protected.append(p)
    assert guid(ROOT/(p+'.meta'))==guid(VP/(p+'.meta')),p
room=src(VP/ROOM);g=guid(ROOT/'Assets/_Project/Scripts/Gameplay/Missions/ChapterOperation.cs.meta')
docs=re.split(r'(?=^--- !u!)',room,flags=re.M);added=[d for d in docs if f'guid: {g},' in d];assert len(added)==1
fid=re.search(r'^--- !u!114 &(\d+)',added[0])[1]
room=room.replace(added[0],'').replace(f'  - component: {{fileID: {fid}}}\n','')
room=re.sub(r'^  chapterMission: \{fileID: 11400000, guid: [a-f0-9]+, type: 2\}\n','',room,flags=re.M)
assert room==src(BACKUP/ROOM),'Unrelated Room content changed'
assert src(VP/MENU).replace('PROTOTYPE  /  BUILD 28','PROTOTYPE  /  BUILD 27')==src(BACKUP/MENU),'Menu prefab changed beyond build label'
g=guid(VP/'Assets/_Project/Prefabs/Networking/ChapterTerminal.prefab.meta')
pattern=r'  - Override: 0\n    Prefab: \{fileID: \d+, guid: '+g+r', type: 3\}\n    SourcePrefabToOverride: \{fileID: 0\}\n    SourceHashToOverride: 0\n    OverridingTargetPrefab: \{fileID: 0\}\n'
registry,n=re.subn(pattern,'',src(VP/LIST));assert n==1;assert registry==src(BACKUP/LIST),'Existing registry entries changed'
(TASK/'PreservationValidation.json').write_text(json.dumps({'passed':True,'protectedAssets':protected,'room':'ChapterOperation + long mission reference','menu':'27 to 28 only','registry':'one terminal prefab'},indent=2))
print('CHAPTER_PRESERVATION_PASS')
