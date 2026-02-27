#!/usr/bin/env python3
"""Dump the look function reflexive from player control block.
6 entries at matg player_control +116/+120 - these may contain
per-zoom-level or per-input-device aim settings."""
import struct
import sys
import math

MAP_FILE = sys.argv[1] if len(sys.argv) > 1 else 'headlong.map'


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
    print(f"=== Look Function Dump: {MAP_FILE} ===\n")
    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'matg':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        matg_offset = raw_offset - secondary_magic

        # Player control block
        pc_count = struct.unpack_from('<i', data, matg_offset + 240)[0]
        pc_ptr = struct.unpack_from('<i', data, matg_offset + 244)[0]
        pc_file = pc_ptr - secondary_magic

        # Look function reflexive at player_control +116
        lf_count = struct.unpack_from('<i', data, pc_file + 116)[0]
        lf_ptr = struct.unpack_from('<i', data, pc_file + 120)[0]
        lf_file = lf_ptr - secondary_magic

        print(f"Look function reflexive: count={lf_count}, file=0x{lf_file:X}")

        # Try to figure out entry size by looking at the data
        # Dump raw data for each entry
        # Typical look function entry might be 4-16 bytes
        total_bytes = 200  # dump enough to see all entries
        print(f"\nRaw dump ({total_bytes} bytes starting at 0x{lf_file:X}):")
        for j in range(total_bytes // 4):
            pos = lf_file + j * 4
            fval = struct.unpack_from('<f', data, pos)[0]
            ival = struct.unpack_from('<I', data, pos)[0]
            raw = data[pos:pos+4].hex()
            flag = ""
            if fval == fval and fval != 0.0:
                if 0.001 < abs(fval) < 100:
                    flag = f"  << {fval:.6f}"
                    if abs(fval) < 7:
                        flag += f" ({math.degrees(fval):.2f} deg)"
            elif ival == 0xFFFFFFFF:
                flag = "  << -1"
            print(f"  +{j*4:4d} [{raw}] f={fval:14.6f} u32=0x{ival:08X}{flag}")

        # Also dump the full player control block with labeled offsets
        print(f"\n{'='*60}")
        print(f"Full player control block (0x{pc_file:X}):")
        print(f"{'='*60}")

        # Known H2 player control fields from Entity plugin:
        field_labels = {
            0: "Magnetism Friction",
            4: "Magnetism Adhesion",
            8: "Inconsequential Target Scale",
            12: "??? (padding?)",
            16: "??? (padding?)",
            20: "??? (padding?)",
            24: "??? (padding?)",
            28: "Crosshair Location / Min Look",
            32: "Look Accel Time",
            36: "Look Accel Scale",
            40: "Look Peg Threshold",
            44: "Look Default Pitch Rate",
            48: "Look Default Yaw Rate",
            52: "Look Autolevelling Scale",
            56: "??? (padding?)",
            60: "??? (padding?)",
            64: "Min Weapon Swap Ticks",
            68: "Min Autolevel Ticks",
            72: "Look Zoom Dampening Factor?",
            76: "??? aim related",
            80: "??? aim related",
            84: "??? aim related",
            88: "??? aim related",
            92: "??? aim related",
            96: "??? (padding?)",
            100: "??? (padding?)",
            104: "??? (padding?)",
            108: "??? flags/enum",
            112: "Autoaim Related Angle?",
            116: "Look Function Count",
            120: "Look Function Pointer",
            124: "??? aim sensitivity",
            128: "??? (padding?)",
            132: "??? aim sensitivity",
            136: "??? aim sensitivity",
            140: "??? aim sensitivity",
            144: "??? aim sensitivity",
            148: "??? aim sensitivity",
            152: "??? aim sensitivity",
            156: "??? aim sensitivity",
        }

        for j in range(40):
            pos = pc_file + j * 4
            fval = struct.unpack_from('<f', data, pos)[0]
            ival = struct.unpack_from('<I', data, pos)[0]
            raw = data[pos:pos+4].hex()
            label = field_labels.get(j * 4, "")
            val_str = ""
            if fval == fval and fval != 0.0 and 0.001 < abs(fval) < 1000:
                val_str = f"{fval:.6f}"
                if abs(fval) < 7:
                    val_str += f" ({math.degrees(fval):.1f}deg)"
            elif ival != 0:
                val_str = f"0x{ival:08X}"

            if label or val_str:
                print(f"  +{j*4:3d}: [{raw}] {val_str:30s} {label}")

        break

if __name__ == '__main__':
    main()
