#!/usr/bin/env python3
"""Verify and fix auto-aim on headlong.map.

Checks:
1. Verify auto-aim float values at offsets 520-536 were written correctly
2. Check weapon flags at offset 300 for "Aim Assists Only When Zoomed" (bit 5)
3. Clear that flag so auto-aim works unscoped too
"""
import struct
import math

MAP_FILE = 'headlong.map'

# Offsets
AUTO_AIM_ANGLE_OFFSET = 520
AUTO_AIM_RANGE_OFFSET = 524
MAGNETISM_ANGLE_OFFSET = 528
MAGNETISM_RANGE_OFFSET = 532
DEVIATION_ANGLE_OFFSET = 536
WEAPON_FLAGS_OFFSET = 300
AIM_ASSIST_ZOOM_ONLY_BIT = 5  # bit 5 in bitmask32

TARGET_WEAPONS = ['sniper_rifle', 'battle_rifle']


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

        is_target = any(t in name.lower() for t in TARGET_WEAPONS)
        if not is_target:
            continue

        print(f"\n{'='*70}")
        print(f"WEAPON: {name}")
        print(f"  File offset: 0x{file_offset:X}")

        # Read auto-aim values
        aa_angle = struct.unpack_from('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET)[0]
        aa_range = struct.unpack_from('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET)[0]
        mag_angle = struct.unpack_from('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET)[0]
        mag_range = struct.unpack_from('<f', data, file_offset + MAGNETISM_RANGE_OFFSET)[0]
        dev_angle = struct.unpack_from('<f', data, file_offset + DEVIATION_ANGLE_OFFSET)[0]

        print(f"\n  Auto Aim Values:")
        print(f"    Auto Aim Angle:  {aa_angle:.6f} rad ({math.degrees(aa_angle):.2f} deg)")
        print(f"    Auto Aim Range:  {aa_range:.2f}")
        print(f"    Magnetism Angle: {mag_angle:.6f} rad ({math.degrees(mag_angle):.2f} deg)")
        print(f"    Magnetism Range: {mag_range:.2f}")
        print(f"    Deviation Angle: {dev_angle:.6f} rad ({math.degrees(dev_angle):.2f} deg)")

        # Read weapon flags at offset 300
        flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
        print(f"\n  Weapon Flags (offset 300): 0x{flags:08X}")

        zoom_only = bool(flags & (1 << AIM_ASSIST_ZOOM_ONLY_BIT))
        print(f"    Aim Assists Only When Zoomed (bit 5): {zoom_only}")

        # Print all set flag bits for diagnosis
        flag_names = {
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
            22: "Can Be Dual Wielded",
        }
        for bit in range(32):
            if flags & (1 << bit):
                fname = flag_names.get(bit, f"Unknown bit {bit}")
                print(f"    [SET] bit {bit}: {fname}")

        # Clear the "Aim Assists Only When Zoomed" flag if set
        if zoom_only:
            print(f"\n  >>> CLEARING 'Aim Assists Only When Zoomed' flag")
            flags &= ~(1 << AIM_ASSIST_ZOOM_ONLY_BIT)
            struct.pack_into('<I', data, file_offset + WEAPON_FLAGS_OFFSET, flags)
            modified = True

            # Verify
            new_flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
            print(f"  >>> New flags: 0x{new_flags:08X}")
            print(f"  >>> Aim Assists Only When Zoomed: {bool(new_flags & (1 << AIM_ASSIST_ZOOM_ONLY_BIT))}")

        # Also dump raw bytes around the auto-aim area for verification
        print(f"\n  Raw bytes at offset 520-540:")
        raw = data[file_offset + 520:file_offset + 540]
        print(f"    {raw.hex()}")

        # Dump magnification info
        zoom_levels = struct.unpack_from('<h', data, file_offset + 510)[0]
        zoom_min = struct.unpack_from('<f', data, file_offset + 512)[0]
        zoom_max = struct.unpack_from('<f', data, file_offset + 516)[0]
        print(f"\n  Zoom levels: {zoom_levels}, min: {zoom_min}, max: {zoom_max}")

    if modified:
        print(f"\n{'='*70}")
        print(f"Saving changes to {MAP_FILE}...")
        with open(MAP_FILE, 'wb') as f:
            f.write(data)
        print("Done!")
    else:
        print(f"\n{'='*70}")
        print("No flag changes needed.")


if __name__ == '__main__':
    main()
