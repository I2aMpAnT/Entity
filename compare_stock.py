#!/usr/bin/env python3
"""Compare stock OG values against current modified values.
Check what deviation angle and bipd+0x23C actually are in the unmodified map."""
import struct
import sys
import math


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


def dump_aim_fields(map_file, label):
    with open(map_file, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)
    print(f"\n{'='*70}")
    print(f"  {label}: {map_file}")
    print(f"{'='*70}")

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]

        if tag_class == 'weap':
            aa_angle = struct.unpack_from('<f', data, file_offset + 0x208)[0]
            aa_range = struct.unpack_from('<f', data, file_offset + 0x20C)[0]
            mag_angle = struct.unpack_from('<f', data, file_offset + 0x210)[0]
            mag_range = struct.unpack_from('<f', data, file_offset + 0x214)[0]
            dev_angle = struct.unpack_from('<f', data, file_offset + 0x218)[0]
            flags = struct.unpack_from('<I', data, file_offset + 0x12C)[0]
            zoom = struct.unpack_from('<h', data, file_offset + 0x1FE)[0]
            zoom_min = struct.unpack_from('<f', data, file_offset + 0x200)[0]
            zoom_max = struct.unpack_from('<f', data, file_offset + 0x204)[0]

            # Also check melee magnetism
            melee_mag_angle = struct.unpack_from('<f', data, file_offset + 0x19C)[0]
            melee_mag_range = struct.unpack_from('<f', data, file_offset + 0x1A0)[0]

            zoom_flag = bool(flags & (1 << 5))
            print(f"  [{i:4d}] {short_name:30s}"
                  f"  aa={aa_angle:.4f}({math.degrees(aa_angle):.1f}d)"
                  f"  rng={aa_range:.0f}"
                  f"  mag={mag_angle:.4f}"
                  f"  dev={dev_angle:.4f}({math.degrees(dev_angle):.1f}d)"
                  f"  zoom={zoom}({zoom_min:.1f}-{zoom_max:.1f})"
                  f"  zoomOnly={zoom_flag}"
                  f"  melee={melee_mag_angle:.3f}/{melee_mag_range:.1f}")

            # Barrel details
            bc = struct.unpack_from('<i', data, file_offset + 0x2D0)[0]
            bp = struct.unpack_from('<i', data, file_offset + 0x2D4)[0]
            if bc > 0:
                bf = bp - secondary_magic
                for b in range(bc):
                    boff = bf + b * 236
                    bflags = struct.unpack_from('<I', data, boff)[0]
                    err_min = struct.unpack_from('<f', data, boff + 108)[0]
                    err_max = struct.unpack_from('<f', data, boff + 112)[0]
                    err_min_z = struct.unpack_from('<f', data, boff + 116)[0]
                    err_max_z = struct.unpack_from('<f', data, boff + 120)[0]
                    print(f"         barrel[{b}] flags=0x{bflags:08X}"
                          f"  err={err_min:.3f}-{err_max:.3f}"
                          f"  errZ={err_min_z:.3f}-{err_max_z:.3f}"
                          f"  useErrUnzoomed={'Y' if bflags & (1<<5) else 'N'}"
                          f"  cantAdjust={'Y' if bflags & (1<<6) else 'N'}")

        elif tag_class == 'bipd':
            # Dump the area around 0x23C to see what's actually there
            print(f"\n  [{i:4d}] BIPD {short_name}")
            for off in range(0x230, 0x260, 4):
                fval = struct.unpack_from('<f', data, file_offset + off)[0]
                ival = struct.unpack_from('<I', data, file_offset + off)[0]
                raw = data[file_offset + off:file_offset + off + 4].hex()
                flag = ""
                if fval == fval and fval != 0:
                    if 0.001 < abs(fval) < 100:
                        flag = f" = {fval:.6f}"
                print(f"         +0x{off:03X} [{raw}] 0x{ival:08X} f={fval:.6f}{flag}")


# Check both maps
import os
for f, label in [('OGturf.map', 'STOCK (OG turf)'), ('headlong.map', 'MODIFIED (headlong)')]:
    if os.path.exists(f):
        dump_aim_fields(f, label)
    else:
        print(f"\n  {f} not found")
