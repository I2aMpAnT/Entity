#!/usr/bin/env python3
"""Revert the 1.01x zoom hack from v3 on weapons that shouldn't have zoom.

Stock weapons with zoom: battle_rifle(1), covenant_carbine(1),
beam_rifle(2), sniper_rifle(2), rocket_launcher(1)
Everything else should be zoom=0.
"""
import struct
import sys

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'

# Weapons that SHOULD have zoom (stock)
ZOOM_WEAPONS = {
    'battle_rifle': (1, 2.0, 2.0),
    'covenant_carbine': (1, 2.0, 2.0),
    'beam_rifle': (2, 2.0, 10.0),
    'sniper_rifle': (2, 2.0, 10.0),
    'rocket_launcher': (1, 2.0, 2.0),
}

ZOOM_LEVELS = 0x1FE
ZOOM_MIN    = 0x200
ZOOM_MAX    = 0x204


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
    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'weap':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        file_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        short_name = name.split('\\')[-1]

        cur_zoom = struct.unpack_from('<h', data, file_offset + ZOOM_LEVELS)[0]

        if short_name in ZOOM_WEAPONS:
            # Restore stock zoom values
            levels, zmin, zmax = ZOOM_WEAPONS[short_name]
            struct.pack_into('<h', data, file_offset + ZOOM_LEVELS, levels)
            struct.pack_into('<f', data, file_offset + ZOOM_MIN, zmin)
            struct.pack_into('<f', data, file_offset + ZOOM_MAX, zmax)
            print(f"  {short_name:30s} zoom={levels} ({zmin}x-{zmax}x) [stock]")
        else:
            # Remove fake zoom
            struct.pack_into('<h', data, file_offset + ZOOM_LEVELS, 0)
            struct.pack_into('<f', data, file_offset + ZOOM_MIN, 0.0)
            struct.pack_into('<f', data, file_offset + ZOOM_MAX, 0.0)
            if cur_zoom != 0:
                print(f"  {short_name:30s} zoom={cur_zoom} -> 0 [reverted]")

    with open(MAP_FILE, 'wb') as f:
        f.write(data)
    print("\nZoom hack reverted. Stock zoom values restored.")


if __name__ == '__main__':
    main()
