//
//  AppLovinPreProcess.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Jonathan Liu on 10/19/2023.
//  Copyright © 2023 AppLovin. All rights reserved.
//

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    public abstract class AppLovinPreProcess
    {
        private const string AppLovinDependenciesFileExportPath = "MaxSdk/AppLovin/Editor/Dependencies.xml";

        private static readonly XmlWriterSettings DependenciesFileXmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        /// <summary>
        /// Adds a string into AppLovin's Dependencies.xml file inside the containerElementString if it doesn't exist
        /// </summary>
        /// <param name="lineToAdd">The line you want to add into the xml file</param>
        /// <param name="containerElementString">The root XML element under which to add the line. For example, to add a new dependency to Android, pass in "androidPackages"</param>
        protected static void TryAddStringToDependencyFile(string lineToAdd, string containerElementString)
        {
            try
            {
                var dependenciesFilePath = MaxSdkUtils.GetAssetPathForExportPath(AppLovinDependenciesFileExportPath);
                var dependencies = XDocument.Load(dependenciesFilePath);
                // Get the container where we are going to insert the line
                var containerElement = dependencies.Descendants(containerElementString).FirstOrDefault();

                if (containerElement == null)
                {
                    Debug.LogError(containerElementString + " not found in Dependencies.xml file");
                    return;
                }

                var elementToAdd = XElement.Parse(lineToAdd);

                // Check if the xml file doesn't already contain the string.
                if (containerElement.Elements().Any(element => XNode.DeepEquals(element, elementToAdd))) return;

                // Append the new element to the container element
                containerElement.Add(elementToAdd);

                using (var xmlWriter = XmlWriter.Create(dependenciesFilePath, DependenciesFileXmlWriterSettings))
                {
                    dependencies.Save(xmlWriter);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Google CMP will not function. Unable to add string to dependency file due to exception: " + exception.Message);
            }
        }

        /// <summary>
        /// Removes a string from AppLovin's Dependencies.xml file inside the containerElementString if it exists
        /// </summary>
        /// <param name="lineToRemove">The line you want to remove from the xml file</param>
        /// <param name="containerElementString">The root XML element from which to remove the line. For example, to remove an Android dependency, pass in "androidPackages"</param>
        protected static void TryRemoveStringFromDependencyFile(string lineToRemove, string containerElementString)
        {
            try
            {
                var dependenciesFilePath = MaxSdkUtils.GetAssetPathForExportPath(AppLovinDependenciesFileExportPath);
                var dependencies = XDocument.Load(dependenciesFilePath);
                var containerElement = dependencies.Descendants(containerElementString).FirstOrDefault();

                if (containerElement == null)
                {
                    Debug.LogError(containerElementString + " not found in Dependencies.xml file");
                    return;
                }

                // Check if the dependency line exists.
                var elementToFind = XElement.Parse(lineToRemove);
                var existingElement = containerElement.Elements().FirstOrDefault(element => XNode.DeepEquals(element, elementToFind));
                if (existingElement == null) return;

                existingElement.Remove();

                using (var xmlWriter = XmlWriter.Create(dependenciesFilePath, DependenciesFileXmlWriterSettings))
                {
                    dependencies.Save(xmlWriter);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Unable to remove string from dependency file due to exception: " + exception.Message);
            }
        }
    }
}
