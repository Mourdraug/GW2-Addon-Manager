using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using GW2_Addon_Manager.App.Configuration;

namespace GW2_Addon_Manager
{
    public class ApprovedList
    {
        private static ApprovedList _instance;
        public static ApprovedList Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ApprovedList();
                }
                return _instance;
            }
        }
        private const string CacheLocation = "addon_cache.json";
        private const string TempFolder = "temp";

        //Approved-addons repository
        private const string RepoUrl = "https://api.github.com/repositories/206052865";

        private AddonCache addonCache = new();

        public List<AddonInfo> Addons { get => addonCache.AddonInfo; }

        private ApprovedList() { }

        public void UpdateList()
        {
            try
            {
                using (var client = UpdateHelpers.GetClient())
                {
                    string raw = client.DownloadStringFromGithubAPI(RepoUrl + "/branches");
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = JsonSerializer.Deserialize<BranchInfo[]>(raw, options);
                    var remoteCommit = result.Single(r => r.Name == "master").Commit.Sha;
                    if (LoadCache() && addonCache.Commit == remoteCommit)
                    {
                        return;
                    }
                    var updatedAddonCache = new AddonCache() { Commit = remoteCommit };
                    if (Directory.Exists(TempFolder))
                    {
                        Directory.Delete(TempFolder, true);
                    }
                    Directory.CreateDirectory(TempFolder);

                    var zipLocation = Path.Combine(TempFolder, "approvedList.zip");
                    client.DownloadFileFromGithubAPI(RepoUrl + "/zipball", zipLocation);
                    ZipFile.ExtractToDirectory(zipLocation, TempFolder);
                    var extractedFolder = Directory.EnumerateDirectories(TempFolder).First();
                    foreach (var addonDir in Directory.EnumerateDirectories(extractedFolder))
                    {
                        var addonInfo = AddonYamlReader.getAddonInInfo(addonDir);
                        if (addonInfo.folder_name == null)
                        {
                            addonInfo.folder_name = new DirectoryInfo(addonDir).Name;
                        }
                        updatedAddonCache.AddonInfo.Add(addonInfo);
                    }
                    addonCache = updatedAddonCache;
                    SaveCache();
                }
            }
            finally
            {
                if (Directory.Exists(TempFolder))
                {
                    Directory.Delete(TempFolder, true);
                }
            }
        }

        private bool LoadCache()
        {
            if (!File.Exists(CacheLocation))
            {
                return false;
            }
            var serializedCache = File.ReadAllText(CacheLocation);
            try
            {
                addonCache = JsonSerializer.Deserialize<AddonCache>(serializedCache);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SaveCache()
        {
            var serializedCache = JsonSerializer.Serialize(addonCache);
            File.WriteAllText(CacheLocation, serializedCache);
        }

        public AddonInfo GetAddonInfo(string addonName)
        {
            return Addons.Find(addon => addon.addon_name == addonName);
        }

        private class BranchInfo
        {
            public string Name { get; set; }
            public HeadInfo Commit { get; set; }
            public bool Protected { get; set; }
        }

        private class HeadInfo
        {
            public string Sha { get; set; }
            public string Url { get; set; }
        }

        private class AddonCache
        {
            public string Commit { get; set; }
            public List<AddonInfo> AddonInfo { get; set; } = new();
        }
    }
}
