"""Ensure retained gameplay runs precede only a single menu string correction."""
import hashlib,json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[1];TASK=ROOT/'Builds/Pacing/20261022-Map1'
rows=json.loads((TASK/'GameplayTestedSources.json').read_text(encoding='utf-8-sig'))
changed=[r['Path'].replace('\\','/') for r in rows if hashlib.sha256((ROOT/r['Path']).read_bytes()).hexdigest().upper()!=r['Hash']]
assert changed==['Assets/_Project/Scripts/Core/LevelCatalog.cs'],changed
before=(TASK/'LevelCatalog-BeforeMenuFix.cs').read_text(encoding='utf-8-sig').splitlines()
after=(ROOT/changed[0]).read_text(encoding='utf-8-sig').splitlines()
assert len(before)==len(after)
diff=[i for i,(a,b) in enumerate(zip(before,after)) if a!=b]
assert len(diff)==1
literal=r'^\s*"(?:[^"\\]|\\.)*",\s*$'
assert re.fullmatch(literal,before[diff[0]]) and re.fullmatch(literal,after[diff[0]])
assert 'CHAPTER 1 / 15-20 MINUTES' in after[diff[0]]
(TASK/'MenuOnlyChangeValidation.json').write_text(json.dumps({'passed':True,'changedFiles':changed,'changedLine':diff[0]+1,'change':'One briefing string, all other tested C# files identical','retainedGameplayRuns':'Chapter 2/4, AI and checkpoint regressions; runtime hash recorded separately'},indent=2))
print('CHAPTER_MENU_ONLY_CHANGE_PASS')
