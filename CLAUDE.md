# Project Notes

## Critical Rules

### NEVER suggest crates (bloc) for multiplayer object placement
- Crates do NOT show off-host in multiplayer. Only the host can see them.
- Scenery (scen) is the ONLY viable approach for multiplayer object placement.
- Do not ever suggest converting scenery back to crates. Period.

### Always provide the raw download link after pushing
- After every push, provide the raw GitHub download link for the .map file.
- Format: `https://github.com/I2aMpAnT/Entity/raw/<branch-name>/turf.map`

### crate_tech_semi is naturally a scenery object
- Tag 1948 in turf.map is class `scen` with OBJE type 6 in the ORIGINAL unmodified map.
- It was never a crate/bloc. Do not treat it as a conversion.
- It was in the OG scenery palette (index 1) but never spawned in the stock map.

## Halo 2 Map Modding Knowledge

### Scenery collision and pathfinding policy
- Scenery extension data starts at OBJE tag offset +196 (after the 196-byte OBJE base).
- The first int16 at +196 is the **pathfinding policy**:
  - 1 = static: engine expects collision baked into BSP at compile time. Will NOT have collision for objects placed post-compilation.
  - 2 = dynamic: engine creates runtime collision body from the COLL tag. Works for post-compilation placements.
- The second int16 at +198 is flags/lightmapping (-1 / 0xFFFF for working objects).
- Working scenery objects (tent_medical_solidpanel_turf, triplicate dumpster) use policy=2 (0xffff0002 at +196).

### Tag data integrity
- Do not zero out or overwrite tag data without understanding what each field does.
- Always compare against the original map (OGturf.map) before modifying tag data.
- The scenery extension contains reflexive counts, pointers, and color values that must be preserved.

### Spawn placement fields
- +4 (flags): OG spawns have 0x00000000. Do not set flag 0x100 unless needed.
- +88 (4 bytes): OG spawns have pattern 0x002000xx (sequential index). Should not be zero.
