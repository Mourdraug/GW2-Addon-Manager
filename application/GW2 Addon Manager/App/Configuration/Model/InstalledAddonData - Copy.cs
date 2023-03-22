using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GW2_Addon_Manager.App.Configuration.Model
{
    public class InstalledAddonData : INotifyPropertyChanged
    {
        private string _name, _version;
        private bool _enabled = true;

        public string Name
        {
            get => _name; set
            {
                if (_name != value)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        public string Version
        {
            get => _version; set
            {
                if (_version != value)
                {
                    _version = value;
                    NotifyPropertyChanged("Version");
                }
            }
        }

        public bool Enabled
        {
            get => _enabled; set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    NotifyPropertyChanged("Enabled");
                }
            }
        }

        public ObservableCollection<string> AddonFiles { get; } = new ObservableCollection<string>();

        public InstalledAddonData()
        {
            AddonFiles.CollectionChanged += (_, _) => NotifyPropertyChanged("AddonFiles");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}