using System.IO;
using System.Reflection;
using System.Text.Json;
using GW2_Addon_Manager.App.Configuration.Model;

namespace GW2_Addon_Manager.App.Configuration
{
    /// <inheritdoc />
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;
        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigurationManager();
                }
                return _instance;
            }
        }

        private const string ConfigFileName = "config.json";

        private static readonly UserConfig UserConfigInstance = CreateConfig();

        private ConfigurationManager()
        {
            UserConfigInstance.PropertyChanged += (_, _) =>
            {
                SaveConfig();
            };
        }

        /// <inheritdoc />
        public string ApplicationVersion
        {
            get
            {
                var currentAppVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return $"v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}";
            }
        }

        /// <inheritdoc />
        public UserConfig UserConfig => UserConfigInstance;

        /// <inheritdoc />
        private void SaveConfig()
        {
            var serializedJson = JsonSerializer.Serialize(UserConfigInstance);
            File.WriteAllText(ConfigFileName, serializedJson);
        }

        private static UserConfig CreateConfig()
        {
            if (!File.Exists(ConfigFileName))
                return new UserConfig();

            var serializedData = File.ReadAllText(ConfigFileName);
            return JsonSerializer.Deserialize<UserConfig>(serializedData);
        }
    }
}