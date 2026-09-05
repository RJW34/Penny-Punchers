# Credit activation and rollback edge-case matrix
Each case needs a real-core test AND at least representative live input traces. The Python spend oracle does not implement state transitions.

| Case | Required behavior |
|---|---|
| Exactly 300, floor0, legal EX | Startup and single 300 debit; zero is valid |
| 299, legal EX chord | No paid startup/freeze/debit/fallback; input occurrence consumed |
| 1500, floor900, EX twice | Balances1200 then900; third rejected |
| Button release completes motion | Ordinary allowed negative-edge behavior only; no paid move |
| Matched paid input in hitstun | No premature spend; only permitted reversal/cancel buffer can later start |
| Buffered action expires before neutral | No spend; no delayed execution after expiry |
| Startup interrupted before active | Cost stays spent |
| Opponent parries all super hits | All damage prevented as appropriate; attacker retains full cost |
| Move whiffs, is blocked, trades or projectile cancels | No refund |
| EX legally canceled to super | Both paid startups charged |
| Leased forward-heavy replaces base | No fallback to unavailable base or duplicate action |
| Super not selected | Cannot execute or buy its activation; no substitute super |
| Rollback changes EX to no action | Restored balance/receipt list excludes the removed EX |
| Rollback repeats same EX | Same final balance and one canonical receipt |
| Late parry prevents predicted KO | No round income or score applied until confirmed corrected result |
| Duplicate preparation lock | Exact idempotent result; no second purchase debit |
| Invalid plan on one player | Neither player's plan commits |
| Refund/transfer/balance command from client | Protocol rejects unknown command; no public money mutation endpoint |
| Last round all-in | Legal/rational; do not invent debt or future repayment |
| Wallet-cap payout | Clip and record amount; never overflow or wrap |
| Cross-up while buffering paid motion | Facing-epoch policy applies; no old-direction unintended super |
| Same-tick paid actions on both sides | Both legal startup debits from precontact state, then fair simultaneous resolution |

Receipt id includes match session id, round id, simulation tick, seat and action ordinal. Keep it deterministic and rollback-restorable. Frame0 of a later round is not the same action as frame0 of an earlier one. Export confirmed spend logs for analysis, but do not use an irreversible external analytics queue as authoritative economic state.

Only confirmed preparation/result operations belong to the reliable coordinator. In-fight inputs are the sole causes of paid move debits. Clients never submit “I spent 300,” “I earned 1200,” or a trusted resulting wallet. The core derives those effects identically on both peers.
