#if UNITY_2019_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppLovinMax.ThirdParty.MiniJson;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public class AppLovinUpmManifest
    {
        private const string KeyUrl = "url";
        private const string KeyName = "name";
        private const string KeyScopes = "scopes";
        private const string KeyScopedRegistry = "scopedRegistries";

        private Dictionary<string, object> manifest;

        private static string ManifestPath
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), "Packages/manifest.json"); }
        }

        // Private constructor to enforce the use of the Load() method
        private AppLovinUpmManifest() { }

        /// <summary>
        /// Creates a new instance of AppLovinUpmManifest and loads the manifest.json file.
        /// </summary>
        /// <returns>An instance of AppLovinUpmManifest</returns>
        public static AppLovinUpmManifest Load()
        {
            return new AppLovinUpmManifest { manifest = GetManifest() };
        }

        /// <summary>
        /// Adds or updates a scoped registry in the manifest.
        /// </summary>
        /// <param name="name">The name of the registry</param>
        /// <param name="url">The url of the registry</param>
        /// <param name="scopes">The scopes of the registry</param>
        public void AddOrUpdateRegistry(string name, string url, List<string> scopes)
        {
            var registry = GetRegistry(name);
            if (registry == null)
            {
                var registries = GetRegistries();
                if (registries == null) return;

                registries.Add(new Dictionary<string, object>
                {
                    {KeyName, name},
                    {KeyUrl, url},
                    {KeyScopes, scopes}
                });

                return;
            }

            UpdateRegistry(registry, scopes);
        }

        /// <summary>
        /// Saves the manifest by serializing it back to JSON and writing to file.
        /// </summary>
        public void Save()
        {
            var content = Json.Serialize(manifest, true);
            File.WriteAllText(ManifestPath, content);
        }

        /// <summary>
        /// Adds a dependency to the manifest.
        /// </summary>
        /// <param name="packageName">The name of the package to add</param>
        /// <param name="version">The version of the package to add</param>
        public void AddPackageDependency(string packageName, string version)
        {
            var manifestDependencies = GetDependencies();
            manifestDependencies[packageName] = version;
        }

        /// <summary>
        /// Removes a dependency from the manifest.
        /// </summary>
        /// <param name="packageName">The name of the package to remove</param>
        public void RemovePackageDependency(string packageName)
        {
            var manifestDependencies = GetDependencies();
            manifestDependencies.Remove(packageName);
        }

        #region Utility

        /// <summary>
        /// Returns the manifest.json file as a dictionary.
        /// </summary>
        private static Dictionary<string, object> GetManifest()
        {
            if (!File.Exists(ManifestPath))
            {
                throw new Exception("Manifest not Found!");
            }

            var manifestJson = File.ReadAllText(ManifestPath);
            if (string.IsNullOrEmpty(manifestJson))
            {
                throw new Exception("Manifest is empty!");
            }

            var deserializedManifest = Json.Deserialize(manifestJson) as Dictionary<string, object>;
            if (deserializedManifest == null)
            {
                throw new Exception("Failed to deserialize manifest");
            }

            return deserializedManifest;
        }

        /// <summary>
        /// Gets the manifest's dependencies section.
        /// </summary>
        /// <returns>The dependencies section of the manifest.</returns>
        private Dictionary<string, object> GetDependencies()
        {
            var dependencies = manifest["dependencies"] as Dictionary<string, object>;
            if (dependencies == null)
            {
                throw new Exception("No dependencies found in manifest.");
            }

            return dependencies;
        }

        /// <summary>
        /// Gets the manifest's registries section. Creates a new registries section if one does not exist.
        /// </summary>
        /// <returns>The registries section of the manifest.</returns>
        private List<object> GetRegistries()
        {
            EnsureScopedRegistryExists();
            return manifest[KeyScopedRegistry] as List<object>;
        }

        /// <summary>
        /// Gets a scoped registry with the given name.
        /// </summary>
        /// <param name="name">The name of the registry</param>
        /// <returns>Returns the registry, or null if it can't be found</returns>
        private Dictionary<string, object> GetRegistry(string name)
        {
            var registries = GetRegistries();
            if (registries == null) return null;

            return registries
                .OfType<Dictionary<string, object>>()
                .FirstOrDefault(registry => MaxSdkUtils.GetStringFromDictionary(registry, KeyName).Equals(name));
        }

        /// <summary>
        /// Creates the section for scoped registries in the manifest.json file if it doesn't exist.
        /// </summary>
        private void EnsureScopedRegistryExists()
        {
            if (manifest.ContainsKey(KeyScopedRegistry)) return;

            manifest.Add(KeyScopedRegistry, new List<object>());
        }

        /// <summary>
        /// Updates a registry to make sure it contains the new scopes.
        /// </summary>
        /// <param name="registry">The registry to update</param>
        /// <param name="newScopes">The scopes we want added to the registry</param>
        private static void UpdateRegistry(Dictionary<string, object> registry, List<string> newScopes)
        {
            var scopes = MaxSdkUtils.GetListFromDictionary(registry, KeyScopes);
            if (scopes == null)
            {
                registry[KeyScopes] = new List<string>(newScopes);
                return;
            }

            // Only add scopes that are not already in the list
            var uniqueNewScopes = newScopes.Where(scope => !scopes.Contains(scope)).ToList();
            scopes.AddRange(uniqueNewScopes);
        }

        #endregion
    }
}
#endif
