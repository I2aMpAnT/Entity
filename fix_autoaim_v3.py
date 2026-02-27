#!/usr/bin/env python3
"""Fix autoaim v3 - Set zoom to 1.01x (imperceptible) so holding zoom activates aim assist.

H2V engine hardcodes aim assist behind zoom state. Map edits can't override this.
Workaround: give all weapons a nearly-invisible zoom (1.01x magnification).
When the virtual controller holds the zoom button, aim assist activates
with no visible change to the camera.

Also restores autoaim angle to 2*pi (the value that was working sporadically
before - the issue was only 2 weapons being modified, not the angle value).
"""
import struct
import sys
import math
import shutil

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Offsets
AUTO_AIM_ANGLE_OFFSET = 520
AUTO_AIM_RANGE_OFFSET = 524
MAGNETISM_ANGLE_OFFSET = 528
MAGNETISM_RANGE_OFFSET = 532
DEVIATION_ANGLE_OFFSET = 536
WEAPON_FLAGS_OFFSET = 300
ZOOM_LEVELS_OFFSET = 510
ZOOM_MIN_FOV_OFFSET = 512
ZOOM_MAX_FOV_OFFSET = 516
BARRELS_REFLEXIVE_OFFSET = 720
BARREL_ENTRY_SIZE = 236

# Values
AUTOAIM_ANGLE = math.pi * 2    # 360 deg - was working before, issue was only 2 weapons
MAX_RANGE = 100000.0
ZOOM_MAG = 1.01                # 1.01x = imperceptible zoom, but engine registers as "zoomed"


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
    print(f"=== Auto-Aim v3: {MAP_FILE} ===")
    print(f"Angle: 2*pi ({math.degrees(AUTOAIM_ANGLE):.0f} deg)")
    print(f"Zoom: {ZOOM_MAG}x (imperceptible)")
    print()

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)
    count = 0

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'weap':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]
        count += 1

        # Autoaim values
        struct.pack_into('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET, AUTOAIM_ANGLE)
        struct.pack_into('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET, AUTOAIM_ANGLE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + DEVIATION_ANGLE_OFFSET, 0.0)

        # Clear zoom-only flag
        flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
        flags &= ~(1 << 5)
        struct.pack_into('<I', data, file_offset + WEAPON_FLAGS_OFFSET, flags)

        # Set zoom to 1.01x for weapons that don't have zoom
        zoom_levels = struct.unpack_from('<h', data, file_offset + ZOOM_LEVELS_OFFSET)[0]
        if zoom_levels == 0:
            struct.pack_into('<h', data, file_offset + ZOOM_LEVELS_OFFSET, 1)
            struct.pack_into('<f', data, file_offset + ZOOM_MIN_FOV_OFFSET, ZOOM_MAG)
            struct.pack_into('<f', data, file_offset + ZOOM_MAX_FOV_OFFSET, ZOOM_MAG)
            print(f"  [{i}] {short_name}: +zoom(1.01x)")
        else:
            print(f"  [{i}] {short_name}: zoom={zoom_levels}lvl (kept)")

        # Fix barrels
        barrel_count = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET)[0]
        barrel_ptr = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET + 4)[0]
        if barrel_count > 0:
            barrel_file_offset = barrel_ptr - secondary_magic
            for b in range(barrel_count):
                boff = barrel_file_offset + (b * BARREL_ENTRY_SIZE)
                bflags = struct.unpack_from('<I', data, boff)[0]
                bflags &= ~(1 << 5)  # Use Error When Unzoomed
                bflags &= ~(1 << 6)  # Projectile Vector Cannot Be Adjusted
                struct.pack_into('<I', data, boff, bflags)
                struct.pack_into('<f', data, boff + 108, 0.0)
                struct.pack_into('<f', data, boff + 112, 0.0)
                struct.pack_into('<f', data, boff + 116, 0.0)
                struct.pack_into('<f', data, boff + 120, 0.0)

    print(f"\nModified {count} weapons. Saving...")
    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done!")
    print()
    print("IMPORTANT: H2V engine hardcodes aim assist behind zoom state.")
    print("Have your virtual controller script HOLD the zoom button (left trigger).")
    print("All weapons without stock zoom now have 1.01x zoom (invisible).")
    print("This tricks the engine into 'zoomed' state, activating aim assist.")


if __name__ == '__main__':
    main()
