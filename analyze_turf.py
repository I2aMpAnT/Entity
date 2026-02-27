#!/usr/bin/env python3
"""Analyze turf.map to find existing crate/bloc tags and spawns."""
import struct
import sys

def main():
    with open('turf.map', 'rb') as f:
        data = bytearray(f.read())

    # Halo 2 Map Header
    sig = struct.unpack_from('<I', data, 0)[0]
    print(f"Signature: 0x{sig:08X} ('head' = 0x68656164)")

    file_size = struct.unpack_from('<i', data, 8)[0]
    print(f"File size: {file_size} (actual: {len(data)})")

    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]
    meta_size = struct.unpack_from('<i', data, 24)[0]
    print(f"Index offset: 0x{index_offset:X}")
    print(f"Meta start (relative): {meta_start}")
    print(f"Meta size: {meta_size}")

    # File names
    file_count = struct.unpack_from('<i', data, 704)[0]
    filenames_offset = struct.unpack_from('<i', data, 708)[0]
    filenames_size = struct.unpack_from('<i', data, 712)[0]
    fileindex_offset = struct.unpack_from('<i', data, 716)[0]
    print(f"File count: {file_count}")
    print(f"Filenames offset: 0x{filenames_offset:X}, size: {filenames_size}")
    print(f"File index offset: 0x{fileindex_offset:X}")

    # Index Header (at index_offset)
    constant = struct.unpack_from('<i', data, index_offset)[0]
    tag_type_count = struct.unpack_from('<i', data, index_offset + 4)[0]
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    scnr_id = struct.unpack_from('<i', data, index_offset + 12)[0]
    matg_id = struct.unpack_from('<i', data, index_offset + 16)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]

    primary_magic = constant - (index_offset + 32)
    tags_offset = raw_tags_offset - primary_magic  # file offset of tag index entries

    print(f"\nIndex Header:")
    print(f"  Constant: 0x{constant:08X}")
    print(f"  Tag type count: {tag_type_count}")
    print(f"  Meta/tag count: {meta_count}")
    print(f"  SCNR ID: 0x{scnr_id:08X}")
    print(f"  Primary magic: 0x{primary_magic:08X}")
    print(f"  Tags offset (file): 0x{tags_offset:X}")

    # Calculate secondary magic by finding lowest tag offset
    min_raw_offset = 0x7FFFFFFF
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16) + 8
        raw_ofs = struct.unpack_from('<i', data, entry_pos)[0]
        if raw_ofs < min_raw_offset:
            min_raw_offset = raw_ofs

    secondary_magic = min_raw_offset - (index_offset + meta_start)
    print(f"  Secondary magic: 0x{secondary_magic:08X}")

    # Read file names for tag lookup
    # File name index: int32 offsets into the filenames table
    tag_names = {}
    for i in range(min(file_count, meta_count)):
        idx_pos = fileindex_offset + (i * 4)
        name_ofs = struct.unpack_from('<i', data, idx_pos)[0]
        abs_ofs = filenames_offset + name_ofs
        end = data.find(b'\x00', abs_ofs, abs_ofs + 256)
        if end == -1:
            end = abs_ofs + 256
        tag_names[i] = data[abs_ofs:end].decode('ascii', errors='replace')

    # Find all tag index entries
    print(f"\n{'='*80}")
    print("BLOC (Crate) tags in map:")
    print(f"{'='*80}\n")

    bloc_tags = []
    scen_tags = []
    scnr_tag_idx = -1

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class_raw = data[entry_pos:entry_pos+4]
        # Tag class is stored reversed in Halo 2
        tag_class = tag_class_raw[::-1].decode('ascii', errors='replace')
        tag_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]
        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        tag_size = struct.unpack_from('<i', data, entry_pos + 12)[0]
        file_offset = raw_offset - secondary_magic

        name = tag_names.get(i, "(no name)")

        if tag_class == 'bloc':
            bloc_tags.append({
                'index': i, 'ident': tag_ident,
                'file_offset': file_offset, 'size': tag_size,
                'name': name
            })
            print(f"  [{i}] bloc  ident=0x{tag_ident:08X}  size={tag_size}  name={name}")

        if tag_class == 'scen':
            scen_tags.append({
                'index': i, 'ident': tag_ident,
                'file_offset': file_offset, 'size': tag_size,
                'name': name
            })

        if tag_class == 'scnr':
            scnr_tag_idx = i
            scnr_file_offset = file_offset
            scnr_size = tag_size

    print(f"\nTotal bloc tags: {len(bloc_tags)}")

    print(f"\n{'='*80}")
    print("SCEN (Scenery) tags in map:")
    print(f"{'='*80}\n")
    for t in scen_tags:
        print(f"  [{t['index']}] scen  ident=0x{t['ident']:08X}  name={t['name']}")
    print(f"\nTotal scen tags: {len(scen_tags)}")

    if scnr_tag_idx < 0:
        print("ERROR: Could not find SCNR tag!")
        return

    print(f"\nSCNR tag at index {scnr_tag_idx}, file offset 0x{scnr_file_offset:X}, size {scnr_size}")

    # Read crate palette reflexive at SCNR meta + 816
    pal_ref_pos = scnr_file_offset + 816
    pal_count = struct.unpack_from('<i', data, pal_ref_pos)[0]
    pal_raw_ptr = struct.unpack_from('<i', data, pal_ref_pos + 4)[0]
    pal_file_offset = pal_raw_ptr - secondary_magic if pal_count > 0 else 0

    print(f"\n{'='*80}")
    print("SCNR Crate Palette:")
    print(f"{'='*80}")
    print(f"  Count: {pal_count}, raw ptr: 0x{pal_raw_ptr:08X}, file offset: 0x{pal_file_offset:X}")

    for i in range(pal_count):
        entry_pos = pal_file_offset + (i * 40)
        p_class_raw = data[entry_pos:entry_pos+4]
        p_class = p_class_raw[::-1].decode('ascii', errors='replace')
        p_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]

        # Look up tag name by ident
        p_name = "(not found)"
        for j in range(meta_count):
            tj_pos = tags_offset + (j * 16) + 4
            tj_ident = struct.unpack_from('<i', data, tj_pos)[0]
            if tj_ident == p_ident:
                p_name = tag_names.get(j, "(no name)")
                break

        print(f"  Palette[{i}]: class='{p_class}', ident=0x{p_ident:08X}, name={p_name}")

    # Read crate spawns reflexive at SCNR meta + 808
    spawn_ref_pos = scnr_file_offset + 808
    spawn_count = struct.unpack_from('<i', data, spawn_ref_pos)[0]
    spawn_raw_ptr = struct.unpack_from('<i', data, spawn_ref_pos + 4)[0]
    spawn_file_offset = spawn_raw_ptr - secondary_magic if spawn_count > 0 else 0

    print(f"\n{'='*80}")
    print("SCNR Crate Spawns:")
    print(f"{'='*80}")
    print(f"  Count: {spawn_count}, raw ptr: 0x{spawn_raw_ptr:08X}, file offset: 0x{spawn_file_offset:X}")

    for i in range(spawn_count):
        entry_pos = spawn_file_offset + (i * 76)
        palette_idx = struct.unpack_from('<h', data, entry_pos)[0]
        name_idx = struct.unpack_from('<h', data, entry_pos + 2)[0]
        flags = struct.unpack_from('<i', data, entry_pos + 4)[0]
        x, y, z = struct.unpack_from('<fff', data, entry_pos + 8)
        yaw, pitch, roll = struct.unpack_from('<fff', data, entry_pos + 20)
        scale = struct.unpack_from('<f', data, entry_pos + 32)[0]
        unique_id = struct.unpack_from('<I', data, entry_pos + 40)[0]
        origin_bsp = struct.unpack_from('<h', data, entry_pos + 44)[0]
        spawn_type = data[entry_pos + 46]

        print(f"  Spawn[{i}]: pal={palette_idx}, pos=({x:.3f}, {y:.3f}, {z:.3f}), "
              f"rot=(yaw={yaw:.3f}, pitch={pitch:.3f}, roll={roll:.3f}), scale={scale:.3f}, "
              f"uid=0x{unique_id:08X}, bsp={origin_bsp}, type={spawn_type}")

    # Also look at scenery spawns for reference positions
    scen_ref_pos = scnr_file_offset + 80
    scen_spawn_count = struct.unpack_from('<i', data, scen_ref_pos)[0]
    scen_spawn_raw_ptr = struct.unpack_from('<i', data, scen_ref_pos + 4)[0]
    scen_spawn_file_offset = scen_spawn_raw_ptr - secondary_magic if scen_spawn_count > 0 else 0

    print(f"\n{'='*80}")
    print("SCNR Scenery Spawns (for reference):")
    print(f"{'='*80}")
    print(f"  Count: {scen_spawn_count}")

    # Scenery palette at offset 88
    scen_pal_ref = scnr_file_offset + 88
    scen_pal_count = struct.unpack_from('<i', data, scen_pal_ref)[0]
    scen_pal_raw = struct.unpack_from('<i', data, scen_pal_ref + 4)[0]
    scen_pal_file = scen_pal_raw - secondary_magic if scen_pal_count > 0 else 0

    print(f"  Scenery palette count: {scen_pal_count}")

    for i in range(min(scen_spawn_count, 20)):  # First 20 for reference
        entry_pos = scen_spawn_file_offset + (i * 92)
        palette_idx = struct.unpack_from('<h', data, entry_pos)[0]
        x, y, z = struct.unpack_from('<fff', data, entry_pos + 8)
        yaw, pitch, roll = struct.unpack_from('<fff', data, entry_pos + 20)
        print(f"  Scenery[{i}]: pal={palette_idx}, pos=({x:.3f}, {y:.3f}, {z:.3f}), "
              f"rot=({yaw:.3f}, {pitch:.3f}, {roll:.3f})")

    # Summary
    print(f"\n{'='*80}")
    print("SUMMARY")
    print(f"{'='*80}")
    print(f"  Bloc tags available: {len(bloc_tags)}")
    print(f"  Scen tags available: {len(scen_tags)}")
    print(f"  Crate palette entries: {pal_count}")
    print(f"  Crate spawns: {spawn_count}")
    print(f"  Scenery spawns: {scen_spawn_count}")
    print(f"  SCNR file offset: 0x{scnr_file_offset:X}")
    print(f"  SCNR size: {scnr_size}")
    print(f"  Secondary magic: 0x{secondary_magic:08X}")

if __name__ == '__main__':
    main()
