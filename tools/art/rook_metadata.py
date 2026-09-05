"""Author explicit Rook sprite cells and verify source pixels read-only. No raster editing."""
from pathlib import Path
import json,hashlib,datetime
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'game/Assets/AfterHours/Fighters/Rook'
SOURCE=ROOT/'design/after-hours-32bit/fighters/rook'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    source=json.loads((SOURCE/'manifest.json').read_text(encoding='utf-8'))
    sheets={p['file'].removesuffix('.png'):p for p in source['images'] if p['file']!='identity.png'}
    cells={}
    # Rectangles are manually chosen on the actual generated sheets, not inferred
    # from the design board's nonuniform reading grid. Pivots below are absolute
    # source positions converted to local cell coordinates for the runtime contract.
    universal=[
        ([52,10,242,306],[172,311]),([375,10,208,308],[478,312]),([675,10,229,307],[759,311]),([905,99,317,213],[1038,307]),
        ([45,338,257,277],[177,608]),([381,430,186,192],[475,616]),([655,315,211,307],[741,615]),([967,320,180,246],[1048,552]),
        ([39,680,249,232],[154,904]),([344,622,226,293],[455,908]),([650,718,203,198],[757,909]),([916,624,308,298],[1030,912]),
        ([30,1010,300,195],[134,1199]),([332,919,261,287],[464,1200]),([602,1087,320,113],[755,1196]),([933,1011,247,195],[1073,1198])]
    normal_y=[0,270,525,778,1010,1246,1536]
    normal_x=[0,260,516,770,1024]
    normal_pivots=[
        [123,255],[369,255],[617,253],[842,254],
        [77,510],[347,510],[606,510],[870,509],
        [131,761],[350,749],[604,748],[852,745],
        [107,978],[381,983],[617,972],[874,986],
        [115,1195],[392,1223],[630,1241],[898,1243],
        [126,1498],[348,1491],[610,1495],[900,1495]]
    normals=[([normal_x[i%4],normal_y[i//4],normal_x[i%4+1]-normal_x[i%4],normal_y[i//4+1]-normal_y[i//4]],p) for i,p in enumerate(normal_pivots)]
    normals[9]=([260,525,246,253],normal_pivots[9])
    normals[10]=([503,525,267,253],normal_pivots[10])
    techniques=[
        ([34,82,220,227],[136,300]),([322,104,233,207],[425,300]),([670,15,196,294],[756,280]),([963,0,203,314],[1094,225]),
        ([77,330,142,240],[131,564]),([336,306,223,263],[467,532]),([637,348,231,226],[750,568]),([892,360,247,215],[1000,564]),
        ([26,575,219,275],[141,834]),([269,650,346,190],[398,830]),([616,647,239,194],[726,836]),([883,691,337,151],[994,836]),
        ([32,884,201,278],[128,1158]),([277,978,352,184],[421,1154]),([675,889,166,273],[745,1154]),([919,918,271,244],[1053,1156])]
    supplemental_y=[0,276,538,750,979,1210,1536]
    supplemental_pivots=[
        [135,263],[380,266],[634,266],[874,254],
        [133,508],[382,458],[629,488],[877,518],
        [113,734],[375,735],[651,725],[870,722],
        [124,971],[396,967],[637,971],[880,971],
        [128,1199],[375,1199],[629,1200],[860,1201],
        [132,1456],[382,1458],[633,1455],[876,1457]]
    supplemental=[([i%4*256,supplemental_y[i//4],256,supplemental_y[i//4+1]-supplemental_y[i//4]],p) for i,p in enumerate(supplemental_pivots)]
    rects={'universal':universal,'normals':normals,'techniques':techniques,'supplemental':supplemental}
    scales={'universal':.85,'normals':1.10,'techniques':1.20,'supplemental':1.20}
    pref={'universal':'u','normals':'n','techniques':'t','supplemental':'s'}
    frame_lookup={}
    for sheet,entries in rects.items():
        for frame,(rect,pivot) in zip(sheets[sheet]['frames'],entries,strict=True):
            name=frame['frame_name']
            if sheet=='normals' and name=='close_hp':continue # clipped source never selected at runtime
            key=pref[sheet]+'_'+name
            cells[key]={'texture':sheet+'.png','rect':rect,'pivot':[pivot[0]-rect[0],pivot[1]-rect[1]],'scale':scales[sheet]}
            if name=='turn':cells[key]['facing']=-1
            frame_lookup[(sheet,name)]=key
    move_active={}
    for sheet in ['normals','techniques']:
        for frame in sheets[sheet]['frames']:
            if frame['frame_name']=='close_hp':continue
            for mid in frame.get('mapped_move_ids',[]):move_active[mid]=frame_lookup[(sheet,frame['frame_name'])]
    move_active['close_hp']='s_close_hp_alternative'
    canonical=json.loads((ROOT/'data/fighters/rook.json').read_text(encoding='utf-8'))['moves']
    assert set(move_active)=={m['id'] for m in canonical}
    moves={}
    for move in canonical:
        mid=move['id'];active=move_active[mid]
        if mid.startswith('c_'):
            startup=['s_crouch_down'];recovery=['u_crouch']
        elif mid.startswith('j_'):
            startup=['u_jump_apex'];recovery=['s_jump_fall']
        elif mid.startswith('rise_') or mid=='super_2':
            startup=['s_crouch_down','n_close_mp'];recovery=['u_jump_ascent','u_landing']
        elif mid.startswith('knee_'):
            startup=['u_crouch','t_knee_basic'];recovery=['u_jump_apex','u_landing']
        elif mid.startswith('pulse_') or mid=='super_1':
            startup=['s_walk_back_second_key','u_block_stand'];recovery=['u_walk_back','u_idle']
        elif mid.startswith('sway_') or mid=='shop_sway_feint':
            startup=['u_walk_back'];recovery=['s_walk_back_second_key','u_idle']
        elif mid in ['throw_forward','throw_back','shop_clinch']:
            startup=['u_walk_forward','t_shop_clinch'];recovery=['u_walk_back','u_idle']
        elif mid in ['shop_low_turn','shop_low_drive']:
            startup=['u_crouch'];recovery=['u_landing','s_crouch_up']
        elif mid=='leap_overhead':
            startup=['s_crouch_down','u_jump_ascent'];recovery=['u_landing','u_idle']
        elif mid=='super_3':
            startup=['u_crouch','u_dash_forward'];recovery=['u_landing','u_idle']
        elif mid in ['close_hp','command_fhp','shop_high_hook']:
            startup=['n_close_mp'];recovery=['u_block_stand','u_idle']
        else:
            startup=['u_idle'];recovery=['s_crouch_up','u_idle']
        moves[mid]={'startup':startup,'active':[active],'recovery':recovery}
    states={
      'idle':['u_idle','s_crouch_up'],
      'walk':['u_walk_forward','s_crouch_up','u_idle'],
      'walk_forward':['u_walk_forward','s_crouch_up','u_idle'],
      'walk_back':['u_walk_back','s_walk_back_second_key'],
      'dash':['u_dash_forward'],'dashback':['u_dash_back'],
      'dash_forward':['u_dash_forward'],'dash_back':['u_dash_back'],
      'crouch':['u_crouch','s_crouch_down'],
      'jump':['u_jump_ascent','u_jump_apex','s_jump_fall'],
      'guard':['u_block_stand'],'crouchguard':['u_block_crouch'],
      'parry':['u_parry_stand'],'crouchparry':['u_parry_crouch'],
      'hit':['u_hit_heavy','s_hit_high_recovery'],
      'knockdown':['u_knockdown'],'dizzy':['s_dizzy'],
      'win':['s_win'],'defeat':['s_defeat'],'draw':['s_draw'],
      'turn':['s_turn'],'crouch_down':['s_crouch_down','u_crouch'],
      'crouch_up':['s_crouch_up','u_idle'],'jump_takeoff':['u_crouch','s_jump_takeoff'],
      'jump_rise':['u_jump_ascent'],'jump_apex':['u_jump_apex'],'jump_fall':['s_jump_fall'],
      'super_jump':['s_super_jump'],'land':['u_landing','s_crouch_up'],
      'block_high':['u_block_stand'],'block_low':['u_block_crouch'],
      'parry_high':['u_parry_stand'],'parry_low':['u_parry_crouch'],'parry_air':['s_parry_air'],
      'parry_red_high':['s_parry_red_high'],'parry_red_low':['s_parry_red_low'],
      'hit_high':['u_hit_heavy','s_hit_high_recovery'],'hit_low':['s_hit_low'],'hit_air':['s_hit_air'],
      'throw_victim':['s_throw_victim'],'throw_tech':['s_throw_tech'],
      'knockdown_fall':['s_knockdown_fall'],'knockdown_ground':['u_knockdown'],
      'wakeup':['u_getup','s_crouch_up'],'taunt':['s_taunt'],'intro':['s_intro']}
    aliases={'walkback':'walk_back','rise':'jump_rise','fall':'jump_fall','apex':'jump_apex','airparry':'parry_air',
             'redparry':'parry_red_high','redlowparry':'parry_red_low','lowhit':'hit_low','airhit':'hit_air',
             'thrown':'throw_victim','tech':'throw_tech','falling':'knockdown_fall','takeoff':'jump_takeoff','landing':'land'}
    for alias,target in aliases.items():states[alias]=list(states[target])
    atlas={'fighter':'rook','height':248,'schema_version':1,'chroma_key':[1,0,1],'chroma_tolerance':.36,
      'alpha_mode':'runtime_magenta_key','texture_filter':'nearest','cells':cells,'states':states,'moves':moves,
      'animation_scope':'Explicit phase-selected design cels. Strength variants share authored family key poses; these are not claimed as independently drawn per-tick inbetweens. Gameplay durations come exclusively from canonical core timelines.',
      'embedded_effects':'Uppercut/super trails and defensive sparks remain cosmetic. Detached pulse/basic/EX/super1 projectile rings are excluded by tight fighter crop rectangles; moving projectiles are rendered separately.',
      'excluded_source_pose':'normals.close_hp is clipped in the reference; complete-foot supplemental.close_hp_alternative is the runtime replacement.'}
    # Pixel validation is read-only. No PNG is written or resampled by this tool.
    images={sheet:Image.open(OUT/(sheet+'.png')) for sheet in sheets}
    cell_checks=[]
    for name,cell in cells.items():
        im=images[cell['texture'].removesuffix('.png')];x,y,w,h=cell['rect'];px,py=cell['pivot']
        assert 0<=x<x+w<=im.width and 0<=y<y+h<=im.height,(name,'bounds')
        assert 0<=px<=w and 0<=py<=h,(name,'pivot')
        count=0;foreground=0
        for sy in range(y,y+h,3):
            for sx in range(x,x+w,3):
                r,g,b=im.getpixel((sx,sy))[:3];count+=1
                if not (r-g>64 and b-g>64 and r+b>230):foreground+=1
        assert foreground>100,(name,'empty cell')
        border=[(sx,sy) for sy in (y,y+h-1) for sx in range(x,x+w)]+[(sx,sy) for sx in (x,x+w-1) for sy in range(y,y+h)]
        border_foreground=0
        for sx,sy in border:
            r,g,b=im.getpixel((sx,sy))[:3]
            if not (r-g>64 and b-g>64 and r+b>230):border_foreground+=1
        assert border_foreground==0,(name,'foreground touches crop border',border_foreground)
        cell_checks.append({'name':name,'rect':cell['rect'],'pivot':cell['pivot'],'sampledForegroundPixels':foreground,'sampledPixels':count,'foregroundCropBorderPixels':border_foreground})
    assert all(cell in cells for phases in moves.values() for keys in phases.values() for cell in keys)
    assert all(cell in cells for keys in states.values() for cell in keys)
    (OUT/'atlas.json').write_text(json.dumps(atlas,indent=2)+'\n',encoding='utf-8')
    origins={
      'universal':'exec-634d3d09-b0a4-4e6c-a77c-c54327135fa3.png',
      'normals':'exec-54cea0a4-5a4f-48a0-90b8-b5c869ac154f.png',
      'techniques':'exec-cff8c6d6-b7d6-4265-a17a-da3f983f4294.png',
      'supplemental':'exec-fd92032c-2f3f-4017-939b-db71610f601a.png'}
    provenance={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'generator':'built-in image_gen__imagegen; no API/CLI fallback',
      'source_pack':'design/after-hours-32bit/fighters/rook','originals_preserved':True,
      'alpha_attempt':{'prompt':'prompts/universal-alpha.txt','output':'tools/art/rook-provenance/universal-alpha-failed.png','mode':'RGB','alpha_present':False,'decision':'Built-in extraction again produced a baked checkerboard. Root authorized magenta-matte runtime shader fallback. No false transparent-PNG claim.'},
      'raster_postprocessing':'None. Generated PNGs copied byte-for-byte; Python only authored JSON and read pixels for validation.',
      'images':[{'texture':sheet+'.png','source_sha256':sha(SOURCE/(sheet+'.png')),'generated_sha256':sha(OUT/(sheet+'.png')),'mode':images[sheet].mode,'dimensions':list(images[sheet].size),'true_alpha':False,'prompt':'prompts/'+sheet+'-chroma.txt','generated_file':origins[sheet]} for sheet in sheets],
      'validation':{'passed':True,'runtime_cells':len(cells),'mapped_moves':len(moves),'state_keys':len(states),'all_bounds_pivots_and_references_valid':True,'read_only_cell_checks':cell_checks},
      'known_limits':['Magenta backdrop values vary slightly around requested FF00FF; use hue/dominance key or tolerant chroma distance, never exact equality.','79 selected cels retain the original design pose coverage; phase changes share suitable anticipation/recovery cels and do not fabricate distinct per-frame artwork.','Runtime renderer must use nearest filtering and the declared chroma shader; PNG files intentionally remain opaque RGB.']}
    (OUT/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'passed':True,'cells':len(cells),'moves':len(moves),'states':len(states),'atlas':str(OUT/'atlas.json')}))

if __name__=='__main__':main()
