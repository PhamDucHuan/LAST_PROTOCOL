"""Audit Netcode's generated default list without overwriting Editor updates."""
import json,pathlib,re
root=pathlib.Path(__file__).resolve().parents[1]
clone=root/'Builds/ValidationProject'
path=pathlib.Path('Assets/DefaultNetworkPrefabs.asset')
before=(clone/path).read_text(encoding='utf-8-sig')
after=(root/path).read_text(encoding='utf-8-sig')
assert after.startswith(before.rstrip()),'Existing default prefab entries/header changed'
pattern=r'    Prefab: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}'
old=re.findall(pattern,before);current=re.findall(pattern,after)
assert current[:len(old)]==old and len(set(current))==len(current)
registered=set(re.findall(pattern,(root/'Assets/_Project/Settings/MvpNetworkPrefabs.asset').read_text(encoding='utf-8-sig')))
added=current[len(old):]
assert all(p in registered for p in added),'Generated entry is not in the actual gameplay registry'
suffix=after[len(before.rstrip()):]
block=r'\s*  - Override: 0\n    Prefab: \{fileID: \d+, guid: [0-9a-f]{32}, type: 3\}\n    SourcePrefabToOverride: \{fileID: 0\}\n    SourceHashToOverride: 0\n    OverridingTargetPrefab: \{fileID: 0\}'
assert not re.sub(block,'',suffix).strip(),'Unexpected generated registry content'
guid=re.search(r'^guid: (\w+)',(root/(str(path)+'.meta')).read_text(),re.M)[1]
references=[str(p.relative_to(root)) for p in (root/'Assets').rglob('*') if p.suffix in {'.prefab','.unity','.asset'} and p!=root/path and guid.encode('ascii') in p.read_bytes()]
assert not references,('Default asset acquired serialized gameplay references',references)
names=[]
for _,entry_guid in added:
    matches=[p for p in (root/'Assets/_Project/Prefabs').rglob('*.meta') if 'guid: '+entry_guid in p.read_text(encoding='utf-8-sig')]
    assert len(matches)==1
    names.append(matches[0].name.removesuffix('.meta'))
report=dict(passed=True,existingEntriesPreserved=True,addedCount=len(added),addedAlreadyRegisteredPrefabs=names,
            gameplayRegistryBytePreservationCheckedSeparately=True,yamlReferencesToGeneratedDefaultAsset=references,
            note='Unity Editor automatically discovered pre-existing Tactical enemy/ChapterTerminal prefabs during refresh. Kept this generated, unreferenced default list; did not overwrite it with the stale clone list.')
out=root/'Builds/CanThoMap/20261024-SketchfabLandmarks/GeneratedDefaultPrefabsAudit.json'
out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('GENERATED_DEFAULT_PREFABS_AUDIT_PASS',len(added))
