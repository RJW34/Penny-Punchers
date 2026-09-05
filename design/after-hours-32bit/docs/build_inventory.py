"""Rebuild additive design metadata from canonical data; never edit prototype files.

Run from any directory with Python 3. Outputs stay beside this script. Images are
reference boards, not validated runtime sprites. Coverage describes assigned
design references and explicitly records production still needed.
"""
from __future__ import annotations
import csv
import hashlib
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
PACK = HERE.parents[2]
ART = HERE.parent

def read(rel):
    return json.loads((PACK / rel).read_text(encoding="utf-8"))

def write_json(name, value):
    (HERE / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

def write_csv(name, rows):
    with (HERE / name).open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

fighter_art_manifests = {fid: json.loads((ART / f"fighters/{fid}/manifest.json").read_text(encoding="utf-8")) for fid in ("rook", "vale")}

def art_bindings(fid, key, value):
    manifest = fighter_art_manifests[fid]
    bindings = []
    for board in manifest.get("boards", manifest.get("images", [])):
        for cell in board.get("cells", board.get("frames", [])):
            if value in cell.get(key, []):
                bindings.append(dict(board=f"fighters/{fid}/{board['file']}", row=cell.get("row"), column=cell.get("column"),
                    frame_name=cell.get("frame_name"), status="mapped_design_reference_not_finished_animation"))
    return bindings

rows = []
def cover(category, ident, component, sources, reference, design, outstanding, states="default", scope="current_contract"):
    rows.append(dict(category=category, id=ident, component=component, canonical_source=sources,
        reference_art=reference, design=design, required_variants=states,
        art_status="assigned_design_reference", runtime_status="not_integrated",
        production_outstanding=outstanding, scope=scope))

screens = {
    "title": ("Title / attract / main menu", "ui/title-backdrop.png;ui/interface-atlas.png", "Stacked STRIKE LEDGER wordmark; original foundry exhibition; readable menu strip", "title idle;keyboard focus;pad focus;mouse hover"),
    "select": ("Lineup and super-art selection", "ui/menu-designs.png;ui/interface-atlas.png;fighters/rook/identity.png;fighters/vale/identity.png", "Two corner cards with portraits, fighter name, archetype, three named arts and numeric costs", "local;CPU;training;mirror match;P1/P2;selected art;device assignment"),
    "prep": ("Local round preparation", "ui/menu-designs.png;ui/lease-icons.png;ui/hud-atlas.png", "Independent procurement cards; three lease slots; budget calculation; lock stamp", "empty;selected;unaffordable;reserve protected;locked;timeout;both revealed"),
    "fight": ("Fight arena", "stages/foundry.png;ui/hud-atlas.png;effects/combat-atlas.png", "Quiet background behind large silhouettes; health and cash remain readable", "CPU;local;private;normal/mirror;round lifecycle"),
    "results": ("Match settlement", "ui/menu-designs.png;fighters/rook/identity.png;fighters/vale/identity.png", "Receipt-like final points and credits with winner portrait and rematch controls", "P1 win;P2 win;draw;rematch;lineup;replay saved"),
    "pause": ("Pause menu", "ui/interface-atlas.png;ui/menu-designs.png", "Dark translucent scrim; compact resume-first menu", "local;private pause;controller disconnected"),
    "help": ("Field manual", "ui/interface-atlas.png;ui/typography.png", "Legible printed workshop manual; diagrams for motion, defense and single wallet", "controls;motions;charge;economy;free kit;page/footer"),
    "moves": ("Command reference", "ui/interface-atlas.png;ui/typography.png", "Fixed columns for move, relative command, startup/active/recovery and credit cost", "Rook/Vale;5 pages each;free;EX;selected/unselected super;lease-only"),
    "settings": ("Preferences", "ui/interface-atlas.png;ui/typography.png", "Volume sliders and clear text/shape toggle states", "master;music;SFX;reduced flashes;shake;fullscreen;resolution;VSync"),
    "controls": ("Controls and live device test", "ui/interface-atlas.png;ui/typography.png", "Separate player/device cards; six attack buttons; raw input and SOCD display", "keyboard;controller;binding capture;deadzone;connected;disconnected;unassigned"),
    "replays": ("Replay archive", "ui/interface-atlas.png;ui/menu-designs.png", "Tape-catalog rows with date/name and verification status", "empty;populated;selected;malformed/rejected"),
    "replay_controls": ("Replay controls", "ui/interface-atlas.png;ui/typography.png", "Transport controls and optional analytic overlays", "play/pause;single step;seek;restart;0.5x/1x/2x;inputs;boxes;receipts;export"),
    "labmenu": ("Training menu", "ui/interface-atlas.png;ui/menu-designs.png", "Workshop panel; plainly marked training-only controls", "drill;reset;swap sides;checkpoint;record/stop dummy;loop;dummy mode;export"),
    "lab_state": ("Training state editor", "ui/interface-atlas.png;ui/typography.png", "Two player columns of exact health/stun/credit/position values", "P1/P2;state increments;0.25x/0.5x/1x;return to practice"),
    "network_menu": ("Private host / join", "ui/interface-atlas.png;ui/menu-designs.png", "Private connection form with address, port, phrase and lineup", "host;join;valid/invalid fields;pad text entry"),
    "network": ("Connecting screen", "ui/interface-atlas.png", "Small stepped connection glyph plus plain peer diagnostic", "connecting;waiting for peer;cancel"),
    "network_prep": ("Private round preparation", "ui/menu-designs.png;ui/lease-icons.png", "Only own draft shown; commitment/reveal explanation", "unlocked;unaffordable;reserve;lock;timeout"),
    "network_wait": ("Commitment sent / waiting reveal", "ui/interface-atlas.png;ui/hud-atlas.png", "Closed ledger seal with waiting text; no opponent draft exposure", "commitment sent;waiting reveal;diagnostics"),
    "network_failed": ("Connection ended", "ui/interface-atlas.png", "Broken-link glyph, reason, reconnect and back", "aborted;disconnected;desync;no incomplete-round payout"),
    "pad_text": ("Controller text editor", "ui/interface-atlas.png;ui/typography.png", "Grid keys and high-contrast caret; large backspace/clear/done targets", "numeric/IP;match phrase;focus;pressed;empty;max length"),
}
for ident, (component, ref, design, states) in screens.items():
    if ident in ("settings", "controls", "replays", "replay_controls", "labmenu", "lab_state", "network_menu", "network", "network_prep", "network_wait", "network_failed", "pad_text", "pause"):
        ref += ";ui/system-designs.png"
    if ident in ("title", "results", "fight"):
        ref += ";ui/branding-announcements.png"
    cover("screen", ident, component, "game/Main*.cs", ref, design,
        "Extract reusable panels; rebuild text with verified font/glyphs; adapt full layout at 640x360; verify every focus path", states)

hud = [
    ("portrait", "Corner portrait", "P1/P2;Rook/Vale;mirror", "Crop identity bust; separate corner frame"),
    ("fighter_name", "Fighter label", "Rook;Vale;P1/P2", "Cream uppercase beside permanent corner shape"),
    ("health_frame", "Health bar frame", "P1/P2", "Thin angular frame; inward-facing fills"),
    ("health_fill", "Health fill and empty track", "full;partial;empty;P1/P2", "Single health resource, readable by occupied length"),
    ("stun", "Nonspendable stun indication", "0;partial;dizzy threshold", "STUN text and numeric value; optional separated diagnostic ticks"),
    ("score", "Round points", "0;half points;5 target;draw", "Round points as numerals or paired half-point pips"),
    ("timer", "Round timer", "00-99;paused;time out", "Cream tabular numerals in central housing"),
    ("round", "Round number", "1-9", "Small amber round caption under timer"),
    ("wallet", "Credit balance", "0-3600;P1/P2", "Large tabular number and CR suffix"),
    ("ex_affordability", "EX availability", "ready;low funds;reserve protected", "300 CR with filled diamond / open diamond / lock notch plus text"),
    ("super_affordability", "Selected super", "art1/2/3;900/1200/1500;ready;low;reserve", "Named art and cost with discrete shape state"),
    ("reserve", "Reserve floor", "0;set;blocking activation", "Small lock/floor mark with numeric amount beside wallet"),
    ("lease_slots", "Equipped round leases", "signature;technique;gambit;empty;base kit", "Three distinct slot shapes with 12 original icons"),
    ("spend_receipt", "Reason-tagged debit", "EX;SUPER;MOVE;P1/P2", "Compact receipt near cash; minus numeral; never contact center"),
    ("rejection", "Credit rejection", "insufficient credits;reserve protected", "Outline lock/rejected stamp with reason text; subtle rate-limited cue"),
    ("settlement", "Confirmed round payout", "win;loss recovery tiers;draw;cap", "Receipt addition only after confirmed round settlement"),
    ("preparation_budget", "Preparation balance preview", "plan cost;after lock;win/loss projection", "Exact numeric arithmetic and assumption caption"),
    ("floor_markers", "Player floor markers", "P1 triangle;P2 notched chevron", "Shape-coded feet location separate from fighter palette"),
    ("match_footer", "Match rule and pause hint", "first to5;max9;keyboard;controller", "Narrow quiet footer"),
    ("network_diagnostic", "Private match status", "connected;delay frames;rollback count;stall", "Link glyph and plain diagnostic values"),
    ("toast", "Toast / validation / save notice", "info;success;rejected;disconnect", "Bounded panel below HUD without hiding contact"),
    ("training_footer", "Training controls/status strip", "live;paused;dummy;drill", "Clearly marked LAB with live input summary"),
    ("input_history", "Input history", "P1/P2;1-9;button/chord;press/release", "Direction glyph plus button token; retain timestamps"),
    ("lab_diagnostics", "Frame and training diagnostics", "startup/active/recovery;hitstun;blockstun;charge;combo;damage;advantage;parry;parsed/denied", "Compact table with tabular font; no ornamental numerals"),
    ("drill_feedback", "Drill status", "in progress;complete;timeout/reset", "Distinct check/loop/clock shapes with text"),
    ("replay_transport", "Replay status strip", "verified playback;paused;tick;speed;end", "Tape corner marker and exact tick numerals"),
    ("wallet_graph", "Replay wallet graph", "P1/P2;0-3600;ticks", "Recorded balance chart with labels and differing line markers"),
    ("replay_receipts", "Replay spend receipts", "preparation;EX;super;settlement;rejected", "Transaction table using same receipt style"),
]
for ident, component, states, design in hud:
    refs = "ui/hud-atlas.png;ui/typography.png"
    if ident in ("lease_slots",): refs += ";ui/lease-icons.png"
    if ident == "portrait": refs += ";fighters/rook/identity.png;fighters/vale/identity.png"
    cover("hud", ident, component, "game/Main.cs;game/Main.PrepDetails.cs;game/Main.LabDiagnostics.cs;game/Main.ReplayDetails.cs;docs/11_UI_CONTROLS_AND_ACCESSIBILITY.md", refs, design,
        "Isolate tile/frame; author numeric layout and every state; preserve canonical values and safe areas", states)

banners = ["LOADOUTS LOCKED", "GET READY", "FIGHT", "K.O.", "TIME OUT", "DRAW", "DOUBLE K.O.", "CONFIRMING RESULT", "ROUND SETTLED", "MATCH COMPLETE", "P1 WINS", "P2 WINS", "PERFECT", "REPLAY VERIFIED", "DRILL COMPLETE"]
for phrase in banners:
    cover("round_feedback", re.sub(r"[^a-z0-9]+", "_", phrase.lower()).strip("_"), phrase,
        "game/Main.cs;src/StrikeLedger.Core/SimulationState.cs", "ui/branding-announcements.png;ui/hud-atlas.png;ui/typography.png",
        "Condensed cream/amber lettering, offset ink shadow, bounded corner-rule frame",
        "Typeset exact string and animate entry/hold/exit; bind only to real terminal reason; PERFECT is optional cosmetic derivation",
        "entry;hold;exit;reduced motion", "current_contract" if phrase in ("LOADOUTS LOCKED", "GET READY", "K.O.", "CONFIRMING RESULT", "ROUND SETTLED", "MATCH COMPLETE") else "presentation_extension")

widgets = [
    ("button", "normal;hover;pressed;focused;disabled"), ("panel", "opaque;translucent;modal;two-corner"),
    ("focus_cursor", "P1 triangle;P2 chevron;keyboard;pad"), ("selection", "selected;unselected;locked"),
    ("slider", "track;fill;thumb;focus;min/max"), ("toggle", "on;off;focus;unavailable"),
    ("text_field", "empty;filled;focused;caret;invalid"), ("tab", "active;inactive;disabled"),
    ("pagination", "previous;next;current page/total"), ("list_row", "normal;focused;disabled;empty"),
    ("tooltip", "move property;lease tradeoff;price"), ("scrollbar", "track;thumb;arrows;focus"),
    ("divider", "horizontal;vertical;corner rules"), ("badge", "P1;P2;CPU;LAB;PRIVATE;REPLAY"),
    ("status_icon", "success;warning;error;info;lock;unlock;ready;waiting"),
    ("transport", "play;pause;record;stop;loop;step;rewind;forward;restart"),
    ("device", "keyboard;generic controller;connected;unplugged;unassigned;deadzone"),
    ("navigation", "back;confirm;cancel;left;right;up;down;swap"),
    ("save_export", "save;folder;replay tape;export;receipt"),
    ("spinner", "4 stepped phases;static reduced-motion state"),
    ("app_icon", "16;32;48;64;128;256px original ledger/ring mark"),
    ("pointer", "default;interactive;busy;OS cursor fallback"),
]
for ident, states in widgets:
    cover("ui_component", ident, ident.replace("_", " ").title(), "game/Main.Menus.cs;game/Main*.cs", "ui/interface-atlas.png" + (";ui/branding-announcements.png" if ident == "app_icon" else ""),
        "Hard pixel corners, ink/cream hierarchy, amber focus; shape and text supplement hue",
        "Extract/export exact-size tiles; define slice insets and state mapping; app icon/pointer may need new small-size redraws", states,
        "presentation_extension" if ident in ("app_icon", "pointer", "tab", "scrollbar", "tooltip") else "current_contract")

glyphs = {
    "display_alphabet": "A-Z;0-9;STRIKE LEDGER wordmark;AFTER HOURS campaign subtitle",
    "body_alphabet": "ASCII U+0020-U+007E;upper/lowercase;tabular 0-9;punctuation",
    "extended_symbols": "←→↑↓↔;✓;●○;×;−;–;—;…;·;▌;±;≤≥;≥;percent;plus;slash;colon;decimal",
    "directions": "numpad1-9;8 directions;neutral;screen-relative vs facing-relative examples",
    "attacks": "LP;MP;HP;LK;MK;HK;P;K;PP;KK;release;held",
    "motions": "QCF;QCB;DP;double QCF;charge back;charge down;hold45ticks;double-tap dash",
    "chords": "LP+LK;MP+MK;HP+HK;back+HP+HK;simultaneous chord bracket",
    "keyboard_caps": "WASD;UIO;JKL;P;semicolon;Enter;Escape;F1-F5;F12;period;arrows;Backspace",
    "controller_caps": "generic A/B/X/Y;D-pad;start;shoulders;triggers;stick;device number",
    "economy": "CR;0-3600;300;600;900;1200;1500;reserve lock;cost/debit/credit signs",
    "combat_labels": "HIT;BLOCK;PARRY;COUNTER;THROW TECH;STUN;START;ACTIVE;REC;FREE;LEASE",
}
for ident, states in glyphs.items():
    cover("typography_glyph", ident, ident.replace("_", " ").title(), "data/inputs.json;game/Main*.cs", "ui/typography.png;ui/interface-atlas.png",
        "Display lettering for large labels; restrained legible body face and tabular numbers; no copied arcade logo",
        "Manually redraw/verify glyphs; assemble bitmap font or use licensed readable font; generated board is not a complete font file", states)

stage_parts = [
    ("foundry", "Foundry Ring composition", "stages/foundry.png", "640x360 viewport;1024x360 world-width master;floor y302"),
    ("grid", "Training measurement composition", "stages/grid.png", "identical floor/corners;20 world-unit grid;100-unit labels"),
    ("far_shell", "Far building silhouette / ceiling", "stages/foundry.png", "static base;low-contrast"),
    ("clerestory", "Amber clerestory window bays", "stages/environment-atlas.png", "lit;dim;bay repetition"),
    ("columns", "Structural columns and rivets", "stages/environment-atlas.png", "left;center;right;bolts;cross braces"),
    ("cable_races", "Ceiling cable races and gantry", "stages/environment-atlas.png", "repeatable beams;cables;hanger"),
    ("sign", "Original exhibition sign", "stages/environment-atlas.png", "Foundry Ring;original ring/ledger emblem;sign supports"),
    ("machinery", "Industrial machinery", "stages/environment-atlas.png", "left/right;grilles;vents;pipes;indicator lamps"),
    ("gallery", "Background audience", "stages/environment-atlas.png", "quiet idle;restrained cheer;multiple silhouettes;no foreground occlusion"),
    ("rail", "Gallery barrier and railing", "stages/environment-atlas.png", "horizontal repeat;posts;corner caps"),
    ("haze", "Interior haze", "stages/environment-atlas.png", "thin alpha mask;reduced effects/static"),
    ("floor", "Flat fighting floor", "stages/foundry.png", "world-anchored perspective joints;contact line at302;no ledges"),
    ("floor_decal", "Exhibition floor decal", "stages/environment-atlas.png", "world-center original wordmark;faded paint"),
    ("corners", "Solid corner pylons", "stages/environment-atlas.png", "left01;right02;hazard stripe decoration;geometry unchanged"),
    ("embers", "Ambient embers / dust", "stages/environment-atlas.png", "1px/2px/3px clusters;slow loop;off/static option"),
    ("light_shafts", "Shallow warm light shafts", "stages/environment-atlas.png", "dithered optional overlay;no contact-zone flash"),
    ("foreground", "Low foreground trim", "stages/environment-atlas.png", "footer industrial label;trim below combat plane;no fighter occlusion"),
    ("shadow", "Fighter ground shadow", "effects/combat-atlas.png", "near;medium;high-jump;soft layered pixel ellipses"),
    ("grid_lines", "Measurement-grid minor and major lines", "stages/grid.png", "world coordinates;20-unit minor;100-unit major;floor and axis labels"),
    ("grid_label", "Measurement floor caption", "stages/grid.png;ui/typography.png", "MEASUREMENT FLOOR;IDENTICAL ARENA GEOMETRY;world-unit numerals"),
    ("debug_boxes", "Hit / hurt / push boxes", "ui/interface-atlas.png", "hit solid;hurt dotted;push dashed;legend;show/hide"),
]
for ident, component, ref, variants in stage_parts:
    cover("stage_layer", ident, component, "data/stages/*.json;game/Presentation/ArenaView.cs", ref,
        "Original after-hours industrial exhibition; clear ground and low-contrast background",
        "Separate layers/masks from reference; author seam-safe horizontal overscan; align camera/parallax and exact ground; validate at both corners", variants)

effects = [
    ("hit_light", "Small four-spoke contact burst", "hit;strength1"),
    ("hit_medium", "Offset six-spoke contact burst", "hit;strength2"),
    ("hit_heavy", "Broad angular contact star", "heavyhit;strength3"),
    ("counterhit", "Double star and brief COUNTER tag", "counterhit;all strengths"),
    ("block_high", "Hard shield arc with square fragments", "block;high;all strengths"),
    ("block_low", "Low shield arc and square fragments", "block;low;all strengths"),
    ("parry_high", "Thin diamond ring and straight streaks", "Parry High;success"),
    ("parry_low", "Low diamond ring with upward tip", "Parry Low;success"),
    ("parry_air", "Suspended hollow diamond ring", "Parry Air;success"),
    ("parry_red_high", "Double diamond ring plus red accent", "RedHigh;success;shape remains distinct in grayscale"),
    ("parry_red_low", "Low double diamond ring plus red accent", "RedLow;success;shape remains distinct in grayscale"),
    ("throw_tech", "Opposed break brackets and short spark", "ThrowTech"),
    ("throw_slam", "Downward impact wedge and floor dust", "Throw detail damage"),
    ("projectile_clash", "Two crossing arcs and collapsing shards", "ProjectileClash"),
    ("projectile_dissipate", "Shrinking pulse segments", "hit;blocked;parried;expired;round reset"),
    ("dash_dust", "Low swept dust puffs", "Dash forward/back"),
    ("jump_dust", "Compact takeoff dust", "Jump normal/super"),
    ("landing_dust", "Flattened landing ring", "Land normal;air attack;knockdown"),
    ("wakeup_dust", "Sparse settle pixels", "Land wakeup;optional restrained cue"),
    ("knockdown", "Floor dust fan with isolated contact notch", "soft;hard;KO;air fall"),
    ("dizzy", "Three orbiting stagger diamonds", "Dizzy start;loop;clear"),
    ("ex_start", "Twin corner brackets around action silhouette", "all8 EX families;all palettes;no earned gauge"),
    ("super_start", "Radial ledger-rule spokes and held silhouette", "six arts;superfreeze hold;no obscuring opaque screen fill"),
    ("motion_trail", "Sparse limb afterimage contour", "kicks;palm;elbow;knee;super;no collision authority"),
    ("round_terminal", "Small diagonal result wipe", "KO;timeout;draw;confirmed result"),
    ("wallet_accept", "Small debit receipt edge stamp", "Spend;preparation;confirmed settlement"),
    ("wallet_deny", "Outline locked/rejected seal", "Rejected credits;Rejected reserve"),
]
for ident, design, variants in effects:
    cover("effect", ident, ident.replace("_", " ").title(), "src/StrikeLedger.Core/SimulationState.cs;game/Main.Rendering.cs;game/Presentation/ArenaView.cs", "effects/combat-atlas.png",
        design, "Extract transparent frames; draw missing temporal phases; set pivot/clip bounds; supply reduced-flash version; bind real event subtype", variants)

universal = [
    ("idle", "Breath/weight shift;guarded silhouette", "idle", 6, "loop"),
    ("walk_forward", "Guarded forward gait;root from sim", "walk plus signed delta", 8, "loop"),
    ("walk_back", "Defensive backward gait;not reverse playback", "walk plus signed delta", 8, "loop"),
    ("turn", "Pivot stance for facing change", "Facing/FacingEpoch", 3, "one_shot"),
    ("crouch_down", "Lower center while retaining guard", "crouch enter", 3, "one_shot"),
    ("crouch", "Compressed resting guard", "crouch", 3, "loop"),
    ("crouch_up", "Rise from low stance", "crouch exit", 3, "one_shot"),
    ("dash_forward", "Low committed forward burst", "dash", 5, "one_shot"),
    ("dash_back", "Backward retreat with face forward", "dashback", 5, "one_shot"),
    ("jump_takeoff", "Coil then release", "JumpStart", 3, "one_shot"),
    ("jump_rise", "Tucked knees as body rises", "jump plus Vy>0", 3, "hold"),
    ("jump_apex", "Open midair balance", "jump plus apex", 2, "hold"),
    ("jump_fall", "Feet anticipate landing", "jump plus Vy<0", 3, "hold"),
    ("super_jump", "Stronger release and distinct tucked ascent", "Jump event detail or launch speed", 4, "one_shot"),
    ("land", "Foot compression and balance recovery", "Land;LandingTicks", 3, "one_shot"),
    ("block_high", "Upright forearm shield", "guard", 3, "hold"),
    ("block_low", "Crouched low coverage", "crouchguard", 3, "hold"),
    ("parry_high", "Open forward hand/forearm interception", "parry plus High", 3, "one_shot"),
    ("parry_low", "Low open-hand interception", "crouchparry plus Low", 3, "one_shot"),
    ("parry_air", "Airborne open interception", "parry plus Air", 3, "one_shot"),
    ("parry_red_high", "Sharp upright interception after guard", "parry plus RedHigh", 3, "one_shot"),
    ("parry_red_low", "Sharp low interception after guard", "crouchparry plus RedLow", 3, "one_shot"),
    ("hit_high", "Head/upper torso recoil", "hit plus contact height", 3, "hold"),
    ("hit_low", "Hip/knee recoil", "hit plus contact height", 3, "hold"),
    ("hit_air", "Airborne recoiling silhouette", "hit plus airborne", 3, "hold"),
    ("throw_victim", "Paired captured/compressed/rotated/released poses", "ThrowAttacker/ThrowAge presentation exposure needed", 5, "one_shot"),
    ("throw_tech", "Hands disengage with retreat", "ThrowTech", 3, "one_shot"),
    ("knockdown_fall", "Tumble to horizontal impact", "Knockdown event;airborne", 4, "one_shot"),
    ("knockdown_ground", "Grounded rest on back/side", "knockdown", 2, "hold"),
    ("wakeup", "Roll/kneel/rise;no authored root drift", "Land detail wakeup", 5, "one_shot"),
    ("dizzy", "Slack guard;orbit glyphs separate", "dizzy", 5, "loop"),
    ("taunt", "Original compact self-directed gesture", "ActionStarted move taunt;24tick cosmetic commit", 6, "one_shot"),
    ("intro", "Enter stance from relaxed posture", "round presentation extension", 6, "one_shot"),
    ("win", "Original restrained victory gesture", "win", 6, "loop_tail"),
    ("defeat", "Grounded loss/KO hold", "knockdown plus Health0", 3, "hold"),
    ("draw", "Recover to mutual guarded rest", "terminal draw presentation extension", 3, "hold"),
]
universal_contract = []
for fid in ("rook", "vale"):
    for ident, design, trigger, frames, loop in universal:
        clip_bindings = art_bindings(fid, "mapped_clip_ids", ident)
        assert clip_bindings, f"Missing universal reference: {fid}.{ident}"
        clip_boards = list(dict.fromkeys(b["board"] for b in clip_bindings))
        cover("fighter_universal", f"{fid}.{ident}", f"{fid.title()} {ident.replace('_', ' ')}",
            "game/Main.Rendering.cs;src/StrikeLedger.Core/SimulationState.cs;docs/08_CONTENT_AND_PRESENTATION.md",
            ";".join(clip_boards + [f"fighters/{fid}/identity.png"]), design,
            "Reference may share related pose; production requires dedicated temporal frames, all contact/entry/exit phases, pivots, mirror palette and facing verification",
            "P1/P2 palette;left/right facing;entry/hold/exit;hitstop;replay seek")
        universal_contract.append(dict(fighter=fid, clip_id=ident, trigger=trigger, suggested_unique_cels=frames,
            loop=loop, source_board=clip_boards[0], reference_frames=clip_bindings, status="animation_production_pending",
            note="Cel estimate is an art planning target, not simulation duration or proof of delivery."))
    for ident in ("full_body_p1", "full_body_p2", "neutral_portrait", "versus_portrait", "victory_portrait", "defeat_portrait", "small_hud_portrait", "select_thumbnail"):
        cover("fighter_identity", f"{fid}.{ident}", f"{fid.title()} {ident.replace('_', ' ')}", "docs/08_CONTENT_AND_PRESENTATION.md;game/Main.Menus.cs",
            f"fighters/{fid}/identity.png", "Preserve original silhouette and expression across crops; mirror palette must be legible in grayscale",
            "Crop/repaint to exact viewport use; clear surrounding labels/background; verify face at target pixels; add mirrored crop if needed",
            "P1/P2;left/right facing;small/large crop")

item_by_move = {(i["eligible_fighters"][0], i["move_id"]): i for i in read("data/items.json")["items"]}
move_rows = []
move_json = []
fighter_sources = {}
for fid in ("rook", "vale"):
    source = read(f"data/fighters/{fid}.json")
    fighter_sources[fid] = {"physics": source["physics"], "pushbox": source["pushbox"], "hurtboxes": source["hurtboxes"], "target_combos": source["target_combos"], "super_arts": source["super_arts"]}
    for idx, move in enumerate(source["moves"]):
        mid = move["id"]
        family = re.sub(r"_(l|m|h|ex)$", "", mid) if move.get("strength") else mid
        strength = move.get("strength") or (mid.rsplit("_", 1)[-1] if mid.rsplit("_", 1)[-1] in ("lp", "mp", "hp", "lk", "mk", "hk") else "unique")
        is_normal = idx < 24
        move_bindings = art_bindings(fid, "mapped_move_ids", mid)
        assert move_bindings, f"Missing move design reference: {fid}.{mid}"
        ref = ";".join(dict.fromkeys(b["board"] for b in move_bindings))
        item = item_by_move.get((fid, mid))
        s, a, r = move["startup"], move["active"], move["recovery"]
        phases = [dict(phase="startup", start=0, end=s, required_cels="anticipation/compression to pre-contact"),
            dict(phase="active", start=s, end=s+a, required_cels="contact/extreme per authored hit interval or projectile release"),
            dict(phase="recovery", start=s+a, end=s+a+r, required_cels="follow-through to recoverable stance")]
        if s == 0:
            phases[0]["required_cels"] = "none: zero startup; do not insert anticipation ticks"
        if r == 0:
            phases[2]["required_cels"] = "none: zero recovery; do not insert recovery ticks"
        reuse = "One canonical move, dedicated action silhouette; transitions may share compatible stance cels."
        if move.get("strength"):
            reuse = "Family silhouette reference shared by L/M/H/EX; each variant needs its own cel-to-tick timeline, active pose/extension and follow-through. L compact, M expanded, H committed; EX has distinct twin-notch accent and authored multihit poses. Do not copy one complete clip for all variants."
        if mid.startswith("throw_") or move.get("throw"):
            reuse += " Paired thrower/victim grabs, rotation and release must match canonical throw timeline; both fighter pairings and facings required."
        if move.get("projectile"):
            reuse += " Caster recovery and projectile lifetime are separate; release at exact spawn_tick; no repeated spawn on loop."
        if mid.startswith("super"):
            reuse += " Distinct named-super extreme; hold on simulation superfreeze; every authored hit group gets readable contact."
        if mid.startswith("shop"):
            reuse += " Lease pose must remain distinct from base command; gambits are committed feints without false invulnerability or hit effects."
        outstanding = "Draw startup/active/recovery and transitions; normalize pixel scale; transparent extraction; frame rectangles/pivots; timing manifest; mirror palette; in-game verification"
        move_rows.append(dict(fighter=fid, move_id=mid, name=move["name"], kind=move["kind"], command=move["command"],
            family=family, strength=strength, source_pose=move.get("pose", ""), availability=move["availability"],
            activation_credit_cost=move["credit_cost"], lease_item=item["id"] if item else "", lease_price=item["price"] if item else "",
            replaces=item.get("replaces") or "" if item else "", startup=s, active=a, recovery=r,
            startup_interval=f"[0,{s})", active_interval=f"[{s},{s+a})", recovery_interval=f"[{s+a},{s+a+r})",
            total_action_ticks=s+a+r, super_freeze=move.get("super_freeze", 0),
            hit_intervals=";".join(f"{h['id']}:[{h['start']},{h['end']})/group{h.get('hit_group',0)}/{h['level']}" for h in move["hitboxes"]),
            projectile_spawn_tick=move["projectile"]["spawn_tick"] if move.get("projectile") else "",
            reference_art=ref, reuse_policy=reuse, production_status="key_pose_reference_assigned_full_animation_pending", production_outstanding=outstanding))
        move_json.append(dict(fighter=fid, move_id=mid, source_file=f"data/fighters/{fid}.json", canonical_index=idx,
            reference_art=ref, reference_frames=move_bindings, reference_only=True, family=family, strength=strength, reuse_policy=reuse,
            phase_intervals=phases, canonical=move, lease=item,
            production=dict(status="not_runtime_ready", frame_rectangles=[], pivots=[], timeline=[], outstanding=outstanding)))
        cover("fighter_move", f"{fid}.{mid}", f"{fid.title()} / {move['name']}", f"data/fighters/{fid}.json#/moves/{idx}", ref,
            f"Original {family} key-pose direction; {reuse}", outstanding,
            f"{strength};startup[0,{s});active[{s},{s+a});recovery[{s+a},{s+a+r});both facings;P1/P2 palette")
        if move.get("projectile"):
            projectile = move["projectile"]
            cover("projectile", f"{fid}.{mid}", f"{fid.title()} / {move['name']} projectile", f"data/fighters/{fid}.json#/moves/{idx}/projectile", "effects/combat-atlas.png",
                ("Rook angular amber pulse core" if fid == "rook" else "Vale segmented teal crescent pulse") + "; ordinary/EX/super/return silhouettes distinct",
                "Create release/travel/contact/clash/dissipate cels and independent lifetime clock; extract projectile ID/move/age from read-only sim snapshot; honor canonical dimensions",
                f"spawn{projectile['spawn_tick']};{projectile['width']}x{projectile['height']}milli;vx{projectile['vx']};life{projectile['life_ticks']};hits{projectile['hits']};turn{projectile.get('turn_after_ticks', 'none')};left/right;P1/P2")

for item in read("data/items.json")["items"]:
    cover("lease_icon", item["id"], item["name"], "data/items.json", "ui/lease-icons.png",
        f"Original pictogram for {item['name']}; {item['slot']} slot frame; {item['price']} CR shown as text",
        "Extract exact icon; produce 16/24/32px legible versions; empty/selected/locked/unaffordable states; confirm canonical item-to-move mapping",
        f"{item['eligible_fighters'][0]};{item['move_id']};replaces{item.get('replaces') or 'none'};one round;zero activation cost")

assert len(move_rows) == 98
assert len({(m["fighter"], m["move_id"]) for m in move_rows}) == 98
assert all(sum(m["fighter"] == f for m in move_rows) == 49 for f in ("rook", "vale"))
write_csv("VISUAL_COVERAGE.csv", rows)
write_csv("MOVE_COVERAGE.csv", move_rows)
hash_inputs = ["data/fighters/rook.json", "data/fighters/vale.json", "data/items.json", "data/physics.json", "data/inputs.json", "data/resource_contract.json", "data/stages/foundry.json", "data/stages/grid.json"]
fingerprints = {rel: hashlib.sha256((PACK / rel).read_bytes()).hexdigest() for rel in hash_inputs}
write_json("move-coverage.json", dict(schema_version=1, purpose="Design reference and complete canonical move inventory; never runtime animation metadata", canonical_source_sha256=fingerprints,
    total_moves=98, fighter_count=2, moves_per_fighter=49, moves=move_json))

contract = dict(schema_version=1, status="proposed_art_production_contract_not_runtime_adapter", scope="traditional fighter only; additive art pack; prototype untouched",
    coordinates=dict(design_canvas=[640,360], current_viewport=[1280,720], integer_display_scale=2,
        design_floor_y=302, current_floor_y=604, world_y_positive="up", world_units_scale=1000,
        current_pixels_per_milliunit=1280/480000, design_pixels_per_milliunit=640/480000,
        world_to_design_x="320 + (worldX - cameraX) * 640 / 480000",
        world_to_design_y="302 - worldY * 640 / 480000", stage_world_width_milli=768000,
        visible_world_width_milli=480000, camera_x_range=[240000,528000], whole_stage_design_width=1024,
        origin="fighter floor contact; preserve signed local offsets", pixel_aspect="square",
        proposed_default_fighter_cell=[256,192], proposed_default_pivot=[128,172], proposed_wide_action_cell=[384,256],
        proposed_wide_pivot=[128,224], proposed_neutral_silhouette_height=dict(rook=124,vale=132),
        note="Target dimensions are production proposals. Inspect generated boards, manually normalize, test against canonical boxes; never auto-fit every pose to a cell."),
    rendering=dict(filter="nearest", mipmaps=False, integer_pixel_snap=True, alpha="straight RGBA, alpha0 outside silhouette; avoid matte halos",
        source_board_policy="Generated sheets are reference art; labels, board backgrounds, varying cell sizes and antialiasing need cleanup. No extraction rectangles are asserted here.",
        no_baked_ui_text="Raster text samples are typography direction. Render live values with verified font/glyph metrics.",
        palette_policy="Use fighter identity boards/manifests for exact P1/P2 maps. Preserve skin/hair identities; distinguish cloth value and corner shapes. Horizontal flip about pivot; no reversed embedded text.",
        collision_policy="Every visual box is cosmetic. Canonical hit/hurt/push boxes, root position and movement remain unchanged."),
    timing=dict(simulation_ticks_per_second=60, interval_semantics="half-open [start,end)",
        clip_source="MoveId+ActionFrame and canonical per-move timeline; not wall-clock animation speed",
        unique_cels="Multiple ticks may hold a cel. Do not add anticipation/recovery ticks to zero-duration phases.",
        active_groups="Match every canonical hitbox interval and hit_group; release projectiles exactly once at spawn_tick.",
        freeze="Hold pose and intended effects during actual hitstop/superfreeze. Reduced flashes alters visual treatment only.",
        cancel="Immediately select new action clip on cancel/interruption; never finish discarded root movement.",
        rollback="Deduplicate event effects through presentation event delivery. Reset timeline/effects on replay seek, rollback correction, round reset and simulation replacement.",
        ambient="Cosmetic ambient loops can use presentation time; must never drive simulation or collision."),
    current_render_states=["idle","walk","crouch","dash","dashback","jump","guard","crouchguard","parry","crouchparry","hit","knockdown","dizzy","win"],
    required_future_presentation_fields=[
        "Fighter signed velocity / movement direction and phase for walk, jump apex/fall, landing and turn",
        "Parry subtype High/Low/Air/RedHigh/RedLow (currently collapsed to parry/crouchparry)",
        "Throw actor/victim link and age / move for paired throw animation (currently not represented by FighterRenderState)",
        "Wakeup, soft/hard knockdown, contact height, dizzy entry/exit and KO terminal reason",
        "Taunt cue duration / state (currently represented internally with 24 zero-velocity dash ticks)",
        "Actual hitstop/superfreeze hold information for animation clock; presentation intensity remains adjustable",
        "Projectile stable ID, source fighter/move, age, and variant; current IsSuper groups EX and supers together",
        "Selected super and lease/item IDs for detailed icon binding; values remain read-only"
    ],
    universal_clips=universal_contract, fighter_geometry_reference=fighter_sources,
    move_inventory="move-coverage.json", coverage_inventory="VISUAL_COVERAGE.csv", canonical_source_sha256=fingerprints,
    runtime_clip_schema_example=dict(fighter="rook", move_id="s_lp", variant="p1", texture="future normalized atlas, not currently delivered",
        cell_rect=[0,0,256,192], pivot=[128,172], timeline=[dict(start_tick=0,end_tick=3,cel="anticipation"),dict(start_tick=3,end_tick=5,cel="contact"),dict(start_tick=5,end_tick=12,cel="recovery")],
        example_only=True),
    completion_gates=["All98 move IDs mapped to verified clips including strength variants and12 lease moves", "Universal states and paired throws cover both fighters and mirror match", "Transparent normalized exact pixel-size sprite and FX atlases exist with rectangle/pivot metadata", "Every screen/widget/state and12 lease icons has verified final-size art", "No source files or prototype assets replaced during design delivery", "A separately authorized integration validates render/physics alignment, timing, low effects, controller focus, replay/rollback and full-match readability"])
write_json("animation-contract.json", contract)
write_json("inventory-summary.json", dict(status="design_inventory_complete_animation_production_pending", total_visual_rows=len(rows),
    categories={k:sum(r["category"]==k for r in rows) for k in sorted({r["category"] for r in rows})},
    move_count=98, unique_moves_per_fighter={f:49 for f in ("rook","vale")}, leases=12,
    assigned_reference_paths=sorted({path for row in rows for path in row["reference_art"].split(";")}),
    note="Assigned reference is a design destination, not a claim that each required cel/icon was already drawn. Fighter manifests describe actual board cells; production_outstanding is authoritative."))
print(json.dumps({"visual_rows":len(rows), "moves":len(move_rows), "universal_clips":len(universal_contract), "output_directory":str(HERE)},indent=2))
