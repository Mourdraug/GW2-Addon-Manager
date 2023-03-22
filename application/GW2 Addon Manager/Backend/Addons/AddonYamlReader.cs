using System.IO;
using YamlDotNet.Serialization;

namespace GW2_Addon_Manager
{
    /// <summary>
    /// Intended to read update.yaml files provided in addons that adhere to a specific set of specifications laid out for use by GW2-UOAOM and GW2-AddOn-Loader.
    /// </summary>
    class AddonYamlReader
    {
        /// <summary>
        /// Gets info for an add-on from update.yaml provided by the author or packaged with the application (when the author hasn't written one yet).
        /// </summary>
        /// <param name="name">The name of the addon folder to read from.</param>
        /// <returns>An object with the information from update.yaml</returns>
        public static AddonInfo getAddonInInfo(string path)
        {
            string yamlPath = Path.Combine(path, "update.yaml");
            string placeholderYamlPath = Path.Combine(path, "update-placeholder.yaml");
            string updateFile = null;

            if (File.Exists(yamlPath))
                updateFile = File.ReadAllText(yamlPath);
            else if (File.Exists(placeholderYamlPath))
                updateFile = File.ReadAllText(placeholderYamlPath);

            return new Deserializer().Deserialize<AddonInfo>(updateFile);
        }
    }
}
