"""Guard main project state and match tested geometry/surfaces before promotion."""
import hashlib,json,pathlib,re,sys
root=pathlib.Path(__file__).resolve().parents[1]
task=root/'Builds/CanThoMap/20261024-LandmarkRedraw'
clone=root/'Builds/ValidationProject'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(name):return json.loads((task/name).read_text(encoding='utf-8-sig'))
after='--after-promotion' in sys.argv
protected=read('ProtectedHashes.json')
for row in protected:assert sha(root/row['Path'])==row['SHA256'],('Protected main file changed',row['Path'])
runtime=read('RuntimeCodeHashes.json')
for row in runtime:
    for base in [root,clone]:assert sha(base/row['Path'])==row['SHA256'],('Runtime changed',row['Path'])
# NavMesh and all gameplay prefabs/settings/scenes must be the same in the build.
checked=0
for row in protected:
    path=row['Path'].replace('\\','/')
    if path.startswith(('Assets/_Project/Prefabs/','Assets/_Project/Settings/','Assets/_Project/ScriptableObjects/','Assets/_Project/Scenes/','ProjectSettings/')):
        if sha(clone/path)!=row['SHA256']:
            assert path in ['ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset'],('Protected clone differs',path)
            # Unity materializes the default Standalone batching tuple during a
            # build and sorts define symbols. Neither changes project behavior.
            def settings(p):
                s=p.read_text(encoding='utf-8-sig').replace('\r\n','\n')
                if p.name=='UnityConnectSettings.asset':
                    # Existing validation clone has its Editor cloud connection
                    # enabled; the user's main Editor setting stays byte-identical.
                    return s.replace('  serializedVersion: 1\n  m_Enabled: 1\n','  serializedVersion: 1\n  m_Enabled: 0\n',1)
                s=s.replace('  m_BuildTargetBatching:\n  - m_BuildTarget: Standalone\n    m_StaticBatching: 1\n    m_DynamicBatching: 0\n','  m_BuildTargetBatching: []\n')
                # The Sentis Editor analytics service also toggles its own
                # analytics-only define as its cloud connection initializes.
                return re.sub(r'(?m)^(    Standalone: )([^\n]*)$',lambda m:m[1]+';'.join(sorted(x for x in m[2].split(';') if x!='SENTIS_ANALYTICS_ENABLED')),s)
            assert settings(root/path)==settings(clone/path),('Unexpected validation settings change',path)
        checked+=1
# All existing surface payloads and texture bytes are retained, including sports.
surfaces=0
for folder in ['Assets/_Project/Materials/CanThoCampus','Assets/_Project/Art/Environments/CanThoCampus/Textures']:
    for p in (root/folder).rglob('*'):
        if not p.is_file():continue
        relative=p.relative_to(root);other=clone/relative
        if sha(p)!=sha(other):
            assert p.suffix=='.mat',('Surface differs',str(relative))
            def material(path):
                s=path.read_text(encoding='utf-8-sig').replace('\r\n','\n')
                return next(d.rstrip() for d in re.findall(r'(?ms)^--- !u!.*?(?=^--- !u!|\Z)',s) if d.startswith('--- !u!21 &2100000\n'))
            assert material(p)==material(other),('Material payload differs',str(relative))
        surfaces+=1
for name in ['CanThoCampus.fbx','CanThoCampus.layout.json']:
    relative=pathlib.Path('Assets/_Project/Art/Environments/CanThoCampus')/name
    assert sha(task/'Candidate01'/name)==sha(clone/relative),('Candidate differs',name)
    if after:assert sha(root/relative)==sha(clone/relative)
scene=pathlib.Path('Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity')
current=(root/scene).read_text(encoding='utf-8-sig')
tested=(clone/scene).read_text(encoding='utf-8-sig')
if not after:
    assert current.count('  artRevision: 12\n')==1
    assert current.replace('  artRevision: 12\n','  artRevision: 13\n')==tested,'Unrelated serialized scene edits'
else:assert current==tested
if after:
    for row in read('PromotedAssets.json'):
        assert sha(root/row['Path'])==row['SHA256'],('Promoted file changed',row['Path'])
        if row.get('MetaSHA256'):assert sha(root/(row['Path']+'.meta'))==row['MetaSHA256']
report=dict(passed=True,afterPromotion=after,protectedMainFiles=len(protected),protectedCloneFiles=checked,unchangedRuntimeSources=len(runtime),unchangedSurfaceFiles=surfaces,navMeshUnchanged=True,protocolVersion=28)
(task/('FinalVerification.json' if after else 'ProtectedAssetsVerification.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
print('LANDMARK_REDRAW_VERIFICATION_PASS',report)
