#!/usr/bin/env python3
"""Fix autoaim v4 - The Se7enSins/DotHalo method done correctly.

Key fix: DEVIATION ANGLE must be set to 2*pi, not 0!
- Autoaim Angle = cone where engine detects targets (reticle turns red)
- Magnetism Angle = cone where crosshair sticks (controller)
- Deviation Angle = max angle a BULLET can bend toward a target (THE ACTUAL AIMBOT)

Setting deviation to 0 = bullets fly straight regardless of autoaim.
That's why it only seemed to work when zoomed (narrow FOV masked the issue).

Also increases biped autoaim width (target pill size at bipd +0x23C).
"""
import struct
import sys
import math
import shutil

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Weapon offsets (confirmed matching Assembly weap.xml plugin)
AUTO_AIM_ANGLE  = 0x208  # 520
AUTO_AIM_RANGE  = 0x20C  # 524
MAGNETISM_ANGLE = 0x210  # 528
MAGNETISM_RANGE = 0x214  # 532
DEVIATION_ANGLE = 0x218  # 536  ← THIS WAS SET TO 0, SHOULD BE 2*pi!
WEAPON_FLAGS    = 0x12C  # 300
ZOOM_LEVELS     = 0x1FE  # 510
ZOOM_MIN_FOV    = 0x200  # 512
ZOOM_MAX_FOV    = 0x204  # 516
BARRELS_COUNT   = 0x2D0  # 720
BARRELS_PTR     = 0x2D4  # 724
BARREL_SIZE     = 236

# Biped offset
BIPD_AUTOAIM_WIDTH = 0x23C  # 572 - autoaim pill width

# Values - the Se7enSins standard
TWO_PI = math.pi * 2       # 6.28318... = 360 degrees
MAX_RANGE = 100000.0
AUTOAIM_WIDTH = 5.0         # Large autoaim pill (stock is ~0.15-0.25)


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
    print(f"=== Auto-Aim v4 (DotHalo method): {MAP_FILE} ===")
    print(f"Autoaim Angle:  {TWO_PI:.4f} rad (360 deg)")
    print(f"Deviation Angle: {TWO_PI:.4f} rad (360 deg) ← THE FIX")
    print(f"Range: {MAX_RANGE}")
    print(f"Biped Autoaim Width: {AUTOAIM_WIDTH}")
    print()

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    weap_count = 0
    bipd_count = 0

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]

        if tag_class == 'weap':
            weap_count += 1

            # Read old values for comparison
            old_aa = struct.unpack_from('<f', data, file_offset + AUTO_AIM_ANGLE)[0]
            old_dev = struct.unpack_from('<f', data, file_offset + DEVIATION_ANGLE)[0]
            old_flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS)[0]
            zoom = struct.unpack_from('<h', data, file_offset + ZOOM_LEVELS)[0]

            # Set all aim assist values
            struct.pack_into('<f', data, file_offset + AUTO_AIM_ANGLE, TWO_PI)
            struct.pack_into('<f', data, file_offset + AUTO_AIM_RANGE, MAX_RANGE)
            struct.pack_into('<f', data, file_offset + MAGNETISM_ANGLE, TWO_PI)
            struct.pack_into('<f', data, file_offset + MAGNETISM_RANGE, MAX_RANGE)
            struct.pack_into('<f', data, file_offset + DEVIATION_ANGLE, TWO_PI)  # THE KEY FIX

            # Clear "Aim Assists Only When Zoomed" (bit 5)
            flags = struct.unpack_from('<I', data, file_offset + WEAPON_FLAGS)[0]
            flags &= ~(1 << 5)
            struct.pack_into('<I', data, file_offset + WEAPON_FLAGS, flags)

            # Fix barrel flags
            barrel_count = struct.unpack_from('<i', data, file_offset + BARRELS_COUNT)[0]
            barrel_ptr = struct.unpack_from('<i', data, file_offset + BARRELS_COUNT + 4)[0]
            if barrel_count > 0:
                barrel_file = barrel_ptr - secondary_magic
                for b in range(barrel_count):
                    boff = barrel_file + (b * BARREL_SIZE)
                    bflags = struct.unpack_from('<I', data, boff)[0]
                    bflags &= ~(1 << 5)  # Clear "Use Error When Unzoomed"
                    bflags &= ~(1 << 6)  # Clear "Projectile Vector Cannot Be Adjusted"
                    struct.pack_into('<I', data, boff, bflags)
                    # Zero out error angles (weapon spread, NOT autoaim)
                    struct.pack_into('<f', data, boff + 108, 0.0)
                    struct.pack_into('<f', data, boff + 112, 0.0)
                    struct.pack_into('<f', data, boff + 116, 0.0)
                    struct.pack_into('<f', data, boff + 120, 0.0)

            print(f"  [{i:4d}] {short_name:30s} dev: {old_dev:.4f} -> {TWO_PI:.4f}  zoom={zoom}")

        elif tag_class == 'bipd':
            bipd_count += 1
            old_width = struct.unpack_from('<f', data, file_offset + BIPD_AUTOAIM_WIDTH)[0]
            struct.pack_into('<f', data, file_offset + BIPD_AUTOAIM_WIDTH, AUTOAIM_WIDTH)
            print(f"  [{i:4d}] BIPD {short_name:26s} autoaim_width: {old_width:.4f} -> {AUTOAIM_WIDTH}")

    print(f"\nModified {weap_count} weapons, {bipd_count} bipeds. Saving...")
    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done!")


if __name__ == '__main__':
    main()
