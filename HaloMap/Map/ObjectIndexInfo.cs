// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectIndexInfo.cs" company="">
//   
// </copyright>
// <summary>
//   The object index info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Map
{
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>
    /// The object index info.
    /// </summary>
    /// <remarks></remarks>
    public class ObjectIndexInfo
    {
        #region Constants and Fields

        /// <summary>
        /// The ident.
        /// </summary>
        public int[] Ident;

        /// <summary>
        /// The offset.
        /// </summary>
        public int[] Offset;

        /// <summary>
        /// The size.
        /// </summary>
        public int[] Size;

        /// <summary>
        /// The tag type.
        /// </summary>
        public string[] TagType;

        /// <summary>
        /// The tag type 2.
        /// </summary>
        public string[] TagType2;

        /// <summary>
        /// The tag type 3.
        /// </summary>
        public string[] TagType3;

        /// <summary>
        /// The tag types.
        /// </summary>
        public Hashtable TagTypes;

        /// <summary>
        /// The tag types count.
        /// </summary>
        public int TagTypesCount;

        /// <summary>
        /// The bitmapindex.
        /// </summary>
        public int[] bitmapindex;

        /// <summary>
        /// The external.
        /// </summary>
        public bool[] external;

        /// <summary>
        /// The highident.
        /// </summary>
        public int highident;

        /// <summary>
        /// The ident ht.
        /// </summary>
        public Hashtable identHT;

        /// <summary>
        /// The lowident.
        /// </summary>
        public int lowident;

        /// <summary>
        /// The stringoffset.
        /// </summary>
        public int[] stringoffset;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectIndexInfo"/> class.
        /// </summary>
        /// <param name="BR">The BR.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public ObjectIndexInfo(ref BinaryReader BR, Map map)
        {
            switch (map.HaloVersion)
            {
                case HaloVersionEnum.Halo2:
                    LoadHalo2ObjectIndexInfo(ref BR, map);
                    break;
                case HaloVersionEnum.Halo2Vista:
                    LoadHalo2ObjectIndexInfo(ref BR, map);
                    break;
                case HaloVersionEnum.HaloCE:
                    LoadHaloCEObjectIndexInfo(ref BR, map);
                    break;
                case HaloVersionEnum.Halo1:
                    LoadHalo1ObjectIndexInfo(ref BR, map);
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The load halo 1 object index info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHalo1ObjectIndexInfo(ref BinaryReader BR, Map map)
        {
            TagTypes = new Hashtable();
            identHT = new Hashtable();
            TagType = new string[map.IndexHeader.metaCount];
            TagType2 = new string[map.IndexHeader.metaCount];
            TagType3 = new string[map.IndexHeader.metaCount];
            Ident = new int[map.IndexHeader.metaCount];
            Offset = new int[map.IndexHeader.metaCount];
            Size = new int[map.IndexHeader.metaCount];
            stringoffset = new int[map.IndexHeader.metaCount];
            string[] temptagtypes = new string[500];
            external = new bool[map.IndexHeader.metaCount];
            bitmapindex = new int[map.IndexHeader.metaCount];
            for (int x = 0; x < map.IndexHeader.metaCount; x++)
            {
                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32);
                char[] tempchar = BR.ReadChars(4);

                Array.Reverse(tempchar);

                // BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 4;
                // char[] tempchar2 = BR.ReadChars(4);
                // Array.Reverse(tempchar2);
                // BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 8;
                // char[] tempchar3 = BR.ReadChars(4);
                // Array.Reverse(tempchar3);
                string tempstring = new string(tempchar);

                // string tempstring2 = new string(tempchar2);
                // string tempstring3 = new string(tempchar3);
                object tempobj = TagTypes[tempstring];
                if (tempobj == null)
                {
                    TagTypes.Add(tempstring, tempstring);
                }

                TagType[x] = tempstring;

                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 12;
                Ident[x] = BR.ReadInt32();
                identHT.Add(Ident[x], x);
                if (x == 0)
                {
                    lowident = Ident[x];
                }

                if (Ident[x] > highident)
                {
                    highident = Ident[x];
                }

                if (Ident[x] < lowident)
                {
                    lowident = Ident[x];
                }

                stringoffset[x] = BR.ReadInt32() - map.PrimaryMagic;
                Offset[x] = BR.ReadInt32();

                Offset[x] -= map.PrimaryMagic;
            }

            for (int x = 0; x < map.IndexHeader.metaCount; x++)
            {
                int end = map.MapHeader.fileSize;

                for (int xx = 0; xx < map.IndexHeader.metaCount; xx++)
                {
                    if (Offset[xx] < end && Offset[xx] > Offset[x] && external[xx] != true && Offset[xx] > 0)
                    {
                        end = Offset[xx];
                    }
                }

                Size[x] = end - Offset[x];
                if (Size[x] < 0)
                {
                    Size[x] = 0;
                }
            }
        }

        /// <summary>
        /// The load halo 2 object index info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHalo2ObjectIndexInfo(ref BinaryReader BR, Map map)
        {
            MapDebugLogger.Separator("TAG INDEX");
            MapDebugLogger.Info("Reading object entries from offset 0x{0:X} (16-byte entries)", map.IndexHeader.tagsOffset);

            TagTypes = new Hashtable();
            identHT = new Hashtable();
            TagType = new string[map.IndexHeader.metaCount];
            Ident = new int[map.IndexHeader.metaCount];
            Offset = new int[map.IndexHeader.metaCount];
            Size = new int[map.IndexHeader.metaCount];
            string[] temptagtypes = new string[500];
            BR.BaseStream.Position = map.IndexHeader.tagsOffset;

            // Count tags by type for summary
            Dictionary<string, int> tagTypeCounts = new Dictionary<string, int>();

            for (int x = 0; x < map.IndexHeader.metaCount; x++)
            {
                long entryOffset = BR.BaseStream.Position;
                char[] tempchar = BR.ReadChars(4);
                Array.Reverse(tempchar);
                string tempstring = new string(tempchar);
                object tempobj = TagTypes[tempstring];
                if (tempobj == null)
                {
                    TagTypes.Add(tempstring, tempstring);
                }

                TagType[x] = tempstring;

                Ident[x] = BR.ReadInt32();
                if (Ident[x] == -1)
                {
                    MapDebugLogger.Warn("Tag {0}: ID=-1, switching to Halo 2 Vista format", x);
                    map.IndexHeader.metaCount = x;
                    map.HaloVersion = HaloVersionEnum.Halo2Vista;
                    break;
                }
                identHT.Add(Ident[x], x);
                if (x == 0)
                {
                    lowident = Ident[x];
                }

                if (Ident[x] > highident)
                {
                    highident = Ident[x];
                }

                if (Ident[x] < lowident)
                {
                    lowident = Ident[x];
                }

                int rawOffset = BR.ReadInt32();
                Offset[x] = rawOffset - map.SecondaryMagic;
                Size[x] = BR.ReadInt32();

                // Determine source file based on offset
                string sourceFile = DetermineSourceFile(rawOffset);

                // Log first 10 tags and any sbsp/scnr tags in detail
                if (x < 10 || tempstring == "sbsp" || tempstring == "scnr" || tempstring == "matg")
                {
                    MapDebugLogger.LogTag(x, tempstring, Ident[x], Offset[x], Size[x], sourceFile);
                }

                // Count tag types
                if (tagTypeCounts.ContainsKey(tempstring))
                    tagTypeCounts[tempstring]++;
                else
                    tagTypeCounts[tempstring] = 1;
            }

            // Log summary of tag types
            MapDebugLogger.Info("Loaded {0} tags", map.IndexHeader.metaCount);
            MapDebugLogger.Debug("All tag classes in map:");
            System.Text.StringBuilder sb = new System.Text.StringBuilder("  ");
            int count = 0;
            foreach (var kvp in tagTypeCounts)
            {
                if (count > 0) sb.Append(", ");
                sb.AppendFormat("{0}:{1}", kvp.Key, kvp.Value);
                count++;
                if (count % 10 == 0)
                {
                    MapDebugLogger.Debug(sb.ToString());
                    sb.Clear();
                    sb.Append("  ");
                }
            }
            if (sb.Length > 2)
                MapDebugLogger.Debug(sb.ToString());

            // Log ident range
            MapDebugLogger.Debug("Tag ID range: 0x{0:X8} to 0x{1:X8}", lowident, highident);
        }

        /// <summary>
        /// Determine which source file contains data at a given raw offset.
        /// </summary>
        private static string DetermineSourceFile(int rawOffset)
        {
            // Halo 2 uses high bits to indicate external files
            // 0x00xxxxxx - local map file
            // 0x10xxxxxx - mainmenu.map
            // 0x18xxxxxx - shared.map
            // 0x30xxxxxx - sp_shared.map (single_player_shared.map)
            // These are approximate masks based on common Halo 2 implementations

            uint uOffset = (uint)rawOffset;
            uint highNibble = (uOffset >> 28) & 0xF;

            // More precise check based on actual Halo 2 resource location bits
            if ((uOffset & 0xC0000000) == 0)
            {
                return null; // Local file
            }

            // Check for external resource flags
            if ((uOffset & 0x40000000) != 0)
            {
                if ((uOffset & 0x80000000) != 0)
                    return "sp_shared.map";
                else
                    return "mainmenu.map";
            }
            else if ((uOffset & 0x80000000) != 0)
            {
                return "shared.map";
            }

            return null;
        }

        /// <summary>
        /// The load halo ce object index info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHaloCEObjectIndexInfo(ref BinaryReader BR, Map map)
        {
            TagTypes = new Hashtable();
            identHT = new Hashtable();
            TagType = new string[map.IndexHeader.metaCount];
            TagType2 = new string[map.IndexHeader.metaCount];
            TagType3 = new string[map.IndexHeader.metaCount];
            Ident = new int[map.IndexHeader.metaCount];
            Offset = new int[map.IndexHeader.metaCount];
            Size = new int[map.IndexHeader.metaCount];
            stringoffset = new int[map.IndexHeader.metaCount];
            string[] temptagtypes = new string[500];
            external = new bool[map.IndexHeader.metaCount];
            bitmapindex = new int[map.IndexHeader.metaCount];
            for (int x = 0; x < map.IndexHeader.metaCount; x++)
            {
                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32);
                char[] tempchar = BR.ReadChars(4);
                Array.Reverse(tempchar);
                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 4;
                char[] tempchar2 = BR.ReadChars(4);
                Array.Reverse(tempchar2);
                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 8;
                char[] tempchar3 = BR.ReadChars(4);
                Array.Reverse(tempchar3);
                string tempstring = new string(tempchar);
                string tempstring2 = new string(tempchar2);
                string tempstring3 = new string(tempchar3);

                object tempobj = TagTypes[tempstring];
                if (tempobj == null)
                {
                    TagTypes.Add(tempstring, tempstring);
                }

                TagType[x] = tempstring;

                BR.BaseStream.Position = map.IndexHeader.tagsOffset + (x * 32) + 12;
                Ident[x] = BR.ReadInt32();
                identHT.Add(Ident[x], x);
                if (x == 0)
                {
                    lowident = Ident[x];
                }

                if (Ident[x] > highident)
                {
                    highident = Ident[x];
                }

                if (Ident[x] < lowident)
                {
                    lowident = Ident[x];
                }

                stringoffset[x] = BR.ReadInt32() - map.PrimaryMagic;
                Offset[x] = BR.ReadInt32();

                if (TagType[x] == "bitm" && map.BitmapLibary.error == false)
                {
                    if (Offset[x] < map.PrimaryMagic)
                    {
                        int tempindex = Offset[x];
                        bitmapindex[x] = tempindex;
                        Size[x] = map.BitmapLibary.RawSize[tempindex];
                        Offset[x] = map.BitmapLibary.RawOffset[tempindex];
                        external[x] = true;
                    }
                    else
                    {
                        Offset[x] -= map.PrimaryMagic;
                    }
                }
                else if (TagType[x] == "bitm" && map.BitmapLibary.error)
                {
                    if (Offset[x] < map.PrimaryMagic)
                    {
                        Size[x] = 0;
                        Offset[x] = 0;
                    }
                    else
                    {
                        Offset[x] -= map.PrimaryMagic;
                    }
                }
                else
                {
                    Offset[x] -= map.PrimaryMagic;
                }
            }

            for (int x = 0; x < map.IndexHeader.metaCount; x++)
            {
                int end = map.MapHeader.fileSize;
                if (external[x] != true)
                {
                    for (int xx = 0; xx < map.IndexHeader.metaCount; xx++)
                    {
                        if (Offset[xx] < end && Offset[xx] > Offset[x] && external[xx] != true && Offset[xx] > 0)
                        {
                            end = Offset[xx];
                        }
                    }

                    Size[x] = end - Offset[x];
                    if (Size[x] < 0)
                    {
                        Size[x] = 0;
                    }
                }
            }
        }

        #endregion
    }
}