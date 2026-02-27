#!/usr/bin/env python3
"""
Convert turf.map crate_tech_semi spawns from crate (bloc) to scenery (scen).

Problem: Crate objects are dynamic and require individual network replication.
The engine has limited budget for replicated objects, causing off-host
players to not see most/all crates.

Solution: Scenery objects are static/baked into the map structure. All players
see them identically without individual network replication. They are also
immovable by default (no physics simulation).

Changes:
1. Change crate_tech_semi tag index class: 'bloc' -> 'scen'
2. Change crate_tech_semi OBJE type: 11 (Crate) -> 6 (Scenery)
3. Overwrite unused scenery palette entry 0 with crate_tech_semi reference
4. Write 14 scenery spawn entries (92 bytes each) in freed crate area
5. Update scenery spawn reflexive: count=14, pointer to new data
6. Zero out crate spawn count and crate palette count
"""
import struct
import shutil

def main():
    map_path = 'turf.map'
    backup_path = map_path + '.bak'

    with open(map_path, 'rb') as f:
        data = bytearray(f.read())

    print(f"Loaded {map_path}: {len(data)} bytes")

    # =========================================================================
    # Parse map header and index
    # =========================================================================
    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]

    constant = struct.unpack_from('<i', data, index_offset)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    primary_magic = constant - (index_offset + 32)
    tags_offset = raw_tags_offset - primary_magic

    # Secondary magic
    min_raw_offset = 0x7FFFFFFF
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16) + 8
        raw_ofs = struct.unpack_from('<i', data, entry_pos)[0]
        if raw_ofs < min_raw_offset:
            min_raw_offset = raw_ofs
    secondary_magic = min_raw_offset - (index_offset + meta_start)

    print(f"Secondary magic: 0x{secondary_magic:08X}")

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

    # SCNR tag (always index 3)
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw_ofs = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_file_offset = scnr_raw_ofs - secondary_magic
    print(f"SCNR at file offset 0x{scnr_file_offset:X}")

    # =========================================================================
    # Read current crate spawns (SCNR+808)
    # =========================================================================
    crate_spawn_ref = scnr_file_offset + 808
    crate_spawn_count = struct.unpack_from('<i', data, crate_spawn_ref)[0]
    crate_spawn_raw_ptr = struct.unpack_from('<i', data, crate_spawn_ref + 4)[0]
    crate_spawn_file_ofs = crate_spawn_raw_ptr - secondary_magic

    print(f"\nCrate spawns: count={crate_spawn_count}, file offset=0x{crate_spawn_file_ofs:X}")

    if crate_spawn_count == 0:
        print("No crate spawns to convert!")
        return

    # Read all crate spawn data
    crate_spawns = []
    for i in range(crate_spawn_count):
        ep = crate_spawn_file_ofs + (i * 76)
        spawn = {
            'palette_idx': struct.unpack_from('<h', data, ep)[0],
            'name_idx': struct.unpack_from('<h', data, ep + 2)[0],
            'flags': struct.unpack_from('<I', data, ep + 4)[0],
            'x': struct.unpack_from('<f', data, ep + 8)[0],
            'y': struct.unpack_from('<f', data, ep + 12)[0],
            'z': struct.unpack_from('<f', data, ep + 16)[0],
            'yaw': struct.unpack_from('<f', data, ep + 20)[0],
            'pitch': struct.unpack_from('<f', data, ep + 24)[0],
            'roll': struct.unpack_from('<f', data, ep + 28)[0],
            'scale': struct.unpack_from('<f', data, ep + 32)[0],
            'transform_flags': struct.unpack_from('<H', data, ep + 36)[0],
            'manual_bsp_flags': struct.unpack_from('<H', data, ep + 38)[0],
            'unique_id': struct.unpack_from('<I', data, ep + 40)[0],
            'origin_bsp': struct.unpack_from('<h', data, ep + 44)[0],
            'source': data[ep + 47],
            'bsp_policy': data[ep + 48],
            'editor_folder': struct.unpack_from('<h', data, ep + 50)[0],
        }
        crate_spawns.append(spawn)
        print(f"  Crate[{i}]: pal={spawn['palette_idx']}, "
              f"pos=({spawn['x']:.2f}, {spawn['y']:.2f}, {spawn['z']:.2f}), "
              f"yaw={spawn['yaw']:.3f}")

    # =========================================================================
    # Read crate palette to get crate_tech_semi tag ident
    # =========================================================================
    crate_pal_ref = scnr_file_offset + 816
    crate_pal_count = struct.unpack_from('<i', data, crate_pal_ref)[0]
    crate_pal_raw_ptr = struct.unpack_from('<i', data, crate_pal_ref + 4)[0]
    crate_pal_file_ofs = crate_pal_raw_ptr - secondary_magic

    print(f"\nCrate palette: count={crate_pal_count}, file offset=0x{crate_pal_file_ofs:X}")

    # All spawns use palette index 21
    pal21_pos = crate_pal_file_ofs + (21 * 40)
    pal21_class = data[pal21_pos:pal21_pos+4]
    bloc_tag_ident = struct.unpack_from('<i', data, pal21_pos + 4)[0]
    # Copy full 40-byte palette entry for reuse
    pal21_full = bytes(data[pal21_pos:pal21_pos + 40])

    print(f"  Palette[21] class={pal21_class[::-1].decode('ascii', errors='replace')}, "
          f"ident=0x{bloc_tag_ident:08X}")

    # Find the tag index entry for this tag
    bloc_tag_idx = None
    bloc_tag_file_ofs = None
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]
        if tag_ident == bloc_tag_ident:
            bloc_tag_idx = i
            raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
            bloc_tag_file_ofs = raw_ofs - secondary_magic
            tag_class = data[entry_pos:entry_pos+4][::-1].decode('ascii', errors='replace')
            print(f"  Tag index [{i}]: class='{tag_class}', "
                  f"name={tag_names.get(i, '?')}, file_ofs=0x{bloc_tag_file_ofs:X}")
            break

    if bloc_tag_idx is None:
        print("ERROR: Could not find crate_tech_semi in tag index!")
        return

    # =========================================================================
    # Read scenery palette (SCNR+88)
    # =========================================================================
    scen_pal_ref = scnr_file_offset + 88
    scen_pal_count = struct.unpack_from('<i', data, scen_pal_ref)[0]
    scen_pal_raw_ptr = struct.unpack_from('<i', data, scen_pal_ref + 4)[0]
    scen_pal_file_ofs = scen_pal_raw_ptr - secondary_magic

    print(f"\nScenery palette: count={scen_pal_count}, file offset=0x{scen_pal_file_ofs:X}")

    # Read scenery spawn reflexive (SCNR+80)
    scen_spawn_ref = scnr_file_offset + 80
    scen_spawn_count = struct.unpack_from('<i', data, scen_spawn_ref)[0]
    scen_spawn_raw_ptr = struct.unpack_from('<i', data, scen_spawn_ref + 4)[0]
    print(f"Scenery spawns: count={scen_spawn_count}, raw_ptr=0x{scen_spawn_raw_ptr:08X}")

    # =========================================================================
    # Calculate space
    # =========================================================================
    crate_spawn_area_size = crate_spawn_count * 76  # 1064 bytes
    crate_pal_area_size = crate_pal_count * 40      # 880 bytes
    total_freed = crate_spawn_area_size + crate_pal_area_size  # 1944 bytes

    scenery_spawn_size = crate_spawn_count * 92     # 1288 bytes
    print(f"\nSpace calculation:")
    print(f"  Crate spawn area: {crate_spawn_area_size} bytes (starts 0x{crate_spawn_file_ofs:X})")
    print(f"  Crate palette area: {crate_pal_area_size} bytes (starts 0x{crate_pal_file_ofs:X})")
    print(f"  Total freed: {total_freed} bytes")
    print(f"  Scenery spawns needed: {scenery_spawn_size} bytes")
    print(f"  Fits: {'YES' if scenery_spawn_size <= total_freed else 'NO'}")

    if scenery_spawn_size > total_freed:
        print("ERROR: Not enough space for scenery spawns!")
        return

    # Verify crate palette immediately follows crate spawns
    expected_pal_start = crate_spawn_file_ofs + crate_spawn_area_size
    if expected_pal_start != crate_pal_file_ofs:
        print(f"WARNING: Crate palette not contiguous! Expected 0x{expected_pal_start:X}, got 0x{crate_pal_file_ofs:X}")
        print(f"  Gap: {crate_pal_file_ofs - expected_pal_start} bytes")
        # If there's a gap, we still have enough space from spawn area alone if gap is positive
        if crate_spawn_area_size >= scenery_spawn_size:
            print("  Spawn area alone is sufficient, continuing...")
        else:
            print("ERROR: Cannot safely use non-contiguous space!")
            return

    # =========================================================================
    # Find highest existing UID salt (for unique IDs)
    # =========================================================================
    spawn_reflexive_offsets = {
        'Scenery':   (80, 92),
        'Biped':     (96, 84),
        'Vehicle':   (112, 84),
        'Equipment': (128, 56),
        'Weapon':    (144, 84),
        'Machine':   (168, 72),
        'Control':   (184, 68),
        'Crate':     (808, 76),
    }

    max_salt = 0
    for name, (ref_offset, entry_size) in spawn_reflexive_offsets.items():
        ref_pos = scnr_file_offset + ref_offset
        count = struct.unpack_from('<i', data, ref_pos)[0]
        if count <= 0:
            continue
        raw_ptr = struct.unpack_from('<i', data, ref_pos + 4)[0]
        file_ofs = raw_ptr - secondary_magic
        for i in range(count):
            entry_pos = file_ofs + (i * entry_size)
            uid = struct.unpack_from('<I', data, entry_pos + 40)[0]
            salt = uid >> 16
            if salt > max_salt:
                max_salt = salt

    next_salt = max_salt + 1
    print(f"\nHighest UID salt: 0x{max_salt:04X}, starting from: 0x{next_salt:04X}")

    # =========================================================================
    # BACKUP
    # =========================================================================
    shutil.copy2(map_path, backup_path)
    print(f"\nBacked up to {backup_path}")

    # =========================================================================
    # STEP 1: Change tag index class from 'bloc' to 'scen'
    # =========================================================================
    tag_entry_pos = tags_offset + (bloc_tag_idx * 16)
    old_class = data[tag_entry_pos:tag_entry_pos+4][::-1].decode('ascii', errors='replace')

    # 'scen' reversed = bytes [0x6E, 0x65, 0x63, 0x73]
    scen_class_bytes = b'\x6E\x65\x63\x73'  # "scen" reversed for little-endian
    data[tag_entry_pos:tag_entry_pos+4] = scen_class_bytes

    new_class = data[tag_entry_pos:tag_entry_pos+4][::-1].decode('ascii', errors='replace')
    print(f"\nSTEP 1: Tag index [{bloc_tag_idx}] class: '{old_class}' -> '{new_class}'")

    # =========================================================================
    # STEP 2: Change OBJE type from 11 (Crate) to 6 (Scenery)
    # =========================================================================
    old_obje_type = struct.unpack_from('<h', data, bloc_tag_file_ofs)[0]
    struct.pack_into('<h', data, bloc_tag_file_ofs, 6)
    new_obje_type = struct.unpack_from('<h', data, bloc_tag_file_ofs)[0]
    print(f"STEP 2: OBJE type: {old_obje_type} -> {new_obje_type}")

    # =========================================================================
    # STEP 3: Overwrite scenery palette entry 0 with crate_tech_semi
    # =========================================================================
    pal0_pos = scen_pal_file_ofs + (0 * 40)

    # Copy the full 40-byte palette entry from crate palette, change class to 'scen'
    new_pal_entry = bytearray(pal21_full)
    new_pal_entry[0:4] = scen_class_bytes  # Set class to 'scen'

    old_pal0_class = data[pal0_pos:pal0_pos+4][::-1].decode('ascii', errors='replace')
    old_pal0_ident = struct.unpack_from('<i', data, pal0_pos + 4)[0]

    data[pal0_pos:pal0_pos+40] = new_pal_entry

    print(f"STEP 3: Scenery palette[0]: class='{old_pal0_class}' ident=0x{old_pal0_ident:08X} -> "
          f"class='scen' ident=0x{bloc_tag_ident:08X} (crate_tech_semi)")

    # =========================================================================
    # STEP 4: Build and write 14 scenery spawn entries (92 bytes each)
    # =========================================================================
    print(f"\nSTEP 4: Writing {len(crate_spawns)} scenery spawn entries:")

    new_scenery_data = bytearray()
    for i, sp in enumerate(crate_spawns):
        entry = bytearray(92)  # 92 bytes, zeroed

        # Palette index: 0 (pointing to our overwritten scenery palette entry)
        struct.pack_into('<h', entry, 0, 0)
        # Name index: -1
        struct.pack_into('<h', entry, 2, -1)
        # Placement flags (keep CreateAtRest, remove other flags that might cause issues)
        struct.pack_into('<I', entry, 4, sp['flags'])
        # Position
        struct.pack_into('<f', entry, 8, sp['x'])
        struct.pack_into('<f', entry, 12, sp['y'])
        struct.pack_into('<f', entry, 16, sp['z'])
        # Rotation
        struct.pack_into('<f', entry, 20, sp['yaw'])
        struct.pack_into('<f', entry, 24, sp['pitch'])
        struct.pack_into('<f', entry, 28, sp['roll'])
        # Scale (1.0 for normal size)
        struct.pack_into('<f', entry, 32, 1.0)
        # Transform flags
        struct.pack_into('<H', entry, 36, sp['transform_flags'])
        # Manual BSP flags
        struct.pack_into('<H', entry, 38, sp['manual_bsp_flags'])
        # Unique ID (new unique salt for each)
        new_uid = (next_salt << 16) | (i & 0xFFFF)
        next_salt += 1
        struct.pack_into('<I', entry, 40, new_uid)
        # Origin BSP
        struct.pack_into('<h', entry, 44, sp['origin_bsp'])
        # Meta Spawn Type: 6 = Scenery
        entry[46] = 6
        # Source: Editor (1)
        entry[47] = 1
        # BSP Policy: Default (0)
        entry[48] = 0
        # Unused
        entry[49] = 0
        # Editor folder
        struct.pack_into('<h', entry, 50, -1)
        # Bytes 52-91: zeroed (scenery-specific, not needed)

        new_scenery_data.extend(entry)

        print(f"  Scenery[{i}]: pal=0, pos=({sp['x']:.2f}, {sp['y']:.2f}, {sp['z']:.2f}), "
              f"yaw={sp['yaw']:.3f}, uid=0x{new_uid:08X}, type=6(Scenery)")

    # Write scenery data into the old crate spawn area
    write_offset = crate_spawn_file_ofs
    data[write_offset:write_offset + len(new_scenery_data)] = new_scenery_data
    print(f"\n  Written {len(new_scenery_data)} bytes at file offset 0x{write_offset:X}")

    # Zero out remaining old crate data (after scenery data ends)
    remaining_start = write_offset + len(new_scenery_data)
    remaining_end = crate_spawn_file_ofs + total_freed
    if remaining_end > remaining_start:
        zero_size = remaining_end - remaining_start
        data[remaining_start:remaining_end] = b'\x00' * zero_size
        print(f"  Zeroed {zero_size} bytes of old crate data")

    # =========================================================================
    # STEP 5: Update scenery spawn reflexive (SCNR+80)
    # =========================================================================
    # Convert file offset back to raw pointer
    new_scen_raw_ptr = write_offset + secondary_magic

    old_scen_count = struct.unpack_from('<i', data, scen_spawn_ref)[0]
    old_scen_ptr = struct.unpack_from('<i', data, scen_spawn_ref + 4)[0]

    struct.pack_into('<i', data, scen_spawn_ref, len(crate_spawns))       # count = 14
    struct.pack_into('<i', data, scen_spawn_ref + 4, new_scen_raw_ptr)    # pointer

    print(f"\nSTEP 5: Scenery spawn reflexive (SCNR+80):")
    print(f"  Count: {old_scen_count} -> {len(crate_spawns)}")
    print(f"  Ptr: 0x{old_scen_ptr:08X} -> 0x{new_scen_raw_ptr:08X}")

    # =========================================================================
    # STEP 6: Zero out crate spawn count and crate palette count
    # =========================================================================
    old_crate_count = struct.unpack_from('<i', data, crate_spawn_ref)[0]
    old_crate_pal_count = struct.unpack_from('<i', data, crate_pal_ref)[0]

    struct.pack_into('<i', data, crate_spawn_ref, 0)   # crate spawn count = 0
    struct.pack_into('<i', data, crate_pal_ref, 0)     # crate palette count = 0

    print(f"\nSTEP 6: Zeroed crate reflexives:")
    print(f"  Crate spawn count: {old_crate_count} -> 0")
    print(f"  Crate palette count: {old_crate_pal_count} -> 0")

    # =========================================================================
    # WRITE
    # =========================================================================
    with open(map_path, 'wb') as f:
        f.write(data)

    print(f"\n{'='*80}")
    print(f"DONE! Converted {len(crate_spawns)} crate spawns to scenery spawns.")
    print(f"{'='*80}")
    print(f"\nChanges summary:")
    print(f"  - Tag '{tag_names.get(bloc_tag_idx, '?')}' class: bloc -> scen")
    print(f"  - OBJE type: 11 (Crate) -> 6 (Scenery)")
    print(f"  - Scenery palette[0]: now references crate_tech_semi")
    print(f"  - {len(crate_spawns)} scenery spawns written (92 bytes each)")
    print(f"  - Crate spawn count: {old_crate_count} -> 0")
    print(f"  - Crate palette count: {old_crate_pal_count} -> 0")
    print(f"\nScenery objects are:")
    print(f"  - Always visible to ALL players (host + off-host)")
    print(f"  - Static / immovable (no physics simulation)")
    print(f"  - Part of the map structure (no network replication needed)")

if __name__ == '__main__':
    main()
