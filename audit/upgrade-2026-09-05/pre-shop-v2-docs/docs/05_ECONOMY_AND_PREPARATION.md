# Single-player wallet economy: meter is removed
**One player's credits are the entire spendable resource.** There is no cash→meter conversion, earned gauge, prepaid super ticket, EX ammunition counter, partner funding, character subsidy purse or separate combat-energy budget.

## Seed economy
Starting credits 600; cap 3600. Win earns 1200. Loss earns 900/1200/1500 according to the PRE-result recovery tier (0/1/2); loss then raises it one, win lowers it one, draw leaves it. Draw pays 900. All income is ordinary fungible credits. Excess above cap is discarded and visibly recorded. No interest, debt, insurance, loot drop, random shop, kill bounty, hit/whiff/block/parry/damage/time income, or persistent carry between matches.

The free kit is the entire basic fighter, including all movement, blocking, parries, throws, throw tech, all ordinary normals and special strengths. EX variants cost 300 per committed use. Selected supers cost 900/1200/1500 depending on art. Free super selection at match start does NOT grant free activation. Expensive arts are situational alternatives, not necessarily universally stronger.

## In-combat transaction
Resolve a motion/chord and check fighter state, selection, cancel window and all ordinary legal prerequisites. Then check `credits - cost >= reserve_floor`. If both action and debit are valid, enter startup and subtract cost atomically ONCE. The action's first startup tick is the spend event, even if it is interrupted before its first active frame.

Cost remains paid on whiff, block, parry, trade, opponent invulnerability, loss of projectile, or interruption. No refund for a poor fighting decision. Illegal commands, insufficient funds, or reserve rejection do not debit and do not substitute an unwanted regular special. Do not show superfreeze/EX flash before affordability is confirmed. Negative-edge releases never cause paid actions.

A canceled EX into a super pays both startups. A queued super that never becomes legal pays nothing. A duplicate event id with changed payload is an integrity error. A rollback restore is not a gameplay refund: restore both wallet and receipt history to the old snapshot, then recompute the corrected inputs. If the corrected action disappears, so does its spend; if it still occurs, charge once.

## Optional save protection
Players may set a minimum remaining wallet balance during preparation. Default is zero. It is a spending preference applied to the SAME balance, not a second purse or replenishing round meter. Lock it for the round; credits above it can be spent through any legal paid action. The UI shows total credits and the small floor marker, not two bars. It cannot exceed the post-lease balance. Do not let an online client edit it midround.

## Preparation shop
Optional leases occupy signature (900), technique (600), gambit (300). One per slot, two alternatives per character, max total 1800. Buying is never mandatory. These add a committed situational option, replace forward heavy, or equip a feint. They never improve global health, damage, invulnerability, speed, parry windows or throw tech. Equipped leases are reusable for their round; they have no stored-use resource and no activation credit cost. All expire after the round regardless of result. EX/supers remain paid every use and are not leased.

Preparation edits a desired plan against the immutable post-settlement opening balance. Show cost and resulting wallet/reserve projection, but do not debit for highlighting an item. Changing/unselecting before lock simply changes the plan; there is no network refund command or source-provenance laundering. Lock validates both complete plans and debits each once atomically. A duplicate exact commit returns the same receipt; conflicting revision or invalid player plan rejects the transaction. Locked plans cannot be refunded after reveal or on disconnect.

Online, both players commit before either hidden plan is revealed; display selected leases, balance, selected super and floor together only after both validate. On a shared-screen local setup, draft navigation may be visible to the opponent; do not claim private screens or secrecy that the hardware does not provide. Final locking remains simultaneous and irreversible; clearly label shared-screen versus private-network information conditions in balance logs. Wallet before preparation is public; motion inputs remain private during combat. No last-mover counterpurchase after revelation. Timer expiry uses each last valid draft, or an empty plan with zero reserve when none exists. Unaffordable stale plans are reset before lock with a clear notice, never negative balances.

## Concrete sequences
Opening 600 can fund two EX attempts, one technique lease, one 300-credit gambit lease plus one EX attempt, or be saved. You cannot buy two items for the same slot. A 600-credit lease leaves no money for EX on the first round. A zero-spend opening win produces 1800 next round. Spending all 600 then losing produces 900 at tier1. Saving all of that and losing again produces 2100 at tier2. That can fund one 1200-credit art, one 600-credit lease and one EX, but only if the 1200 art was selected for the match and the player chooses zero reserve.

At 1500 credits with a 900-credit floor: two EX moves leave 900; a third EX or 900-credit super is denied. Setting the floor to zero next preparation makes the saved credits usable. At 3600 and zero floor a player can attempt twelve EX moves: that concentration of paid power is a REAL design risk, not prevented by an unmentioned meter. Test it against free defense and tune prices/cap if necessary.

## Incentives
A free parry can defeat an expensive attack without taking away the defender's ability to fight. A confirm may justify spending while a speculative reversal risks both health and future purchasing power. Opening-round tempo, round score and final-round incentives matter. It is rational to empty the wallet when there is no future round; do not classify that as a bug. Intentionally throwing earlier rounds to farm comeback income must be tested, not assumed impossible.
