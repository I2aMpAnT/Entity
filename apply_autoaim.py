#!/usr/bin/env python3
"""Apply 360 auto-aim to sniper rifle and battle rifle in a Halo 2 map.

DotHalo tutorial equivalent: Go to weapon tag (weap), find auto-aim fields,
set auto aim angle to max. This script does the same thing via direct binary edit.

Entity plugin reference (weap.ent):
  offset 520: Auto Aim Angle (float, radians)
  offset 524: Auto Aim Range (float, world units)
  offset 528: Magnetism Angle (float, radians)
  offset 532: Magnetism Range (float, world units)
  offset 536: Deviation Angle (float, radians)
"""
import struct
import sys
import math
import shutil

MAP_FILE = 'headlong.map'
BACKUP_FILE = 'headlong_backup.map'

# Weapon tag data offsets (from weap.ent plugin)
AUTO_AIM_ANGLE_OFFSET = 520     # float, radians
AUTO_AIM_RANGE_OFFSET = 524     # float, world units
MAGNETISM_ANGLE_OFFSET = 528    # float, radians
MAGNETISM_RANGE_OFFSET = 532    # float, world units
DEVIATION_ANGLE_OFFSET = 536    # float, radians

# 360-degree values
FULL_CIRCLE = math.pi * 2       # 6.2832 radians = 360 degrees
MAX_RANGE = 100000.0            # massive range
ZERO_DEVIATION = 0.0            # perfect accuracy

# Target weapons - match by tag path substring
TARGET_WEAPONS = ['sniper_rifle', 'battle_rifle']


def parse_map(data):
    """Parse Halo 2 map header and tag index."""
    sig = struct.unpack_from('<I', data, 0)[0]
    assert sig == 0x68656164, f"Not a valid Halo 2 map (sig: 0x{sig:08X})"

    index_offset = struct.unpack_from('<i', data, 16)[0]
    meta_start = struct.unpack_from('<i', data, 20)[0]

    # Index header
    constant = struct.unpack_from('<i', data, index_offset)[0]
    raw_tags_offset = struct.unpack_from('<i', data, index_offset + 8)[0]
    meta_count = struct.unpack_from('<i', data, index_offset + 24)[0]

    primary_magic = constant - (index_offset + 32)
    tags_offset = raw_tags_offset - primary_magic

    # Secondary magic from lowest raw tag offset
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
        end = data.find(b'\x00', abs_ofs, abs_ofs + 512)
        if end == -1:
            end = abs_ofs + 512
        tag_names[i] = data[abs_ofs:end].decode('ascii', errors='replace')

    return tags_offset, meta_count, secondary_magic, tag_names


def find_weapon_tags(data, tags_offset, meta_count, secondary_magic, tag_names):
    """Find all weap tags and return info about target weapons."""
    weapons = []
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class_raw = data[entry_pos:entry_pos + 4]
        tag_class = tag_class_raw[::-1].decode('ascii', errors='replace')

        if tag_class == 'weap':
            raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
            tag_size = struct.unpack_from('<i', data, entry_pos + 12)[0]
            file_offset = raw_offset - secondary_magic
            name = tag_names.get(i, "(unknown)")
            weapons.append({
                'index': i,
                'name': name,
                'file_offset': file_offset,
                'size': tag_size,
            })
    return weapons


def read_current_values(data, file_offset):
    """Read current auto-aim values from a weapon tag."""
    aa_angle = struct.unpack_from('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET)[0]
    aa_range = struct.unpack_from('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET)[0]
    mag_angle = struct.unpack_from('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET)[0]
    mag_range = struct.unpack_from('<f', data, file_offset + MAGNETISM_RANGE_OFFSET)[0]
    dev_angle = struct.unpack_from('<f', data, file_offset + DEVIATION_ANGLE_OFFSET)[0]
    return aa_angle, aa_range, mag_angle, mag_range, dev_angle


def apply_autoaim(data, file_offset):
    """Write 360 auto-aim values to a weapon tag."""
    struct.pack_into('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET, FULL_CIRCLE)
    struct.pack_into('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET, MAX_RANGE)
    struct.pack_into('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET, FULL_CIRCLE)
    struct.pack_into('<f', data, file_offset + MAGNETISM_RANGE_OFFSET, MAX_RANGE)
    struct.pack_into('<f', data, file_offset + DEVIATION_ANGLE_OFFSET, ZERO_DEVIATION)


def main():
    print(f"Loading {MAP_FILE}...")
    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())
    print(f"  Map size: {len(data)} bytes")

    # Create backup
    shutil.copy2(MAP_FILE, BACKUP_FILE)
    print(f"  Backup saved to {BACKUP_FILE}")

    # Parse map
    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)
    print(f"  Tag count: {meta_count}")
    print(f"  Secondary magic: 0x{secondary_magic:08X}")

    # Find all weapon tags
    all_weapons = find_weapon_tags(data, tags_offset, meta_count, secondary_magic, tag_names)
    print(f"\nFound {len(all_weapons)} weapon tags total:")
    for w in all_weapons:
        print(f"  [{w['index']}] {w['name']}")

    # Find and modify target weapons
    modified = 0
    for weapon in all_weapons:
        is_target = any(t in weapon['name'].lower() for t in TARGET_WEAPONS)
        if not is_target:
            continue

        print(f"\n{'='*70}")
        print(f"TARGET: {weapon['name']}")
        print(f"  Tag index: {weapon['index']}")
        print(f"  File offset: 0x{weapon['file_offset']:X}")
        print(f"  Tag size: {weapon['size']}")

        # Read before values
        before = read_current_values(data, weapon['file_offset'])
        print(f"\n  BEFORE:")
        print(f"    Auto Aim Angle:  {before[0]:.6f} rad ({math.degrees(before[0]):.2f} deg)")
        print(f"    Auto Aim Range:  {before[1]:.2f}")
        print(f"    Magnetism Angle: {before[2]:.6f} rad ({math.degrees(before[2]):.2f} deg)")
        print(f"    Magnetism Range: {before[3]:.2f}")
        print(f"    Deviation Angle: {before[4]:.6f} rad ({math.degrees(before[4]):.2f} deg)")

        # Apply changes
        apply_autoaim(data, weapon['file_offset'])

        # Read after values
        after = read_current_values(data, weapon['file_offset'])
        print(f"\n  AFTER:")
        print(f"    Auto Aim Angle:  {after[0]:.6f} rad ({math.degrees(after[0]):.2f} deg)")
        print(f"    Auto Aim Range:  {after[1]:.2f}")
        print(f"    Magnetism Angle: {after[2]:.6f} rad ({math.degrees(after[2]):.2f} deg)")
        print(f"    Magnetism Range: {after[3]:.2f}")
        print(f"    Deviation Angle: {after[4]:.6f} rad ({math.degrees(after[4]):.2f} deg)")

        modified += 1

    if modified == 0:
        print("\nERROR: No target weapons found! Check tag names.")
        sys.exit(1)

    # Save modified map
    print(f"\n{'='*70}")
    print(f"Modified {modified} weapon(s). Saving {MAP_FILE}...")
    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done!")


if __name__ == '__main__':
    main()
