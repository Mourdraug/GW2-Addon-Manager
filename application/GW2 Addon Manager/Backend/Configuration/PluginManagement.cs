using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GW2_Addon_Manager.App.Configuration;

namespace GW2_Addon_Manager
{
    class PluginManagement
    {
        /// <summary>
        /// Sets version fields of all installed and enabled addons to a dummy value so they are redownloaded, then starts update process.
        /// Intended for use if a user borks their install (probably by manually deleting something in the /addons/ folder).
        /// </summary>
        public bool ForceRedownload()
        {
            /*string redownloadmsg = "This will forcibly redownload all installed addons regardless of their version. Do you wish to continue?";
            if (MessageBox.Show(redownloadmsg, "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _configurationManager.UserConfig.AddonsList.ToList().ForEach(a => a.Version = "dummy value");
                return true;
            }*/
            return false; 
        }

        /// <summary>
        /// Deletes all addons and resets config to default state.
        /// <seealso cref="OpeningViewModel.CleanInstall"/>
        /// <seealso cref="Configuration.DeleteAllAddons"/>
        /// </summary>
        public void DeleteAll()
        {
            string deletemsg = "This will delete ALL add-ons from Guild Wars 2 and all data associated with them! Are you sure you wish to continue?";
            string secondPrecautionaryMsg = "Are you absolutely sure you want to delete all addons? This action cannot be undone.";

            //precautionary "are you SURE" messages x2
            if (MessageBox.Show(deletemsg, "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                if (MessageBox.Show(secondPrecautionaryMsg, "Absolutely Sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    new Configuration().DeleteAllAddons();
                    //post-delete info message
                    MessageBox.Show("All addons have been removed.", "Reverted to Clean Install", MessageBoxButton.OK, MessageBoxImage.Information);
                }
        }

        /// <summary>
        /// Deletes the currently selected addons.
        /// <seealso cref="OpeningViewModel.DeleteSelected"/>
        /// </summary>
        public void DeleteSelected()
        {
            string deletemsg = "This will delete any add-ons that are selected and all data associated with them! Are you sure you wish to continue?";
            if (MessageBox.Show(deletemsg, "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (AddonInfo addon in GetSelectedAddons())
                    new GenericUpdater(addon).Delete();
            }
        }

        /// <summary>
        /// Disables the currently selected addons.
        /// <seealso cref="OpeningViewModel.DisableSelected"/>
        /// </summary>
        public void DisableSelected()
        {
            string disablemsg = "This will disable the selected add-ons until you choose to re-enable them. Do you wish to continue?";
            if (MessageBox.Show(disablemsg, "Disable", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                foreach (AddonInfo addon in GetSelectedAddons())
                    new GenericUpdater(addon).Disable();
            }
        }

        /// <summary>
        /// Enables the currently selected addons.
        /// <seealso cref="OpeningViewModel.EnableSelected"/>
        /// </summary>
        public void EnableSelected()
        {
            string enablemsg = "This will enable any of the selected add-ons that are disabled. Do you wish to continue?";
            if (MessageBox.Show(enablemsg, "Enable", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                foreach (AddonInfo addon in GetSelectedAddons())
                    new GenericUpdater(addon).Enable();
            }
        }

        public IEnumerable<AddonInfo> GetSelectedAddons()
        {
            return OpeningViewModel.GetInstance.AddonList.Where(addon => addon.IsSelected).Select(row => row.AddonInfo).ToList();
        }
    }
}
