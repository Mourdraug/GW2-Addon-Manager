using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using GW2_Addon_Manager.App.Configuration;

namespace GW2_Addon_Manager
{
    static class UpdateHelpers
    {
        public static WebClient GetClient()
        {
            var client = new WebClient();
            return client;
        }

        public static string DownloadStringFromGithubAPI(this WebClient wc, string url)
        {
            try
            {
                wc.AddGithubAPIHeaders();
                wc.Headers.Add(HttpRequestHeader.Accept, "application/json");
                return wc.DownloadString(url);
            }
            catch (WebException ex)
            {
                MessageBox.Show("Github servers returned an error; please try again in a few minutes.\n\nThe error was: " + ex.Message, "Github API Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        public static void DownloadFileFromGithubAPI(this WebClient wc, string url, string destPath)
        {
            try
            {
                wc.AddGithubAPIHeaders();
                wc.DownloadFile(url, destPath);
            }
            catch (WebException ex)
            {
                MessageBox.Show("Github servers returned an error; please try again in a few minutes.\n\nThe error was: " + ex.Message, "Github API Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw ex;
            }
        }

        private static void AddGithubAPIHeaders(this WebClient webClient)
        {
            webClient.Headers.Add("User-Agent", "Gw2 Addon Manager");
            var githubToken = ConfigurationManager.Instance.UserConfig.GithubToken;
            if (githubToken != null)
            {
                webClient.Headers[HttpRequestHeader.Authorization] = $"Token {githubToken}";
            }
        }

        public static GitRelease GitReleaseInfo(string gitUrl)
        {
            using (var client = UpdateHelpers.GetClient())
            {
                var release_info_json = client.DownloadStringFromGithubAPI(gitUrl);
                return JsonSerializer.Deserialize<GitRelease>(release_info_json);
            }

        }



        public static async void UpdateAll()
        {
            UpdatingViewModel viewModel = UpdatingViewModel.GetInstance;

            LoaderSetup settingUp = new LoaderSetup();
            await settingUp.HandleLoaderUpdate();

            List<AddonInfo> addons = (List<AddonInfo>)Application.Current.Properties["Selected"];

            foreach (AddonInfo addon in addons.Where(add => add != null))
            {
                GenericUpdater updater = new GenericUpdater(addon);

                if (!(addon.additional_flags != null && addon.additional_flags.Contains("self-updating")))
                    await updater.InstallOrUpdate();
            }

            viewModel.ProgBarLabel = "Updates Complete";
            viewModel.DownloadProgress = 100;
            viewModel.CloseBtnEnabled = true;
            viewModel.BackBtnEnabled = true;
        }

        public class GitRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }

            [JsonPropertyName("assets")]
            public GitAsset[] Assets { get; set; }
        }

        public class GitAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
