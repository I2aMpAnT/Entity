#!/usr/bin/env python3
"""Fix 360-degree auto-aim for ALL weapons in a Halo 2 map.

Previous scripts used 2*pi for the autoaim angle, but in Halo 2 the autoaim angle
is a HALF-ANGLE cone. pi radians (180 deg) = full 360-degree sphere of coverage.
Using 2*pi can overflow and cause sporadic/inconsistent lock-on behavior.

This script:
1. Applies to ALL weapon tags (not just sniper/BR)
2. Uses pi (not 2*pi) for correct full-sphere autoaim
3. Clears "Aim Assists Only When Zoomed" weapon flag (bit 5 at offset 300)
4. Clears barrel "Projectile Vector Cannot Be Adjusted" flag (bit 6)
5. Clears barrel "Use Error When Unzoomed" flag (bit 5)
6. Zeros all barrel error angles for perfect accuracy
"""
import struct
import sys
import math
import shutil

# Configurable - pass map name as argument or default to headlong
MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Weapon tag field offsets
AUTO_AIM_ANGLE_OFFSET = 520     # float, radians (HALF-ANGLE of cone)
AUTO_AIM_RANGE_OFFSET = 524     # float, world units
MAGNETISM_ANGLE_OFFSET = 528    # float, radians (HALF-ANGLE of cone)
MAGNETISM_RANGE_OFFSET = 532    # float, world units
DEVIATION_ANGLE_OFFSET = 536    # float, radians
WEAPON_FLAGS_OFFSET = 300       # bitmask32

# Barrel reflexive
BARRELS_REFLEXIVE_OFFSET = 720  # count(4) + pointer(4)
BARREL_ENTRY_SIZE = 236

# Correct values - pi = 180 deg half-angle = full 360-degree sphere
FULL_SPHERE_ANGLE = math.pi     # 3.14159 rad = 180 deg half-angle = 360 deg total
MAX_RANGE = 100000.0
ZERO_DEVIATION = 0.0


def parse_map(data):
    """Parse Halo 2 map header and tag index."""
    sig = struct.unpack_from('<I', data, 0)[0]
    assert sig == 0x68656164, f"Not a valid Halo 2 map (sig: 0x{sig:08X})"

    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]

    constant = struct.unpack_from('<i', data, index_offset)[0]
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]

    primary_magic = constant - (index_offset + 32)
    tags_offset = raw_tags_offset - primary_magic

    min_raw_offset = 0x7FFFFFFF
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16) + 8
        raw_ofs = struct.unpack_from('<i', data, entry_pos)[0]
        if raw_ofs < min_raw_offset:
            min_raw_offset = raw_ofs
    secondary_magic = min_raw_offset - (index_offset + meta_start)

    file_count = struct.unpack_from('<i', data, 704)[0]
    filenames_offset = struct.unpack_from('<i', data, 708)[0]
    fileindex_offset = struct.unpack_from('<i', data, 716)[0]

    tag_names = {}
    for i in range(min(file_count, meta_count)):
        idx_pos = fileindex_offset + (i * 4)
        name_ofs = struct.unpack_from('<i', data, idx_pos)[0]
        abs_ofs = filenames_offset + name_ofs
        end = data.find(b'\x00', abs_ofs, abs_ofs + 512)
        if end == -1:
            end = abs_ofs + 512
        tag_names[i] = data[abs_ofs:end].decode('ascii', errors='replace')

    return tags_offset, meta_count, secondary_magic, tag_names


def main():
    print(f"=== Fix ALL Weapon Auto-Aim: {MAP_FILE} ===")
    print(f"Using angle = pi ({math.degrees(FULL_SPHERE_ANGLE):.1f} deg half-angle = 360 deg sphere)")
    print()

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    # Backup
    backup = MAP_FILE.replace('.map', '_backup_aa.map')
    shutil.copy2(MAP_FILE, backup)
    print(f"Backup: {backup}")

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)
    print(f"Tags: {meta_count}, Secondary magic: 0x{secondary_magic:08X}\n")

    weapon_count = 0
    modified_count = 0

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')

        if tag_class != 'weap':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        weapon_count += 1

        print(f"{'='*70}")
        print(f"[{i}] {name}")
        print(f"    File offset: 0x{file_offset:X}")

        # --- Read current autoaim values ---
        aa_angle = struct.unpack_from('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET)[0]
        aa_range = struct.unpack_from('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET)[0]
        mag_angle = struct.unpack_from('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET)[0]
        mag_range = struct.unpack_from('<f', data, file_offset + MAGNETISM_RANGE_OFFSET)[0]
        dev_angle = struct.unpack_from('<f', data, file_offset + DEVIATION_ANGLE_OFFSET)[0]

        print(f"    BEFORE: AA={math.degrees(aa_angle):.1f}deg/{aa_range:.0f}wu  "
              f"Mag={math.degrees(mag_angle):.1f}deg/{mag_range:.0f}wu  "
              f"Dev={math.degrees(dev_angle):.2f}deg")

        # --- Write corrected autoaim values ---
        struct.pack_into('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET, FULL_SPHERE_ANGLE)
        struct.pack_into('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET, FULL_SPHERE_ANGLE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + DEVIATION_ANGLE_OFFSET, ZERO_DEVIATION)

        print(f"    AFTER:  AA={math.degrees(FULL_SPHERE_ANGLE):.1f}deg/{MAX_RANGE:.0f}wu  "
              f"Mag={math.degrees(FULL_SPHERE_ANGLE):.1f}deg/{MAX_RANGE:.0f}wu  "
              f"Dev=0.00deg")

        # --- Clear "Aim Assists Only When Zoomed" flag (bit 5 at offset 300) ---
        flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
        if flags & (1 << 5):
            flags &= ~(1 << 5)
            struct.pack_into('<I', data, file_offset + WEAPON_FLAGS_OFFSET, flags)
            print(f"    CLEARED 'Aim Assists Only When Zoomed' flag")

        # --- Fix barrels ---
        barrel_count = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET)[0]
        barrel_ptr = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET + 4)[0]

        if barrel_count > 0:
            barrel_file_offset = barrel_ptr - secondary_magic
            for b in range(barrel_count):
                boff = barrel_file_offset + (b * BARREL_ENTRY_SIZE)
                bflags = struct.unpack_from('<I', data, boff)[0]
                changes = []

                # Clear "Projectile Vector Cannot Be Adjusted" (bit 6)
                if bflags & (1 << 6):
                    bflags &= ~(1 << 6)
                    changes.append("Projectile Vector Cannot Be Adjusted")

                # Clear "Use Error When Unzoomed" (bit 5)
                if bflags & (1 << 5):
                    bflags &= ~(1 << 5)
                    changes.append("Use Error When Unzoomed")

                if changes:
                    struct.pack_into('<I', data, boff, bflags)
                    print(f"    Barrel[{b}]: CLEARED {', '.join(changes)}")

                # Zero error values for perfect accuracy
                min_error = struct.unpack_from('<f', data, boff + 112)[0]
                error_min = struct.unpack_from('<f', data, boff + 116)[0]
                error_max = struct.unpack_from('<f', data, boff + 120)[0]
                dist_angle = struct.unpack_from('<f', data, boff + 108)[0]

                if any(v != 0.0 for v in [min_error, error_min, error_max, dist_angle]):
                    struct.pack_into('<f', data, boff + 108, 0.0)  # distribution angle
                    struct.pack_into('<f', data, boff + 112, 0.0)  # min_error
                    struct.pack_into('<f', data, boff + 116, 0.0)  # error_min
                    struct.pack_into('<f', data, boff + 120, 0.0)  # error_max
                    print(f"    Barrel[{b}]: ZEROED error angles")

        modified_count += 1

    print(f"\n{'='*70}")
    print(f"Modified {modified_count}/{weapon_count} weapon tags")
    print(f"Saving to {MAP_FILE}...")

    with open(MAP_FILE, 'wb') as f:
        f.write(data)

    print("Done!")


if __name__ == '__main__':
    main()
