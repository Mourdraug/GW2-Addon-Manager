﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;

namespace GW2_Addon_Manager
{
    class UpdateHelpers
    {
        public static dynamic GitReleaseInfo(string gitUrl)
        {
            var client = new WebClient();
            client.Headers.Add("User-Agent", "request");
            string release_info_json = client.DownloadString(gitUrl);
            return JsonConvert.DeserializeObject(release_info_json);
        }

        public static async void UpdateAll()
        {
            UpdatingViewModel viewModel = UpdatingViewModel.GetInstance();

            LoaderSetup settingUp = new LoaderSetup();
            await settingUp.HandleLoaderUpdate();

            List<AddonInfo> addons = (List<AddonInfo>)Application.Current.Properties["Selected"];
            
            foreach (AddonInfo addon in addons.Where(add => add != null))
            {
                GenericUpdater updater = new GenericUpdater(addon);
                await updater.Update();
            }

            viewModel.progBarLabel = "Updates Complete";
            viewModel.showProgress = 100;
            viewModel.closeButtonEnabled = true;
        }
    }
}
