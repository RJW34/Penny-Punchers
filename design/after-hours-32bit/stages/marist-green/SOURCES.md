# Sources and generation record

The three stage PNGs were created with built-in **image_gen** for this project. Exact generation and edit prompts, including reference roles, are preserved in [prompts.json](prompts.json). No existing game artwork was replaced.

## Architectural references

- [Murray Student Center and Rotunda — Robert A. M. Stern Architects](https://www.ramsa.com/projects/project/murray-student-center-and-rotunda): architectural context and the relationship to the campus green.
- [Official Marist article featuring the Rotunda](https://www.marist.edu/w/marist-news-marist-to-take-lead-on-iconic-mindset-list-in-2019), with [official Rotunda photograph](https://www.marist.edu/documents/86200/131519/RotundaHEADER.jpg/4ec0870d-cd8b-1dec-a671-9af1f85da92d?t=1673463233283). Local reference: `references/rotunda-official.jpg`. Used to guide the cylindrical stone façade, tall glazing and low, stepped green-glass dome.
- [Marist homepage](https://www.marist.edu/) and its [campus landscape photograph](https://d3cdqbpg48x0ib.cloudfront.net/images/2025_01_Anthem_1920x930_THUMB.jpg). Local reference: `references/campus-green-official.jpg`. Used to understand the broad lawn, paths and globe lamps around the Rotunda.

Reference photographs were inspected and used as architectural guidance. They are reference-only materials, retain their original rights, and are excluded from the generated-asset manifest. Do not ship the reference photographs with the game.

The stage is an original illustrated interpretation, not an architectural survey. The real landscape has been simplified and flattened into an uninterrupted grass fighting plane; stairs, slopes and paths remain behind that plane. Golden-hour lighting and pixel treatment are original art direction. No institutional endorsement is claimed.

## Project style references

- [Marist Gates background](../marist-gates/background.png): the existing AFTER HOURS palette, pixel clusters and environmental rendering.
- [Marist Gates selection card](../marist-gates/select-card.png): the shared stage-card frame and typography treatment.
- The new Green background was used to keep the panorama and selection-card imagery consistent.

These are original project illustrations. The late-1990s arcade treatment is a broad aesthetic homage; no franchise backgrounds, characters, sprites or ROM assets were used.

## Asset records

[ASSET_MANIFEST.json](ASSET_MANIFEST.json), [ASSET_REGISTER.csv](ASSET_REGISTER.csv) and [VERIFICATION.json](VERIFICATION.json) record inspected native dimensions, formats, hashes and verification results. All three PNGs are source art for future integration; production normalization and runtime wiring remain pending.
