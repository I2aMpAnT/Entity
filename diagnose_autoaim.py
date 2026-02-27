#!/usr/bin/env python3
"""Deep diagnostic on BR vs sniper auto-aim - check barrel flags and reflexive data."""
import struct
import math

MAP_FILE = 'headlong.map'
TARGET_WEAPONS = ['sniper_rifle', 'battle_rifle']

# Reflexive offsets in weapon header
TRIGGERS_REFLEXIVE_OFFSET = 712   # count(4) + pointer(4)
BARRELS_REFLEXIVE_OFFSET = 720    # count(4) + pointer(4)
TRIGGER_ENTRY_SIZE = 64
BARREL_ENTRY_SIZE = 236

BARREL_FLAG_NAMES = {
    0: "Tracks Fired Projectile",
    1: "Random Firing Effects",
    2: "Can Fire With Partial Ammo",
    3: "Projectiles use Weapon Origin",
    4: "Ejects During Chamber",
    5: "Use Error When Unzoomed",
    6: "Projectile Vector Cannot Be Adjusted",
    7: "Projectiles Have Identical Error",
    8: "Projectiles Fire Parallel",
    9: "Cant Fire When Others Firing",
    10: "Cant Fire When Others Recovering",
    11: "Don't Clear Fire Bit After Recovering",
    12: "Stagger Fire Across Multiple Markers",
    13: "Fires Locked Projectiles",
}


def parse_map(data):
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
    print(f"Loading {MAP_FILE}...")
    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)
    modified = False

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'weap':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")

        if not any(t in name.lower() for t in TARGET_WEAPONS):
            continue

        print(f"\n{'='*70}")
        print(f"WEAPON: {name}")
        print(f"  Tag data at file offset: 0x{file_offset:X}")

        # Auto-aim values (already set)
        aa_angle = struct.unpack_from('<f', data, file_offset + 520)[0]
        aa_range = struct.unpack_from('<f', data, file_offset + 524)[0]
        mag_angle = struct.unpack_from('<f', data, file_offset + 528)[0]
        mag_range = struct.unpack_from('<f', data, file_offset + 532)[0]
        dev_angle = struct.unpack_from('<f', data, file_offset + 536)[0]
        print(f"\n  Auto-Aim: angle={math.degrees(aa_angle):.1f}deg range={aa_range:.0f}")
        print(f"  Magnetism: angle={math.degrees(mag_angle):.1f}deg range={mag_range:.0f}")
        print(f"  Deviation: {math.degrees(dev_angle):.2f}deg")

        # Weapon flags at offset 300
        wep_flags = struct.unpack_from('<I', data, file_offset + 300)[0]
        print(f"\n  Weapon Flags (offset 300): 0x{wep_flags:08X}")

        # Barrels reflexive
        barrel_count = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET)[0]
        barrel_ptr = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET + 4)[0]
        barrel_file_offset = barrel_ptr - secondary_magic if barrel_count > 0 else 0

        print(f"\n  Barrels: count={barrel_count}, ptr=0x{barrel_ptr:08X}, file=0x{barrel_file_offset:X}")

        for b in range(barrel_count):
            boff = barrel_file_offset + (b * BARREL_ENTRY_SIZE)
            bflags = struct.unpack_from('<I', data, boff)[0]
            print(f"\n  Barrel[{b}] flags: 0x{bflags:08X}")
            for bit in range(14):
                if bflags & (1 << bit):
                    print(f"    [SET] bit {bit}: {BARREL_FLAG_NAMES.get(bit, 'Unknown')}")

            # Check for "Projectile Vector Cannot Be Adjusted"
            if bflags & (1 << 6):
                print(f"    >>> PROBLEM: 'Projectile Vector Cannot Be Adjusted' IS SET!")
                print(f"    >>> CLEARING bit 6...")
                bflags &= ~(1 << 6)
                struct.pack_into('<I', data, boff, bflags)
                modified = True

            # Also clear "Use Error When Unzoomed" if set
            if bflags & (1 << 5):
                print(f"    >>> 'Use Error When Unzoomed' IS SET - CLEARING")
                bflags &= ~(1 << 5)
                struct.pack_into('<I', data, boff, bflags)
                modified = True

            # Read error values
            rps_min = struct.unpack_from('<f', data, boff + 4)[0]
            rps_max = struct.unpack_from('<f', data, boff + 8)[0]
            shots_lower = struct.unpack_from('<h', data, boff + 28)[0]
            shots_upper = struct.unpack_from('<h', data, boff + 30)[0]
            min_error = struct.unpack_from('<f', data, boff + 112)[0]
            error_min = struct.unpack_from('<f', data, boff + 116)[0]
            error_max = struct.unpack_from('<f', data, boff + 120)[0]
            dist_angle = struct.unpack_from('<f', data, boff + 108)[0]

            print(f"    RPS: {rps_min:.1f}-{rps_max:.1f}, Shots/fire: {shots_lower}-{shots_upper}")
            print(f"    Min error: {min_error:.6f} ({math.degrees(min_error):.3f}deg)")
            print(f"    Error angle: {error_min:.6f}-{error_max:.6f} rad")
            print(f"    Distribution angle: {dist_angle:.6f} rad")

            # Zero out error values for perfect accuracy
            if error_min != 0.0 or error_max != 0.0 or min_error != 0.0:
                print(f"    >>> ZEROING error values for perfect accuracy")
                struct.pack_into('<f', data, boff + 112, 0.0)  # min_error
                struct.pack_into('<f', data, boff + 116, 0.0)  # error_min
                struct.pack_into('<f', data, boff + 120, 0.0)  # error_max
                struct.pack_into('<f', data, boff + 108, 0.0)  # distribution angle
                modified = True

        # Triggers reflexive
        trig_count = struct.unpack_from('<i', data, file_offset + TRIGGERS_REFLEXIVE_OFFSET)[0]
        trig_ptr = struct.unpack_from('<i', data, file_offset + TRIGGERS_REFLEXIVE_OFFSET + 4)[0]
        trig_file_offset = trig_ptr - secondary_magic if trig_count > 0 else 0

        print(f"\n  Triggers: count={trig_count}")
        for t in range(trig_count):
            toff = trig_file_offset + (t * TRIGGER_ENTRY_SIZE)
            tflags = struct.unpack_from('<I', data, toff)[0]
            button = struct.unpack_from('<h', data, toff + 4)[0]
            behavior = struct.unpack_from('<h', data, toff + 6)[0]
            button_names = {0: "Right Trigger", 1: "Left Trigger", 2: "Melee", 3: "Automated Fire"}
            behavior_names = {0: "Spew", 1: "Latch", 2: "1-Latch 2-Autofire", 3: "Charge"}
            print(f"  Trigger[{t}]: flags=0x{tflags:08X}, button={button_names.get(button, button)}, behavior={behavior_names.get(behavior, behavior)}")

    if modified:
        print(f"\n{'='*70}")
        print(f"Saving fixes to {MAP_FILE}...")
        with open(MAP_FILE, 'wb') as f:
            f.write(data)
        print("Done!")
    else:
        print("\nNo changes needed.")


if __name__ == '__main__':
    main()
