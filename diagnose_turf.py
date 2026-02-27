#!/usr/bin/env python3
"""Detailed diagnosis of turf.map crate issues."""
import struct

def main():
    with open('turf.map', 'rb') as f:
        data = bytearray(f.read())

    # Parse header
    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]

    # Index header
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

    # SCNR tag
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw_ofs = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_file_offset = scnr_raw_ofs - secondary_magic

    # =========================================================================
    # 1) Crate spawns - FULL detail
    # =========================================================================
    spawn_ref_pos = scnr_file_offset + 808
    spawn_count = struct.unpack_from('<i', data, spawn_ref_pos)[0]
    spawn_raw_ptr = struct.unpack_from('<i', data, spawn_ref_pos + 4)[0]
    spawn_file_offset = spawn_raw_ptr - secondary_magic

    print("=" * 80)
    print("CRATE SPAWNS - FULL DETAIL")
    print("=" * 80)

    for i in range(spawn_count):
        ep = spawn_file_offset + (i * 76)
        palette_idx = struct.unpack_from('<h', data, ep)[0]
        name_idx = struct.unpack_from('<h', data, ep + 2)[0]
        flags = struct.unpack_from('<I', data, ep + 4)[0]
        x, y, z = struct.unpack_from('<fff', data, ep + 8)
        yaw, pitch, roll = struct.unpack_from('<fff', data, ep + 20)
        scale = struct.unpack_from('<f', data, ep + 32)[0]
        transform_flags = struct.unpack_from('<H', data, ep + 36)[0]
        manual_bsp_flags = struct.unpack_from('<H', data, ep + 38)[0]
        unique_id = struct.unpack_from('<I', data, ep + 40)[0]
        origin_bsp = struct.unpack_from('<h', data, ep + 44)[0]
        spawn_type = data[ep + 46]
        source = data[ep + 47]
        bsp_policy = data[ep + 48]
        editor_folder = struct.unpack_from('<h', data, ep + 50)[0]

        # Flag breakdown
        flag_names = []
        if flags & 0x01: flag_names.append("NotAutomatically")
        if flags & 0x02: flag_names.append("NotOnEasy")
        if flags & 0x04: flag_names.append("NotOnNormal")
        if flags & 0x08: flag_names.append("NotOnHard")
        if flags & 0x10: flag_names.append("LockTypeToEnvObject")
        if flags & 0x20: flag_names.append("LockTransformToEnvObject")
        if flags & 0x40: flag_names.append("NeverPlaced")
        if flags & 0x80: flag_names.append("LockNameToEnvObject")
        if flags & 0x100: flag_names.append("CreateAtRest")
        if not flag_names: flag_names.append("(none)")

        print(f"\n  Spawn[{i}]:")
        print(f"    Palette Index:     {palette_idx}")
        print(f"    Name Index:        {name_idx}")
        print(f"    Placement Flags:   0x{flags:08X} = {', '.join(flag_names)}")
        print(f"    Position:          ({x:.4f}, {y:.4f}, {z:.4f})")
        print(f"    Rotation:          yaw={yaw:.4f}, pitch={pitch:.4f}, roll={roll:.4f}")
        print(f"    Scale:             {scale:.6f} (raw bytes: {data[ep+32:ep+36].hex()})")
        print(f"    Transform Flags:   0x{transform_flags:04X}")
        print(f"    Manual BSP Flags:  0x{manual_bsp_flags:04X}")
        print(f"    Unique ID:         0x{unique_id:08X} (salt=0x{unique_id>>16:04X}, idx=0x{unique_id&0xFFFF:04X})")
        print(f"    Origin BSP:        {origin_bsp}")
        print(f"    Spawn Type:        {spawn_type} (11=Crate)")
        print(f"    Source:            {source} (0=Struct,1=Editor,2=Dynamic,3=Legacy)")
        print(f"    BSP Policy:        {bsp_policy} (0=Default,1=AlwaysPlaces,2=Manual)")
        print(f"    Editor Folder:     {editor_folder}")

        # Dump remaining bytes 52-75
        extra = data[ep+52:ep+76]
        print(f"    Extra bytes [52-75]: {extra.hex()}")

    # =========================================================================
    # 2) BLOC tag meta - check the crate_tech_semi tag (palette index 21)
    # =========================================================================
    print("\n" + "=" * 80)
    print("BLOC TAG ANALYSIS - crate_tech_semi (palette entry 21)")
    print("=" * 80)

    # Read palette to get tag ident
    pal_ref_pos = scnr_file_offset + 816
    pal_count = struct.unpack_from('<i', data, pal_ref_pos)[0]
    pal_raw_ptr = struct.unpack_from('<i', data, pal_ref_pos + 4)[0]
    pal_file_offset = pal_raw_ptr - secondary_magic

    # Palette entry 21
    pal21_pos = pal_file_offset + (21 * 40)
    pal21_ident = struct.unpack_from('<i', data, pal21_pos + 4)[0]
    print(f"  Palette[21] ident: 0x{pal21_ident:08X}")

    # Find tag index entry matching this ident
    bloc_tag_file_offset = None
    bloc_tag_size = None
    bloc_tag_idx = None
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]
        if tag_ident == pal21_ident:
            raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
            bloc_tag_file_offset = raw_ofs - secondary_magic
            bloc_tag_size = struct.unpack_from('<i', data, entry_pos + 12)[0]
            bloc_tag_idx = i
            print(f"  Found at tag index {i}, file offset 0x{bloc_tag_file_offset:X}, size {bloc_tag_size}")
            break

    if bloc_tag_file_offset is not None:
        bto = bloc_tag_file_offset

        # OBJE (Object) base fields - BLOC inherits from OBJE
        # Offset 0: Object type (short) - should be 11 for crate
        obj_type = struct.unpack_from('<h', data, bto)[0]
        print(f"\n  OBJE Object Type:    {obj_type} (11=Crate, 6=Scenery)")

        # Offset 2: flags (short)
        obj_flags = struct.unpack_from('<H', data, bto + 2)[0]
        print(f"  OBJE Flags:          0x{obj_flags:04X}")

        # Offset 4: bounding radius (float)
        bounding_radius = struct.unpack_from('<f', data, bto + 4)[0]
        print(f"  Bounding Radius:     {bounding_radius:.4f}")

        # Offset 8-19: bounding offset (3 floats)
        bx, by, bz = struct.unpack_from('<fff', data, bto + 8)
        print(f"  Bounding Offset:     ({bx:.4f}, {by:.4f}, {bz:.4f})")

        # Offset 20: acceleration scale (float) - important for physics!
        accel_scale = struct.unpack_from('<f', data, bto + 20)[0]
        print(f"  Acceleration Scale:  {accel_scale:.4f} (0=locked, >0=can be pushed)")

        # Offset 24: lightmap shadow mode (short)
        shadow_mode = struct.unpack_from('<h', data, bto + 24)[0]
        print(f"  Shadow Mode:         {shadow_mode}")

        # Offset 28-35: sweetener sizes
        # Model ref at offset 40
        # Offset 40-47: model reflexive/reference
        model_ref_class = data[bto+40:bto+44][::-1].decode('ascii', errors='replace')
        model_ref_ident = struct.unpack_from('<i', data, bto + 44)[0]
        print(f"\n  Model Ref:           class='{model_ref_class}', ident=0x{model_ref_ident:08X}")

        # Look up model name
        for j in range(meta_count):
            tj_ident = struct.unpack_from('<i', data, tags_offset + (j * 16) + 4)[0]
            if tj_ident == model_ref_ident:
                print(f"  Model Name:          {tag_names.get(j, '(unknown)')}")
                break

        # Physics reference - usually at offset 116 in OBJE
        # But offset can vary; let's look at a few key offsets
        # Collision model ref at offset 48
        coll_ref_class = data[bto+48:bto+52][::-1].decode('ascii', errors='replace')
        coll_ref_ident = struct.unpack_from('<i', data, bto + 52)[0]
        print(f"  Collision Model Ref: class='{coll_ref_class}', ident=0x{coll_ref_ident:08X}")
        if coll_ref_ident != -1 and coll_ref_ident != 0:
            for j in range(meta_count):
                tj_ident = struct.unpack_from('<i', data, tags_offset + (j * 16) + 4)[0]
                if tj_ident == coll_ref_ident:
                    print(f"  Collision Model:     {tag_names.get(j, '(unknown)')}")
                    break
        else:
            print(f"  Collision Model:     NONE (ident={coll_ref_ident})")

        # Physics model ref at offset 56
        phys_ref_class = data[bto+56:bto+60][::-1].decode('ascii', errors='replace')
        phys_ref_ident = struct.unpack_from('<i', data, bto + 60)[0]
        print(f"  Physics Model Ref:   class='{phys_ref_class}', ident=0x{phys_ref_ident:08X}")
        if phys_ref_ident != -1 and phys_ref_ident != 0:
            for j in range(meta_count):
                tj_ident = struct.unpack_from('<i', data, tags_offset + (j * 16) + 4)[0]
                if tj_ident == phys_ref_ident:
                    print(f"  Physics Model:       {tag_names.get(j, '(unknown)')}")
                    break
        else:
            print(f"  Physics Model:       NONE (ident={phys_ref_ident})")

        # Dump first 128 bytes of BLOC tag for reference
        print(f"\n  BLOC tag raw hex (first 128 bytes):")
        for off in range(0, min(128, bloc_tag_size), 16):
            chunk = data[bto+off:bto+off+16]
            hex_str = ' '.join(f'{b:02X}' for b in chunk)
            ascii_str = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
            print(f"    +{off:03d}: {hex_str}  {ascii_str}")

    # =========================================================================
    # 3) Check tag index class for the BLOC tag
    # =========================================================================
    if bloc_tag_idx is not None:
        tag_entry_pos = tags_offset + (bloc_tag_idx * 16)
        tag_class_bytes = data[tag_entry_pos:tag_entry_pos+4]
        tag_class = tag_class_bytes[::-1].decode('ascii', errors='replace')
        print(f"\n  Tag Index Class:     '{tag_class}' (raw bytes: {tag_class_bytes.hex()})")
        if tag_class != 'bloc':
            print(f"  *** WARNING: Tag class is '{tag_class}', not 'bloc'! This affects collision!")

    # =========================================================================
    # 4) Compare with a known-working crate (e.g., crate_packing at palette 0)
    # =========================================================================
    print("\n" + "=" * 80)
    print("COMPARISON: crate_packing (palette entry 0) - known working crate")
    print("=" * 80)

    pal0_pos = pal_file_offset + (0 * 40)
    pal0_ident = struct.unpack_from('<i', data, pal0_pos + 4)[0]
    print(f"  Palette[0] ident: 0x{pal0_ident:08X}")

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]
        if tag_ident == pal0_ident:
            raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
            pal0_file_ofs = raw_ofs - secondary_magic
            pal0_size = struct.unpack_from('<i', data, entry_pos + 12)[0]
            print(f"  Found at tag index {i}, file offset 0x{pal0_file_ofs:X}, size {pal0_size}")

            obj_type = struct.unpack_from('<h', data, pal0_file_ofs)[0]
            accel_scale = struct.unpack_from('<f', data, pal0_file_ofs + 20)[0]
            print(f"  OBJE Object Type:    {obj_type}")
            print(f"  Acceleration Scale:  {accel_scale:.4f}")

            coll_ident = struct.unpack_from('<i', data, pal0_file_ofs + 52)[0]
            phys_ident = struct.unpack_from('<i', data, pal0_file_ofs + 60)[0]
            print(f"  Collision ident:     0x{coll_ident:08X}")
            print(f"  Physics ident:       0x{phys_ident:08X}")

            print(f"\n  Tag raw hex (first 128 bytes):")
            for off in range(0, min(128, pal0_size), 16):
                chunk = data[pal0_file_ofs+off:pal0_file_ofs+off+16]
                hex_str = ' '.join(f'{b:02X}' for b in chunk)
                print(f"    +{off:03d}: {hex_str}")
            break

if __name__ == '__main__':
    main()
