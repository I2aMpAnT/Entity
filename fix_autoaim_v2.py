#!/usr/bin/env python3
"""Fix autoaim v2 - use large angle values and ensure all weapons have zoom capability.

Findings:
- 2*pi worked sporadically (but only 2 weapons were modded)
- pi (half-angle theory) made it WORSE - only works zoomed
- Conclusion: pi is NOT correct. The engine does NOT treat this as a half-angle.

Fix approach:
1. Use a very large angle (100 radians) - engine will clamp internally
2. Give weapons with 0 zoom levels a zoom_levels=1 with subtle FOV so
   the engine treats them as zoom-capable (which may be required for autoaim)
3. Max out all ranges
4. Zero all deviation and barrel errors
"""
import struct
import sys
import math
import shutil

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Weapon tag offsets
AUTO_AIM_ANGLE_OFFSET = 520
AUTO_AIM_RANGE_OFFSET = 524
MAGNETISM_ANGLE_OFFSET = 528
MAGNETISM_RANGE_OFFSET = 532
DEVIATION_ANGLE_OFFSET = 536
WEAPON_FLAGS_OFFSET = 300

# Zoom fields
ZOOM_LEVELS_OFFSET = 510       # int16 - number of zoom levels
ZOOM_MIN_FOV_OFFSET = 512      # float - min magnification
ZOOM_MAX_FOV_OFFSET = 516      # float - max magnification

# Barrel reflexive
BARRELS_REFLEXIVE_OFFSET = 720
BARREL_ENTRY_SIZE = 236

# Values to write
HUGE_ANGLE = 100.0              # ~5730 degrees - engine will clamp to max
MAX_RANGE = 100000.0
ZERO = 0.0

# Subtle zoom: 1.0x magnification = no visible zoom change but engine thinks we're zoomed
SUBTLE_ZOOM_FOV = 1.0


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
    print(f"=== Auto-Aim Fix v2: {MAP_FILE} ===")
    print(f"Angle: {HUGE_ANGLE} rad ({math.degrees(HUGE_ANGLE):.0f} deg)")
    print(f"Range: {MAX_RANGE}")
    print()

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    weapon_count = 0
    zoom_added = 0

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'weap':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]
        weapon_count += 1

        # --- Set autoaim values ---
        struct.pack_into('<f', data, file_offset + AUTO_AIM_ANGLE_OFFSET, HUGE_ANGLE)
        struct.pack_into('<f', data, file_offset + AUTO_AIM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_ANGLE_OFFSET, HUGE_ANGLE)
        struct.pack_into('<f', data, file_offset + MAGNETISM_RANGE_OFFSET, MAX_RANGE)
        struct.pack_into('<f', data, file_offset + DEVIATION_ANGLE_OFFSET, ZERO)

        # --- Force clear "Aim Assists Only When Zoomed" ---
        flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS_OFFSET)[0]
        flags &= ~(1 << 5)
        struct.pack_into('<I', data, file_offset + WEAPON_FLAGS_OFFSET, flags)

        # --- Check zoom levels ---
        zoom_levels = struct.unpack_from('<h', data, file_offset + ZOOM_LEVELS_OFFSET)[0]
        zoom_min = struct.unpack_from('<f', data, file_offset + ZOOM_MIN_FOV_OFFSET)[0]
        zoom_max = struct.unpack_from('<f', data, file_offset + ZOOM_MAX_FOV_OFFSET)[0]

        status = f"AA=100rad Mag=100rad Dev=0"
        if zoom_levels == 0:
            # Give it a zoom level with 1.0x magnification (no visible change)
            struct.pack_into('<h', data, file_offset + ZOOM_LEVELS_OFFSET, 1)
            struct.pack_into('<f', data, file_offset + ZOOM_MIN_FOV_OFFSET, SUBTLE_ZOOM_FOV)
            struct.pack_into('<f', data, file_offset + ZOOM_MAX_FOV_OFFSET, SUBTLE_ZOOM_FOV)
            zoom_added += 1
            status += " +ZOOM(1x)"
        else:
            status += f" zoom={zoom_levels}lvl"

        # --- Fix barrels ---
        barrel_count = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET)[0]
        barrel_ptr = struct.unpack_from('<i', data, file_offset + BARRELS_REFLEXIVE_OFFSET + 4)[0]

        if barrel_count > 0:
            barrel_file_offset = barrel_ptr - secondary_magic
            for b in range(barrel_count):
                boff = barrel_file_offset + (b * BARREL_ENTRY_SIZE)
                bflags = struct.unpack_from('<I', data, boff)[0]

                # Clear restrictive barrel flags
                bflags &= ~(1 << 5)  # Use Error When Unzoomed
                bflags &= ~(1 << 6)  # Projectile Vector Cannot Be Adjusted
                struct.pack_into('<I', data, boff, bflags)

                # Zero error values
                struct.pack_into('<f', data, boff + 108, 0.0)  # distribution angle
                struct.pack_into('<f', data, boff + 112, 0.0)  # min_error
                struct.pack_into('<f', data, boff + 116, 0.0)  # error_min
                struct.pack_into('<f', data, boff + 120, 0.0)  # error_max

        print(f"  [{i}] {short_name}: {status}")

    print(f"\n{'='*60}")
    print(f"Modified {weapon_count} weapons, added zoom to {zoom_added}")
    print(f"Saving to {MAP_FILE}...")

    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done!")


if __name__ == '__main__':
    main()
