// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IndexHeaderInfo.cs" company="">
//   
// </copyright>
// <summary>
//   The index header info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Map
{
    using System.IO;

    /// <summary>
    /// The index header info.
    /// </summary>
    /// <remarks></remarks>
    public class IndexHeaderInfo
    {
        #region Constants and Fields

        /// <summary>
        /// The indices object count.
        /// </summary>
        public int IndicesObjectCount;

        /// <summary>
        /// The model indices offset.
        /// </summary>
        public int ModelIndicesOffset;

        /// <summary>
        /// The model raw data offset.
        /// </summary>
        public int ModelRawDataOffset;

        /// <summary>
        /// The model raw data size.
        /// </summary>
        public int ModelRawDataSize;

        /// <summary>
        /// The vertice object count.
        /// </summary>
        public int VerticeObjectCount;

        /// <summary>
        /// The constant.
        /// </summary>
        public int constant;

        /// <summary>
        /// ID number of the globals tag [MATG]
        /// </summary>
        public int matgID;

        /// <summary>
        /// The meta count.
        /// </summary>
        public int metaCount;

        /// <summary>
        /// ID Number of the Scenario tag [SCNR]
        /// </summary>
        public int scnrID;

        /// <summary>
        /// The tag type count.
        /// </summary>
        public int tagTypeCount;

        /// <summary>
        /// The tags offset.
        /// </summary>
        public int tagsOffset;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexHeaderInfo"/> class.
        /// </summary>
        /// <param name="BR">The BR.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public IndexHeaderInfo(ref BinaryReader BR, Map map)
        {
            switch (map.HaloVersion)
            {
                case HaloVersionEnum.Halo2:
                    LoadHalo2IndexHeaderInfo(ref BR, map);
                    break;
                case HaloVersionEnum.Halo2Vista:
                    LoadHalo2IndexHeaderInfo(ref BR, map);
                    break;
                case HaloVersionEnum.HaloCE:
                    LoadHaloCEIndexHeaderInfo(ref BR, map);
                    break;
                case HaloVersionEnum.Halo1:
                    LoadHalo1IndexHeaderInfo(ref BR, map);
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The load halo 1 index header info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHalo1IndexHeaderInfo(ref BinaryReader BR, Map map)
        {
            BR.BaseStream.Position = map.MapHeader.indexOffset;
            constant = BR.ReadInt32();
            map.PrimaryMagic = constant - (map.MapHeader.indexOffset + 36);

            BR.ReadInt32();
            BR.ReadInt32();
            metaCount = BR.ReadInt32();
            VerticeObjectCount = BR.ReadInt32();
            ModelRawDataOffset = BR.ReadInt32() - map.PrimaryMagic;
            IndicesObjectCount = BR.ReadInt32();
            ModelIndicesOffset = BR.ReadInt32() - map.PrimaryMagic;
            tagsOffset = map.MapHeader.indexOffset + 36;
        }

        /// <summary>
        /// The load halo 2 index header info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHalo2IndexHeaderInfo(ref BinaryReader BR, Map map)
        {
            MapDebugLogger.Separator("INDEX HEADER");
            MapDebugLogger.Info("Reading tag index at offset 0x{0:X}", map.MapHeader.indexOffset);

            BR.BaseStream.Position = map.MapHeader.indexOffset;
            constant = BR.ReadInt32();
            MapDebugLogger.LogOffset("Index constant (base address)", map.MapHeader.indexOffset, constant, true);

            map.PrimaryMagic = constant - (map.MapHeader.indexOffset + 32);
            MapDebugLogger.Debug("Primary magic calculated: 0x{0:X} (constant - indexOffset - 32)", map.PrimaryMagic);

            tagTypeCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Tag type count", map.MapHeader.indexOffset + 4, tagTypeCount);

            int rawTagsOffset = BR.ReadInt32();
            tagsOffset = rawTagsOffset - map.PrimaryMagic;
            MapDebugLogger.Debug("Tags offset: raw=0x{0:X}, calculated=0x{1:X} (file offset)", rawTagsOffset, tagsOffset);

            scnrID = BR.ReadInt32();
            MapDebugLogger.LogOffset("Scenario tag ID (scnr)", map.MapHeader.indexOffset + 12, scnrID, true);

            matgID = BR.ReadInt32();
            MapDebugLogger.LogOffset("Globals tag ID (matg)", map.MapHeader.indexOffset + 16, matgID, true);

            BR.BaseStream.Position = map.MapHeader.indexOffset + 24;
            metaCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Meta/tag count", map.MapHeader.indexOffset + 24, metaCount);

            // Find lowest offset tag to calculate secondary magic
            MapDebugLogger.Debug("Scanning {0} tags to find lowest offset for secondary magic...", metaCount);
            int metaNum = 0;
            int metaOfs = int.MaxValue;
            for (int i = 0; i < metaCount; i++)
            {
                BR.BaseStream.Position = tagsOffset + i * 16 + 8;
                int ofs = BR.ReadInt32();
                if (ofs < metaOfs)
                {
                    metaOfs = ofs;
                    metaNum = i;
                }
            }

            map.SecondaryMagic = metaOfs - (map.MapHeader.indexOffset + map.MapHeader.metaStart);
            MapDebugLogger.Debug("Lowest tag offset: 0x{0:X} at tag index {1}", metaOfs, metaNum);
            MapDebugLogger.Debug("Secondary magic calculated: 0x{0:X}", map.SecondaryMagic);

            MapDebugLogger.Info("Index header parsed - {0} tags, {1} tag types, tags at 0x{2:X}",
                metaCount, tagTypeCount, tagsOffset);
        }

        /// <summary>
        /// The load halo ce index header info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public void LoadHaloCEIndexHeaderInfo(ref BinaryReader BR, Map map)
        {
            BR.BaseStream.Position = map.MapHeader.indexOffset;
            constant = BR.ReadInt32();
            map.PrimaryMagic = constant - (map.MapHeader.indexOffset + 40);

            BR.ReadInt32();
            BR.ReadInt32();
            metaCount = BR.ReadInt32();
            VerticeObjectCount = BR.ReadInt32();
            ModelRawDataOffset = BR.ReadInt32();
            IndicesObjectCount = BR.ReadInt32();
            ModelIndicesOffset = BR.ReadInt32();
            ModelRawDataSize = BR.ReadInt32();
            tagsOffset = map.MapHeader.indexOffset + 40;
        }

        #endregion
    }
}