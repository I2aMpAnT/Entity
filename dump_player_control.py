#!/usr/bin/env python3
"""Dump player control and player information blocks from matg globals tag.

These contain magnetism friction, adhesion, and other aim-assist parameters
that may be gating autoaim behind zoom state.
"""
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


def dump_block(data, offset, size, label):
    """Dump a block of data with interpreted values."""
    print(f"\n{'='*60}")
    print(f"{label} at file offset 0x{offset:X} ({size} bytes)")
    print(f"{'='*60}")

    for j in range(size // 4):
        pos = offset + j * 4
        fval = struct.unpack_from('<f', data, pos)[0]
        ival = struct.unpack_from('<I', data, pos)[0]
        i16a = struct.unpack_from('<h', data, pos)[0]
        i16b = struct.unpack_from('<h', data, pos + 2)[0]
        raw = data[pos:pos+4].hex()

        flag = ""
        if fval == fval and fval != 0.0:  # not NaN and not zero
            if 0.001 < abs(fval) < 100:
                if abs(fval) < 7:
                    deg = math.degrees(fval)
                    flag = f"  << {fval:.6f} rad ({deg:.2f} deg)"
                else:
                    flag = f"  << {fval:.4f}"
            elif abs(fval) >= 100 and abs(fval) < 1000000:
                flag = f"  << {fval:.2f}"
            elif ival == 0xFFFFFFFF:
                flag = "  << -1"
        elif ival == 0xFFFFFFFF:
            flag = "  << -1 / 0xFFFF"
        elif 0 < ival < 1000 and fval != fval:
            flag = f"  << int: {ival}"

        if flag or ival != 0:
            print(f"  +{j*4:4d} [{raw}] f={fval:14.6f}  u32=0x{ival:08X}  i16=({i16a},{i16b}){flag}")


def main():
    print(f"=== Player Control & Info Dump: {MAP_FILE} ===\n")

    with open(MAP_FILE, 'rb') as f:
        data = bytearray(f.read())

    tags_offset, meta_count, secondary_magic, tag_names = parse_map(data)

    # Find matg tag
    for i in range(meta_count):
        entry_pos = tags_offset + (i * 16)
        tag_class = data[entry_pos:entry_pos + 4][::-1].decode('ascii', errors='replace')
        if tag_class != 'matg':
            continue

        raw_offset = struct.unpack_from('<i', data, entry_pos + 8)[0]
        matg_offset = raw_offset - secondary_magic
        name = tag_names.get(i, "(unknown)")
        print(f"Found matg: {name} at 0x{matg_offset:X}")

        # Known reflexive offsets in matg:
        # +240: player control (count=1)
        # +320: player information (count=1)

        # Player Control block
        pc_count = struct.unpack_from('<i', data, matg_offset + 240)[0]
        pc_ptr = struct.unpack_from('<i', data, matg_offset + 244)[0]
        if pc_count > 0:
            pc_file = pc_ptr - secondary_magic
            # Player control block is about 128 bytes
            # Known fields:
            # +0: float magnetism friction
            # +4: float magnetism adhesion
            # +8: float inconsequential target scale
            # +12-...: look acceleration, autoleveling, etc.
            print(f"\nPlayer Control: count={pc_count}, file=0x{pc_file:X}")
            dump_block(data, pc_file, 160, "PLAYER CONTROL")

            # Label known fields
            mag_fric = struct.unpack_from('<f', data, pc_file + 0)[0]
            mag_adh = struct.unpack_from('<f', data, pc_file + 4)[0]
            incon_scale = struct.unpack_from('<f', data, pc_file + 8)[0]
            print(f"\n  KNOWN FIELDS:")
            print(f"    Magnetism Friction:           {mag_fric:.6f}")
            print(f"    Magnetism Adhesion:           {mag_adh:.6f}")
            print(f"    Inconsequential Target Scale: {incon_scale:.6f}")

        # Player Information block
        pi_count = struct.unpack_from('<i', data, matg_offset + 320)[0]
        pi_ptr = struct.unpack_from('<i', data, matg_offset + 324)[0]
        if pi_count > 0:
            pi_file = pi_ptr - secondary_magic
            print(f"\nPlayer Information: count={pi_count}, file=0x{pi_file:X}")
            dump_block(data, pi_file, 200, "PLAYER INFORMATION")

        # Also dump the block at +248 (difficulty?) and +296 (weapon list?)
        for ref_off, ref_name, dump_size in [
            (248, "Difficulty", 200),
            (296, "Weapon List", 120),
            (304, "Cheat Powerups", 80),
        ]:
            rc = struct.unpack_from('<i', data, matg_offset + ref_off)[0]
            rp = struct.unpack_from('<i', data, matg_offset + ref_off + 4)[0]
            if rc > 0:
                rf = rp - secondary_magic
                print(f"\n{ref_name}: count={rc}, file=0x{rf:X}")
                dump_block(data, rf, dump_size, ref_name)

        break


if __name__ == '__main__':
    main()
