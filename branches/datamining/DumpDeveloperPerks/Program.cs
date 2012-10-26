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
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Unreflect.Core;
using Newtonsoft.Json;

namespace DumpDeveloperPerks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var developerPerksDefinitionClass = engine.GetClass("WillowGame.DeveloperPerksDefinition");
            if (developerPerksDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            dynamic developerPerks = engine.Objects.FirstOrDefault(o => o.IsA(developerPerksDefinitionClass) &&
                                                                        o.GetName().StartsWith("Default__") == false);
            if (developerPerks == null)
            {
                throw new InvalidOperationException();
            }

            Directory.CreateDirectory("dumps");

            using (var output = new StreamWriter(Path.Combine("dumps", "Developer Perks.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                writer.WritePropertyName("developers");
                writer.WriteStartArray();
                foreach (var developerInfo in developerPerks.DeveloperInfo)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("gamertag");
                    writer.WriteValue(developerInfo.Gamertag);

                    writer.WritePropertyName("unique_id");
                    writer.WriteValue(developerInfo.UniqueId);

                    writer.WritePropertyName("platform");
                    writer.WriteValue(((DeveloperPerksPlatforms)developerInfo.Platform).ToString());

                    if (developerInfo.UnlocksGamerpics != null &&
                        developerInfo.UnlocksGamerpics.Length > 0)
                    {
                        writer.WritePropertyName("unlock_gamerpics");
                        writer.WriteStartArray();
                        foreach (var b in developerInfo.UnlocksGamerpics)
                        {
                            writer.WriteValue((byte)b);
                        }
                        writer.WriteEnd();
                    }

                    writer.WritePropertyName("eligible_for_gearbox_customizations");
                    writer.WriteValue(developerInfo.bEligibleForGearboxCustomizations);

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("perks");
                writer.WriteStartArray();
                foreach (var perkInfo in developerPerks.PerkInfo)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("button_chain");
                    writer.WriteStartArray();
                    foreach (var button in perkInfo.ButtonChain)
                    {
                        writer.WriteValue(button);
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("command");
                    writer.WriteValue(perkInfo.Command);

                    writer.WritePropertyName("must_be_developer");
                    writer.WriteValue(perkInfo.bMustBeDeveloper);

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("developer_customization_unlocks");
                writer.WriteStartArray();
                foreach (var developerCustomizationUnlock in developerPerks.DeveloperCustomizationUnlocks)
                {
                    if (developerCustomizationUnlock != null)
                    {
                        writer.WriteValue(developerCustomizationUnlock.GetPath());
                    }
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
                writer.Flush();
            }
        }
    }
}
