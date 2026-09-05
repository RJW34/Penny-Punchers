# Production C# and executable verification

`StrikeLedger.Core` implements the deterministic fighting simulation, input recognition, collisions, reactions, match phases, credit spending and canonical serialization. `StrikeLedger.App` adds bots, training, competitive replays and private UDP rollback sessions. The Godot presentation and menus live in `../game`.

`StrikeLedger.CoreTests` exercises production combat. `StrikeLedger.NetworkLab --self-test` exercises application, training and network behavior; its other modes run actual peer processes. `StrikeLedger.BalanceLab` runs seeded bot experiments. `StrikeLedger.ContractTests` retains the original seed arithmetic fixtures and is deliberately narrower than gameplay acceptance.

Run the commands in [the audit guide](../AUDIT_GUIDE.md) from the repository root. Numeric content is in `../data`. Historical test reports describe their recorded source/build identities; new code changes require new runs.
