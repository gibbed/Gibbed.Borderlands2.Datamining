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

namespace DumpMissions
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var missionDefinitionClass = engine.GetClass("WillowGame.MissionDefinition");
            if (missionDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            Directory.CreateDirectory("dumps");

            using (var output = new StreamWriter(Path.Combine("dumps", "Missions.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var missionDefinitions = engine.Objects
                    .Where(o => o.IsA(missionDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic missionDefinition in missionDefinitions)
                {
                    writer.WritePropertyName(missionDefinition.GetPath());
                    writer.WriteStartObject();

                    writer.WritePropertyName("number");
                    writer.WriteValue(missionDefinition.MissionNumber);

                    string missionName = missionDefinition.MissionName;
                    if (string.IsNullOrEmpty(missionName) == false)
                    {
                        writer.WritePropertyName("name");
                        writer.WriteValue(missionName);
                    }

                    string missionDescription = missionDefinition.MissionDescription;
                    if (string.IsNullOrEmpty(missionDescription) == false)
                    {
                        writer.WritePropertyName("description");
                        writer.WriteValue(missionDescription);
                    }

                    writer.WritePropertyName("is_plot_critical");
                    writer.WriteValue(missionDefinition.bPlotCritical);

                    writer.WritePropertyName("can_be_failed");
                    writer.WriteValue(missionDefinition.bCanBeFailed);

                    // TODO: objective info

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.Flush();
            }
        }
    }
}
