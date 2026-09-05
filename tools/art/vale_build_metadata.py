"""Generate Vale runtime JSON from inspected pixels and reviewed anchors.

This tool does not write, crop, recolor, resize, or otherwise edit raster images.
Image edits were performed using built-in image_gen; RGB keying occurs at runtime.
"""
from pathlib import Path
import json,hashlib,datetime
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'game/Assets/AfterHours/Fighters/Vale'
SOURCE=ROOT/'design/after-hours-32bit/fighters/vale'
ANCHORS={
'universal':[(154,311),(463,311),(757,308),(1045,298),(185,548),(444,595),(743,564),(1063,540),(159,868),(447,882),(767,882),(1040,884),(128,1202),(441,1201),(779,1193),(1085,1205)],
'normals':[(138,242),(381,242),(630,242),(859,238),(83,491),(332,491),(637,479),(866,482),(105,690),(351,689),(660,695),(891,695),(119,898),(354,902),(644,921),(888,912),(121,1106),(371,1075),(673,1118),(870,1119),(67,1376),(370,1376),(571,1367),(869,1373)],
'techniques':[(122,265),(447,267),(752,218),(1066,224),(141,565),(379,564),(760,529),(991,563),(151,838),(443,838),(743,833),(1046,835),(128,1142),(443,1136),(780,1141),(1060,1142)],
'supplemental':[(140,250),(389,246),(647,242),(921,215),(126,475),(386,437),(635,482),(905,486),(125,683),(411,664),(651,676),(901,687),(140,902),(394,902),(638,910),(900,910),(114,1130),(383,1128),(655,1127),(902,1128),(111,1381),(394,1378),(669,1369),(923,1378)]}
SCALES={'universal':264/289,'normals':264/211,'techniques':264/221,'supplemental':264/215}
PREFIX={'universal':'u','normals':'n','techniques':'t','supplemental':'s'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    inspected=json.loads((DEST/'pixel-inspection.json').read_text())['boards']
    source=json.loads((SOURCE/'manifest.json').read_text())
    canonical=json.loads((ROOT/'data/fighters/vale.json').read_text())['moves']
    cells={};clipmap={};move_active={};sources=[]
    for board in inspected:
        name=board['file'].split('-')[0];ref=next(b for b in source['boards'] if b['file']==name+'.png')
        sources.append({'file':board['file'],'sha256':board['sha256'],'width':board['width'],'height':board['height'],'sourceFile':str((SOURCE/(name+'.png')).relative_to(ROOT)).replace('\\','/'),'sourceSha256':sha(SOURCE/(name+'.png')),'scale':SCALES[name]})
        for measured,definition,anchor in zip(board['cells'],ref['cells'],ANCHORS[name]):
            key=PREFIX[name]+'_'+definition['frame_name'];x,y,w,h=measured['rect']
            x=max(0,x-2);y=max(0,y-2);w=min(w+4,board['width']-x);h=min(h+4,board['height']-y)
            cell={'texture':board['file'],'rect':[x,y,w,h],'pivot':[anchor[0]-x,anchor[1]-y],'scale':round(SCALES[name],7),'facing':-1 if key=='n_c_mk' else 1,'source_cell':definition['cell']}
            if key in ['n_close_mp','n_close_hp']:cell['usable']=False;cell['note']='Source lower legs are cropped; all runtime mappings use full-body supplemental replacement.'
            if name=='techniques' and key not in ['t_pulse','t_shop_return_pulse','t_shop_long_check']:cell['baked_effects']=True
            cells[key]=cell
            for clip in definition.get('mapped_clip_ids',[]):clipmap[clip]=[key]
            if definition['frame_name']!='super_freeze':
                for move in definition.get('mapped_move_ids',[]):move_active[move]=key
    # The original super2 orb overlaps the fingers horizontally, so a straight
    # crop cannot remove it without cutting the hand. A targeted built-in edit
    # supplies only this one replacement cel; all other original cells/textures
    # remain byte-identical and never select the edited board's other poses.
    clean='super2-clean-matte.png'
    assert (DEST/clean).is_file(),'Missing targeted super2 cleanup'
    sources.append({'file':clean,'sha256':sha(DEST/clean),'width':1254,'height':1254,'sourceFile':'game/Assets/AfterHours/Fighters/Vale/techniques-matte.png','sourceSha256':sha(DEST/'techniques-matte.png'),'scale':SCALES['techniques']})
    cells['t_super_2'].update({'texture':clean,'rect':[890,374,203,190],'pivot':[101,189],'baked_effects':False,'note':'Targeted built-in imagegen removal of detached projectile; full fingers/body retained. Other poses continue using the original techniques sheet.'})
    # EX uses the clean family release cel. The actual canonical EX projectile
    # and its renderer effects supply the distinct orb; no second baked orb.
    cells['t_pulse_ex']={**cells['t_pulse'],'baked_effects':False,'shared_pose_from':'t_pulse','note':'Normal and EX pulse share the clean caster release pose. The authoritative canonical projectile and EX effects are rendered separately.'}
    # Prefer original grounded hit and normal wakeup; retain precise alternatives.
    clipmap.update({'hit_high':['s_hit_high'],'wakeup':['s_wakeup'],'wakeup_quickrise':['s_wakeup_quickrise']})
    states={**clipmap,'idle':['u_idle'],'walk':['u_idle','u_walk_forward','u_idle','u_walk_back'],'walk_forward':['u_idle','u_walk_forward'],'walk_back':['u_idle','u_walk_back'],'dash':['u_dash_forward'],'dashback':['u_dash_back'],'crouch':['u_crouch'],'jump':['s_jump_rise','u_jump_apex','u_jump_fall'],'guard':['u_block_high'],'crouchguard':['u_block_low'],'parry':['u_parry_high'],'crouchparry':['u_parry_low'],'hit':['s_hit_high'],'knockdown':['u_knockdown'],'dizzy':['s_dizzy'],'win':['s_win']}
    states.update({'walkback':['u_idle','u_walk_back','u_idle','u_walk_forward'],'takeoff':['u_jump_takeoff'],'rise':['s_jump_rise'],'apex':['u_jump_apex'],'fall':['u_jump_fall'],'superjump':['s_super_jump'],'landing':['u_land'],'airparry':['s_parry_air'],'redparry':['s_parry_red_high'],'redlowparry':['s_parry_red_low'],'lowhit':['s_hit_low'],'airhit':['s_hit_air'],'thrown':['s_throw_victim'],'tech':['s_throw_tech'],'falling':['s_knockdown_fall'],'quickrise':['s_wakeup_quickrise'],'crouchdown':['s_crouch_down'],'crouchup':['s_crouch_up']})
    moves={}
    for move in canonical:
        mid=move['id'];active=move_active[mid]
        if mid.startswith('j_'):startup=['s_jump_rise'];recovery=['u_jump_fall']
        elif mid.startswith('c_') or mid=='shop_low_palm':startup=['u_crouch'];recovery=['s_crouch_up','u_crouch']
        elif mid.startswith('throw_'):startup=['s_throw_tech'];recovery=['s_turn','u_idle']
        elif mid.startswith('super_'):startup=['t_super_freeze'];recovery=['u_land','u_idle']
        elif mid=='leap_overhead':startup=['u_jump_takeoff'];recovery=['u_land']
        elif mid.startswith('rise_'):startup=['u_crouch'];recovery=['u_jump_fall','u_land']
        elif mid.startswith('heel_'):startup=['u_walk_back'];recovery=['u_idle']
        elif mid.startswith('palm_') or mid=='shop_step_feint':startup=['u_walk_forward'];recovery=['u_idle']
        elif mid.startswith('pulse_') or mid=='shop_return_pulse':startup=['u_idle'];recovery=['u_block_high','u_idle']
        else:startup=['u_idle'];recovery=['u_idle']
        moves[mid]={'startup':startup,'active':[active],'recovery':recovery}
    atlas={'schema_version':1,'fighter':'vale','height':264,'chroma_key':[1,0,1],'alpha_mode':'runtime_magenta_key','cells':cells,'states':states,'moves':moves,'textures':sources,'timing':'Phase selection comes from canonical move startup/active/recovery and simulation state. Shared anticipation/recovery keys are intentional; source timings are unchanged.','animation_scope':'All49 canonical moves and universal states have explicit mappings from80 generated source key poses. This is phase-composed sprite animation, not a claim that80 new in-between animation sequences were drawn.','sampling':'nearest; draw rects and local foot pivots at common per-board scale; mirror around pivot. RGB textures require saturated-magenta shader removal.'}
    (DEST/'atlas.json').write_text(json.dumps(atlas,indent=2)+'\n')
    required={'idle','walk','dash','dashback','crouch','jump','guard','crouchguard','parry','crouchparry','hit','knockdown','dizzy','win'}
    refs=[c for ids in states.values() for c in ids]+[c for v in moves.values() for ids in v.values() for c in ids]
    checks={'canonical49Exact':len(moves)==49 and set(moves)=={m['id'] for m in canonical},'allThreeMovePhasesNonempty':all(set(m)=={'startup','active','recovery'} and all(m.values()) for m in moves.values()),'allReferencesResolve':all(c in cells for c in refs),'croppedCellsNeverUsed':not set(refs)&{'n_close_mp','n_close_hp'},'rootStateContractCovered':required<=set(states),'allRectsInsideActualDimensions':all(0<=c['rect'][0]<c['rect'][0]+c['rect'][2]<=next(t for t in sources if t['file']==c['texture'])['width'] and 0<=c['rect'][1]<c['rect'][1]+c['rect'][3]<=next(t for t in sources if t['file']==c['texture'])['height'] for c in cells.values()),'allPivotsInsideRect':all(0<=c['pivot'][0]<=c['rect'][2] and 0<=c['pivot'][1]<=c['rect'][3] for c in cells.values()),'allSourceBoardsUnchanged':all(sha(SOURCE/b['file']).upper()==b['sha256'].upper() for b in source['boards'] if b['file'] in {'universal.png','normals.png','techniques.png','supplemental.png'})}
    checks['pulseExUsesCleanCasterRelease']=all(cells['t_pulse_ex'][k]==cells['t_pulse'][k] for k in ['texture','rect','pivot','scale']) and cells['t_pulse_ex']['baked_effects'] is False
    checks['super2UsesCleanProjectileFreeCell']=cells['t_super_2']['texture']=='super2-clean-matte.png' and cells['t_super_2']['baked_effects'] is False
    validation={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':all(checks.values()),'checks':checks,'moves':len(moves),'cells':len(cells),'states':len(states),'scope':'Actual pixel and JSON metadata checks, not gameplay rendering or physical-device verification.','atlas_sha256':sha(DEST/'atlas.json')}
    (DEST/'validation.json').write_text(json.dumps(validation,indent=2)+'\n');print(json.dumps(validation,indent=2))
    if not validation['passed']:raise SystemExit(1)
if __name__=='__main__':main()
