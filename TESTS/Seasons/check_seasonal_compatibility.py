"""Source dependency checks for exact seasonal save bridges; no Godot/disk-save execution.
Run from repo root. Git history must contain the shipped revisions below.
"""
from pathlib import Path
import hashlib,re,subprocess
ROOT=Path(__file__).resolve().parents[2]
def sha(s):return hashlib.sha256(s.replace('\r\n','\n').encode()).hexdigest().upper()
def read(path):return (ROOT/path.removeprefix('res://')).read_text()
def historic(rev,path):return subprocess.check_output(['git','show',f'{rev}:{path.removeprefix("res://")}'],cwd=ROOT,text=True)
def fingerprint(reader,substitutions=None):
    files={};substitutions=substitutions or {}
    def visit(path):
        if path in files:return
        source=reader(path).replace('\r\n','\n')
        if path in substitutions:source=read(substitutions[path]).replace('\r\n','\n')
        files[path]='\n'.join(line for line in source.split('\n') if not line.startswith(('RandomSeedOnLoad = ','FixedSeed = ')))
        for dependency in re.findall(r'path="(res://[^\"]+\.(?:tres|tscn))"',source):visit(dependency)
    visit('res://WORLD/DefaultWorldConfig.tres');visit('res://SCENES/World/World.tscn')
    return sha(''.join(path+'\n'+files[path]+'\n' for path in sorted(files)))
prefix='res://SAVE/Migrations/References/'
oak='res://WORLD/Content/Vegetation/Trees/Oak/Oak.tres';season='res://ENVIRONMENT/Seasons/DefaultSeasonConfig.tres'
sets=[{oak:prefix+'SeasonsPreSE1Oak.txt','res://ENVIRONMENT/DefaultEnvironmentConfig.tres':prefix+'SeasonsPreSE0Environment.txt'},
      {oak:prefix+'SeasonsPreSE1Oak.txt',season:prefix+'SeasonsSE0Config.txt'},
      {season:prefix+'SeasonsSE0Config.txt','res://WORLD/Content/Vegetation/Trees/Oak/OakSeasonalFoliage.tres':prefix+'SeasonsSE1Palette.txt'}]
source=read('res://SAVE/Migrations/SeasonalSaveCompatibility.cs')
gates=re.findall(r'\("(res://[^\"]+)", "([A-F0-9]{64})"\)',source)
assert len(gates)==4 and all(sha(read(path))==expected for path,expected in gates)
# The previous PNG bridge freezes the terrain state shared by the shipped seasonal revisions.
terrain={'res://WORLD/Biomes/Definitions/OakForest/Biome.tres':prefix+'PngDirtOakBefore.txt',
         'res://WORLD/Terrain/Ground/Surfaces/Profiles/Dirt.tres':prefix+'PngDirtProfileBefore.txt'}
revisions=['01237bec1e0fe17b494d705380d470c5cd71aec7','45b55ff9a5c3edc80389ae1b999a37bd9287830e','3156681a149978d0f745a0dcebc83767218f6a4f']
for references,revision in zip(sets,revisions):
    expected=fingerprint(lambda p:historic(revision,p))
    actual=fingerprint(read,{**terrain,**references})
    assert expected==actual,(revision,expected,actual)
print('PASS: exact pre-SE0 / SE0 / SE1 dependency fingerprints reconstructed across the gated terrain bridge')
current=fingerprint(read)
assert current==fingerprint(lambda p:historic('c1db99b',p))
print('PASS: SE2 does not change the current content fingerprint or save schema')
# Changing unrelated gameplay definitions must still change every reconstructed fingerprint.
for references in sets:
    substitutions={**terrain,**references}
    expected=fingerprint(read,substitutions)
    altered=lambda p:read(p)+('\nUnrelatedGameplayChange = true\n' if p=='res://WORLD/DefaultWorldConfig.tres' else '')
    assert fingerprint(altered,substitutions)!=expected
assert all(sha(read(path)+'\nUnexpected = true\n')!=expected for path,expected in gates)
# Frozen old Oak must differ only by the exact visual mask/profile additions, never placement/collision.
old=read(prefix+'SeasonsPreSE1Oak.txt');new=read(oak)
stripped='\n'.join(line for line in new.split('\n') if not any(x in line for x in ['id="foliage_mask"','id="seasonal"','FoliageMask =','SeasonalFoliage =']))
assert stripped.replace('\n\n\n','\n\n')==old
print('PASS: unrelated changes reject, changed current visuals disable bridge, and Oak footprint/generation data is unchanged')
