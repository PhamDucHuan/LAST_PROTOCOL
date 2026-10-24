"""Validate official GLB containers and the retained attribution manifest."""
import hashlib,json,pathlib,struct
root=pathlib.Path(__file__).resolve().parents[1]
folder=root/'ArtSource/ThirdParty/CanThoLandmarks'
manifest=json.loads((folder/'SOURCE_AND_LICENSE.json').read_text(encoding='utf-8-sig'))
checks=[]
for entry in manifest:
    data=(folder/entry['file']).read_bytes()
    magic,version,length=struct.unpack_from('<4sII',data)
    assert magic==b'glTF' and version==2 and length==len(data)
    assert hashlib.sha256(data).hexdigest().upper()==entry['sha256'] and len(data)==entry['bytes']
    position=12;chunks=[]
    while position<len(data):
        size,kind=struct.unpack_from('<II',data,position);position+=8
        assert position+size<=len(data)
        chunks.append((kind,data[position:position+size]));position+=size
    assert position==len(data) and chunks[0][0]==0x4e4f534a
    gltf=json.loads(chunks[0][1]);assert gltf['asset']['version']=='2.0'
    assert all(not b.get('uri') or b['uri'].startswith('data:') for b in gltf.get('buffers',[]))
    assert all(not im.get('uri') or im['uri'].startswith('data:') for im in gltf.get('images',[]))
    checks.append(dict(file=entry['file'],passed=True,bytes=len(data),meshes=len(gltf['meshes']),images=len(gltf.get('images',[])),selfContained=True))
report=dict(passed=True,models=checks)
output=root/'Builds/CanThoMap/20261024-SketchfabLandmarks/DownloadValidation.json'
output.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('LANDMARK_DOWNLOAD_VALIDATION_PASS',len(checks))
