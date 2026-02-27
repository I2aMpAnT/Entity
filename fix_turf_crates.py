#!/usr/bin/env python3
"""
Fix turf.map crate spawns for off-host visibility, collision, and physics.

Issues found:
1. All 14 crate spawns had identical UniqueID (0xF6910038) [fixed in v1]
   - The engine needs unique IDs to replicate each object separately
2. Placement flags = 0 (CreateAtRest not set) [fixed in v1]
   - Crates should have CreateAtRest (bit 8 = 0x100) to prevent physics jitter
3. Scale = 0.0 on all crate spawns [NEW - v2]
   - Scale 0 means the object has zero size: invisible, no collision hull
   - Host may partially render some objects but off-host sees nothing
   - Must be set to 1.0 for proper rendering, collision, and replication

This script:
- Backs up turf.map -> turf.map.bak
- Patches each crate spawn with a unique ID
- Sets CreateAtRest flag on all crate spawns
- Sets Scale to 1.0 on all crate spawns
"""
import struct
import shutil
import sys

def main():
    map_path = 'turf.map'
    backup_path = map_path + '.bak'

    # Load map
    with open(map_path, 'rb') as f:
        data = bytearray(f.read())

    print(f"Loaded {map_path}: {len(data)} bytes")

    # Parse header
    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]

    # Index header
    constant = struct.unpack_from('<i', data, index_offset)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]
    primary_magic = constant - (index_offset + 32)
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    tags_offset = raw_tags_offset - primary_magic

    # Secondary magic
    min_raw_offset = 0x7FFFFFFF
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16) + 8
        raw_ofs = struct.unpack_from('<i', data, entry_pos)[0]
        if raw_ofs < min_raw_offset:
            min_raw_offset = raw_ofs
    secondary_magic = min_raw_offset - (index_offset + meta_start)

    # Find SCNR tag (index 3)
    scnr_entry = tags_offset + (3 * 16)
    scnr_raw_ofs = struct.unpack_from('<i', data, scnr_entry + 8)[0]
    scnr_file_offset = scnr_raw_ofs - secondary_magic
    print(f"SCNR at file offset 0x{scnr_file_offset:X}")

    # Read crate spawns reflexive at SCNR + 808
    spawn_ref_pos = scnr_file_offset + 808
    spawn_count = struct.unpack_from('<i', data, spawn_ref_pos)[0]
    spawn_raw_ptr = struct.unpack_from('<i', data, spawn_ref_pos + 4)[0]
    spawn_file_offset = spawn_raw_ptr - secondary_magic

    print(f"Crate spawns: count={spawn_count}, file offset=0x{spawn_file_offset:X}")

    if spawn_count == 0:
        print("No crate spawns to fix!")
        return

    # Dump raw bytes of first spawn for analysis
    print(f"\nFirst spawn raw hex (76 bytes):")
    first_raw = data[spawn_file_offset:spawn_file_offset + 76]
    for off in range(0, 76, 16):
        chunk = first_raw[off:off+16]
        hex_str = ' '.join(f'{b:02X}' for b in chunk)
        print(f"  +{off:02d}: {hex_str}")

    # Find highest existing unique ID salt across ALL spawn types to avoid collisions
    # Check multiple spawn reflexives
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

    print(f"\nHighest existing UID salt across all spawns: 0x{max_salt:04X} ({max_salt})")

    next_salt = max_salt + 1
    print(f"Starting new salt from: 0x{next_salt:04X}")

    # Backup
    shutil.copy2(map_path, backup_path)
    print(f"\nBacked up to {backup_path}")

    # Fix each crate spawn
    CREATE_AT_REST = 0x100  # bit 8
    TARGET_SCALE = 1.0      # proper object scale
    print(f"\nFixing {spawn_count} crate spawns:")

    for i in range(spawn_count):
        entry_pos = spawn_file_offset + (i * 76)

        # Read current data
        old_uid = struct.unpack_from('<I', data, entry_pos + 40)[0]
        old_flags = struct.unpack_from('<i', data, entry_pos + 4)[0]
        old_scale = struct.unpack_from('<f', data, entry_pos + 32)[0]
        palette_idx = struct.unpack_from('<h', data, entry_pos)[0]
        x, y, z = struct.unpack_from('<fff', data, entry_pos + 8)

        # Generate new unique ID
        new_uid = (next_salt << 16) | (i & 0xFFFF)
        next_salt += 1

        # Set CreateAtRest flag
        new_flags = old_flags | CREATE_AT_REST

        # Write new unique ID
        struct.pack_into('<I', data, entry_pos + 40, new_uid)

        # Write new flags
        struct.pack_into('<i', data, entry_pos + 4, new_flags)

        # Write scale = 1.0 (was 0.0 which makes object invisible with no collision)
        struct.pack_into('<f', data, entry_pos + 32, TARGET_SCALE)

        print(f"  Spawn[{i}]: pal={palette_idx}, pos=({x:.2f},{y:.2f},{z:.2f}), "
              f"uid 0x{old_uid:08X} -> 0x{new_uid:08X}, "
              f"flags 0x{old_flags:08X} -> 0x{new_flags:08X}, "
              f"scale {old_scale:.1f} -> {TARGET_SCALE:.1f}")

    # Write modified map
    with open(map_path, 'wb') as f:
        f.write(data)

    print(f"\nDone! Modified {spawn_count} crate spawns in {map_path}")
    print(f"Backup at {backup_path}")
    print(f"\nChanges made:")
    print(f"  - Each crate spawn now has a unique ID (salt 0x{max_salt+1:04X} through 0x{next_salt-1:04X})")
    print(f"  - CreateAtRest flag (0x{CREATE_AT_REST:X}) set on all spawns")
    print(f"  - Scale set to {TARGET_SCALE:.1f} on all spawns (was 0.0)")

if __name__ == '__main__':
    main()
