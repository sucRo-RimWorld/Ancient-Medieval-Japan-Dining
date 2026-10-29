"""Validate paired package ownership without launching RimWorld."""
from pathlib import Path
import argparse, hashlib, json, xml.etree.ElementTree as ET
from validate_png_assets import validate_inputs
p=argparse.ArgumentParser();p.add_argument('--agriculture',type=Path,required=True);a=p.parse_args()
d=Path(__file__).resolve().parents[1];ag=a.agriculture
for root in (d,ag):
    for folder in ('About','Defs','Patches','Languages'):
        for f in (root/folder).rglob('*.xml'): ET.parse(f)
def deps(root): return {e.text.lower() for e in ET.parse(root/'About/About.xml').findall('.//modDependencies/li/packageId')}
assert deps(d)=={'brrainz.harmony','daily.menu.manager'}, deps(d)
assert 'sucro.ancientmedievaljapan.dining' in deps(ag)
for path in ('Assemblies/AMJAgriculture.Dining.dll','Source/AMJAgriculture.Dining/Dining.cs','Defs/ThingDefs_Items/Items_MealPlate.xml','Languages/Japanese/DefInjected/ThingDef/AMJA_MealPlate.xml','Languages/Japanese/DefInjected/JobDef/AMJA_Dining.xml'):
    assert (d/path).is_file(), path
    assert not (ag/path).exists(), 'Duplicate owner: '+path
for entry in json.loads((d/'Docs/Migration.json').read_text())['unchangedFiles']:
    data=(d/entry['destination']).read_bytes()
    if Path(entry['destination']).suffix in ('.cs','.csproj','.xml'): data=data.replace(b'\r\n',b'\n')
    assert hashlib.sha256(data).hexdigest()==entry['sha256'], entry
plate=ET.parse(d/'Defs/ThingDefs_Items/Items_MealPlate.xml')
assert plate.findtext('.//ThingDef/defName')=='AMJA_MealPlate'
assert plate.findtext('.//JobDef/defName')=='AMJA_TakeMeal'
texture=plate.findtext('.//graphicData/texPath')
assert len(list((d/'Textures'/texture).glob('*.png')))==3
roles=ET.parse(ag/'Patches/DailyMenu_MealRoles.xml')
assert len(roles.findall('./Operation'))==3
assert 'DailyMenu_Buffet' not in (ag/'Patches/DailyMenu_MealRoles.xml').read_text()
assert len(ET.parse(d/'Patches/DailyMenu_TableMass.xml').findall('./Operation'))==2
for f in (d/'Defs').rglob('*.xml'):
    text=f.read_text()
    assert 'DankPyon_' not in text and 'Things/Item/Meal/AMJC_' not in text
count,errors=validate_inputs([d/'Textures']);assert not errors, errors
print(f'PASS: paired dependency direction, single runtime owner, preserved identities/bytes, XML and {count} PNGs. No game/save acceptance claimed.')
