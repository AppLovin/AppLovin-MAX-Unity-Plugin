//
//  AppLovinPreProcess.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 10/19/2023.
//  Copyright © 2023 AppLovin. All rights reserved.
//

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public abstract class AppLovinPreProcess
    {
        // Use a slightly lower value than max value so pubs have the option to run a post process script after ours.
        internal const int CallbackOrder = int.MaxValue - 10;
        private const string AppLovinDependenciesFileExportPath = "MaxSdk/AppLovin/Editor/Dependencies.xml";
        private const string ElementNameDependencies = "dependencies";

        private static readonly XmlWriterSettings DependenciesFileXmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        protected static string AppLovinDependenciesFilePath
        {
            get { return AppLovinIntegrationManager.IsPluginInPackageManager ? Path.Combine("Assets", AppLovinDependenciesFileExportPath) : MaxSdkUtils.GetAssetPathForExportPath(AppLovinDependenciesFileExportPath); }
        }

        /// <summary>
        /// Gets the AppLovin Dependencies.xml file. If `createIfNotExists` is true, a new file will be created if one does not exist.
        /// </summary>
        /// <param name="path">The path to the AppLovin Dependencies.xml file</param>
        /// <param name="createIfNotExists">Whether to create a new Dependencies.xml file if one does not exist</param>
        /// <returns></returns>
        protected static XDocument GetAppLovinDependenciesFile(string path, bool createIfNotExists = false)
        {
            try
            {
                if (File.Exists(path))
                {
                    return XDocument.Load(path);
                }
                
                if (createIfNotExists)
                {
                    return new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                        new XElement(ElementNameDependencies));
                }
            }
            catch (Exception exception)
            {
                MaxSdkLogger.E("Unable to load Dependencies file due to exception: " + exception.Message);
            }

            return null;
        }

        /// <summary>
        /// Updates a dependency if it exists, otherwise adds a new dependency.
        /// </summary>
        /// <param name="dependenciesDocument">The dependencies document we are writing to</param>
        /// <param name="parentTag">The parent tag that we want to search for the dependency. For example, to add a new dependency to Android, pass in "androidPackages"</param>
        /// <param name="elementTag">The element we are looking to update/add. For example, to add a new dependency to Android, pass in "androidPackage"</param>
        /// <param name="matchAttribute">The attribute name we want in the dependency. For example, to add something to the spec attribute, pass in "spec" </param>
        /// <param name="matchValuePrefix">The attribute value prefix we are looking to replace. For example, "com.google.android.ump:user-messaging-platform"</param>
        /// <param name="newDependency">The new dependency we want to add.</param>
        protected static void AddOrUpdateDependency(
            XDocument dependenciesDocument,
            string parentTag,
            string elementTag,
            string matchAttribute,
            string matchValuePrefix,
            XElement newDependency)
        {
            var parentElement = dependenciesDocument.Root.Element(parentTag);
            if (parentElement == null)
            {
                parentElement = new XElement(parentTag);
                dependenciesDocument.Root.Add(parentElement);
            }

            // Check if a dependency exists that matches the attributes name and value
            var existingElement = parentElement.Elements(elementTag)
                .FirstOrDefault(element =>
                {
                    var attr = element.Attribute(matchAttribute);
                    return attr != null && attr.Value.StartsWith(matchValuePrefix, StringComparison.OrdinalIgnoreCase);
                });

            if (existingElement != null)
            {
                foreach (var attr in newDependency.Attributes())
                {
                    existingElement.SetAttributeValue(attr.Name, attr.Value);
                }
            }
            else
            {
                parentElement.Add(newDependency);
            }
        }

        /// <summary>
        /// Removes a dependency from an xml file.
        /// </summary>
        /// <param name="doc">The xml file to remove a dependency from</param>
        /// <param name="parentTag">The parent tag that we want to search for the dependency to remove. For example: "androidPackages"</param>
        /// <param name="elementTag">The element we are looking to remove. For example: "androidPackage"</param>
        /// <param name="matchAttribute">The attribute name we want to remove. For example: "spec" </param>
        /// <param name="matchValuePrefix">The attribute value prefix we are looking to replace. For example: "com.google.android.ump:user-messaging-platform"</param>
        /// <returns>True if the dependency was removed successfully, otherwise return false.</returns>
        protected static bool RemoveDependency(
            XDocument doc,
            string parentTag,
            string elementTag,
            string matchAttribute,
            string matchValuePrefix)
        {
            var root = doc.Root;
            if (root == null) return false;

            var parentElement = root.Element(parentTag);
            if (parentElement == null) return false;

            XElement toRemove = null;
            foreach (var e in parentElement.Elements(elementTag))
            {
                var attr = e.Attribute(matchAttribute);
                if (attr != null && attr.Value.StartsWith(matchValuePrefix))
                {
                    toRemove = e;
                    break;
                }
            }

            if (toRemove == null) return false;

            toRemove.Remove();
            return true;
        }

        /// <summary>
        /// Saves an xml file.
        /// </summary>
        /// <param name="doc">The document to save</param>
        /// <param name="path">The path to the document to save</param>
        /// <returns>Returns true if the file was saved successfully</returns>
        protected static bool SaveDependenciesFile(XDocument doc, string path)
        {
            try
            {
                // Ensure directory exists before saving the file
                var directory = Path.GetDirectoryName(path);
                if (MaxSdkUtils.IsValidString(directory))
                {
                    // Does nothing if directory already exists
                    Directory.CreateDirectory(directory);
                }

                using (var xmlWriter = XmlWriter.Create(path, DependenciesFileXmlWriterSettings))
                {
                    doc.Save(xmlWriter);
                    return true;
                }
            }
            catch (Exception exception)
            {
                MaxSdkLogger.E("Unable to save Dependencies file due to exception: " + exception.Message);
            }

            return false;
        }
    }
}
