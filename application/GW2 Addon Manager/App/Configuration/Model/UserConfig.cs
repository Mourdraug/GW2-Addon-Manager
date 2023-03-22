using Localization;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace GW2_Addon_Manager.App.Configuration.Model
{
    public class UserConfig : INotifyPropertyChanged
    {
        private string _loaderVersion, _githubToken;
        private string _exePath = "C:\\Program Files\\Guild Wars 2\\Gw2-64.exe";
        private string _culture = CultureConstants.English;
        private bool _launchGame = false;
        private ObservableCollection<InstalledAddonData> _addonsList = new ObservableCollection<InstalledAddonData>();

        public string LoaderVersion
        {
            get => _loaderVersion;
            set
            {
                if (_loaderVersion != value)
                {
                    _loaderVersion = value;
                    NotifyPropertyChanged("LoaderVersion");
                }
            }
        }

        public string GithubToken
        {
            get => _githubToken;
            set
            {
                if (_githubToken != value)
                {
                    _githubToken = value;
                    NotifyPropertyChanged("GithubToken");
                }
            }
        }

        public string ExePath
        {
            get => _exePath;
            set
            {
                if (_exePath != value)
                {
                    _exePath = value;
                    NotifyPropertyChanged("ExePath");
                }
            }
        }

        public string Culture
        {
            get => _culture;
            set
            {
                if (_culture != value)
                {
                    _culture = value;
                    NotifyPropertyChanged("Culture");
                }
            }
        }

        public bool LaunchGame
        {
            get => _launchGame;
            set
            {
                if (_launchGame != value)
                {
                    _launchGame = value;
                    NotifyPropertyChanged("LaunchGame");
                }
            }
        }

        public ObservableCollection<InstalledAddonData> AddonsList
        {
            get => _addonsList; set
            {
                if (_addonsList != value)
                {
                    _addonsList.CollectionChanged -= OnAddonListChanged;
                    _addonsList = value;
                    _addonsList.CollectionChanged += OnAddonListChanged;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string GamePath => Path.GetDirectoryName(ExePath);
        public string BinFolder => "bin64";

        public UserConfig()
        {
            AddonsList.CollectionChanged += OnAddonListChanged; 
        }

        private void OnAddonListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("AddonsList");
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}