#!/usr/bin/env python3
"""Fix autoaim v6 - Exact DotHalo / Se7enSins method.

From the "how to mod halo 2 using dothalo" tutorial on Se7enSins:
  - "acquire target distance" = 100000
  - "deviation angle 1cf" = 360
  - "auto aim range 1cf" = 100000
  - two fields beneath = 0, 0

From the "360 auto aim" thread: pattern is 360, 1000, 360, 1000

The DotHalo field names map to Assembly offsets:
  0x208: Autoaim Angle    = 360 (raw radians, NOT 6.28)
  0x20C: Autoaim Range    = 100000
  0x210: Magnetism Angle  = 0   (NOT 2*pi like I was setting!)
  0x214: Magnetism Range  = 0   (NOT 100000!)
  0x218: Deviation Angle  = leave at stock (0.0 for handheld weapons)

Key differences from my previous attempts:
  - Use 360 not 6.28 for the angle
  - Set magnetism to 0, not to matching values
  - Don't touch deviation angle
"""
import struct
import sys
import math

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# THE EXACT DOTHALO VALUES
AUTOAIM_ANGLE = 360.0       # Raw 360 radians (what DotHalo users type)
AUTOAIM_RANGE = 100000.0
MAGNETISM_ANGLE = 0.0       # DotHalo method sets these to 0!
MAGNETISM_RANGE = 0.0


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
    print(f"=== Auto-Aim v6 (Exact DotHalo method): {MAP_FILE} ===")
    print(f"Autoaim Angle:    {AUTOAIM_ANGLE}")
    print(f"Autoaim Range:    {AUTOAIM_RANGE}")
    print(f"Magnetism Angle:  {MAGNETISM_ANGLE}")
    print(f"Magnetism Range:  {MAGNETISM_RANGE}")
    print(f"Deviation Angle:  stock (untouched)")
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

        # Read current deviation (keep stock)
        cur_dev = struct.unpack_from('<f', data, file_offset + 0x218)[0]

        # THE EXACT DOTHALO VALUES
        struct.pack_into('<f', data, file_offset + 0x208, AUTOAIM_ANGLE)   # Autoaim Angle = 360
        struct.pack_into('<f', data, file_offset + 0x20C, AUTOAIM_RANGE)   # Autoaim Range = 100000
        struct.pack_into('<f', data, file_offset + 0x210, MAGNETISM_ANGLE) # Magnetism Angle = 0
        struct.pack_into('<f', data, file_offset + 0x214, MAGNETISM_RANGE) # Magnetism Range = 0
        # 0x218 Deviation Angle = UNTOUCHED (stock value)

        # Clear "Aim Assists Only When Zoomed" flag (bit 5)
        flags = struct.unpack_from('<I', data, file_offset + 0x12C)[0]
        flags &= ~(1 << 5)
        struct.pack_into('<I', data, file_offset + 0x12C, flags)

        # Fix barrels - clear restrictive flags, zero error
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
                struct.pack_into('<f', data, boff + 108, 0.0)
                struct.pack_into('<f', data, boff + 112, 0.0)
                struct.pack_into('<f', data, boff + 116, 0.0)
                struct.pack_into('<f', data, boff + 120, 0.0)

        print(f"  [{i:4d}] {short_name:30s} dev={cur_dev:.4f}(stock)")

    print(f"\nModified {count} weapons. Saving...")
    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("Done! Exact DotHalo method applied.")


if __name__ == '__main__':
    main()
