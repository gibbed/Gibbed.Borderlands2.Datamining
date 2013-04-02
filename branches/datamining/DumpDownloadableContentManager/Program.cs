/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Unreflect.Core;
using Newtonsoft.Json;

namespace DumpDownloadableContentManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var willowDownloadableContentManagerClass = engine.GetClass("WillowGame.WillowDownloadableContentManager");
            var downloadablePackageDefinitionClass = engine.GetClass("WillowGame.DownloadablePackageDefinition");
            if (willowDownloadableContentManagerClass == null ||
                downloadablePackageDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            Directory.CreateDirectory("dumps");

            using (var output = new StreamWriter(Path.Combine("dumps", "Downloadable Contents.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var willowDownloadableContentManagers = engine.Objects
                    .Where(o => o.IsA(willowDownloadableContentManagerClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath())
                    .ToArray();

                if (willowDownloadableContentManagers.Length != 1)
                {
                    throw new InvalidOperationException();
                }

                dynamic willowDownloadableContentManager = willowDownloadableContentManagers.First();
                var allContent = willowDownloadableContentManager.AllContent;

                foreach (var content in allContent)
                {
                    writer.WritePropertyName(content.GetPath());
                    writer.WriteStartObject();

                    UnrealClass uclass = content.GetClass();
                    if (uclass.Path != "WillowGame.DownloadableExpansionDefinition" &&
                        uclass.Path != "WillowGame.DownloadableCustomizationSetDefinition" &&
                        uclass.Path != "WillowGame.DownloadableItemSetDefinition" &&
                        uclass.Path != "WillowGame.DownloadableVehicleDefinition" &&
                        uclass.Path != "WillowGame.DownloadableCharacterDefinition" &&
                        uclass.Path != "WillowGame.DownloadableBalanceModifierDefinition")
                    {
                        throw new NotSupportedException();
                    }

                    writer.WritePropertyName("id"); // content_id
                    writer.WriteValue(content.ContentId);

                    writer.WritePropertyName("name"); // content_display_name
                    writer.WriteValue(content.ContentDisplayName);

                    if (content.PackageDef == null)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName("package");
                    writer.WriteValue(content.PackageDef.GetPath());

                    writer.WritePropertyName("type");
                    writer.WriteValue(_ContentTypeMapping[uclass.Path]);

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.Flush();
            }

            using (var output = new StreamWriter(Path.Combine("dumps", "Downloadable Packages.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var downloadablePackageDefinitions = engine.Objects
                    .Where(o => o.IsA(downloadablePackageDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic downloadablePackageDefinition in downloadablePackageDefinitions)
                {
                    writer.WritePropertyName(downloadablePackageDefinition.GetPath());
                    writer.WriteStartObject();

                    writer.WritePropertyName("id");
                    writer.WriteValue(downloadablePackageDefinition.PackageId);

                    writer.WritePropertyName("dlc_name");
                    writer.WriteValue(downloadablePackageDefinition.DLCName);

                    writer.WritePropertyName("display_name");
                    writer.WriteValue(downloadablePackageDefinition.PackageDisplayName);

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.Flush();
            }
        }

        private static readonly Dictionary<string, string> _ContentTypeMapping = new Dictionary<string, string>()
        {
            {"WillowGame.DownloadableExpansionDefinition", "Expansion"},
            {"WillowGame.DownloadableCustomizationSetDefinition", "CustomizationSet"},
            {"WillowGame.DownloadableItemSetDefinition", "ItemSet"},
            {"WillowGame.DownloadableVehicleDefinition", "Vehicle"},
            {"WillowGame.DownloadableCharacterDefinition", "Character"},
            {"WillowGame.DownloadableBalanceModifierDefinition", "BalanceModifier"},
        };
    }
}
