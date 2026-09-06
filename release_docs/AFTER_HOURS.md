# After Hours graphics

Penny Punchers uses the supplied arcade-style art for Thomas and Vincent, their portraits, effects, menus and capability HUD. Legacy folder/asset IDs `rook` and `vale` remain internal identifiers. Visible title, fighter names, health, score, time, ownership and next-shop awards are live game data.

Foundry Ring and Calibration Grid are joined by **Marist Green — Golden Hour** and **Marist Gates — Blue Hour**. The original Marist panoramas are copied unchanged; the renderer preserves their aspect and independently covers the visible arena when the fighter camera zooms out. A deterministic presentation camera fits both complete fighter-cel envelopes at legal separation. Stage scenery never changes Core movement, collision or economy.

Current Core and Expanded libraries contain **100 and 115 runtime action nodes**, including branches. Explicit cel selection and simulation-tick holds preserve startup/contact/recovery timing with compatible existing key poses. This does not mean every action has a separate newly drawn animation, or that every simulation tick has a unique in-between cel. Feints share the intended first six anticipation ticks with their source attack; field markers and projectiles follow actual object state.

The working fighter sheets have a removable magenta matte. The runtime shader keys that RGB material and applies alternate palette masks; genuine RGBA cleanup has not passed technical validation. Palette and edge limitations remain documented. Projectiles are rendered independently of the caster, and confirmed contact metadata positions impact cues. Combat cues respect simulation freeze and pause; background ambience is cosmetic.

Source boards, prompts and provenance remain in `design/after-hours-32bit`. Runtime images and measured mappings are in `game/Assets/AfterHours`. `assets/MARIST_STAGES.json`, `assets/BUYABLE_PRESENTATION_BINDINGS.json`, `assets/UI_FONT_SOURCES.json` and `assets/PRESENTATION_UPGRADE.md` identify current source mappings and limitations. `reports/AFTER_HOURS_INTEGRATION.md` is the historical import report, not a claim that v2 gameplay assemblies are unchanged.

The current version also changes Core/App/data for the shop-only rules. Use the current candidate/replay identity and verification status; visual continuity does not imply compatibility with historical recordings.
