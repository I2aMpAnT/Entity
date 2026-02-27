#!/usr/bin/env python3
"""
Convert crate_tech_semi spawns to scenery reflexive while preserving collision.

Key insight: Keep OBJE type as 11 (Crate) so the engine creates a Havok physics
body for collision. Change only the tag index class to 'scen' so the scenery
palette can reference it. Place spawns in the scenery reflexive for static
placement (visible to all, no network budget).

The engine should:
1. Load scenery spawns from SCNR+80
2. Look up scenery palette -> find tag ident
3. Load tag data -> see OBJE type 11 -> create Havok physics body (collision!)
4. Place as static object (no physics simulation, no network replication needed)
"""
import struct

def main():
    map_path = 'turf.map'

    with open(map_path, 'rb') as f:
        data = bytearray(f.read())

    # Parse header
    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]
    constant = struct.unpack_from('<i', data, index_offset)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    primary_magic = constant - (index_offset + 32)
    tags_offset = raw_tags_offset - primary_magic

    # Secondary magic
    min_raw = 0x7FFFFFFF
    for i in range(meta_count):
        r = struct.unpack_from('<i', data, tags_offset + i * 16 + 8)[0]
        if r < min_raw:
            min_raw = r
    secondary_magic = min_raw - (index_offset + meta_start)

    # File names
    file_count = struct.unpack_from('<i', data, 704)[0]
    filenames_offset = struct.unpack_from('<i', data, 708)[0]
    fileindex_offset = struct.unpack_from('<i', data, 716)[0]
    tag_names = {}
    for i in range(min(file_count, meta_count)):
        idx_pos = fileindex_offset + (i * 4)
        name_ofs = struct.unpack_from('<i', data, idx_pos)[0]
        abs_ofs = filenames_offset + name_ofs
        end = data.find(b'\x00', abs_ofs, abs_ofs + 256)
        if end == -1:
            end = abs_ofs + 256
        tag_names[i] = data[abs_ofs:end].decode('ascii', errors='replace')

    # SCNR tag
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_fo = scnr_raw - secondary_magic

    # =====================================================================
    # Read crate spawns (SCNR+808, 76 bytes each)
    # =====================================================================
    crate_count = struct.unpack_from('<i', data, scnr_fo + 808)[0]
    crate_ptr = struct.unpack_from('<i', data, scnr_fo + 812)[0]
    crate_fo = crate_ptr - secondary_magic

    print(f"Crate spawns: {crate_count} at file offset 0x{crate_fo:X}")

    # Read crate palette entry 21 (our crate_tech_semi)
    crate_pal_count = struct.unpack_from('<i', data, scnr_fo + 816)[0]
    crate_pal_ptr = struct.unpack_from('<i', data, scnr_fo + 820)[0]
    crate_pal_fo = crate_pal_ptr - secondary_magic
    pal21_fo = crate_pal_fo + 21 * 40
    bloc_tag_ident = struct.unpack_from('<i', data, pal21_fo + 4)[0]

    # Find the tag
    bloc_tag_idx = None
    bloc_tag_fo = None
    for i in range(meta_count):
        ep = tags_offset + i * 16
        if struct.unpack_from('<i', data, ep + 4)[0] == bloc_tag_ident:
            bloc_tag_idx = i
            bloc_tag_fo = struct.unpack_from('<i', data, ep + 8)[0] - secondary_magic
            break

    print(f"Tag {bloc_tag_idx}: {tag_names.get(bloc_tag_idx, '?')}")
    print(f"  Current class: {data[tags_offset + bloc_tag_idx*16:tags_offset + bloc_tag_idx*16 + 4][::-1].decode('ascii')}")
    print(f"  Current OBJE type: {struct.unpack_from('<h', data, bloc_tag_fo)[0]}")

    # Read crate spawn data
    spawns = []
    for i in range(crate_count):
        base = crate_fo + i * 76
        spawns.append(bytes(data[base:base + 76]))
        x = struct.unpack_from('<f', data, base + 8)[0]
        y = struct.unpack_from('<f', data, base + 12)[0]
        z = struct.unpack_from('<f', data, base + 16)[0]
        uid = struct.unpack_from('<I', data, base + 40)[0]
        print(f"  Crate[{i}]: pos=({x:.2f}, {y:.2f}, {z:.2f}) uid={uid:#010x}")

    # =====================================================================
    # Read scenery reflexives
    # =====================================================================
    scen_spawn_count = struct.unpack_from('<i', data, scnr_fo + 80)[0]
    scen_spawn_ptr = struct.unpack_from('<i', data, scnr_fo + 84)[0]
    scen_spawn_fo = scen_spawn_ptr - secondary_magic

    scen_pal_count = struct.unpack_from('<i', data, scnr_fo + 88)[0]
    scen_pal_ptr = struct.unpack_from('<i', data, scnr_fo + 92)[0]
    scen_pal_fo = scen_pal_ptr - secondary_magic

    print(f"\nScenery spawns: {scen_spawn_count} at 0x{scen_spawn_fo:X}")
    print(f"Scenery palette: {scen_pal_count} at 0x{scen_pal_fo:X}")

    # =====================================================================
    # STEP 1: Change tag index class to 'scen'
    # DO NOT change OBJE type - keep as 11 for Havok collision
    # =====================================================================
    tag_entry = tags_offset + bloc_tag_idx * 16
    scen_class = b'\x6E\x65\x63\x73'  # 'scen' reversed
    data[tag_entry:tag_entry + 4] = scen_class
    print(f"\nSTEP 1: Tag class -> 'scen' (OBJE type stays 11)")

    # =====================================================================
    # STEP 2: Write scenery palette entry at palette[0]
    # Use class 'scen' in the palette to match tag index class
    # =====================================================================
    # Copy the 40-byte palette entry from crate palette[21]
    new_pal = bytearray(data[pal21_fo:pal21_fo + 40])
    # Change class from 'bloc' to 'scen'
    new_pal[0:4] = scen_class
    data[scen_pal_fo:scen_pal_fo + 40] = new_pal
    print(f"STEP 2: Scenery palette[0] -> class='scen', ident={bloc_tag_ident:#010x}")

    # Update scenery palette count to 1
    struct.pack_into('<i', data, scnr_fo + 88, 1)

    # =====================================================================
    # STEP 3: Build scenery spawn entries (92 bytes each)
    # Write into the freed crate spawn area
    # =====================================================================
    scenery_data = bytearray()
    for i, crate_raw in enumerate(spawns):
        entry = bytearray(92)

        # Copy the 52-byte base from crate data
        entry[0:52] = crate_raw[0:52]

        # Override: palette index = 0 (our new scenery palette entry)
        struct.pack_into('<h', entry, 0, 0)

        # Override: name index = -1
        struct.pack_into('<h', entry, 2, -1)

        # Keep MetaSpawnType as 11 (Crate) at offset 46 - match the OBJE type
        # This tells the engine what kind of object to create
        entry[46] = 11  # Crate type - for collision!

        # Keep source as Editor (1)
        entry[47] = 1

        # Keep BSP policy as Default (0)
        entry[48] = 0

        # Bytes 52-91: zero (scenery-specific, unused)

        scenery_data.extend(entry)

    # Write to crate spawn area (it's being freed)
    write_fo = crate_fo
    data[write_fo:write_fo + len(scenery_data)] = scenery_data
    print(f"STEP 3: Wrote {crate_count} scenery entries ({len(scenery_data)} bytes) at 0x{write_fo:X}")

    # =====================================================================
    # STEP 4: Update scenery spawn reflexive pointer
    # =====================================================================
    new_scen_ptr = write_fo + secondary_magic
    struct.pack_into('<i', data, scnr_fo + 80, crate_count)  # count
    struct.pack_into('<i', data, scnr_fo + 84, new_scen_ptr)  # pointer
    print(f"STEP 4: Scenery spawn reflexive: count={crate_count}, ptr=0x{new_scen_ptr:08X}")

    # =====================================================================
    # STEP 5: Zero out crate spawn count (keep palette for other crates)
    # =====================================================================
    struct.pack_into('<i', data, scnr_fo + 808, 0)
    print(f"STEP 5: Crate spawn count -> 0")

    # Zero remaining old data
    remaining_start = write_fo + len(scenery_data)
    remaining_end = crate_fo + crate_count * 76
    if remaining_end > remaining_start:
        data[remaining_start:remaining_end] = b'\x00' * (remaining_end - remaining_start)

    # =====================================================================
    # Verify
    # =====================================================================
    print(f"\nVerification:")
    verify_count = struct.unpack_from('<i', data, scnr_fo + 80)[0]
    verify_ptr = struct.unpack_from('<i', data, scnr_fo + 84)[0]
    verify_fo = verify_ptr - secondary_magic
    print(f"  Scenery count: {verify_count}")
    for i in range(verify_count):
        base = verify_fo + i * 92
        pal = struct.unpack_from('<h', data, base)[0]
        x = struct.unpack_from('<f', data, base + 8)[0]
        y = struct.unpack_from('<f', data, base + 12)[0]
        z = struct.unpack_from('<f', data, base + 16)[0]
        uid = struct.unpack_from('<I', data, base + 40)[0]
        stype = data[base + 46]
        print(f"    [{i}] pal={pal} pos=({x:.2f},{y:.2f},{z:.2f}) uid={uid:#010x} spawnType={stype}")

    obje_type = struct.unpack_from('<h', data, bloc_tag_fo)[0]
    tag_class = data[tag_entry:tag_entry+4][::-1].decode('ascii')
    print(f"  Tag class: '{tag_class}', OBJE type: {obje_type}")

    with open(map_path, 'wb') as f:
        f.write(data)

    print(f"\nDone! Scenery placement with OBJE type 11 for Havok collision.")

if __name__ == '__main__':
    main()
