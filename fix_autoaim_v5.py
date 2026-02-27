#!/usr/bin/env python3
"""Fix autoaim v5 - Revert v4 mistakes, restore working scoped state,
then apply correct unscoped fix.

v4 mistakes:
1. Set deviation angle to 6.28 on handheld weapons (stock is 0.0!)
   - Only vehicle turrets have non-zero deviation in stock
   - Setting 6.28 makes bullets deviate wildly
2. Set bipd+0x23C to 5.0 (stock is 0.0 for masterchief)

This script:
1. Restores deviation angle to STOCK values per weapon
2. Restores bipd+0x23C to stock values
3. Keeps aa/mag angles at 6.28 and ranges at 100000
4. Keeps "aim assists only when zoomed" flag cleared
5. Keeps barrel error angles at 0 (perfect accuracy)
"""
import struct
import sys
import math

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

TWO_PI = math.pi * 2
MAX_RANGE = 100000.0

# Stock deviation angles per weapon (from OGturf.map comparison)
# Most handheld weapons: 0.0
# Vehicle weapons with tracking: non-zero
STOCK_DEVIATION = {
    'chaingun_turret': 0.1745,    # 10 deg
    'ghost_gun': 0.7854,          # 45 deg
    'minigun_turret': 0.7854,     # 45 deg
    'banshee_gun': 0.7854,        # 45 deg
    'plasma_turret': 0.1745,      # 10 deg
    'big_needler': 0.1745,        # 10 deg
    'cannon_turret': 0.0,         # stock 0
    'cannon_turret_mp': 0.0,      # stock 0
    'mortar_turret': 0.0,         # stock 0
    'mortar_turret_mp': 0.0,      # stock 0
}


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
    print(f"=== Auto-Aim v5: {MAP_FILE} ===\n")

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]

        if tag_class == 'weap':
            # Set autoaim and magnetism (the detection/stickiness cones)
            struct.pack_into('<f', data, file_offset + 0x208, TWO_PI)     # autoaim angle
            struct.pack_into('<f', data, file_offset + 0x20C, MAX_RANGE)  # autoaim range
            struct.pack_into('<f', data, file_offset + 0x210, TWO_PI)     # magnetism angle
            struct.pack_into('<f', data, file_offset + 0x214, MAX_RANGE)  # magnetism range

            # Restore deviation angle to STOCK value
            dev = STOCK_DEVIATION.get(short_name, 0.0)
            struct.pack_into('<f', data, file_offset + 0x218, dev)

            # Clear "Aim Assists Only When Zoomed" (bit 5 of weapon flags)
            flags = struct.unpack_from('<I', data, file_offset + 0x12C)[0]
            flags &= ~(1 << 5)
            struct.pack_into('<I', data, file_offset + 0x12C, flags)

            # Fix barrel flags and zero error angles
            bc = struct.unpack_from('<i', data, file_offset + 0x2D0)[0]
            bp = struct.unpack_from('<i', data, file_offset + 0x2D4)[0]
            if bc > 0:
                bf = bp - secondary_magic
                for b in range(bc):
                    boff = bf + b * 236
                    bflags = struct.unpack_from('<I', data, boff)[0]
                    bflags &= ~(1 << 5)  # Clear "Use Error When Unzoomed"
                    bflags &= ~(1 << 6)  # Clear "Projectile Vector Cannot Be Adjusted"
                    struct.pack_into('<I', data, boff, bflags)
                    # Zero error angles (weapon spread)
                    struct.pack_into('<f', data, boff + 108, 0.0)
                    struct.pack_into('<f', data, boff + 112, 0.0)
                    struct.pack_into('<f', data, boff + 116, 0.0)
                    struct.pack_into('<f', data, boff + 120, 0.0)

            print(f"  [{i:4d}] {short_name:30s} aa=2pi  dev={dev:.4f}  flags=0x{flags:08X}")

        elif tag_class == 'bipd':
            # RESTORE bipd+0x23C to stock value (0.0 for masterchief, 0.05 for elite_mp)
            cur_val = struct.unpack_from('<f', data, file_offset + 0x23C)[0]
            if 'elite' in short_name:
                stock_val = 0.05
            else:
                stock_val = 0.0
            struct.pack_into('<f', data, file_offset + 0x23C, stock_val)
            print(f"  [{i:4d}] BIPD {short_name:26s} +0x23C: {cur_val:.4f} -> {stock_val:.4f} (stock)")

    print(f"\nSaving...")
    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done! Reverted v4 mistakes. Scoped autoaim should work again.")


if __name__ == '__main__':
    main()
