// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BSPContainer.cs" company="">
//   
// </copyright>
// <summary>
//   The bsp container.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Map
{
    using System.Globalization;
    using System.Windows.Forms;

    /// <summary>
    /// The bsp container.
    /// </summary>
    /// <remarks></remarks>
    public class BSPContainer
    {
        #region Constants and Fields

        /// <summary>
        /// The bspcount.
        /// </summary>
        public int bspcount;

        /// <summary>
        /// The sbsp.
        /// </summary>
        public BSPInfo[] sbsp;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BSPContainer"/> class.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public BSPContainer(Map map)
        {
            switch (map.HaloVersion)
            {
                case HaloVersionEnum.Halo2:
                    Halo2BSPContainer(map);
                    break;
                case HaloVersionEnum.Halo2Vista:
                    Halo2BSPContainer(map);
                    break;
                case HaloVersionEnum.Halo1:
                    Halo1Container(map);
                    break;
                case HaloVersionEnum.HaloCE:

                    HaloCEContainer(map);
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The find bsp number by bsp ident.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The find bsp number by bsp ident.</returns>
        /// <remarks></remarks>
        public int FindBSPNumberByBSPIdent(int id)
        {
            for (int x = 0; x < sbsp.Length; x++)
            {
                if (sbsp[x].ident == id)
                {
                    return x;
                }
            }

            return -1;
        }

        /// <summary>
        /// The find bsp number by light map ident.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The find bsp number by light map ident.</returns>
        /// <remarks></remarks>
        public int FindBSPNumberByLightMapIdent(int id)
        {
            for (int x = 0; x < sbsp.Length; x++)
            {
                if (sbsp[x].lightmapident == id)
                {
                    return x;
                }
            }

            return -1;
        }

        /// <summary>
        /// The halo 1 container.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void Halo1Container(Map map)
        {
            map.OpenMap(MapTypes.Internal);
            map.BR.BaseStream.Position = map.MetaInfo.Offset[0] + 1444;
            bspcount = map.BR.ReadInt32();
            int tempr = map.BR.ReadInt32() - map.PrimaryMagic;
            sbsp = new BSPInfo[bspcount];
            for (int x = 0; x < bspcount; x++)
            {
                sbsp[x] = new BSPInfo();
                sbsp[x].pointerOffset = tempr + (x * 32) - map.MetaInfo.Offset[0];
                map.BR.BaseStream.Position = tempr + (x * 32);
                sbsp[x].offset = map.BR.ReadInt32();
                sbsp[x].size = map.BR.ReadInt32();
                sbsp[x].magic = map.BR.ReadInt32() - sbsp[x].offset;
                map.BR.BaseStream.Position = tempr + (x * 32) + 28;
                sbsp[x].ident = map.BR.ReadInt32();
                sbsp[x].TagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].ident);
                map.MetaInfo.Offset[sbsp[x].TagIndex] = sbsp[x].offset;
                map.MetaInfo.Size[sbsp[x].TagIndex] = sbsp[x].size;

                map.BR.BaseStream.Position = sbsp[x].offset + 4;
                sbsp[x].Halo1VerticeCount = map.BR.ReadInt32();

                map.BR.BaseStream.Position = sbsp[x].offset + 24 + (sbsp[x].Halo1VerticeCount * 12) + 4;
                int temprr = map.BR.ReadInt32() - sbsp[x].magic;

                sbsp[x].lightmapoffset = temprr - sbsp[x].offset;
                map.BR.BaseStream.Position = sbsp[x].offset + sbsp[x].lightmapoffset + 12;

                sbsp[x].lightmapident = map.BR.ReadInt32();
                sbsp[x].lightmapTagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].lightmapident);
            }

            map.CloseMap();
        }

        /// <summary>
        /// The halo 2 bsp container.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void Halo2BSPContainer(Map map)
        {
            MapDebugLogger.Separator("BSP CONTAINER");
            MapDebugLogger.Info("Loading BSP container from scenario tag...");

            map.OpenMap(MapTypes.Internal);
            long bspCountOffset = map.MetaInfo.Offset[3] + 528;
            map.BR.BaseStream.Position = bspCountOffset;
            bspcount = map.BR.ReadInt32();
            int rawBspArrayPtr = map.BR.ReadInt32();
            int tempr = rawBspArrayPtr - map.SecondaryMagic;

            MapDebugLogger.LogOffset("BSP count", bspCountOffset, bspcount);
            MapDebugLogger.Debug("BSP array pointer: raw=0x{0:X8}, file=0x{1:X8}", rawBspArrayPtr, tempr);

            sbsp = new BSPInfo[bspcount];

            if (bspcount == 0)
            {
                MapDebugLogger.Warn("No BSP structures found in scenario!");
            }

            for (int x = 0; x < bspcount; x++)
            {
                sbsp[x] = new BSPInfo();
                sbsp[x].pointerOffset = tempr + (x * 68) - map.MetaInfo.Offset[3];

                long entryOffset = tempr + (x * 68);
                map.BR.BaseStream.Position = entryOffset;
                int rawOffset = map.BR.ReadInt32();
                sbsp[x].offset = rawOffset;
                map.Functions.ParsePointer(ref sbsp[x].offset, ref sbsp[x].location);
                sbsp[x].size = map.BR.ReadInt32();
                sbsp[x].magic = map.BR.ReadInt32() - sbsp[x].offset;

                map.BR.BaseStream.Position = tempr + (x * 68) + 20;
                sbsp[x].ident = map.BR.ReadInt32();

                // Determine source file for BSP data
                string sourceFile = GetBspSourceFile(rawOffset, sbsp[x].location);

                MapDebugLogger.Info("BSP {0}:", x);
                MapDebugLogger.Debug("  Entry at file offset: 0x{0:X8}", entryOffset);
                MapDebugLogger.Debug("  Raw offset: 0x{0:X8}, File offset: 0x{1:X8}", rawOffset, sbsp[x].offset);
                MapDebugLogger.Debug("  Size: {0} bytes (0x{0:X})", sbsp[x].size);
                MapDebugLogger.Debug("  Magic: 0x{0:X8}", sbsp[x].magic);
                MapDebugLogger.Debug("  Ident: 0x{0:X8}", sbsp[x].ident);
                MapDebugLogger.Debug("  Source: {0}", sourceFile ?? "local map");

                sbsp[x].TagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].ident);
                if (sbsp[x].TagIndex >= 0)
                {
                    map.MetaInfo.Offset[sbsp[x].TagIndex] = sbsp[x].offset;
                    map.MetaInfo.Size[sbsp[x].TagIndex] = sbsp[x].size;
                    MapDebugLogger.Debug("  Tag index: {0}", sbsp[x].TagIndex);
                }
                else
                {
                    MapDebugLogger.Warn("  Could not find tag by ident!");
                }

                map.BR.BaseStream.Position = tempr + (x * 68) + 28;
                sbsp[x].lightmapident = map.BR.ReadInt32();
                sbsp[x].lightmapTagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].lightmapident);

                if (sbsp[x].lightmapTagIndex == -1)
                {
                    MapDebugLogger.Debug("  No lightmap for this BSP");
                    continue;
                }

                map.BR.BaseStream.Position = sbsp[x].offset + 8;
                sbsp[x].lightmapoffset = map.BR.ReadInt32();
                if (sbsp[x].lightmapoffset == 0)
                {
                    sbsp[x].lightmapident = -1;
                    sbsp[x].lightmapTagIndex = -1;
                    MapDebugLogger.Warn("  Lightmap offset is 0 - broken lightmap reference");
                    if (
                        MessageBox.Show(
                            "There is no lightmap for this bsp and the scenario is currently linked to a broken ID.\n Would you like Entity to fix it?",
                            "Error",
                            MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        map.BW.BaseStream.Position = tempr + (x * 68) + 28;
                        map.BW.Write(int.Parse("FFFFFFFF", NumberStyles.HexNumber));
                    }

                    continue;
                }

                sbsp[x].lightmapoffset += -sbsp[x].magic;
                sbsp[x].lightmapsize = sbsp[x].size + sbsp[x].offset - sbsp[x].lightmapoffset;

                MapDebugLogger.Debug("  Lightmap: offset=0x{0:X8}, size={1}, ident=0x{2:X8}",
                    sbsp[x].lightmapoffset, sbsp[x].lightmapsize, sbsp[x].lightmapident);

                map.MetaInfo.Offset[sbsp[x].lightmapTagIndex] = sbsp[x].lightmapoffset;
                map.MetaInfo.Size[sbsp[x].lightmapTagIndex] = sbsp[x].lightmapsize;

                // light map bitmap
                map.BR.BaseStream.Position = sbsp[x].lightmapoffset + 128;
                int tempc = map.BR.ReadInt32();
                int temprx = map.BR.ReadInt32() - sbsp[x].magic;
                map.BR.BaseStream.Position = temprx + 28;
                sbsp[x].LightMap_TagNumber = map.Functions.ForMeta.FindMetaByID(map.BR.ReadInt32());

                if (sbsp[x].LightMap_TagNumber >= 0)
                {
                    MapDebugLogger.Debug("  Lightmap bitmap tag index: {0}", sbsp[x].LightMap_TagNumber);
                }

                ///light map palettes
                map.BR.BaseStream.Position = temprx + 8;
                int tempc2 = map.BR.ReadInt32();
                int tempr2 = map.BR.ReadInt32() - sbsp[x].magic;
                sbsp[x].palettesoffset = tempr2;

                if (tempc2 > 0)
                {
                    MapDebugLogger.Debug("  Loading {0} lightmap palettes from offset 0x{1:X8}", tempc2, tempr2);
                }

                for (int y = 0; y < tempc2; y++)
                {
                    map.BR.BaseStream.Position = tempr2 + (y * 1024);
                    Palette_Color[] pc = new Palette_Color[256];
                    for (int z = 0; z < 256; z++)
                    {
                        pc[z] = new Palette_Color();
                        pc[z].r = map.BR.ReadByte();
                        pc[z].g = map.BR.ReadByte();
                        pc[z].b = map.BR.ReadByte();
                        pc[z].a = map.BR.ReadByte();
                    }

                    sbsp[x].LightMap_Palettes.Add(pc);
                }

                map.BR.BaseStream.Position = temprx + 40;
                tempc2 = map.BR.ReadInt32();
                tempr2 = map.BR.ReadInt32() - sbsp[x].magic;
                sbsp[x].VisualChunk_Bitmap_Index = new int[tempc2];
                sbsp[x].VisualChunk_LightMap_Index = new int[tempc2];

                if (tempc2 != 0)
                {
                    MapDebugLogger.Debug("  Visual chunks: {0} entries at offset 0x{1:X8}", tempc2, tempr2);
                    map.BR.BaseStream.Position = tempr2;
                    for (int y = 0; y < tempc2; y++)
                    {
                        sbsp[x].VisualChunk_Bitmap_Index[y] = map.BR.ReadInt16();
                        sbsp[x].VisualChunk_LightMap_Index[y] = map.BR.ReadInt16();
                    }
                }

                map.BR.BaseStream.Position = temprx + 72;
                tempc2 = map.BR.ReadInt32();
                tempr2 = map.BR.ReadInt32() - sbsp[x].magic;
                sbsp[x].SceneryChunk_Bitmap_Index = new int[tempc2];
                sbsp[x].SceneryChunk_LightMap_Index = new int[tempc2];

                if (tempc2 > 0)
                {
                    MapDebugLogger.Debug("  Scenery chunks: {0} entries at offset 0x{1:X8}", tempc2, tempr2);
                    map.BR.BaseStream.Position = tempr2;
                    for (int y = 0; y < tempc2; y++)
                    {
                        sbsp[x].SceneryChunk_Bitmap_Index[y] = map.BR.ReadInt16();
                        sbsp[x].SceneryChunk_LightMap_Index[y] = map.BR.ReadInt16();
                    }
                }
            }

            map.CloseMap();
            MapDebugLogger.Info("BSP container loaded: {0} BSP structure(s)", bspcount);
        }

        /// <summary>
        /// Determine which source file contains BSP data.
        /// </summary>
        private string GetBspSourceFile(int rawOffset, MapTypes location)
        {
            // Location value from ParsePointer tells us the source
            switch (location)
            {
                case MapTypes.Internal: return null; // Local
                case MapTypes.MainMenu: return "mainmenu.map";
                case MapTypes.Shared: return "shared.map";
                case MapTypes.SPShared: return "sp_shared.map";
                default: return null;
            }
        }

        /// <summary>
        /// The halo ce container.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void HaloCEContainer(Map map)
        {
            map.OpenMap(MapTypes.Internal);
            map.BR.BaseStream.Position = map.MetaInfo.Offset[0] + 1444;
            bspcount = map.BR.ReadInt32();
            int tempr = map.BR.ReadInt32() - map.PrimaryMagic;
            sbsp = new BSPInfo[bspcount];
            for (int x = 0; x < bspcount; x++)
            {
                sbsp[x] = new BSPInfo();
                sbsp[x].pointerOffset = tempr + (x * 32) - map.MetaInfo.Offset[0];
                map.BR.BaseStream.Position = tempr + (x * 32);
                sbsp[x].offset = map.BR.ReadInt32();
                sbsp[x].size = map.BR.ReadInt32();
                sbsp[x].magic = map.BR.ReadInt32() - sbsp[x].offset;
                map.BR.BaseStream.Position = tempr + (x * 32) + 28;
                sbsp[x].ident = map.BR.ReadInt32();
                sbsp[x].TagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].ident);
                map.MetaInfo.Offset[sbsp[x].TagIndex] = sbsp[x].offset;
                map.MetaInfo.Size[sbsp[x].TagIndex] = sbsp[x].size;
                map.BR.BaseStream.Position = sbsp[x].offset + 4;
                sbsp[x].Halo1VerticeCount = map.BR.ReadInt32();

                sbsp[x].lightmapoffset = 24;
                map.BR.BaseStream.Position = sbsp[x].lightmapoffset + 12;

                sbsp[x].lightmapident = map.BR.ReadInt32();
                sbsp[x].lightmapTagIndex = map.Functions.ForMeta.FindMetaByID(sbsp[x].ident);
            }

            map.CloseMap();
        }

        #endregion

        /// <summary>
        /// The palette_ color.
        /// </summary>
        /// <remarks></remarks>
        public class Palette_Color
        {
            #region Constants and Fields

            /// <summary>
            /// The a.
            /// </summary>
            public int a;

            /// <summary>
            /// The b.
            /// </summary>
            public int b;

            /// <summary>
            /// The g.
            /// </summary>
            public int g;

            /// <summary>
            /// The r.
            /// </summary>
            public int r;

            #endregion
        }
    }
}