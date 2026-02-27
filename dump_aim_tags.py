#!/usr/bin/env python3
"""Dump globals (matg) and player biped (bipd) tags to find aim-assist gating.

The weapon-level autoaim fields are set correctly but autoaim only works when
zoomed. The restriction must be coming from the globals or biped tags.
"""
import struct
import sys
import math

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Tags to look for
TARGET_CLASSES = ['matg', 'bipd', 'unit']


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


def dump_floats_and_ints(data, offset, count, label=""):
    """Dump a range of bytes as both floats and ints to find interesting values."""
    print(f"\n  {label} (offset range {offset} - {offset + count*4}):")
    for j in range(count):
        pos = offset + j * 4
        fval = struct.unpack_from('<f', data, pos)[0]
        ival = struct.unpack_from('<I', data, pos)[0]
        i16a = struct.unpack_from('<h', data, pos)[0]
        i16b = struct.unpack_from('<h', data, pos + 2)[0]
        raw = data[pos:pos+4].hex()

        # Flag interesting values
        flag = ""
        if 0.001 < abs(fval) < 1000 and fval == fval:  # reasonable float, not NaN
            deg = math.degrees(fval) if abs(fval) < 10 else fval
            flag = f"  <-- {fval:.4f} ({deg:.1f} deg)" if abs(fval) < 10 else f"  <-- {fval:.2f}"
        elif ival == 0xFFFFFFFF:
            flag = "  <-- -1 / 0xFFFFFFFF"
        elif ival == 0:
            flag = ""
        elif 0 < ival < 100:
            flag = f"  <-- int: {ival}"

        print(f"    +{j*4:4d} [{raw}] f={fval:12.6f} i32=0x{ival:08X} i16={i16a},{i16b}{flag}")


def main():
    print(f"=== Dump Aim-Assist Tags: {MAP_FILE} ===\n")

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')

        if tag_class not in TARGET_CLASSES:
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        tag_size = struct.unpack_from('<i', data, entry_pos + 12)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")

        print(f"{'='*70}")
        print(f"[{i}] {tag_class} - {name}")
        print(f"  File offset: 0x{file_offset:X}, Size: {tag_size}")

        if tag_class == 'matg':
            # Globals tag - dump first 400 bytes looking for player info block
            print("\n  --- GLOBALS TAG RAW DUMP (first 600 bytes) ---")
            dump_floats_and_ints(data, file_offset, 150, "matg data")

            # Look for reflexive blocks (count + pointer pairs)
            print("\n  --- Scanning for reflexive blocks ---")
            for off in range(0, min(tag_size, 400), 4):
                count = struct.unpack_from('<i', data, file_offset + off)[0]
                if 0 < count < 50:  # reasonable reflexive count
                    ptr = struct.unpack_from('<i', data, file_offset + off + 4)[0]
                    if ptr != 0 and abs(ptr) > 0x1000:
                        ref_file = ptr - secondary_magic
                        print(f"    +{off}: count={count}, ptr=0x{ptr:08X} (file=0x{ref_file:X})")

        elif tag_class == 'bipd':
            print(f"\n  --- BIPED TAG ---")
            # Unit/biped tags have aim-related fields
            # OBJE base is about 196 bytes, then UNIT extends it
            # Unit has camera, aim, and targeting fields

            # Dump a large range to find aim-assist fields
            # Unit aim fields are typically around offset 400-600
            print("\n  Scanning for aim-assist related floats (offset 0-800):")
            for off in range(0, min(tag_size, 800), 4):
                fval = struct.unpack_from('<f', data, file_offset + off)[0]
                if fval != fval:  # NaN
                    continue
                if 0.01 < abs(fval) < 7.0:  # likely an angle in radians
                    deg = math.degrees(fval)
                    if 0.5 < abs(deg) < 360:
                        print(f"    +{off}: {fval:.6f} rad ({deg:.2f} deg)")
                elif 1.0 < fval < 200.0:  # likely a range in world units
                    print(f"    +{off}: {fval:.2f} (possible range/distance)")

            # Also dump raw around likely aim assist areas
            dump_floats_and_ints(data, file_offset + 400, 80, "bipd @400-720")

        print()


if __name__ == '__main__':
    main()
