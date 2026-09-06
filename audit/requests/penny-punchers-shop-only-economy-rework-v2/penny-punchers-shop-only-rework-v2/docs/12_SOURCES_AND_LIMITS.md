# Sources and limits
Repository compared: RJW34/Penny-Punchers at `1623ee62c4f4b6d5574bbb9909eb6fe4e32f5441`. Head was rechecked through the connected GitHub tool for this task. AGENTS and Simulation.Input/Combat paths were reread; broader migration seams derive from the pinned source audit. The current live local checkout and concurrent edits are not visible here. Do not claim otherwise.

Prior attached expanded audit and Buyables Design v1 were read. Their user-independent restrictions on combat income, EX unlocking and finite purchased supers are explicitly superseded. Their mechanical design and counterplay requirements remain useful where compatible. The baseline numeric files in provenance are historical comparison inputs, not new runtime data.

Primary technical references checked 2026-09-05:
- https://www.ggpo.net/ — save/restore and resimulation requirement for rollback. Existing custom rollback remains; no automatic library adoption.
- https://docs.godotengine.org/en/stable/tutorials/scripting/idle_and_physics_processing.html — fixed physics tick versus render processing. Timing-classification uses deterministic Core ticks, not real-time UI callbacks.

All proposed reward definitions, prices, ownership limits and finite-use rules are original design decisions. The precision-parry bonus is not a factual claim about Street Fighter III or VI's exact perfect-parry behavior. No franchise asset or source dependency. No new installed toolchain version is claimed; receiving agent inspects current repo pin and available SDK. Packaging host lacks dotnet/Godot, so native compilation/launch are not verified here.
