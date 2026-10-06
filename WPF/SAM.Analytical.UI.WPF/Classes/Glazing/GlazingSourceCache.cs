// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI.WPF
{
    /// <summary>
    /// A JSON copy of a converted .tcd (a <see cref="ConstructionManager"/>), so loading the same database a second
    /// time takes a fraction of a second instead of re-reading TCD.exe (about 5 ms per material: a minute for the
    /// International Glazing Database). Keyed by the file's full path, size and last-write time and by the SAM_Tas
    /// assembly (version and build time), so a changed file or a changed importer is never answered from an old copy.
    /// Best effort: any failure to read or write is a cache miss, never an error.
    /// </summary>
    internal static class GlazingSourceCache
    {
        private static string directory;

        /// <summary>The cache folder; <c>%LOCALAPPDATA%\SAM\cache\tcd</c> unless a test sets another.</summary>
        internal static string Directory
        {
            get => directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SAM", "cache", "tcd");
            set => directory = value;
        }

        /// <summary>The cache file for a database, or null when it cannot be keyed (missing file).</summary>
        internal static string PathOf(string path_Database)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path_Database);
                if (!fileInfo.Exists)
                {
                    return null;
                }

                System.Reflection.Assembly assembly = typeof(Tas.Convert).Assembly;
                string build = File.GetLastWriteTimeUtc(assembly.Location).Ticks.ToString();
                string key = string.Join("|", fileInfo.FullName.ToLowerInvariant(), fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks, assembly.GetName().Version, build);

                using (SHA256 sha256 = SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty).Substring(0, 32);
                    return Path.Combine(Directory, Path.GetFileNameWithoutExtension(fileInfo.Name) + "_" + hash + ".json");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static ConstructionManager Read(string path_Database)
        {
            string path = PathOf(path_Database);
            if (path == null || !File.Exists(path))
            {
                return null;
            }

            try
            {
                return new ConstructionManager(JsonNode.Parse(File.ReadAllText(path)).AsObject());
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void Write(string path_Database, ConstructionManager constructionManager)
        {
            string path = PathOf(path_Database);
            if (path == null || constructionManager == null)
            {
                return;
            }

            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
                string path_Temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllText(path_Temp, constructionManager.ToJsonObject().ToJsonString());
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(path_Temp, path);
            }
            catch (Exception)
            {
                // A cache that cannot be written only means the next load reads the database again.
            }
        }
    }
}
