# Work-package execution order

Run every mandatory package. Local first-playable is an early milestone, not the stopping point. Device/human gates are distinct from software. Dependency readiness means required SOFTWARE interfaces and integration evidence are available; a genuine pending external device/human gate must not prevent downstream software implementation or its final handoff. Keep external requirements visibly pending, not passed.

| Package | Scope | Dependencies | Gates |
|---|---|---|---:|
| WP-000 | Workspace, toolchain and scope verification | — | 3 |
| WP-001 | Deterministic core and content contracts | WP-000 | 5 |
| WP-002 | Six-button execution and grounded movement | WP-001 | 7 |
| WP-003 | Ordinary attacks, defense and contact rules | WP-002 | 8 |
| WP-004 | Directional parry and combat-feel slice | WP-003 | 5 |
| WP-005 | One-wallet direct combat spending | WP-003 | 6 |
| WP-006 | Preparation and full economic match | WP-004, WP-005 | 6 |
| WP-007 | Both complete fighters and original presentation | WP-006 | 5 |
| WP-008 | Bots, training and deterministic replay | WP-007 | 6 |
| WP-009 | Private two-peer rollback | WP-006 | 7 |
| WP-010 | Complete UI, control routing and settings | WP-007, WP-008 | 5 |
| WP-011 | 1v1 economic and combat balance experiments | WP-008, WP-010 | 4 |
| WP-012 | Integration hardening and evidence automation | WP-009, WP-010, WP-011 | 5 |
| WP-013 | Native builds and real-device verification | WP-012 | 7 |
| WP-014 | Acceptance, handoff and actual player feel | WP-013 | 3 |
