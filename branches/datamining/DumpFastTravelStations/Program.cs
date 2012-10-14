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
            {
                output.WriteLine("{");

                var fastTravelStationDefinitions = engine.Objects
                    .Where(o => o.IsA(fastTravelStationDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic fastTravelStationDefinition in fastTravelStationDefinitions)
                {
                    output.WriteLine("  \"{0}\":", fastTravelStationDefinition.GetPath());
                    output.WriteLine("  {");

                    string stationLevelName = fastTravelStationDefinition.StationLevelName;
                    if (string.IsNullOrEmpty(stationLevelName) == false)
                    {
                        output.WriteLine("    level_name: \"{0}\",", stationLevelName);
                    }

                    var dlcExpansion = fastTravelStationDefinition.DlcExpansion;
                    if (dlcExpansion != null)
                    {
                        output.WriteLine("    dlc_expansion: \"{0}\",", dlcExpansion.GetPath());
                    }

                    if (fastTravelStationDefinition.PreviousStation != null)
                    {
                        output.WriteLine("    previous_station: \"{0}\",",
                                         fastTravelStationDefinition.PreviousStation.GetPath());
                    }

                    string stationDisplayName = fastTravelStationDefinition.StationDisplayName;
                    if (string.IsNullOrEmpty(stationDisplayName) == false)
                    {
                        output.WriteLine("    display_name: \"{0}\",", stationDisplayName);
                    }

                    var missionDependencies = ((IEnumerable<dynamic>)fastTravelStationDefinition.MissionDependencies)
                        .Where(md => md.MissionDefinition != null)
                        .OrderBy(md => md.MissionDefinition.GetPath())
                        .ToArray();
                    if (missionDependencies.Length > 0)
                    {
                        output.WriteLine("    mission_dependencies:");
                        output.WriteLine("    [");

                        foreach (var missionDependency in missionDependencies)
                        {
                            output.WriteLine("      {");

                            if (missionDependency.MissionDefinition != null)
                            {
                                output.WriteLine("        mission_definition: \"{0}\",",
                                                 missionDependency.MissionDefinition.GetPath());
                            }

                            output.WriteLine("        mission_status: \"{0}\",",
                                             (MissionStatus)missionDependency.MissionStatus);
                            if ((bool)missionDependency.bIsObjectiveSpecific == true)
                            {
                                output.WriteLine("        is_objective_specific: {0},",
                                                 missionDependency.bIsObjectiveSpecific.ToString().ToLowerInvariant());

                                if (missionDependency.MissionObjective != null)
                                {
                                    output.WriteLine("        objective_definition: \"{0}\",",
                                                     missionDependency.MissionObjective.GetPath());
                                }

                                output.WriteLine("        objective_status: \"{0}\",",
                                                 (ObjectiveDependencyStatus)missionDependency.ObjectiveStatus);
                            }

                            output.WriteLine("      },");
                        }

                        output.WriteLine("    ],");
                    }

                    output.WriteLine("    initially_active: {0},",
                                     fastTravelStationDefinition.bInitiallyActive.ToString().ToLowerInvariant());
                    output.WriteLine("    send_only: {0},",
                                     fastTravelStationDefinition.bSendOnly.ToString().ToLowerInvariant());

                    string stationDescription = fastTravelStationDefinition.StationDescription;
                    if (string.IsNullOrEmpty(stationDescription) == false &&
                        stationDescription != "No Description" &&
                        stationDescription != stationDisplayName)
                    {
                        output.WriteLine("    description: \"{0}\",", stationDescription);
                    }

                    string stationSign = fastTravelStationDefinition.StationSign;
                    if (string.IsNullOrEmpty(stationSign) == false &&
                        stationSign != stationDisplayName)
                    {
                        output.WriteLine("    sign: \"{0}\",", stationSign);
                    }

                    if (fastTravelStationDefinition.InaccessibleObjective != null)
                    {
                        output.WriteLine("    inaccessible_objective: \"{0}\",",
                                         fastTravelStationDefinition.InaccessibleObjective.GetPath());
                    }

                    if (fastTravelStationDefinition.AccessibleObjective != null)
                    {
                        output.WriteLine("    accessible_objective: \"{0}\",",
                                         fastTravelStationDefinition.AccessibleObjective.GetPath());
                    }

                    output.WriteLine("  },");
                }

                output.WriteLine("}");
            }
        }
    }
}
