#!/usr/bin/env python3
"""
Fix collision: revert tag class back to 'bloc' and OBJE type back to 11 (Crate),
while keeping spawns in the scenery reflexive for network visibility.

The scenery reflexive ensures all players can see the objects (static placement).
The 'bloc' tag class ensures the engine applies crate collision behavior.
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
    min_raw_offset = 0x7FFFFFFF
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16) + 8
        raw_ofs = struct.unpack_from('<i', data, entry_pos)[0]
        if raw_ofs < min_raw_offset:
            min_raw_offset = raw_ofs
    secondary_magic = min_raw_offset - (index_offset + meta_start)

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

    # SCNR
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw_ofs = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_file_offset = scnr_raw_ofs - secondary_magic

    # Find crate_tech_semi by searching for the tag we modified
    # It's the tag that was previously changed to 'scen'
    bloc_class_bytes = b'\x63\x6F\x6C\x62'  # "bloc" reversed
    scen_class_bytes = b'\x6E\x65\x63\x73'  # "scen" reversed

    target_name = 'objects\\gear\\human\\industrial\\crate_tech_semi\\crate_tech_semi'
    target_idx = None
    target_file_ofs = None

    for i in range(meta_count):
        if tag_names.get(i, '') == target_name:
            target_idx = i
            entry_pos = tags_offset + (i * 16)
            raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
            target_file_ofs = raw_ofs - secondary_magic
            current_class = data[entry_pos:entry_pos+4][::-1].decode('ascii', errors='replace')
            current_obje = struct.unpack_from('<h', data, target_file_ofs)[0]
            print(f"Found crate_tech_semi at tag index {i}")
            print(f"  Current class: '{current_class}'")
            print(f"  Current OBJE type: {current_obje}")
            break

    if target_idx is None:
        print("ERROR: Could not find crate_tech_semi!")
        return

    # STEP 1: Change tag index class back to 'bloc'
    tag_entry_pos = tags_offset + (target_idx * 16)
    data[tag_entry_pos:tag_entry_pos+4] = bloc_class_bytes
    new_class = data[tag_entry_pos:tag_entry_pos+4][::-1].decode('ascii', errors='replace')
    print(f"\nSTEP 1: Tag index class -> '{new_class}'")

    # STEP 2: Change OBJE type back to 11 (Crate)
    struct.pack_into('<h', data, target_file_ofs, 11)
    print(f"STEP 2: OBJE type -> 11 (Crate)")

    # STEP 3: Update scenery palette entry 0 class to 'bloc'
    scen_pal_ref = scnr_file_offset + 88
    scen_pal_raw_ptr = struct.unpack_from('<i', data, scen_pal_ref + 4)[0]
    scen_pal_file_ofs = scen_pal_raw_ptr - secondary_magic
    pal0_pos = scen_pal_file_ofs + (0 * 40)

    old_pal_class = data[pal0_pos:pal0_pos+4][::-1].decode('ascii', errors='replace')
    data[pal0_pos:pal0_pos+4] = bloc_class_bytes
    new_pal_class = data[pal0_pos:pal0_pos+4][::-1].decode('ascii', errors='replace')
    print(f"STEP 3: Scenery palette[0] class: '{old_pal_class}' -> '{new_pal_class}'")

    # Verify scenery spawns are still intact
    scen_spawn_ref = scnr_file_offset + 80
    scen_count = struct.unpack_from('<i', data, scen_spawn_ref)[0]
    print(f"\nVerification: Scenery spawn count = {scen_count}")

    with open(map_path, 'wb') as f:
        f.write(data)

    print(f"\nDone! Tag is now 'bloc' (collision enabled) while spawns remain in scenery reflexive (always visible)")

if __name__ == '__main__':
    main()
