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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Unreflect.Core;
using Newtonsoft.Json;

namespace DumpFastTravelStations
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var fastTravelStationDefinitionClass = engine.GetClass("WillowGame.FastTravelStationDefinition");

            using (var output = new StreamWriter("Fast Travel Stations.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var fastTravelStationDefinitions = engine.Objects
                    .Where(o => o.IsA(fastTravelStationDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic fastTravelStationDefinition in fastTravelStationDefinitions)
                {
                    writer.WritePropertyName(fastTravelStationDefinition.GetPath());
                    writer.WriteStartObject();

                    string stationLevelName = fastTravelStationDefinition.StationLevelName;
                    if (string.IsNullOrEmpty(stationLevelName) == false)
                    {
                        writer.WritePropertyName("level_name");
                        writer.WriteValue(stationLevelName);
                    }

                    var dlcExpansion = fastTravelStationDefinition.DlcExpansion;
                    if (dlcExpansion != null)
                    {
                        writer.WritePropertyName("dlc_expansion");
                        writer.WriteValue(dlcExpansion.GetPath());
                    }

                    if (fastTravelStationDefinition.PreviousStation != null)
                    {
                        writer.WritePropertyName("previous_station");
                        writer.WriteValue(fastTravelStationDefinition.PreviousStation.GetPath());
                    }

                    string stationDisplayName = fastTravelStationDefinition.StationDisplayName;
                    if (string.IsNullOrEmpty(stationDisplayName) == false)
                    {
                        writer.WritePropertyName("display_name");
                        writer.WriteValue(stationDisplayName);
                    }

                    var missionDependencies = ((IEnumerable<dynamic>)fastTravelStationDefinition.MissionDependencies)
                        .Where(md => md.MissionDefinition != null)
                        .OrderBy(md => md.MissionDefinition.GetPath())
                        .ToArray();
                    if (missionDependencies.Length > 0)
                    {
                        writer.WritePropertyName("mission_dependencies");
                        writer.WriteStartArray();

                        foreach (var missionDependency in missionDependencies)
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("mission_definition");
                            writer.WriteValue(missionDependency.MissionDefinition.GetPath());

                            writer.WritePropertyName("mission_status");
                            writer.WriteValue(((MissionStatus)missionDependency.MissionStatus).ToString());

                            if ((bool)missionDependency.bIsObjectiveSpecific == true)
                            {
                                writer.WritePropertyName("is_objective_specific");
                                writer.WriteValue(true);

                                if (missionDependency.MissionObjective != null)
                                {
                                    writer.WritePropertyName("objective_definition");
                                    writer.WriteValue(missionDependency.MissionObjective.GetPath());
                                }

                                writer.WritePropertyName("objective_status");
                                writer.WriteValue(
                                    ((ObjectiveDependencyStatus)missionDependency.ObjectiveStatus).ToString());
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                    }

                    writer.WritePropertyName("initially_active");
                    writer.WriteValue((bool)fastTravelStationDefinition.bInitiallyActive);

                    writer.WritePropertyName("send_only");
                    writer.WriteValue((bool)fastTravelStationDefinition.bSendOnly);

                    string stationDescription = fastTravelStationDefinition.StationDescription;
                    if (string.IsNullOrEmpty(stationDescription) == false &&
                        stationDescription != "No Description" &&
                        stationDescription != stationDisplayName)
                    {
                        writer.WritePropertyName("description");
                        writer.WriteValue(stationDescription);
                    }

                    string stationSign = fastTravelStationDefinition.StationSign;
                    if (string.IsNullOrEmpty(stationSign) == false &&
                        stationSign != stationDisplayName)
                    {
                        writer.WritePropertyName("sign");
                        writer.WriteValue(stationSign);
                    }

                    if (fastTravelStationDefinition.InaccessibleObjective != null)
                    {
                        writer.WritePropertyName("inaccessible_objective");
                        writer.WriteValue(fastTravelStationDefinition.InaccessibleObjective.GetPath());
                    }

                    if (fastTravelStationDefinition.AccessibleObjective != null)
                    {
                        writer.WritePropertyName("accessible_objective");
                        writer.WriteValue(fastTravelStationDefinition.AccessibleObjective.GetPath());
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.Flush();
            }
        }
    }
}
