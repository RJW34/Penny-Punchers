# Presentation review

The Shop-Only v2 candidate `327be61cf6003b56cbc01e0f9abfd8c9a811fa13a80b33175829c93dfc5f6989` completed the exported Windows UI exercise with 299 assertions, 40 native screenshots, exit code 0 and no engine errors. Vincent is the orange motion fighter; Thomas is the teal charge fighter. The run used software controller events inside Godot, with actual competitive input sequences for the shop, reward and health-bar checks.

Both health fills and delayed-damage trails now follow the original angled frame apertures. Native captures show both fighters at full, 475/1000 and 90/1000 health, produced by ordinary attacks and reconstructed from replay. The cream borders remain visible and the two bars drain from opposite anchors.

The reviewed shop screens retain the selected product's description and tradeoff after refresh, and show readable prior-round facts. A real anti-air earns 75 pending credits; confirmed settlement funds a mixed rental/EX/super cart. The HUD shows the purchased super READY, then USED, and rejects a second command while retaining the same saved bank.

Marist Green and Marist Gates cover the play area while the camera contains both complete fighters at opposite corners and during simultaneous jumps. The untimed catalog, explicit practice kit, result portrait and fresh rematch shop were also visually reviewed at 1280×720.

The fresh renamed UI MP4 passed a complete decode over 36.1 seconds. Windows free-kit, two native network peers and Linux WSLg also completed their matches and decoded recordings. Representative frames were directly inspected for names, health containment, characters and HUD readability. Fixed-FPS movie duration does not measure real-time performance; the Linux 5fps recording is a platform smoke check.

Repository evidence is recorded in `reports/evidence/native-shop-v2-renamed-ui/`, `reports/evidence/shop-v2-completed-movies-visual-review.json` and `audit/upgrade-2026-09-05/SHOP_V2_FINAL_PRESENTATION_FINDINGS.json`, with exact source, binary and image hashes. See [verification status](VERIFICATION_STATUS.md) for the full acceptance boundary. The replacement combat showcase completed and decoded successfully. Its earlier names and the earlier 30-image UI review remain preserved under `reports/evidence/name-swap-audit/result.json`, which verifies unchanged mechanics and the exact label-only source changes. Interrupted attempts do not count as completed evidence.

Existing key-pose reuse, RGB chroma key, heuristic palette masks and panorama backgrounds remain disclosed art limitations. This review does not certify physical controllers, separate LAN computers, speakers, human feel/balance or readability at every display size. The software-test banner visible in evidence can cover part of the second fighter's heading; normal play does not show that banner.
