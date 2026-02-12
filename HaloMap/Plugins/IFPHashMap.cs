// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFPHashMap.cs" company="">
//   
// </copyright>
// <summary>
//   The ifp hash map.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HaloMap.Plugins
{
    using System;
    using System.Collections;
    using System.IO;

    using Globals;

    using HaloMap.Map;

    /// <summary>
    /// The ifp hash map.
    /// </summary>
    /// <remarks></remarks>
    public sealed class IFPHashMap
    {
        #region Constants and Fields

        /// <summary>
        /// The h 1 ifp hash.
        /// </summary>
        public static Hashtable H1IFPHash = new Hashtable();

        /// <summary>
        /// The h 2 ifp hash.
        /// </summary>
        public static Hashtable H2IFPHash = new Hashtable();

        /// <summary>
        /// Cached resolved path to Halo 2 plugins folder.
        /// </summary>
        private static string resolvedH2PluginsFolder = null;

        /// <summary>
        /// Cached resolved path to Halo 1 plugins folder.
        /// </summary>
        private static string resolvedH1PluginsFolder = null;

        #endregion

        #region Private Methods

        /// <summary>
        /// Searches up from the exe directory to find a Plugins subfolder.
        /// </summary>
        private static string FindPluginsFolder(string subPath)
        {
            string searchDir = Global.StartupPath;
            for (int i = 0; i < 8; i++)
            {
                string candidate = Path.Combine(searchDir, subPath);
                if (Directory.Exists(candidate))
                    return candidate;
                string parent = Directory.GetParent(searchDir) != null
                    ? Directory.GetParent(searchDir).FullName
                    : null;
                if (parent == null || parent == searchDir) break;
                searchDir = parent;
            }
            return null;
        }

        /// <summary>
        /// Gets the resolved Halo 2 plugins folder, searching if needed.
        /// </summary>
        private static string GetH2PluginsFolder()
        {
            if (resolvedH2PluginsFolder != null)
                return resolvedH2PluginsFolder;

            // Try the configured path first
            if (Directory.Exists(Prefs.pathPluginsFolder))
            {
                resolvedH2PluginsFolder = Prefs.pathPluginsFolder;
                return resolvedH2PluginsFolder;
            }

            // Search up from exe directory
            string found = FindPluginsFolder(Path.Combine("Plugins", "Halo 2", "ent"));
            if (found != null)
            {
                resolvedH2PluginsFolder = found;
                return resolvedH2PluginsFolder;
            }

            // Also check inside an "Entity" subfolder (repo structure)
            found = FindPluginsFolder(Path.Combine("Entity", "Plugins", "Halo 2", "ent"));
            if (found != null)
            {
                resolvedH2PluginsFolder = found;
                return resolvedH2PluginsFolder;
            }

            // Fall back to configured path
            resolvedH2PluginsFolder = Prefs.pathPluginsFolder;
            return resolvedH2PluginsFolder;
        }

        /// <summary>
        /// Gets the resolved Halo 1 plugins folder, searching if needed.
        /// </summary>
        private static string GetH1PluginsFolder()
        {
            if (resolvedH1PluginsFolder != null)
                return resolvedH1PluginsFolder;

            string defaultPath = Path.Combine(Global.StartupPath, "Plugins", "Halo 1", "ent");
            if (Directory.Exists(defaultPath))
            {
                resolvedH1PluginsFolder = defaultPath;
                return resolvedH1PluginsFolder;
            }

            string found = FindPluginsFolder(Path.Combine("Plugins", "Halo 1", "ent"));
            if (found != null)
            {
                resolvedH1PluginsFolder = found;
                return resolvedH1PluginsFolder;
            }

            found = FindPluginsFolder(Path.Combine("Entity", "Plugins", "Halo 1", "ent"));
            if (found != null)
            {
                resolvedH1PluginsFolder = found;
                return resolvedH1PluginsFolder;
            }

            resolvedH1PluginsFolder = defaultPath;
            return resolvedH1PluginsFolder;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// The get ifp.
        /// </summary>
        /// <param name="TagType">The tag type.</param>
        /// <param name="map">The map.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IFPIO GetIfp(string TagType, HaloVersionEnum HaloVersion)
        {
            IFPIO tempifp;
            if (HaloVersion == HaloVersionEnum.Halo2 ||
                HaloVersion == HaloVersionEnum.Halo2Vista)
            {
                tempifp = (IFPIO)H2IFPHash[TagType];
                if (tempifp == null)
                {
                    tempifp = new IFPIO();

                    string pluginsDir = GetH2PluginsFolder();
                    string fileName = TagType.Trim() + ".ent";
                    fileName = fileName.Replace("<", "_");
                    fileName = fileName.Replace(">", "_");
                    string temps = Path.Combine(pluginsDir, fileName);
                    if (!File.Exists(temps))
                    {
                        Global.ShowErrorMsg("Plugin file not found: " + temps, new FileNotFoundException(temps));
                        return tempifp;
                    }

                    try
                    {
                        tempifp.ReadIFP(temps);
                    }
                    catch (Exception e)
                    {
                        Global.ShowErrorMsg("Error Reading Ent: " + TagType, e);
                    }

                    H2IFPHash.Add(TagType, tempifp);
                }
            }
            else
            {
                // Halo 1 or Halo CE
                tempifp = (IFPIO)H1IFPHash[TagType];
                if (tempifp == null)
                {
                    tempifp = new IFPIO();

                    string pluginsDir = GetH1PluginsFolder();
                    string fileName = TagType.Trim() + ".ent";
                    fileName = fileName.Replace("<", "_");
                    fileName = fileName.Replace(">", "_");
                    string temps = Path.Combine(pluginsDir, fileName);
                    try
                    {
                        tempifp.ReadIFP(temps);
                    }
                    catch (Exception ex)
                    {
                        Global.ShowErrorMsg("Error Reading Ent: " + TagType, ex);
                    }

                    H1IFPHash.Add(TagType, tempifp);
                }
            }

            return tempifp;
        }

        /// <summary>
        /// The remove ifp.
        /// </summary>
        /// <param name="TagType">The tag type.</param>
        /// <param name="map">The map.</param>
        /// <remarks></remarks>
        public static void RemoveIfp(string TagType, Map map)
        {
            if (map.HaloVersion == HaloVersionEnum.Halo2)
            {
                try
                {
                    H2IFPHash.Remove(TagType);
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    H1IFPHash.Remove(TagType);
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}