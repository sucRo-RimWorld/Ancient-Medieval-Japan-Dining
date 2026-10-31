"""One explicit production-DLL acceptance in an isolated Linux game copy."""
from pathlib import Path
import argparse,subprocess,os,shutil,hashlib,json,re,xml.etree.ElementTree as ET
p=argparse.ArgumentParser();p.add_argument('--input',required=True,type=Path);p.add_argument('--run',required=True,type=Path);a=p.parse_args()
source=a.input/'Agriculture';dining=a.input/'Dining';root=Path('/srv/amj-rimworld/game');run=a.run
run.mkdir(exist_ok=False);game=run/'game';game.mkdir();mods=game/'Mods';mods.mkdir()
for name in ['Data','RimWorldLinux_Data']:(game/name).symlink_to(root/name,target_is_directory=True)
for name in ['RimWorldLinux','UnityPlayer.so','Version.txt','steam_appid.txt']:
 if (root/name).exists():shutil.copy2(root/name,game/name)
providers=['Framework_Harmony','Framework_Pickle','Framework_Quickstarts','Framework_RimLogging','MO_MedievalOverhaul','MO_SYR','MO_VEF']
for name in providers:(mods/name).symlink_to(root/'Mods'/name,target_is_directory=True)
shutil.copytree(source,mods/'Agriculture',ignore=shutil.ignore_patterns('.git'))
shutil.copytree(dining,mods/'Dining',ignore=shutil.ignore_patterns('.git','bin','obj','build'))
managed=root/'RimWorldLinux_Data/Managed';sdk=Path('/home/amjrunner/.dotnet');csc=sorted((sdk/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
refs=list(managed.glob('*.dll'))+[root/'Mods/Framework_Harmony/Current/Assemblies/0Harmony.dll']
def compile(name,files,out,extra=()):
 args=[str(sdk/'dotnet'),str(csc),'-nologo','-target:library','-nostdlib+','-langversion:latest','-out:'+str(out)]+['-r:'+str(r) for r in refs+list(extra)]+list(map(str,files))
 r=subprocess.run(args,text=True,capture_output=True);(run/(name+'.build.log')).write_text(r.stdout+r.stderr);print(r.stdout+r.stderr,flush=True)
 if r.returncode:raise SystemExit(r.returncode)
dll=mods/'Dining/Assemblies/AMJAgriculture.Dining.dll'
# Exercise the shipped DLL; independent source compilation is performed before staging.
if not dll.is_file(): raise SystemExit('Missing shipped Dining DLL')
test=mods/'DiningAcceptance';(test/'About').mkdir(parents=True);(test/'Assemblies').mkdir();(test/'Pickle/Assemblies').mkdir(parents=True);(test/'Pickle/Features').mkdir()
(test/'About/About.xml').write_text('<ModMetaData><name>Agriculture dining acceptance</name><author>AMJ</author><packageId>amj.dining.acceptance</packageId><supportedVersions><li>1.6</li></supportedVersions></ModMetaData>')
extra=[root/'Mods/Framework_Quickstarts/1.6/Assemblies/Quickstarts.dll',root/'Mods/Framework_Pickle/1.6/Assemblies/RimWorks.Pickle.dll',root/'Mods/Framework_Pickle/1.6/Assemblies/RimWorks.Pickle.Core.dll',dll]
compile('acceptance',[dining/'Tools/Acceptance/IndependentDining.cs'],test/'Assemblies/ProductionAcceptance.dll',extra)
shutil.copy2(test/'Assemblies/ProductionAcceptance.dll',test/'Pickle/Assemblies/ProductionAcceptance.dll')
(test/'Pickle/Features/ProductionDining.feature').write_text('@quickstart:DiningQuickstart @timeout:240\nFeature: Agriculture dining production\n Scenario: Real Agriculture meals and MO dishes\n  Then Agriculture production meals are prepared served and preserved\n')
cfg=run/'save/Config';cfg.mkdir(parents=True)
ids=['brrainz.harmony','ludeon.rimworld','rimworks.rimlogging','rimworks.pickle','rimworks.quickstarts','oskarpotocki.vanillafactionsexpanded.core','syrchalis.processor.framework','dankpyon.medieval.overhaul','sucro.ancientmedievaljapan.dining','sucro.ancientmedievaljapan.core','amj.dining.acceptance']
x=ET.Element('ModsConfigData');ET.SubElement(x,'version').text=(root/'Version.txt').read_text().strip();active=ET.SubElement(x,'activeMods')
for id in ids:ET.SubElement(active,'li').text=id
ET.SubElement(x,'knownExpansions');ET.ElementTree(x).write(cfg/'ModsConfig.xml',encoding='utf-8',xml_declaration=True)
(cfg/'Prefs.xml').write_text('<PrefsData><devMode>True</devMode><resetModsConfigOnCrash>False</resetModsConfigOnCrash></PrefsData>')
oldmeal=dining/'Tests/Fixtures/PreviousDailyMeal.xml'
if oldmeal.exists():shutil.copy2(oldmeal,run/'save/previous-daily-meal.xml')
(run/'home').mkdir();env=os.environ.copy();env.update(HOME=str(run/'home'),XDG_CONFIG_HOME=str(run/'home/.config'),XDG_DATA_HOME=str(run/'home/.local/share'))
args=['xvfb-run','-a','-s','-screen 0 1280x720x24',str(game/'RimWorldLinux'),'-savedatafolder='+str(run/'save'),'-logFile',str(run/'unity.log'),'-screen-width','1280','-screen-height','720','-pickle-run=ProductionDining.feature','-pickle-mode=fast','-pickle-report-dir='+str(run/'report'),'-pickle-run-timeout=6','-pickle-no-browser','-pickle-no-http','-pickle-max-film-seconds=0']
print('RUNNING_AGRICULTURE_PRODUCTION',flush=True)
with (run/'launcher.log').open('w') as stream:
 try:code=subprocess.run(args,cwd=game,env=env,stdout=stream,stderr=subprocess.STDOUT,timeout=360).returncode
 except subprocess.TimeoutExpired:code=-99
log=(run/'unity.log').read_text(errors='replace') if (run/'unity.log').exists() else ''
r={'exit_code':code,'version':next((s for s in log.splitlines() if s.startswith('RimWorld 1.6.')),'unknown'),'production_dll_sha256':hashlib.sha256(dll.read_bytes()).hexdigest(),'daily_absent':not (mods/'DailyMenu').exists() and 'daily.menu.manager' not in ids,'markers':[s for s in log.splitlines() if 'AGRI_DINING_' in s],'errors':[s for s in log.splitlines() if re.search(r'\[ERROR\]|^Error|Exception|Could not|XML error|Config error',s)]}
if (run/'report/summary.json').exists():r['pickle']=json.loads((run/'report/summary.json').read_text())
(run/'result.json').write_text(json.dumps(r,indent=2));print(json.dumps(r,indent=2),flush=True)

# Known unchanged upstream FMOD errors are retained in the report, never hidden.
unknown=[e for e in r['errors'] if not ('FMOD::Sound instance' in e and 'Unsupported file or audio format' in e)]
summary=r.get('pickle',{})
if code != 0 or not r['daily_absent'] or unknown or summary.get('passed') != 1 or summary.get('failed',1) or summary.get('skipped',1) or summary.get('flaky',1) or not any('AGRI_DINING_PRODUCTION_PASS' in line for line in r['markers']):
 raise SystemExit('Dining acceptance failed; inspect result.json and complete unity.log')
