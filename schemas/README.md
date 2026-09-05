# JSON contracts
Strict scalar-wallet, input, preparation, spend-receipt and replay headers exclude inherited team fields and earned meters. Content schemas validate authored structure; semantic validator checks costs, action ranges, references, scope and dependency graph. The core must enforce full runtime legality too.

Combat receipts are derived from simulated actions, never messages accepted from a client. Preparation messages express a desired plan, not a balance or direct-money command. Unknown network keys/types/commands reject; do not silently deserialize new authority fields.
