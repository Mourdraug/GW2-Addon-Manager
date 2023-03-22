using System;
using System.IO;
using System.IO.Abstractions;
using System.Windows;
using GW2_Addon_Manager.App.Configuration;
using GW2_Addon_Manager.App.Configuration.Model;
using Localization;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// The <c>configuration</c> class contains various functions dealing with application configuration. 
    /// </summary>
    public class Configuration
    {
        static readonly string applicationRepoUrl = "https://api.github.com/repos/fmmmlee/GW2-Addon-Manager/releases/latest";

        /// <summary>
        /// <c>SetGamePath</c> both sets the game path for the current application session to <paramref name="path"/> and records it in the configuration file.
        /// </summary>
        /// <param name="path">The game path.</param>
        public void SetGamePath(string path)
        {
            try
            {
                Application.Current.Properties["game_path"] = path.Replace("\\", "\\\\");
            }
            catch (Exception)
            { }

            ConfigurationManager.Instance.UserConfig.ExePath = path;
        }

        /// <summary>
        /// <c>SetCulture</c> both sets the culture for the current application session to <paramref name="culture"/> and records it in the configuration file.
        /// </summary>
        /// <param name="culture"></param>
        public void SetCulture(string culture)
        {
            Application.Current.Properties["culture"] = culture;
            ConfigurationManager.Instance.UserConfig.Culture = culture;
            RestartApplication();
        }

        /// <summary>
        /// Restarts the application.
        /// </summary>
        private void RestartApplication()
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Checks if there is a new version of the application available.
        /// </summary>
        public void CheckSelfUpdates()
        {
            var release_info = UpdateHelpers.GitReleaseInfo(applicationRepoUrl);
            var latestVersion = release_info.TagName;

            if (latestVersion == ConfigurationManager.Instance.ApplicationVersion) return;

            OpeningViewModel.GetInstance.UpdateAvailable = $"{latestVersion} {StaticText.Available.ToLower()}!";
            OpeningViewModel.GetInstance.UpdateLinkVisibility = Visibility.Visible;
        }

        /// <summary>
        /// Deletes all addons, addon loader, and configuration data related to addons.
        /// </summary>
        public void DeleteAllAddons()
        {
            //set installed, disabled, default, and version collections to the default installation setting
            ConfigurationManager.Instance.UserConfig.AddonsList.Clear();

            //clear loader_version
            ConfigurationManager.Instance.UserConfig.LoaderVersion = null;

            //delete disabled plugins folder: ${install dir}/disabled plugins
            if(Directory.Exists("Disabled Plugins"))
                Directory.Delete("Disabled Plugins", true);
            //delete addons: {game folder}/addons
            if(Directory.Exists(Path.Combine(ConfigurationManager.Instance.UserConfig.GamePath, "addons")))
                Directory.Delete(Path.Combine(ConfigurationManager.Instance.UserConfig.GamePath, "addons"), true);
            //delete addon loader: {game folder}/{bin/64}/d3d9.dll
            File.Delete(Path.Combine(Path.Combine(ConfigurationManager.Instance.UserConfig.GamePath, ConfigurationManager.Instance.UserConfig.BinFolder), "d3d9.dll"));
        }
    }
}
