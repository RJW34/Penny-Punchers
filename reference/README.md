# Reference model history

The Python files in this directory and `fixtures/economy_vectors.json` preserve the original direct-spend arithmetic model. Run those historical checks against the archived pre-v2 content, not the active shop-only game.

The current independent shop-only reference model and its 95 package tests are preserved in `audit/requests/penny-punchers-shop-only-economy-rework-v2/penny-punchers-shop-only-rework-v2`. The production C# arithmetic and mixed-cart contract harness uses the hand-specified `fixtures/shop_v2_economy_vectors.json`. It verifies payout and catalog arithmetic only; actual combat, rollback, native UI, and network evidence are separate suites.
