# Integration seeds, not a game patch
`CapabilityContracts.cs` demonstrates how a startup authorizer can be structurally unable to read/spend bank credits, and how a candidate reward classifier consumes precomputed facts. It does not implement Core's collision/input/serialization/ledger interfaces. Merge deliberately; do not copy over current code.

Optional seed compile: `dotnet build integration/ShopOnly.Seeds.csproj` from this pack. Packaging host has no dotnet, so this was NOT run. The seed is not considered game evidence even after compilation. The production input intent parser must recognize locked EX/super before the authorizer, and production snapshots must include the entire finite-use/reward/identity state.

Python `reference/model.py` and generated vectors are an independent proposed-rules oracle. Cross-check current C# implementations against vectors, then execute real contacts and full native matches. Direct calls with supplied `legal=true` are never evidence that input/collision legality works.
