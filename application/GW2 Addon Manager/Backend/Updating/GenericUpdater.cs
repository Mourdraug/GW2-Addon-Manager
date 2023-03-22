using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GW2_Addon_Manager.App.Configuration;
using GW2_Addon_Manager.App.Configuration.Model;
using System;
using System.Collections.Generic;

namespace GW2_Addon_Manager
{
    class GenericUpdater
    {
        private const string DisabledFolder = "disabledAddons";
        private readonly string addonsPath;
        private readonly string disabledAddonsPath;

        UpdatingViewModel viewModel;

        InstalledAddonData _installedAddonData;
        AddonInfo _addonInfo;

        /*string fileName;
        string addon_expanded_path;
        string addon_install_path;
        
        string latestVersion;*/

        public GenericUpdater(AddonInfo addonInfo)
        {
            _addonInfo = addonInfo;
            _installedAddonData = ConfigurationManager.Instance.UserConfig.AddonsList.FirstOrDefault(addon => addon.Name == addonInfo.addon_name);
            viewModel = UpdatingViewModel.GetInstance;
            addonsPath = Path.Combine(ConfigurationManager.Instance.UserConfig.GamePath, "addons");
            disabledAddonsPath = new DirectoryInfo(DisabledFolder).FullName;

            /*addon_expanded_path = Path.Combine(Path.GetTempPath(), addon_name);
            addon_install_path = */
        }
        public async Task InstallOrUpdate()
        {
            if (_installedAddonData != null)
            {
                await UpdateAddon();
            }
            else
            {
                await InstallAddon();
            }
        }

        private async Task InstallAddon()
        {
            (var updateUrl, var updateVersion) = (_addonInfo.host_type == "github") ? await GetUpdateInfoGit() : await GetUpdateInfoStandalone();
            if (updateUrl == null)
            {
                return;
            }
            string tempFilePath = null;
            try
            {
                string fileName;
                viewModel.ProgBarLabel = $"Downloading {_addonInfo.addon_name} {updateVersion}";
                (tempFilePath, fileName) = await Download(updateUrl);
                viewModel.ProgBarLabel = $"Installing {_addonInfo.addon_name} {updateVersion}";
                var installedFiles = InstallFiles(tempFilePath, fileName);
                var newAddon = new InstalledAddonData(_addonInfo.addon_name, updateVersion, true, installedFiles);
                PushAddonChange(newAddon);
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private async Task UpdateAddon()
        {
            if (_installedAddonData == null || !_installedAddonData.Enabled)
            {
                return;
            }
            (var updateUrl, var updateVersion) = (_addonInfo.host_type == "github") ? await GetUpdateInfoGit() : await GetUpdateInfoStandalone();

            if (updateUrl == null || (updateVersion != null && updateVersion == _installedAddonData.Version))
            {
                return;
            }

            string tempFilePath = null;
            try
            {
                string fileName;
                viewModel.ProgBarLabel = $"Downloading {_addonInfo.addon_name} {updateVersion}";
                (tempFilePath, fileName) = await Download(updateUrl);
                viewModel.ProgBarLabel = $"Installing {_addonInfo.addon_name} {updateVersion}";
                var installedFiles = InstallFiles(tempFilePath, fileName);
                var updatedAddon = _installedAddonData.SetVersion(updateVersion).SetAddonFiles(installedFiles);
                PushAddonChange(updatedAddon);
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        /***** UPDATE CHECK *****/

        /// <summary>
        /// Checks whether an update is required and performs it for an add-on hosted on Github.
        /// </summary>
        private async Task<(string url, string latestVersion)> GetUpdateInfoGit()
        {
            var release_info = UpdateHelpers.GitReleaseInfo(_addonInfo.host_url);
            var latestVersion = release_info.TagName;
            return (release_info.Assets[0].BrowserDownloadUrl, latestVersion);
        }

        private async Task<(string url, string latestVersion)> GetUpdateInfoStandalone()
        {
            string downloadURL = _addonInfo.host_url;
            if (_addonInfo.version_url == null)
            {
                return (downloadURL, null);
            }

            using (var client = UpdateHelpers.GetClient())
            {
                var latestVersion = client.DownloadString(_addonInfo.version_url);
                return (downloadURL, latestVersion);
            }
        }


        /***** DOWNLOAD *****/

        /// <summary>
        /// Downloads an add-on from the url specified in <paramref name="url"/> using the WebClient provided in <paramref name="client"/>.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="client"></param>
        private async Task<(string tempFilePath, string fileName)> Download(string url)
        {
            //this calls helper method to fetch filename if it is not exposed in URL
            var tempFilePath = Path.Combine(Path.GetTempPath(), $@"{Guid.NewGuid()}");
            var fileName = GetFilenameFromWebServer(url);
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            using (var client = UpdateHelpers.GetClient())
            {
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(addon_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(addon_DownloadCompleted);

                await client.DownloadFileTaskAsync(new System.Uri(url), tempFilePath);
            }
            return (tempFilePath, fileName);
        }

        /***** INSTALL *****/

        /// <summary>
        /// Performs archive extraction and file IO operations to install the downloaded addon.
        /// </summary>
        private string[] InstallFiles(string tempFilePath, string fileName)
        {
            List<string> installedFiles = new List<string>();
            viewModel.ProgBarLabel = "Installing " + _addonInfo.addon_name;
            RemoveFiles();

            if (_addonInfo.download_type == "archive")
            {

                var extractPath = $"{tempFilePath}_unzip";
                try
                {
                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }
                    ZipFile.ExtractToDirectory(tempFilePath, extractPath);
                    if (_addonInfo.install_mode == "arc")
                    {
                        InstallFromDirectory(extractPath, Path.Combine(addonsPath, "arcdps"));
                    }
                    else
                    {
                        InstallFromDirectory(extractPath, addonsPath);
                    }
                }
                finally
                {
                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }
                }
            }
            else
            {
                if (_addonInfo.install_mode == "arc")
                {
                    InstallFromFile(tempFilePath, Path.Combine(addonsPath, "arcdps", fileName));
                }
                else
                {
                    InstallFromFile(tempFilePath, Path.Combine(addonsPath, _addonInfo.folder_name, fileName));
                }
            }

            return installedFiles.ToArray();

            void InstallFromDirectory(string sourcePath, string targetPath)
            {
                foreach (string file in Directory.EnumerateFiles(sourcePath, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    string fileTargetPath = file.Replace(sourcePath, targetPath);
                    new FileInfo(fileTargetPath).Directory.Create();
                    File.Copy(file, fileTargetPath, true);
                    installedFiles.Add(fileTargetPath);
                }
            }

            void InstallFromFile(string sourceFile, string targetFile)
            {
                new FileInfo(targetFile).Directory.Create();
                File.Copy(sourceFile, targetFile, true);
                installedFiles.Add(targetFile);
            }
        }

        public void Disable()
        {
            List<string> newFileList = new List<string>();
            foreach (string file in _installedAddonData.AddonFiles.ToList())
            {
                var targetFile = file.Replace(addonsPath, disabledAddonsPath);
                MoveFile(file, targetFile);
                newFileList.Add(targetFile);
            }
            var updatedAddon = _installedAddonData.SetEnabled(false).SetAddonFiles(newFileList.ToArray());
            PushAddonChange(updatedAddon);
        }

        public void Enable()
        {
            List<string> newFileList = new List<string>();
            foreach (string file in _installedAddonData.AddonFiles.ToList())
            {
                var targetFile = file.Replace(disabledAddonsPath, addonsPath);
                MoveFile(file, targetFile);
                newFileList.Add(targetFile);
            }
            var updatedAddon = _installedAddonData.SetEnabled(true).SetAddonFiles(newFileList.ToArray());
            PushAddonChange(updatedAddon);
        }

        /***** DELETE *****/
        public void Delete()
        {
            RemoveFiles();
            ConfigurationManager.Instance.UserConfig.AddonsList.Remove(_installedAddonData);
        }

        private string GetFilenameFromWebServer(string url)
        {
            string result = "";

            var req = System.Net.WebRequest.Create(url);
            req.Method = "GET";
            using (System.Net.WebResponse resp = req.GetResponse())
            {
                string header = resp.Headers["Content-Disposition"] ?? string.Empty;
                const string filename = "filename=";
                int index = header.LastIndexOf(filename, StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    result = header.Substring(index + filename.Length);
                }
                else
                {
                    result = Path.GetFileName(resp.ResponseUri.AbsoluteUri);
                }
            }
            return result;
        }

        private void RemoveFiles()
        {
            if (_installedAddonData == null)
            {
                return;
            }
            foreach (string file in _installedAddonData.AddonFiles)
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                    CleanupDirectories(fileInfo.Directory);
                }
            }
        }

        private void CleanupDirectories(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.EnumerateFileSystemInfos().Any())
            {
                directoryInfo.Delete(true);
                CleanupDirectories(directoryInfo.Parent);
            }
        }

        private void MoveFile(string source, string target)
        {
            if (!File.Exists(source))
            {
                return;
            }
            else
            {
                new FileInfo(target).Directory.Create();
                File.Move(source, target);
                CleanupDirectories(new FileInfo(source).Directory);
            }
        }

        private void PushAddonChange(InstalledAddonData newAddon)
        {
            (var oldAddon, var index) = ConfigurationManager.Instance.UserConfig.AddonsList.Select((addon, index) => (addon, index)).FirstOrDefault(t => t.addon.Name == newAddon.Name);
            if (oldAddon != null)
            {
                ConfigurationManager.Instance.UserConfig.AddonsList[index] = newAddon;
            }
            else
            {
                ConfigurationManager.Instance.UserConfig.AddonsList.Add(newAddon);
            }
        }

        /***** DOWNLOAD EVENTS *****/
        void addon_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            viewModel.DownloadProgress = e.ProgressPercentage;
        }

        void addon_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }
    }
}
