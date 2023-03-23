using GalaSoft.MvvmLight.Command;
using IWshRuntimeLibrary;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GW2_Addon_Manager.App.Configuration;
using File = System.IO.File;
using Localization;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using GW2_Addon_Manager.App.Configuration.Model;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// <c>OpeningViewModel</c> serves as the DataContext for OpeningView.xaml, which is the first screen encountered upon opening the application.
    /// </summary>
    public class OpeningViewModel : INotifyPropertyChanged
    {
        private readonly PluginManagement _pluginManagement;
        private readonly Configuration _configuration;

        /***** private fields for ui bindings *****/
        private ObservableCollection<AddonRow> _addonList;
        private string _developer;
        private string _description;
        private string _addonwebsite;
        private Visibility _developer_visibility;
        private string _updateAvailable;
        private int updateProgress;
        private Visibility _updateLinkVisibility;
        private Visibility _updateProgressVisibility;

        /********** UI BINDINGS **********/

        /***** Addon List Box *****/
        /// <summary>
        /// List of Addons
        /// </summary>
        public ObservableCollection<AddonRow> AddonList
        {
            get => _addonList;
            set => SetProperty(ref _addonList, value);
        }

        /***** Description Panel *****/
        /// <summary>
        /// Text content for the description panel.
        /// </summary>
        public string DescriptionText
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        /// <summary>
        /// The informational text showing the developer of the selected add-on.
        /// </summary>
        public string DeveloperText
        {
            get => _developer;
            set => SetProperty(ref _developer, value);
        }
        /// <summary>
        /// The website link in the description panel.
        /// </summary>
        public string AddonWebsiteLink
        {
            get => _addonwebsite;
            set => SetProperty(ref _addonwebsite, value);
        }

        /***** Show/Hide Elements *****/
        /// <summary>
        /// Visibility of the informational text showing the developer of the selected add-on.
        /// </summary>
        public Visibility DeveloperVisibility
        {
            get => _developer_visibility;
            set => SetProperty(ref _developer_visibility, value);
        }
        /// <summary>
        /// A string representing a visibility value for the Github releases link.
        /// </summary>
        public Visibility UpdateLinkVisibility
        {
            get => _updateLinkVisibility;
            set => SetProperty(ref _updateLinkVisibility, value);
        }
        /// <summary>
        /// A string representing a visibility value for the self-update download progress bar.
        /// </summary>
        public Visibility UpdateProgressVisibility
        {
            get => _updateProgressVisibility;
            set => SetProperty(ref _updateProgressVisibility, value);
        }

        /***************************/
        /***** Button Handlers *****/
        /***************************/

        /* [Configuration Options] drop-down menu */

        /// <summary>
        /// Handles the disable selected addons button.
        /// </summary>
        public ICommand DisableSelected
        {
            get => new RelayCommand<object>(param => _pluginManagement.DisableSelected(), true);
        }
        /// <summary>
        /// Handles the enable selected addons button.
        /// </summary>
        public ICommand EnableSelected
        {
            get => new RelayCommand<object>(param => _pluginManagement.EnableSelected(), true);
        }
        /// <summary>
        /// Handles the delete selected addons button.
        /// </summary>
        public ICommand DeleteSelected
        {
            get => new RelayCommand<object>(param => _pluginManagement.DeleteSelected(), true);
        }
        /// <summary>
        /// Handles the Reset to Clean Install button.
        /// </summary>
        public ICommand CleanInstall
        {
            get => new RelayCommand<object>(param => _pluginManagement.DeleteAll(), true);
        }
        /// <summary>
        /// Handles the Change Language buttons.
        /// </summary>
        public ICommand ChangeLanguage
        {
            get => new RelayCommand<string>(param => _configuration.SetCulture(param), true);
        }

        /******************************************/

        /// <summary>
        /// Handler for small button to download application update.
        /// </summary>
        public ICommand DownloadSelfUpdate
        {
            get => new RelayCommand<object>(param => SelfUpdate.Update(), true);
        }
        /// <summary>
        /// Handles the create shortcut button under the options menu. <see cref="cs_logic"/>
        /// </summary>
        public ICommand CreateShortcut
        {
            get => new RelayCommand<object>(param => cs_logic(), true);
        }

        /***** Misc *****/
        /// <summary>
        /// Binding for the value shown in the mini progress bar displayed when downloading a new version of the application.
        /// </summary>
        public int UpdateDownloadProgress
        {
            get => updateProgress;
            set => SetProperty(ref updateProgress, value);
        }

        /// <summary>
        /// Content of the text box that contains the game path the program is set to look for the game in.
        /// </summary>
        public string GamePath
        {
            get => _gamePath;
            set
            {
                if (value == _gamePath) return;
                SetProperty(ref _gamePath, value);

                new Configuration().SetGamePath(_gamePath);
            }
        }
        private string _gamePath;

        /// <summary>
        /// A string that is assigned a value if there is an update available.
        /// </summary>
        public string UpdateAvailable
        {
            get => _updateAvailable;
            set => SetProperty(ref _updateAvailable, value);
        }


        /********** Class Structure/Other Methods **********/

        //credit to Freesnow on discord
        protected bool SetProperty<T>(ref T property, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(property, newValue) || propertyName == null) return false;

            property = newValue;
            propertyChanged(propertyName);
            return true;
        }

        /* Singleton Setup */
        private static OpeningViewModel onlyInstance;
        /// <summary>
        /// This constructor initializes various default properties across the class and then
        /// applies any updated values to them using <c>ApplyDefaultConfig</c>.
        /// </summary>
        private OpeningViewModel()
        {
            onlyInstance = this;

            _pluginManagement = new PluginManagement();
            _configuration = new Configuration();
            ApprovedList.Instance.UpdateList();
            AddonList = BuildObservableAddonList(ApprovedList.Instance.Addons);
            ConfigurationManager.Instance.UserConfig.AddonsList.CollectionChanged += UpdateAddonsModel;

            DescriptionText = StaticText.SelectAnAddonToSeeMoreInformationAboutIt;
            DeveloperVisibility = Visibility.Hidden;

            UpdateLinkVisibility = Visibility.Hidden;
            UpdateProgressVisibility = Visibility.Hidden;

            GamePath = ConfigurationManager.Instance.UserConfig.ExePath;
        }

        private void UpdateAddonsModel(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    foreach (InstalledAddonData installedAddon in e.NewItems)
                    {
                        if (installedAddon.Name == "d3d9 wrapper")
                        {
                            continue;
                        }
                        var row = AddonList.First(row => row.AddonInfo.addon_name == installedAddon.Name);
                        row.IsInstalled = true;
                        row.IsEnabled = installedAddon.Enabled;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var addon in AddonList)
                    {
                        addon.IsInstalled = false;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (InstalledAddonData installedAddon in e.OldItems)
                    {
                        var row = AddonList.First(row => row.AddonInfo.addon_name == installedAddon.Name);
                        row.IsInstalled = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// Fetches the only instance of the OpeningViewModel and creates it if it has not been initialized yet.
        /// </summary>
        /// <returns>An instance of OpeningViewModel</returns>
        public static OpeningViewModel GetInstance
        { get => (onlyInstance == null) ? new OpeningViewModel() : onlyInstance; }

        /*** Notify UI of Changed Binding Items ***/
        /// <summary>
        /// An event used to indicate that a property's value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// A method used in notifying that a property's value has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed value.</param>
        public void propertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //TODO: Move this to a more appropriate class and just use a ICommand that invokes it
        /* Button Helper */
        /// <summary>
        /// Creates a shortcut in the current user's start menu.
        /// </summary>
        //see the accepted answer at https://stackoverflow.com/questions/25024785/how-to-create-start-menu-shortcut
        //- I did some modifications (based on another SO question) to avoid admin access requirement
        private void cs_logic()
        {
            string appPath = Directory.GetCurrentDirectory();
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string appShortcutPath = Path.Combine(startMenuPath, @"GW2-UOAOM");
            string shortcutNamePath = Path.Combine(appShortcutPath, "GW2 Addon Manager" + ".lnk");

            if (!Directory.Exists(appShortcutPath))
                Directory.CreateDirectory(appShortcutPath);

            if (!File.Exists(shortcutNamePath))
            {
                WshShell quickShell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)quickShell.CreateShortcut(shortcutNamePath);
                shortcut.Description = "The Guild Wars 2 Unofficial Add-On Manager";
                shortcut.IconLocation = Path.Combine(appPath, "resources\\logo.ico");
                shortcut.WorkingDirectory = appPath;
                shortcut.TargetPath = Path.Combine(appPath, "GW2 Addon Manager" + ".exe");
                shortcut.Save();
            }
        }

        private ObservableCollection<AddonRow> BuildObservableAddonList(IEnumerable<AddonInfo> addonInfo)
        {
            var output = new ObservableCollection<AddonRow>();
            foreach (AddonInfo addon in addonInfo)
            {
                if (addon.addon_name == "d3d9 wrapper")
                {
                    continue;
                }
                var installedAddon = ConfigurationManager.Instance.UserConfig.AddonsList.FirstOrDefault(installedAddon => installedAddon.Name == addon.addon_name);
                if (installedAddon == null)
                {
                    output.Add(new AddonRow() { AddonInfo = addon });
                }
                else
                {
                    output.Add(new AddonRow() { AddonInfo = addon, IsInstalled = true, IsEnabled = installedAddon.Enabled, IsSelected = installedAddon.Enabled });
                }
            }
            return output;
        }

        public class AddonRow : INotifyPropertyChanged
        {
            bool _isSelected = false;
            bool _isEnabled = false;
            bool _isInstalled = false;

            public AddonInfo AddonInfo { get; set; }
            public bool IsSelected
            {
                get => _isSelected; set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
                    }
                }
            }

            public bool IsEnabled
            {
                get => _isEnabled; set
                {
                    if (_isEnabled != value)
                    {
                        _isEnabled = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEnabled"));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayString"));
                    }
                }
            }

            public bool IsInstalled
            {
                get => _isInstalled; set
                {
                    if (_isInstalled != value)
                    {
                        _isInstalled = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsInstalled"));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayString"));
                    }
                }
            }

            public string DisplayString => $"{AddonInfo.addon_name}{(IsInstalled ? $" ({(IsEnabled ? "installed" : "disabled")})" : "")}";

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
