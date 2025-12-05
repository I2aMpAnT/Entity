// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MapHeaderInfo.cs" company="">
//   
// </copyright>
// <summary>
//   The map header info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Map
{
    using System.IO;

    /// <summary>
    /// The map header info.
    /// </summary>
    /// <remarks></remarks>
    public class MapHeaderInfo
    {
        #region Constants and Fields

        /// <summary>
        /// The combined size.
        /// </summary>
        public int combinedSize;

        /// <summary>
        /// The file count.
        /// </summary>
        public int fileCount;

        /// <summary>
        /// The file names size.
        /// </summary>
        public int fileNamesSize;

        /// <summary>
        /// The file size.
        /// </summary>
        public int fileSize;

        /// <summary>
        /// The index offset.
        /// </summary>
        public int indexOffset;

        /// <summary>
        /// The map name.
        /// </summary>
        public string mapName;

        /// <summary>
        /// The map type.
        /// </summary>
        public MapTypes mapType;

        /// <summary>
        /// The meta size.
        /// </summary>
        public int metaSize;

        /// <summary>
        /// The meta start.
        /// </summary>
        public int metaStart;

        /// <summary>
        /// The offset to crazy.
        /// </summary>
        public int offsetToCrazy;

        /// <summary>
        /// The offset to string index.
        /// </summary>
        public int offsetToStringIndex;

        /// <summary>
        /// The offset to string names 1.
        /// </summary>
        public int offsetToStringNames1;

        /// <summary>
        /// The offset to string names 2.
        /// </summary>
        public int offsetToStringNames2;

        /// <summary>
        /// The offset tofile index.
        /// </summary>
        public int offsetTofileIndex;

        /// <summary>
        /// The offset tofile names.
        /// </summary>
        public int offsetTofileNames;

        /// <summary>
        /// The scenario path.
        /// </summary>
        public string scenarioPath;

        /// <summary>
        /// The script reference count.
        /// </summary>
        public int scriptReferenceCount;

        /// <summary>
        /// The signature.
        /// </summary>
        public int signature;

        /// <summary>
        /// The size of crazy.
        /// </summary>
        public int sizeOfCrazy;

        /// <summary>
        /// The size of script reference.
        /// </summary>
        public int sizeOfScriptReference;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MapHeaderInfo"/> class.
        /// </summary>
        /// <param name="BR">The BR.</param>
        /// <param name="haloversion">The haloversion.</param>
        /// <remarks></remarks>
        public MapHeaderInfo(ref BinaryReader BR, HaloVersionEnum haloversion)
        {
            switch (haloversion)
            {
                case HaloVersionEnum.Halo2:
                    LoadHalo2MapHeaderInfo(ref BR);
                    break;
                case HaloVersionEnum.Halo2Vista:
                    LoadHaloCEMapHeaderInfo(ref BR);
                    break;
                case HaloVersionEnum.HaloCE:
                case HaloVersionEnum.Halo1:
                    LoadHalo2MapHeaderInfo(ref BR);
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The load halo 2 map header info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <remarks></remarks>
        public void LoadHalo2MapHeaderInfo(ref BinaryReader BR)
        {
            MapDebugLogger.Info("Parsing Halo 2 map header...");

            // Read signature first
            BR.BaseStream.Position = 0;
            int sig = BR.ReadInt32();
            MapDebugLogger.LogOffset("Header signature ('head')", 0, sig, true);

            // map stuff
            BR.BaseStream.Position = 8;
            fileSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("File size", 8, fileSize);

            BR.BaseStream.Position = 16;
            indexOffset = BR.ReadInt32();
            MapDebugLogger.LogOffset("Index offset", 16, indexOffset);

            metaStart = BR.ReadInt32();
            MapDebugLogger.LogOffset("Meta start", 20, metaStart);

            metaSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("Meta size", 24, metaSize);

            combinedSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("Combined size", 28, combinedSize);

            BR.BaseStream.Position = 340;
            sizeOfCrazy = BR.ReadInt32();
            MapDebugLogger.LogOffset("Crazy data size", 340, sizeOfCrazy);

            offsetToCrazy = BR.ReadInt32();
            MapDebugLogger.LogOffset("Crazy data offset", 344, offsetToCrazy);

            // string stuff
            BR.BaseStream.Position = 352;
            offsetToStringNames1 = BR.ReadInt32();
            MapDebugLogger.LogOffset("String names offset 1", 352, offsetToStringNames1);

            scriptReferenceCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Script reference count", 356, scriptReferenceCount);

            sizeOfScriptReference = BR.ReadInt32();
            MapDebugLogger.LogOffset("Script reference size", 360, sizeOfScriptReference);

            offsetToStringIndex = BR.ReadInt32();
            MapDebugLogger.LogOffset("String index offset", 364, offsetToStringIndex);

            offsetToStringNames2 = BR.ReadInt32();
            MapDebugLogger.LogOffset("String names offset 2", 368, offsetToStringNames2);

            // map names and code to check if it is an external map
            BR.BaseStream.Position = 408;
            mapName = new string(BR.ReadChars(36));
            MapDebugLogger.Debug("Map name at offset 0x198: '{0}'", mapName.TrimEnd('\0'));

            BR.BaseStream.Position = 444;
            scenarioPath = new string(BR.ReadChars(64));
            MapDebugLogger.Debug("Scenario path at offset 0x1BC: '{0}'", scenarioPath.TrimEnd('\0'));

            mapType = MapTypes.Internal;
            if (scenarioPath.IndexOf("scenarios\\ui\\mainmenu\\mainmenu") != -1)
            {
                mapType = MapTypes.MainMenu;
                MapDebugLogger.Info("Map type: MainMenu");
            }
            else if (scenarioPath.IndexOf("scenarios\\shared\\shared") != -1)
            {
                mapType = MapTypes.MPShared;
                MapDebugLogger.Info("Map type: MPShared");
            }
            else if (scenarioPath.IndexOf("scenarios\\shared\\single_player_shared") != -1)
            {
                mapType = MapTypes.SPShared;
                MapDebugLogger.Info("Map type: SPShared");
            }
            else
            {
                MapDebugLogger.Info("Map type: Internal (playable map)");
            }

            // read in stuff about meta names
            BR.BaseStream.Position = 704;
            fileCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Tag file count", 704, fileCount);

            offsetTofileNames = BR.ReadInt32();
            MapDebugLogger.LogOffset("File names offset", 708, offsetTofileNames);

            fileNamesSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("File names size", 712, fileNamesSize);

            offsetTofileIndex = BR.ReadInt32();
            MapDebugLogger.LogOffset("File index offset", 716, offsetTofileIndex);

            // signature
            signature = BR.ReadInt32();
            MapDebugLogger.LogOffset("Map signature", 720, signature, true);

            MapDebugLogger.Info("Header parsed - Map: {0}, Tags: {1}, IndexOffset: 0x{2:X}",
                mapName.TrimEnd('\0'), fileCount, indexOffset);
        }

        /// <summary>
        /// Halo 2 Vista map header info.
        /// </summary>
        /// <param name="BR">The br.</param>
        /// <remarks></remarks>
        public void LoadHaloCEMapHeaderInfo(ref BinaryReader BR)
        {
            MapDebugLogger.Info("Parsing Halo 2 Vista/CE map header...");

            // Read signature first
            BR.BaseStream.Position = 0;
            int sig = BR.ReadInt32();
            MapDebugLogger.LogOffset("Header signature ('head')", 0, sig, true);

            BR.BaseStream.Position = 8;
            fileSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("File size", 8, fileSize);

            BR.BaseStream.Position = 16;
            indexOffset = BR.ReadInt32();
            MapDebugLogger.LogOffset("Index offset", 16, indexOffset);

            metaStart = BR.ReadInt32();
            MapDebugLogger.LogOffset("Meta start", 20, metaStart);

            metaSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("Meta size", 24, metaSize);

            combinedSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("Combined size", 28, combinedSize);

            BR.BaseStream.Position = 340;
            sizeOfCrazy = BR.ReadInt32();
            MapDebugLogger.LogOffset("Crazy data size", 340, sizeOfCrazy);

            offsetToCrazy = BR.ReadInt32();
            MapDebugLogger.LogOffset("Crazy data offset", 344, offsetToCrazy);

            // string stuff
            BR.BaseStream.Position = 364;
            offsetToStringNames1 = BR.ReadInt32();
            MapDebugLogger.LogOffset("String names offset 1", 364, offsetToStringNames1);

            scriptReferenceCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Script reference count", 368, scriptReferenceCount);

            sizeOfScriptReference = BR.ReadInt32();
            MapDebugLogger.LogOffset("Script reference size", 372, sizeOfScriptReference);

            offsetToStringIndex = BR.ReadInt32();
            MapDebugLogger.LogOffset("String index offset", 376, offsetToStringIndex);

            offsetToStringNames2 = BR.ReadInt32();
            MapDebugLogger.LogOffset("String names offset 2", 380, offsetToStringNames2);

            // map names and code to check if it is an external map
            BR.BaseStream.Position = 420;
            mapName = new string(BR.ReadChars(36));
            MapDebugLogger.Debug("Map name at offset 0x1A4: '{0}'", mapName.TrimEnd('\0'));

            BR.BaseStream.Position = 456;
            scenarioPath = new string(BR.ReadChars(80));
            MapDebugLogger.Debug("Scenario path at offset 0x1C8: '{0}'", scenarioPath.TrimEnd('\0'));

            mapType = MapTypes.Internal;
            if (scenarioPath.IndexOf("scenarios\\ui\\mainmenu\\mainmenu") != -1)
            {
                mapType = MapTypes.MainMenu;
                MapDebugLogger.Info("Map type: MainMenu");
            }
            else if (scenarioPath.IndexOf("scenarios\\shared\\shared") != -1)
            {
                mapType = MapTypes.MPShared;
                MapDebugLogger.Info("Map type: MPShared");
            }
            else if (scenarioPath.IndexOf("scenarios\\shared\\single_player_shared") != -1)
            {
                mapType = MapTypes.SPShared;
                MapDebugLogger.Info("Map type: SPShared");
            }
            else
            {
                MapDebugLogger.Info("Map type: Internal (playable map)");
            }

            // read in stuff about meta names
            BR.BaseStream.Position = 716;
            fileCount = BR.ReadInt32();
            MapDebugLogger.LogOffset("Tag file count", 716, fileCount);

            offsetTofileNames = BR.ReadInt32();
            MapDebugLogger.LogOffset("File names offset", 720, offsetTofileNames);

            fileNamesSize = BR.ReadInt32();
            MapDebugLogger.LogOffset("File names size", 724, fileNamesSize);

            offsetTofileIndex = BR.ReadInt32();
            MapDebugLogger.LogOffset("File index offset", 728, offsetTofileIndex);

            // signature
            BR.BaseStream.Position = 752;
            signature = BR.ReadInt32();
            MapDebugLogger.LogOffset("Map signature", 752, signature, true);

            MapDebugLogger.Info("Header parsed - Map: {0}, Tags: {1}, IndexOffset: 0x{2:X}",
                mapName.TrimEnd('\0'), fileCount, indexOffset);
        }
        #endregion
    }
}