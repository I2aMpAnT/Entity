#!/usr/bin/env python3
"""Fix autoaim only working when zoomed - diagnose and force-clear all zoom restrictions.

Dumps weapon flags for all weapons and force-clears bit 5 ("Aim Assists Only When Zoomed")
on EVERY weapon regardless of current state. Also checks for any other zoom-related fields.
"""
import struct
import sys
import math

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

WEAPON_FLAGS_OFFSET = 300
AUTO_AIM_ANGLE_OFFSET = 520
AUTO_AIM_RANGE_OFFSET = 524
MAGNETISM_ANGLE_OFFSET = 528
MAGNETISM_RANGE_OFFSET = 532
DEVIATION_ANGLE_OFFSET = 536

# Zoom fields
ZOOM_LEVELS_OFFSET = 510       # int16
ZOOM_MIN_FOV_OFFSET = 512      # float
ZOOM_MAX_FOV_OFFSET = 516      # float

# Weapon flag bit definitions
WEAPON_FLAG_NAMES = {
    0: "Vertical Heat Display",
    1: "Mutually Exclusive Triggers",
    2: "Attacks Automatically on Bump",
    3: "Must Be Readied",
    4: "Doesn't Count Toward Maximum",
    5: "Aim Assists Only When Zoomed",
    6: "Prevents Grenade Throwing",
    7: "Must Be Picked Up",
    8: "Holds Triggers When Dropped",
    9: "Prevents Melee Attack",
    10: "Detonates When Dropped",
    11: "Cannot Fire At Maximum Range",
    12: "Secondary Trigger Overrides Grenades",
    13: "Obsolete Does Not Depower Active Camo",
    14: "Enables Integrated Night Vision",
    15: "AIs Use Weapon Melee Damage",
    16: "Forces No Binoculars",
    17: "Loop FP Firing Animation",
    18: "Prevents Sprinting",
    19: "Cannot Fire While Boosting",
    20: "Prevents Crouching",
    21: "Prevents Jumping",
    22: "Can Be Dual Wielded",
}

# Additional offsets to check for aim-assist behavior
# OBJE base flags at offset 2 (int16)
OBJE_FLAGS_OFFSET = 2


def parse_map(data):
    sig = struct.unpack_from('<I', data, 0)[0]
    assert sig == 0x68656164
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
    print(f"=== Zoom Auto-Aim Fix: {MAP_FILE} ===\n")

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

        # Read current state
        flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
        aa_angle = struct.unpack_from('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET)[0]
        zoom_levels = struct.unpack_from('<h', data, file_offset + ZOOM_LEVELS_OFFSET)[0]
        zoom_min = struct.unpack_from('<f', data, file_offset + ZOOM_MIN_FOV_OFFSET)[0]
        zoom_max = struct.unpack_from('<f', data, file_offset + ZOOM_MAX_FOV_OFFSET)[0]

        zoom_only = bool(flags & (1 << 5))
        short_name = name.split('\\')[-1]

        print(f"[{i}] {short_name}")
        print(f"     Flags: 0x{flags:08X}  AimAssistZoomOnly={zoom_only}")
        print(f"     AA angle: {math.degrees(aa_angle):.1f}deg  Zoom: levels={zoom_levels} fov={zoom_min:.1f}-{zoom_max:.1f}")

        # Print all set flag bits
        set_bits = []
        for bit in range(23):
            if flags & (1 << bit):
                set_bits.append(f"b{bit}:{WEAPON_FLAG_NAMES.get(bit, '?')}")
        if set_bits:
            print(f"     Set flags: {', '.join(set_bits)}")

        # FORCE clear bit 5 regardless of current state (write the full flags with bit 5 guaranteed off)
        new_flags = flags & ~(1 << 5)
        if new_flags != flags:
            struct.pack_into('<I', data, file_offset + WEAPON_FLAGS_OFFSET, new_flags)
            print(f"     >>> CLEARED bit 5 (was set!)")
            modified = True
        else:
            print(f"     bit 5 already clear")

        # Dump raw bytes around offset 300 for any other suspicious fields
        raw_296 = data[file_offset + 296:file_offset + 308].hex()
        print(f"     Raw @296-308: {raw_296}")

        # Check bytes at offset 304 (secondary weapon flags or additional fields)
        secondary_flags = struct.unpack_from('<I', data, file_offset + 304)[0]
        if secondary_flags != 0:
            print(f"     Secondary field @304: 0x{secondary_flags:08X}")

        print()

    if modified:
        print(f"Saving fixes to {MAP_FILE}...")
        with open(MAP_FILE, 'wb') as f:
            f.write(data)
        print("Done!")
    else:
        print("Bit 5 was already clear on ALL weapons.")
        print("The zoom-only behavior may be caused by something else.")
        print("\nPossible causes:")
        print("  1. Auto-aim angle value issue (try different values)")
        print("  2. Game engine built-in zoom requirement for aim assist")
        print("  3. Controller vs mouse behavior difference")


if __name__ == '__main__':
    main()
