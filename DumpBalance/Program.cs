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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Unreflect.Core;
using Gibbed.Unreflect.Runtime;

namespace DumpBalance
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var config = Configuration.Load("Borderlands 2.json");

            var processes = Process.GetProcessesByName("borderlands2");
            if (processes.Length == 0)
            {
                return;
            }

            var process = processes.Last();
            config.AdjustAddresses(process.MainModule);

            using (var runtime = new RuntimeProcess())
            {
                if (runtime.OpenProcess(process) == false)
                {
                    return;
                }

                runtime.SuspendThreads();

                var engine = new Engine(config, runtime);

                var weaponBalanceDefinitionClass = engine.GetClass("WillowGame.WeaponBalanceDefinition");
                var missionWeaponBalanceDefinitionClass = engine.GetClass("WillowGame.MissionWeaponBalanceDefinition");
                var inventoryBalanceDefinitionClass = engine.GetClass("WillowGame.InventoryBalanceDefinition");
                var itemBalanceDefinitionClass = engine.GetClass("WillowGame.ItemBalanceDefinition");
                var classModBalanceDefinitionClass = engine.GetClass("WillowGame.ClassModBalanceDefinition");
                if (weaponBalanceDefinitionClass == null ||
                    missionWeaponBalanceDefinitionClass == null ||
                    inventoryBalanceDefinitionClass == null ||
                    itemBalanceDefinitionClass == null ||
                    classModBalanceDefinitionClass == null)
                {
                    throw new InvalidOperationException();
                }

                using (var output = new StreamWriter("Weapon Balance.json", false, Encoding.Unicode))
                {
                    output.WriteLine("{");

                    var balanceDefinitions = engine.Objects.Where(o => o.IsA(weaponBalanceDefinitionClass) &&
                                                                       o.GetName().StartsWith("Default__") == false)
                        .OrderBy(o => o.GetPath());
                    foreach (dynamic balanceDefinition in balanceDefinitions)
                    {
                        output.WriteLine("  \"{0}\":", balanceDefinition.GetPath());
                        output.WriteLine("  {");

                        var baseDefinition = balanceDefinition.BaseDefinition;
                        if (baseDefinition != null)
                        {
                            output.WriteLine("    base: \"{0}\",", baseDefinition.GetPath());
                        }

                        var inventoryDefinition = balanceDefinition.InventoryDefinition;
                        if (inventoryDefinition != null)
                        {
                            output.WriteLine("    types: [\"{0}\"],", inventoryDefinition.GetPath());
                        }

                        if (balanceDefinition.PartListCollection != null)
                        {
                            throw new NotSupportedException();
                        }

                        var partListCollection = balanceDefinition.WeaponPartListCollection;
                        if (partListCollection == null)
                        {
                            throw new InvalidOperationException();
                        }

                        if (partListCollection != null)
                        {
                            output.WriteLine("    parts:");
                            output.WriteLine("    {");

                            output.WriteLine("      mode: \"{0}\",",
                                             (PartReplacementMode)partListCollection.PartReplacementMode);

                            var associatedWeaponType = partListCollection.AssociatedWeaponType;
                            if (associatedWeaponType != null)
                            {
                                output.WriteLine("      type: \"{0}\",", associatedWeaponType.GetPath());
                            }

                            DumpWeaponCustomPartTypeData(output, "body", partListCollection.BodyPartData);
                            DumpWeaponCustomPartTypeData(output, "grip", partListCollection.GripPartData);
                            DumpWeaponCustomPartTypeData(output, "barrel", partListCollection.BarrelPartData);
                            DumpWeaponCustomPartTypeData(output, "sight", partListCollection.SightPartData);
                            DumpWeaponCustomPartTypeData(output, "stock", partListCollection.StockPartData);
                            DumpWeaponCustomPartTypeData(output, "elemental", partListCollection.ElementalPartData);
                            DumpWeaponCustomPartTypeData(output, "accessory1", partListCollection.Accessory1PartData);
                            DumpWeaponCustomPartTypeData(output, "accessory2", partListCollection.Accessory2PartData);
                            DumpWeaponCustomPartTypeData(output, "material", partListCollection.MaterialPartData);

                            output.WriteLine("    },");
                        }

                        output.WriteLine("  },");
                    }

                    output.WriteLine("}");
                }

                using (var output = new StreamWriter("Item Balance.json", false, Encoding.Unicode))
                {
                    output.WriteLine("{");

                    var balanceDefinitions = engine.Objects.Where(
                        o =>
                        (o.IsA(inventoryBalanceDefinitionClass) || o.IsA(itemBalanceDefinitionClass) ||
                         o.IsA(classModBalanceDefinitionClass)) &&
                        o.GetName().StartsWith("Default__") == false)
                        .OrderBy(o => o.GetPath());
                    foreach (dynamic balanceDefinition in balanceDefinitions)
                    {
                        var uclass = balanceDefinition.GetClass();

                        if (uclass != inventoryBalanceDefinitionClass &&
                            uclass != classModBalanceDefinitionClass)
                        {
                            throw new NotSupportedException();
                        }

                        output.WriteLine("  \"{0}\":", balanceDefinition.GetPath());
                        output.WriteLine("  {");

                        var baseDefinition = balanceDefinition.BaseDefinition;
                        if (baseDefinition != null)
                        {
                            output.WriteLine("    base: \"{0}\",", baseDefinition.GetPath());
                        }

                        if (uclass == classModBalanceDefinitionClass &&
                            balanceDefinition.ClassModDefinitions.Length > 0)
                        {
                            dynamic[] classModDefinitions = balanceDefinition.ClassModDefinitions;

                            if (classModDefinitions.Length > 1)
                            {
                                output.WriteLine("    types:");
                                output.WriteLine("    [");

                                foreach (var classModDefinition in classModDefinitions.OrderBy(cmd => cmd.GetPath()))
                                {
                                    output.WriteLine("      \"{0}\",", classModDefinition.GetPath());
                                }

                                output.WriteLine("    ],");
                            }
                            else
                            {
                                output.WriteLine("    types: [\"{0}\"],", classModDefinitions[0].GetPath());
                            }
                        }
                        else
                        {
                            var inventoryDefinition = balanceDefinition.InventoryDefinition;
                            if (inventoryDefinition != null)
                            {
                                output.WriteLine("    types: [\"{0}\"],", inventoryDefinition.GetPath());
                            }
                        }

                        var partListCollection = uclass == classModBalanceDefinitionClass
                                                     ? balanceDefinition.ItemPartListCollection
                                                     : balanceDefinition.PartListCollection;
                        if (partListCollection != null)
                        {
                            if (partListCollection.GetClass().Path != "WillowGame.ItemPartListCollectionDefinition")
                            {
                                throw new InvalidOperationException();
                            }

                            output.WriteLine("    parts:");
                            output.WriteLine("    {");
                            output.WriteLine("      mode: \"{0}\",",
                                             (PartReplacementMode)partListCollection.PartReplacementMode);

                            var associatedItem = partListCollection.AssociatedItem;
                            if (associatedItem != null)
                            {
                                output.WriteLine("      type: \"{0}\",", associatedItem.GetPath());
                            }

                            DumpItemCustomPartTypeData(output, "alpha", partListCollection.AlphaPartData);
                            DumpItemCustomPartTypeData(output, "beta", partListCollection.BetaPartData);
                            DumpItemCustomPartTypeData(output, "gamma", partListCollection.GammaPartData);
                            DumpItemCustomPartTypeData(output, "delta", partListCollection.DeltaPartData);
                            DumpItemCustomPartTypeData(output, "epsilon", partListCollection.EpsilonPartData);
                            DumpItemCustomPartTypeData(output, "zeta", partListCollection.ZetaPartData);
                            DumpItemCustomPartTypeData(output, "eta", partListCollection.EtaPartData);
                            DumpItemCustomPartTypeData(output, "theta", partListCollection.ThetaPartData);
                            DumpItemCustomPartTypeData(output, "material", partListCollection.MaterialPartData);

                            output.WriteLine("    },");
                        }

                        output.WriteLine("  },");
                    }

                    output.WriteLine("}");
                }

                runtime.ResumeThreads();
                runtime.CloseProcess();
            }
        }

        private static void DumpWeaponCustomPartTypeData(StreamWriter output, string name, dynamic customPartTypeData)
        {
            if (customPartTypeData == null)
            {
                throw new ArgumentNullException("customPartTypeData");
            }

            if ((bool)customPartTypeData.bEnabled == false)
            {
                return;
            }

            dynamic[] weightedParts = customPartTypeData.WeightedParts;
            if (weightedParts.Length > 1)
            {
                output.WriteLine("      {0}:", name);
                output.WriteLine("      [");

                foreach (var weightedPart in weightedParts)
                {
                    output.WriteLine("        \"{0}\",", weightedPart.Part.GetPath());
                }

                output.WriteLine("      ],");
            }
            else if (weightedParts.Length == 1)
            {
                output.WriteLine("      {0}: [\"{1}\"],",
                                 name,
                                 weightedParts[0].Part.GetPath());
            }
            else
            {
                output.WriteLine("      {0}: [],", name);
            }
        }

        private static void DumpItemCustomPartTypeData(StreamWriter output, string name, dynamic customPartTypeData)
        {
            if (customPartTypeData == null)
            {
                throw new ArgumentNullException("customPartTypeData");
            }

            if ((bool)customPartTypeData.bEnabled == false)
            {
                return;
            }

            dynamic[] weightedParts = customPartTypeData.WeightedParts;
            if (weightedParts.Length > 1)
            {
                output.WriteLine("      {0}:", name);
                output.WriteLine("      [");

                foreach (var weightedPart in weightedParts)
                {
                    output.WriteLine("        \"{0}\",", weightedPart.Part.GetPath());
                }

                output.WriteLine("      ],");
            }
            else if (weightedParts.Length == 1)
            {
                output.WriteLine("      {0}: [\"{1}\"],",
                                 name,
                                 weightedParts[0].Part.GetPath());
            }
            else
            {
                output.WriteLine("      {0}: [],", name);
            }
        }
    }
}
