"""Verify targeted collider updates preserve every unrelated Unity scene document."""
import difflib,json,re,sys
from pathlib import Path
before,after,report=map(Path,sys.argv[1:4])
def documents(path):
    text=path.read_text(encoding='utf-8-sig')
    chunks=re.split(r'(?m)(^--- !u!\d+ &\d+[^\n]*\n)',text)
    return {re.search(r'&(\d+)',chunks[i]).group(1):chunks[i]+chunks[i+1]for i in range(1,len(chunks),2)}
a=documents(before);b=documents(after)
assert a.keys()==b.keys(),('Scene objects added or removed',set(a)^set(b))
gos={}
allowed=set()
for key,doc in a.items():
    if not doc.startswith('--- !u!1 &'):continue
    match=re.search(r'^  m_Name: (.*)$',doc,re.M)
    if match and ('trunk' in match[1] or match[1]=='Turtle auditorium body' or match[1].startswith('Turtle Hall ')):
        gos[key]=match[1]
for key,doc in a.items():
    go=re.search(r'^  m_GameObject: \{fileID: (\d+)\}',doc,re.M)
    if go and go[1] in gos and (doc.startswith('--- !u!4 &')or doc.startswith('--- !u!65 &')):allowed.add(key)
changed=[];unexpected=[]
for key,doc in a.items():
    if doc==b[key]:continue
    changed.append(key)
    if key not in allowed:
        unexpected.append(dict(id=key,diff=''.join(difflib.unified_diff(doc.splitlines(True),b[key].splitlines(True)))[:8000]))
result=dict(passed=not unexpected,changedDocuments=changed,unchangedUnrelatedDocuments=len(a)-len(changed),unexpected=unexpected)
report.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result)[:12000])
assert not unexpected,'Unexpected edits to user scene'
