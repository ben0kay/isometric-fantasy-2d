"""Offline PNG/import contract audit. No runtime readbacks or asset edits.
Run: python TESTS/Seasons/check_foliage_masks.py [--self-test] [healthy.png mask.png]
Without paths, audit explicit inline/tree-variant FoliageMask references in canonical tree .tres files.
"""
from pathlib import Path
import argparse,re,tempfile
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]

def audit(healthy,mask):
    errors=[];warnings=[];stats={}
    try:
        with Image.open(healthy) as image: source=np.asarray(image.convert('RGBA'));size=image.size
        with Image.open(mask) as image:
            if image.format!='PNG':errors.append('mask must be a PNG')
            data=np.asarray(image.convert('RGBA'));mask_size=image.size
    except (OSError,ValueError) as error:return [str(error)],warnings,stats
    if size!=mask_size:return [f'mask {mask_size} differs from artwork {size}'],warnings,stats
    if not np.array_equal(data[:,:,0],data[:,:,1]) or not np.array_equal(data[:,:,0],data[:,:,2]):errors.append('mask is not grayscale; shader reads red only')
    if np.any(data[:,:,3]!=255):errors.append('mask alpha is not opaque; export grayscale weights in red, not alpha')
    red=data[:,:,0];visible=source[:,:,3]>0
    stats={'size':size,'selected_visible':int(np.sum(visible & (red>0))),'grey_visible':int(np.sum(visible & (red>0) & (red<255))),
           'selected_transparent_padding':int(np.sum(~visible & (red>0)))}
    if not stats['selected_visible']:warnings.append('no visible source pixels selected; check whether this is intentional')
    if stats['selected_transparent_padding']:warnings.append('mask selects transparent padding; source alpha still clips it, but export black padding')
    imported=Path(str(mask)+'.import')
    if not imported.exists():warnings.append('no .import file yet; import in Godot and verify lossless/no mipmaps/no resize')
    else:
        text=imported.read_text()
        for setting,expected in [('compress/mode','0'),('mipmaps/generate','false'),('process/size_limit','0'),('process/premult_alpha','false'),('process/channel_remap/red','0')]:
            match=re.search(r'^'+re.escape(setting)+r'=(.+)$',text,re.M)
            if not match or match[1].strip()!=expected:errors.append(f'import requires {setting}={expected}')
    return errors,warnings,stats

def pairs():
    for path in sorted((ROOT/'WORLD/Content/Vegetation/Trees').rglob('*.tres')):
        text=path.read_text();refs={}
        for section in re.findall(r'\[ext_resource[^\n]+',text):
            target=re.search(r'path="res://([^\"]+)"',section);ident=re.search(r'\bid="([^\"]+)"',section)
            if target and ident:refs[ident[1]]=ROOT/target[1]
        for block in re.split(r'(?m)^\[',text):
            mask=re.search(r'^FoliageMask\s*=\s*ExtResource\("([^\"]+)"\)',block,re.M)
            if not mask:continue
            art=re.search(r'^Texture\s*=\s*ExtResource\("([^\"]+)"\)',block,re.M)
            if not art or art[1] not in refs or mask[1] not in refs:raise ValueError(f'{path}: unresolved explicit artwork/mask reference')
            yield path,refs[art[1]],refs[mask[1]]

def self_test():
    with tempfile.TemporaryDirectory() as folder:
        folder=Path(folder);art=folder/'art.png';mask=folder/'mask.png'
        Image.new('RGBA',(4,3),(40,80,40,255)).save(art)
        Image.new('RGBA',(4,3),(128,128,128,255)).save(mask)
        assert not audit(art,mask)[0] and audit(art,mask)[2]['grey_visible']==12
        for size,colour in [((2,3),(255,255,255,255)),((4,3),(255,0,0,255)),((4,3),(255,255,255,128))]:
            Image.new('RGBA',size,colour).save(mask);assert audit(art,mask)[0]
        assert audit(art,folder/'missing.png')[0]
        Image.new('RGBA',(4,3),(128,128,128,255)).save(mask)
        Path(str(mask)+'.import').write_text('compress/mode=2\nmipmaps/generate=true\n')
        assert audit(art,mask)[0]
    print('PASS: valid fractional mask; wrong dimensions, coloured/alpha masks, missing PNG and bad import rejection')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--self-test',action='store_true');parser.add_argument('paths',nargs='*',type=Path);args=parser.parse_args()
    if args.self_test:self_test()
    if len(args.paths) not in [0,2]:parser.error('supply both healthy.png and mask.png, or neither')
    cases=[('explicit pair',*args.paths)] if args.paths else list(pairs())
    if not cases:raise SystemExit('No explicit foliage-mask references found')
    failures=0
    for owner,art,mask in cases:
        errors,warnings,stats=audit(art,mask);failures+=len(errors)
        print(f'{"FAIL" if errors else "PASS"}: {owner}: {mask.name}: {stats}')
        for message in errors:print('  ERROR:',message)
        for message in warnings:print('  NOTE:',message)
    raise SystemExit(1 if failures else 0)
