# After Hours graphics

This version uses the completed 32-bit arcade style artwork for Rook, Vale, the foundry and calibration room, combat effects, portraits, menus, lease icons and the HUD. Health, timer, points and wallet values are still live game data.

The production pack contains 19 bitmap files and 159 measured source cells. Every one of the 98 moves has an explicit startup, active and recovery binding. Related strengths share compatible key poses; the source boards do not contain independently drawn in-between frames for every tick. The alternate costumes remain distinct in mirror matches.

The eight original fighter boards were prepared through the built-in image generation tool with a removable magenta background. A ninth working sheet supplies Vale's cleaned super release pose. The runtime shader removes the matte, preserves skin and clothing detail, and applies alternate costume colors. Flying projectiles render separately from the caster.

The original boards are preserved in the source package under `design/after-hours-32bit`. Production images, atlas rectangles, root pivots and generation prompts are in `game/Assets/AfterHours`. `reports/AFTER_HOURS_INTEGRATION.md` records the verification scope.

The gameplay libraries and all canonical numeric data are unchanged. Disabling the automatically added Git version suffix preserves the prior gameplay assembly bytes and competitive replay compatibility across this graphics-only commit.
