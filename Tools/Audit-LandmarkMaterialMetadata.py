"""Allow only URP's editor version subasset after importing tested materials."""
import hashlib,json,pathlib,re
root=pathlib.Path(__file__).resolve().parents[1]
task=root/'Builds/CanThoMap/20261024-SketchfabLandmarks'
manifest=json.loads((task/'PromotedAssets.json').read_text(encoding='utf-8-sig'))
audited=[]
version_tail=r'''\s*--- !u!114 &\d+
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: \{fileID: 0\}
  m_PrefabInstance: \{fileID: 0\}
  m_PrefabAsset: \{fileID: 0\}
  m_GameObject: \{fileID: 0\}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: \{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3\}
  m_Name: *
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion
  version: 10\s*'''
for entry in manifest:
    path=pathlib.Path(entry['Path'].replace('\\','/'))
    if path.suffix!='.mat':continue
    data=(root/path).read_bytes();actual=hashlib.sha256(data).hexdigest().upper()
    if actual==entry['SHA256']:continue
    assert path.as_posix().startswith('Assets/_Project/Materials/CanThoCampus/CT12_')
    tested=(root/'Builds/ValidationProject'/path).read_bytes()
    assert hashlib.sha256(tested).hexdigest().upper()==entry['SHA256']
    before=tested.decode('utf-8-sig').replace('\r\n','\n')
    after=data.decode('utf-8-sig').replace('\r\n','\n')
    def documents(text):return re.findall(r'(?ms)^--- !u!.*?(?=^--- !u!|\Z)',text)
    old_docs=documents(before);new_docs=documents(after)
    original_material=next(d for d in old_docs if d.startswith('--- !u!21 &2100000\n'))
    current_material=next(d for d in new_docs if d.startswith('--- !u!21 &2100000\n'))
    assert original_material.rstrip()==current_material.rstrip(),('Rendering material payload changed',path)
    assert len(old_docs)==1 and len(new_docs)==2
    version=next(d for d in new_docs if d!=current_material)
    assert re.match(r'^--- !u!114 &-?\d+\nMonoBehaviour:\n',version)
    values=[line.strip().split(':',1) for line in version.splitlines()[2:] if line.strip()]
    actual_fields={key:value.strip() for key,value in values}
    expected_fields={'m_ObjectHideFlags':'11','m_CorrespondingSourceObject':'{fileID: 0}','m_PrefabInstance':'{fileID: 0}',
        'm_PrefabAsset':'{fileID: 0}','m_GameObject':'{fileID: 0}','m_Enabled':'1','m_EditorHideFlags':'0',
        'm_Script':'{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}',
        'm_Name':'','m_EditorClassIdentifier':'Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion','version':'10'}
    assert len(values)==len(expected_fields) and actual_fields==expected_fields,('Unexpected editor metadata',path)
    audited.append(dict(path=entry['Path'],testedSHA256=entry['SHA256'],currentSHA256=actual,renderingPayloadUnchanged=True,addedEditorAssetVersion=10))
report=dict(passed=True,materials=audited,note='Only an editor-only URP AssetVersion 10 subasset was added. Material fileID 2100000, shader, keywords, colors, textures and all rendering properties are unchanged.')
(task/'GeneratedMaterialMetadataAudit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('URP_VERSION_METADATA_AUDIT_PASS',len(audited))
