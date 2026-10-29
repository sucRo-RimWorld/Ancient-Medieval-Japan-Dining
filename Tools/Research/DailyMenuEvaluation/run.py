from pathlib import Path
import subprocess, shutil, os, json, hashlib, xml.etree.ElementTree as ET, re
root=Path('/srv/amj-rimworld/game')
workspace=Path('/mnt/d/SteamLibrary/steamapps/common/RimWorld/Mods/DailyMenuEvaluation')
run=Path('/tmp/amj-daily-menu-20261028')
run.mkdir(exist_ok=False)
game=run/'game';game.mkdir();mods=game/'Mods';mods.mkdir()
for name in ['Data','RimWorldLinux_Data']:(game/name).symlink_to(root/name,target_is_directory=True)
for name in ['RimWorldLinux','UnityPlayer.so','Version.txt','steam_appid.txt']:
 if (root/name).exists():shutil.copy2(root/name,game/name)
for name in ['Framework_Harmony','Framework_Pickle','Framework_Quickstarts','Framework_RimLogging']:(mods/name).symlink_to(root/'Mods'/name,target_is_directory=True)
daily=Path('/mnt/d/SteamLibrary/steamapps/workshop/content/294100/3812908178')
shutil.copytree(daily,mods/'DailyMenu')
probe=mods/'AMJDailyProbe';(probe/'About').mkdir(parents=True);(probe/'Assemblies').mkdir();(probe/'Pickle/Assemblies').mkdir(parents=True);(probe/'Pickle/Features').mkdir()
(probe/'About/About.xml').write_text('<ModMetaData><name>AMJ Daily Menu disposable probe</name><author>AMJ</author><packageId>amj.daily.probe</packageId><supportedVersions><li>1.6</li></supportedVersions></ModMetaData>')
managed=root/'RimWorldLinux_Data/Managed'
refs=list(managed.glob('*.dll'))+[root/'Mods/Framework_Quickstarts/1.6/Assemblies/Quickstarts.dll', root/'Mods/Framework_Pickle/1.6/Assemblies/RimWorks.Pickle.dll',root/'Mods/Framework_Pickle/1.6/Assemblies/RimWorks.Pickle.Core.dll',mods/'DailyMenu/Assemblies/DailyMenu.dll']
refs += [root/'Mods/Framework_Harmony/Current/Assemblies/0Harmony.dll']
sdk=Path('/home/amjrunner/.dotnet');csc=sorted((sdk/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
args=[str(sdk/'dotnet'),str(csc),'-nologo','-target:library','-nostdlib+','-langversion:latest', '-out:'+str(probe/'Assemblies/AMJDailyProbe.dll')]+['-r:'+str(p) for p in refs]+[str(workspace/'Probe.cs'),str(workspace/'PlatePrototype.cs')]
build=subprocess.run(args,text=True,capture_output=True);(run/'build.log').write_text(build.stdout+build.stderr)
print(build.stdout+build.stderr,flush=True)
if build.returncode:raise SystemExit(build.returncode)
# One-line vendor helper rename is evaluated only on the copied provider.
# Reuse the author-supplied source; do not redistribute it with AMJ's probe.
src=(daily/'Source/Supply.cs').read_text(encoding='utf-8-sig').replace('Prepare(', 'PrepareIngredients(')
(run/'Supply.repaired.cs').write_text(src)
fixargs=[str(sdk/'dotnet'),str(csc),'-nologo','-target:library','-nostdlib+','-langversion:latest','-out:'+str(mods/'DailyMenu/Assemblies/DailyMenu.Supply.dll')]+['-r:'+str(p) for p in refs]+[str(run/'Supply.repaired.cs')]
fix=subprocess.run(fixargs,text=True,capture_output=True);(run/'supply-build.log').write_text(fix.stdout+fix.stderr)
if fix.returncode:print(fix.stdout+fix.stderr);raise SystemExit(fix.returncode)
# Independent XML/Harmony compatibility patches; original vendor DLL/XML stay
# byte-identical. Mass=1 is a disposable probe value, not accepted game balance.
(probe/'Patches').mkdir()
(probe/'Patches/Compatibility.xml').write_text('''<Patch><Operation Class="PatchOperationAdd"><xpath>Defs/ThingDef[defName="DailyMenu_Buffet" or defName="DailyMenu_DrinkTable"]/statBases</xpath><value><Mass>1</Mass></value></Operation></Patch>''')
(probe/'Defs').mkdir()
(probe/'Defs/Plate.xml').write_text('''<Defs><ThingDef ParentName="MealCooked"><defName>AMJ_ProbePlate</defName><label>disposable probe plate</label><description>Isolated feasibility probe only.</description><stackLimit>1</stackLimit><graphicData><texPath>Things/Building/DailyMenu/BuffetVat</texPath><graphicClass>Graphic_Single</graphicClass></graphicData><statBases><Nutrition>0.9</Nutrition><MarketValue>1</MarketValue></statBases><ingestible><preferability>MealSimple</preferability><ingestEffect>EatVegetarian</ingestEffect><ingestSound>Meal_Eat</ingestSound></ingestible><comps><li Class="AmjDailyProbe.PlateProps"/></comps></ThingDef></Defs>''')
# Pickle step discovery uses its dedicated assembly folder; quickstart stays in Assemblies.
shutil.copy2(probe/'Assemblies/AMJDailyProbe.dll',probe/'Pickle/Assemblies/AMJDailyProbe.dll')
(probe/'Pickle/Features/DailyMenu.feature').write_text('@quickstart:DailyQuickstart @timeout:180\nFeature: Daily Menu feasibility\n Scenario: Existing multi-dish food flow\n  Then Daily Menu is assessed through real food jobs\n')
cfg=run/'save/Config';cfg.mkdir(parents=True)
ids=['brrainz.harmony','ludeon.rimworld','rimworks.rimlogging','rimworks.pickle','rimworks.quickstarts','daily.menu.manager','amj.daily.probe']
x=ET.Element('ModsConfigData');ET.SubElement(x,'version').text=(root/'Version.txt').read_text().strip();a=ET.SubElement(x,'activeMods')
for pid in ids:ET.SubElement(a,'li').text=pid
ET.SubElement(x,'knownExpansions');ET.ElementTree(x).write(cfg/'ModsConfig.xml',encoding='utf-8',xml_declaration=True)
(cfg/'Prefs.xml').write_text('<PrefsData><devMode>True</devMode><resetModsConfigOnCrash>False</resetModsConfigOnCrash></PrefsData>')
env=os.environ.copy();env.update(HOME=str(run/'home'),XDG_CONFIG_HOME=str(run/'home/.config'),XDG_DATA_HOME=str(run/'home/.local/share'))
(run/'home').mkdir()
args=['xvfb-run','-a','-s','-screen 0 1280x720x24',str(game/'RimWorldLinux'),'-savedatafolder='+str(run/'save'),'-logFile',str(run/'unity.log'),'-screen-width','1280','-screen-height','720','-pickle-run=DailyMenu.feature','-pickle-mode=fast','-pickle-report-dir='+str(run/'report'),'-pickle-run-timeout=5','-pickle-no-browser','-pickle-no-http','-pickle-max-film-seconds=0']
print('RUNNING_ISOLATED_DAILY_MENU',flush=True)
with (run/'launcher.log').open('w') as out:
 try:r=subprocess.run(args,cwd=game,env=env,stdout=out,stderr=subprocess.STDOUT,timeout=300);code=r.returncode
 except subprocess.TimeoutExpired:code=-99
log=(run/'unity.log').read_text(errors='replace') if (run/'unity.log').exists() else ''
result={'exit_code':code,'game_version_file':(root/'Version.txt').read_text().strip(),'game_version_log':next((s for s in log.splitlines() if s.startswith('RimWorld 1.6.')),'unknown'),'daily_dll_sha256':hashlib.sha256((daily/'Assemblies/DailyMenu.dll').read_bytes()).hexdigest(),'vendor_copy_unchanged':all(hashlib.sha256(p.read_bytes()).digest()==hashlib.sha256((mods/'DailyMenu'/p.relative_to(daily)).read_bytes()).digest() for p in daily.rglob('*') if p.is_file()),'markers':[s for s in log.splitlines() if 'AMJ_DAILY_' in s],'errors':[s for s in log.splitlines() if re.search(r'(\[ERROR\]|^Error|Exception|Could not|XML error|Config error)',s)]}
if (run/'report/summary.json').exists():result['pickle']=json.loads((run/'report/summary.json').read_text())
(run/'result.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2),flush=True)
dest=workspace/'Evidence';dest.mkdir(exist_ok=True)
for name in ['result.json','unity.log','launcher.log','build.log']:shutil.copy2(run/name,dest/name)
if (run/'report/summary.json').exists():shutil.copy2(run/'report/summary.json',dest/'summary.json')
