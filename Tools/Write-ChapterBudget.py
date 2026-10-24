"""Report configured timing; this is not a measured human playthrough."""
import json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[1]
TASK=ROOT/'Builds/Pacing/20261022-Map1'
asset=(ROOT/'Builds/ValidationProject/Assets/_Project/ScriptableObjects/Missions/CampusRescue.asset').read_text(encoding='utf-8-sig')
stages=[]
for block in re.split(r'(?=^  - id:)',asset,flags=re.M)[1:]:
    def field(name): return re.search(r'^    '+name+r': (.+)$',block,re.M)[1]
    stages.append({'id':re.search(r'^  - id: (.+)$',block,re.M)[1],'title':field('title'),'kind':int(field('kind')),'seconds':float(field('holdSeconds'))})
assert len(stages)==12
seconds=sum(s['seconds'] for s in stages if s['kind'] in (2,3))
assert seconds==810
report={'configuredOperationSeconds':seconds,'configuredOperationMinutes':seconds/60,'stageCount':len(stages),'successfulRunTargetMinutes':[15,20],
        'timingIsEstimate':True,'measuredHumanPlaythrough':False,'additionalTime':'Travel, initial relay tutorial, terminal interactions, combat and extraction. Retry may exceed the target.',
        'automaticValidationClockMultiplier':60,'productionClockMultiplier':1,'stages':stages}
(TASK/'PacingBudget.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CHAPTER_PACING_BUDGET_PASS 810 seconds, 12 stages')
