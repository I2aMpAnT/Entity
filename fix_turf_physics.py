#!/usr/bin/env python3
"""
Fix turf.map crate physics - make crate_tech_semi immovable.

The crates jiggle and tumble when players walk into them because
the PHMO (physics model) has a low mass. Entity treats objects as
immovable when mass >= 9,999,999.0.

Reference chain: BLOC tag -> HLMT (offset +56) -> PHMO (offset +36)
PHMO structure:
  - Variations reflexive at +56, entry size 144, mass at +60
  - Mass points reflexive at +96, entry size 144, mass at +24
"""
import struct
import shutil

IMMOVABLE_MASS = 9999999.0

def main():
    map_path = 'turf.map'
    backup_path = map_path + '.bak'

    with open(map_path, 'rb') as f:
        data = bytearray(f.read())

    print(f"Loaded {map_path}: {len(data)} bytes")

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

    # File names for lookups
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

    # Build tag lookup by ident
    def find_tag(ident):
        for i in range(meta_count):
            entry_pos = tags_offset + (i * 16)
            tag_ident = struct.unpack_from('<i', data, entry_pos + 4)[0]
            if tag_ident == ident:
                raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
                tag_class = data[entry_pos:entry_pos+4][::-1].decode('ascii', errors='replace')
                return {
                    'index': i,
                    'class': tag_class,
                    'file_offset': raw_ofs - secondary_magic,
                    'size': struct.unpack_from('<i', data, entry_pos + 12)[0],
                    'name': tag_names.get(i, '(unknown)')
                }
        return None

    # SCNR tag
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw_ofs = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_file_offset = scnr_raw_ofs - secondary_magic

    # Read crate palette to find crate_tech_semi (palette index 21)
    pal_ref_pos = scnr_file_offset + 816
    pal_count = struct.unpack_from('<i', data, pal_ref_pos)[0]
    pal_raw_ptr = struct.unpack_from('<i', data, pal_ref_pos + 4)[0]
    pal_file_offset = pal_raw_ptr - secondary_magic

    # Get palette entry 21
    pal21_pos = pal_file_offset + (21 * 40)
    bloc_ident = struct.unpack_from('<i', data, pal21_pos + 4)[0]

    bloc_tag = find_tag(bloc_ident)
    if not bloc_tag:
        print(f"ERROR: Could not find BLOC tag with ident 0x{bloc_ident:08X}")
        return

    print(f"\n1. BLOC tag: {bloc_tag['name']}")
    print(f"   Index: {bloc_tag['index']}, File offset: 0x{bloc_tag['file_offset']:X}")

    bto = bloc_tag['file_offset']

    # BLOC -> HLMT reference at offset +56
    # The OBJE tag dependency layout: the HLMT ident is at the object tag's offset
    # From the Entity code: Object Tag -> HLMT at offset +56
    hlmt_ident = struct.unpack_from('<i', data, bto + 56)[0]
    print(f"\n2. HLMT ident at BLOC+56: 0x{hlmt_ident:08X}")

    # But let me also dump the area around offset 48-64 to verify
    print(f"   BLOC bytes [48-72]:")
    for off in [48, 64]:
        chunk = data[bto+off:bto+off+16]
        hex_str = ' '.join(f'{b:02X}' for b in chunk)
        ascii_str = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f"     +{off:03d}: {hex_str}  {ascii_str}")

    hlmt_tag = find_tag(hlmt_ident)
    if not hlmt_tag:
        # Try scanning nearby offsets for the hlmt reference
        print(f"   Direct lookup failed. Scanning BLOC tag for hlmt references...")
        for scan_off in range(0, min(bloc_tag['size'], 200), 4):
            candidate_ident = struct.unpack_from('<i', data, bto + scan_off)[0]
            candidate_tag = find_tag(candidate_ident)
            if candidate_tag and candidate_tag['class'] == 'hlmt':
                print(f"   Found hlmt at BLOC+{scan_off}: {candidate_tag['name']}")
                hlmt_tag = candidate_tag
                break

    if not hlmt_tag:
        print("ERROR: Could not find HLMT tag!")
        # Try finding hlmt by name pattern
        print("\nSearching all hlmt tags:")
        for i in range(meta_count):
            entry_pos = tags_offset + (i * 16)
            tc = data[entry_pos:entry_pos+4][::-1].decode('ascii', errors='replace')
            if tc == 'hlmt' and 'crate_tech_semi' in tag_names.get(i, ''):
                raw_ofs = struct.unpack_from('<i', data, entry_pos + 8)[0]
                print(f"  [{i}] hlmt: {tag_names.get(i, '')}, ident=0x{struct.unpack_from('<i', data, entry_pos+4)[0]:08X}")
                hlmt_tag = {
                    'index': i,
                    'class': 'hlmt',
                    'file_offset': raw_ofs - secondary_magic,
                    'size': struct.unpack_from('<i', data, entry_pos + 12)[0],
                    'name': tag_names.get(i, '(unknown)')
                }
                break

    if not hlmt_tag:
        print("ERROR: Could not find HLMT tag by any method!")
        return

    print(f"\n3. HLMT tag: {hlmt_tag['name']}")
    print(f"   Index: {hlmt_tag['index']}, File offset: 0x{hlmt_tag['file_offset']:X}")

    hto = hlmt_tag['file_offset']

    # Dump HLMT header area
    print(f"   HLMT bytes [0-64]:")
    for off in range(0, 64, 16):
        chunk = data[hto+off:hto+off+16]
        hex_str = ' '.join(f'{b:02X}' for b in chunk)
        ascii_str = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f"     +{off:03d}: {hex_str}  {ascii_str}")

    # HLMT -> PHMO at offset +36
    phmo_ident = struct.unpack_from('<i', data, hto + 36)[0]
    print(f"\n4. PHMO ident at HLMT+36: 0x{phmo_ident:08X}")

    phmo_tag = find_tag(phmo_ident)
    if not phmo_tag:
        # Scan HLMT for phmo references
        print(f"   Direct lookup failed. Scanning HLMT for phmo references...")
        for scan_off in range(0, min(hlmt_tag['size'], 200), 4):
            candidate_ident = struct.unpack_from('<i', data, hto + scan_off)[0]
            candidate_tag = find_tag(candidate_ident)
            if candidate_tag and candidate_tag['class'] == 'phmo':
                print(f"   Found phmo at HLMT+{scan_off}: {candidate_tag['name']}")
                phmo_tag = candidate_tag
                break

    if not phmo_tag:
        print("ERROR: Could not find PHMO tag!")
        return

    print(f"\n5. PHMO tag: {phmo_tag['name']}")
    print(f"   Index: {phmo_tag['index']}, File offset: 0x{phmo_tag['file_offset']:X}, size: {phmo_tag['size']}")

    pto = phmo_tag['file_offset']

    # Dump PHMO header
    print(f"   PHMO bytes [0-128]:")
    for off in range(0, min(128, phmo_tag['size']), 16):
        chunk = data[pto+off:pto+off+16]
        hex_str = ' '.join(f'{b:02X}' for b in chunk)
        ascii_str = ''.join(chr(b) if 32 <= b < 127 else '.' for b in chunk)
        print(f"     +{off:03d}: {hex_str}  {ascii_str}")

    # PHMO Variations reflexive at +56
    var_count = struct.unpack_from('<i', data, pto + 56)[0]
    var_raw_ptr = struct.unpack_from('<i', data, pto + 60)[0]
    print(f"\n6. PHMO Variations: count={var_count}")

    if var_count > 0 and var_count < 100:
        var_file_offset = var_raw_ptr - secondary_magic
        print(f"   File offset: 0x{var_file_offset:X}")

        for v in range(var_count):
            vp = var_file_offset + (v * 144)
            mass = struct.unpack_from('<f', data, vp + 60)[0]
            print(f"   Variation[{v}]: mass={mass:.2f} (at file offset 0x{vp+60:X})")
            # Dump first 80 bytes of variation
            print(f"   Variation[{v}] bytes [0-80]:")
            for off in range(0, 80, 16):
                chunk = data[vp+off:vp+off+16]
                hex_str = ' '.join(f'{b:02X}' for b in chunk)
                print(f"     +{off:03d}: {hex_str}")

    # PHMO Mass Points reflexive at +96
    mp_count = struct.unpack_from('<i', data, pto + 96)[0]
    mp_raw_ptr = struct.unpack_from('<i', data, pto + 100)[0]
    print(f"\n7. PHMO Mass Points: count={mp_count}")

    if mp_count > 0 and mp_count < 100:
        mp_file_offset = mp_raw_ptr - secondary_magic
        print(f"   File offset: 0x{mp_file_offset:X}")

        for m in range(mp_count):
            mp = mp_file_offset + (m * 144)
            mass = struct.unpack_from('<f', data, mp + 24)[0]
            print(f"   MassPoint[{m}]: mass={mass:.2f} (at file offset 0x{mp+24:X})")

    # =========================================================================
    # APPLY FIX: Set mass to immovable value
    # =========================================================================
    print(f"\n{'='*80}")
    print(f"APPLYING FIX: Setting mass to {IMMOVABLE_MASS:.0f} (immovable threshold)")
    print(f"{'='*80}")

    # Backup
    shutil.copy2(map_path, backup_path)
    print(f"Backed up to {backup_path}")

    changes = 0

    # Fix variation masses
    if var_count > 0 and var_count < 100:
        var_file_offset = var_raw_ptr - secondary_magic
        for v in range(var_count):
            vp = var_file_offset + (v * 144)
            old_mass = struct.unpack_from('<f', data, vp + 60)[0]
            struct.pack_into('<f', data, vp + 60, IMMOVABLE_MASS)
            print(f"  Variation[{v}]: mass {old_mass:.2f} -> {IMMOVABLE_MASS:.0f}")
            changes += 1

    # Fix mass point masses
    if mp_count > 0 and mp_count < 100:
        mp_file_offset = mp_raw_ptr - secondary_magic
        for m in range(mp_count):
            mp = mp_file_offset + (m * 144)
            old_mass = struct.unpack_from('<f', data, mp + 24)[0]
            struct.pack_into('<f', data, mp + 24, IMMOVABLE_MASS)
            print(f"  MassPoint[{m}]: mass {old_mass:.2f} -> {IMMOVABLE_MASS:.0f}")
            changes += 1

    if changes == 0:
        print("WARNING: No mass values were modified!")
        return

    # Write
    with open(map_path, 'wb') as f:
        f.write(data)

    print(f"\nDone! Modified {changes} mass values in {map_path}")
    print(f"Crate_tech_semi should now be immovable (mass >= {IMMOVABLE_MASS:.0f})")

if __name__ == '__main__':
    main()
