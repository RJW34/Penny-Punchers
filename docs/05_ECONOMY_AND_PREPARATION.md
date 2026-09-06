# Shop-only round economy

The full active semantics and provenance are in docs/21_SHOP_ONLY_V2.md. Prices and rules are loaded from current data, with data-driven catalog validation and a new incompatible replay/content identity.

Starting bank600; cap3600; shop cap2400. One optional signature, technique and gambit, up to two EX licenses and at most one super permit. Equipment expires for both players each round; unspent bank carries. EX costs600 or900 for the vertical reversal and repeats under ordinary legality. Super products cost900/1200/1500 and buy one legal startup. A new shop may select a different art. The ordinary complete kit remains free.

Validate both players' complete product-ID carts before mutation, including fighter eligibility, duplicate IDs, slot limits, replacement/conflict rules, budget, bank and implemented content identity. Same commit key/payload is idempotent; changed payload fails closed. Draft highlighting never spends. Each accepted edit updates the last-valid cart; timeout commits it. A saved plan is re-priced and validated against current content. Local drafts may be visible on the shared screen; private network plans use commit/reveal and must not permit a last-mover counterpurchase.

No combat bank writes, bank affordability reads, reserve floor, per-use EX price, emergency buying or cash refund. Recognize explicit locked EX/super syntax before ownership filters, preserving motion/charge/cancel/recovery rules and chord release suppression. Legal super startup consumes its use even on whiff, block, parry or interruption; illegal and buffered attempts do not. Rollback restores the original capability/use/reward state and re-simulates corrected inputs.

CH50, AA75 and precision parry100 are nonspendable receipts, capped at two paid awards per category and300 combined each round. Precise definitions, origin roots, simultaneous prestate, frozen-edge exclusions, deterministic order and no-farming rules are in the active v2 contract. Training is diagnostic only.

Confirmed outcome payout uses the old recovery tier: win1200, loss1200/1200/1500, draw900. Update tier after calculating the result. Grant result income first, then skill receipts within remaining wallet space, recording both clipping amounts. Next shop exposes opening bank, cart, frozen saved bank, outcome and skill grants, clipping and closing bank. A match's final income is unused postmatch balance, not comeback liquidity; rematch resets600 and aborts do not settle unresolved rounds.
