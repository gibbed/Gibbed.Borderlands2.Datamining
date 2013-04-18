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

namespace DumpCustomizations
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var customizationDefinitionClass = engine.GetClass("WillowGame.CustomizationDefinition");
            if (customizationDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            Directory.CreateDirectory("dumps");

            using (var output = new StreamWriter(Path.Combine("dumps", "Customizations.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var customizationDefinitions = engine.Objects
                    .Where(o => o.IsA(customizationDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic customizationDefinition in customizationDefinitions)
                {
                    writer.WritePropertyName(customizationDefinition.GetPath());
                    writer.WriteStartObject();

                    string customizationName = customizationDefinition.CustomizationName;
                    if (customizationName == null)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName("name");
                    writer.WriteValue(customizationName);

                    UnrealClass customizationType = customizationDefinition.CustomizationType;
                    if (customizationType == null)
                    {
                        throw new InvalidOperationException();
                    }

                    if (_TypeMapping.ContainsKey(customizationType.Path) == false)
                    {
                        throw new NotSupportedException();
                    }

                    writer.WritePropertyName("type");
                    writer.WriteValue(_TypeMapping[customizationType.Path]);

                    var usageFlags = ((IEnumerable<UnrealClass>)customizationDefinition.UsageFlags).ToArray();

                    if (usageFlags.Length > 0)
                    {
                        writer.WritePropertyName("usage");
                        writer.WriteStartArray();
                        foreach (var usageFlag in usageFlags.OrderBy(uf => uf.Path))
                        {
                            if (_UsageFlagMapping.ContainsKey(usageFlag.Path) == false)
                            {
                                throw new NotSupportedException();
                            }

                            writer.WriteValue(_UsageFlagMapping[usageFlag.Path]);
                        }
                        writer.WriteEndArray();
                    }

                    var otherUsageFlags = customizationDefinition.OtherUsageFlags;
                    if (otherUsageFlags.Length > 0)
                    {
                        throw new NotSupportedException();
                    }

                    var dlcCustomizationSet = customizationDefinition.DlcCustomizationSetDef;
                    if (dlcCustomizationSet != null)
                    {
                        writer.WritePropertyName("dlc");
                        writer.WriteValue(dlcCustomizationSet.GetPath());
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();

                writer.Flush();
            }
        }

        private static readonly Dictionary<string, string> _TypeMapping = new Dictionary<string, string>()
        {
            {"WillowGame.CustomizationType_Head", "Head"},
            {"WillowGame.CustomizationType_Skin", "Skin"},
        };

        private static readonly Dictionary<string, string> _UsageFlagMapping = new Dictionary<string, string>()
        {
            {"WillowGame.CustomizationUsage_Assassin", "Assassin"},
            {"WillowGame.CustomizationUsage_Mercenary", "Mercenary"},
            {"WillowGame.CustomizationUsage_Soldier", "Soldier"},
            {"WillowGame.CustomizationUsage_Siren", "Siren"},
            {"WillowGame.CustomizationUsage_ExtraPlayerA", "Mechromancer"},
            {"WillowGame.CustomizationUsage_ExtraPlayerB", "Psycho"},
            {"WillowGame.CustomizationUsage_Runner", "Runner"},
            {"WillowGame.CustomizationUsage_BanditTech", "BanditTech"},
            {"WillowGame.CustomizationUsage_Hovercraft", "Hovercraft"},
            {"WillowGame.CustomizationUsage_FanBoat", "FanBoat"},
        };
    }
}
