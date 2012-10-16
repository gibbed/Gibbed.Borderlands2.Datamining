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

namespace DumpItems
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
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

            var weaponTypes = new List<dynamic>();
            {
                var balanceDefinitions = engine.Objects.Where(o => o.IsA(weaponBalanceDefinitionClass) &&
                                                                   o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balanceDefinition in balanceDefinitions)
                {
                    if (balanceDefinition.PartListCollection != null)
                    {
                        throw new NotSupportedException();
                    }

                    if (balanceDefinition.InventoryDefinition != null)
                    {
                        weaponTypes.Add(balanceDefinition.InventoryDefinition);
                    }

                    var partListCollection = balanceDefinition.WeaponPartListCollection;
                    if (partListCollection == null)
                    {
                        throw new InvalidOperationException();
                    }

                    if (partListCollection != null)
                    {
                        if (partListCollection.AssociatedWeaponType != null)
                        {
                            weaponTypes.Add(partListCollection.AssociatedWeaponType);
                        }
                    }
                }
            }

            using (var output = new StreamWriter("Weapon Types.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (var weaponType in weaponTypes.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    writer.WritePropertyName(weaponType.GetPath());
                    writer.WriteStartObject();

                    UnrealClass weaponPartClass = weaponType.GetClass();
                    if (weaponPartClass.Path != "WillowGame.WeaponTypeDefinition")
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName("type");
                    writer.WriteValue(((WeaponType)weaponType.WeaponType).ToString());

                    writer.WritePropertyName("name");
                    writer.WriteValue(weaponType.Typename);

                    if (weaponType.TitleList != null &&
                        weaponType.TitleList.Length > 0)
                    {
                        writer.WritePropertyName("titles");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> titleList = weaponType.TitleList;
                        foreach (var title in titleList
                            .Where(tp => tp != null)
                            .OrderBy(tp => tp.GetPath()))
                        {
                            writer.WriteValue(title.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (weaponType.PrefixList != null &&
                        weaponType.PrefixList.Length > 0)
                    {
                        writer.WritePropertyName("prefixes");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> prefixList = weaponType.PrefixList;
                        foreach (var prefix in prefixList
                            .Where(pp => pp != null)
                            .OrderBy(pp => pp.GetPath()))
                        {
                            writer.WriteValue(prefix.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    DumpCustomPartTypeData(writer, "body_parts", weaponType.BodyParts);
                    DumpCustomPartTypeData(writer, "grip_parts", weaponType.GripParts);
                    DumpCustomPartTypeData(writer, "barrel_parts", weaponType.BarrelParts);
                    DumpCustomPartTypeData(writer, "sight_parts", weaponType.SightParts);
                    DumpCustomPartTypeData(writer, "stock_parts", weaponType.StockParts);
                    DumpCustomPartTypeData(writer, "elemental_parts", weaponType.ElementalParts);
                    DumpCustomPartTypeData(writer, "accessory1_parts", weaponType.Accessory1Parts);
                    DumpCustomPartTypeData(writer, "accessory2_parts", weaponType.Accessory2Parts);
                    DumpCustomPartTypeData(writer, "material_parts", weaponType.MaterialParts);

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            var itemTypes = new List<dynamic>();
            {
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

                    if (uclass == classModBalanceDefinitionClass &&
                        balanceDefinition.ClassModDefinitions.Length > 0)
                    {
                        IEnumerable<dynamic> classModDefinitions = balanceDefinition.ClassModDefinitions;
                        itemTypes.AddRange(classModDefinitions.Where(cmd => cmd != null));
                    }

                    if (balanceDefinition.InventoryDefinition != null)
                    {
                        itemTypes.Add(balanceDefinition.InventoryDefinition);
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

                        if (partListCollection.AssociatedItem != null)
                        {
                            itemTypes.Add(partListCollection.AssociatedItem);
                        }
                    }
                }
            }

            using (var output = new StreamWriter("Item Types.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (var itemType in itemTypes.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    writer.WritePropertyName(itemType.GetPath());
                    writer.WriteStartObject();

                    UnrealClass itemPartClass = itemType.GetClass();
                    if (itemPartClass.Path != "WillowGame.UsableItemDefinition" &&
                        itemPartClass.Path != "WillowGame.ArtifactDefinition" &&
                        itemPartClass.Path != "WillowGame.UsableCustomizationItemDefinition" &&
                        itemPartClass.Path != "WillowGame.ClassModDefinition" &&
                        itemPartClass.Path != "WillowGame.GrenadeModDefinition" &&
                        itemPartClass.Path != "WillowGame.ShieldDefinition")
                    {
                        throw new InvalidOperationException();
                    }

                    if (string.IsNullOrEmpty((string)itemType.ItemName) == false)
                    {
                        writer.WritePropertyName("name");
                        writer.WriteValue(itemType.ItemName);
                    }

                    writer.WritePropertyName("type");
                    writer.WriteValue(_ItemTypeMapping[itemPartClass.Path]);

                    if (itemType.TitleList != null &&
                        itemType.TitleList.Length > 0)
                    {
                        writer.WritePropertyName("titles");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> titleList = itemType.TitleList;
                        foreach (var title in titleList
                            .Where(tp => tp != null)
                            .OrderBy(tp => tp.GetPath()))
                        {
                            writer.WriteValue(title.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (itemType.PrefixList != null &&
                        itemType.PrefixList.Length > 0)
                    {
                        writer.WritePropertyName("prefixes");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> prefixList = itemType.PrefixList;
                        foreach (var prefix in prefixList
                            .Where(pp => pp != null)
                            .OrderBy(pp => pp.GetPath()))
                        {
                            writer.WriteValue(prefix.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    DumpCustomPartTypeData(writer, "alpha_parts", itemType.AlphaParts);
                    DumpCustomPartTypeData(writer, "beta_parts", itemType.BetaParts);
                    DumpCustomPartTypeData(writer, "gamma_parts", itemType.GammaParts);
                    DumpCustomPartTypeData(writer, "delta_parts", itemType.DeltaParts);
                    DumpCustomPartTypeData(writer, "epsilon_parts", itemType.EpsilonParts);
                    DumpCustomPartTypeData(writer, "zeta_parts", itemType.ZetaParts);
                    DumpCustomPartTypeData(writer, "eta_parts", itemType.EtaParts);
                    DumpCustomPartTypeData(writer, "theta_parts", itemType.ThetaParts);
                    DumpCustomPartTypeData(writer, "material_parts", itemType.MaterialParts);

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }

        private static void DumpCustomPartTypeData(JsonWriter writer, string name, dynamic customPartTypeData)
        {
            if (customPartTypeData != null)
            {
                var weightedParts = ((IEnumerable<dynamic>)customPartTypeData.WeightedParts).ToArray();
                if (weightedParts.Length > 0)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();

                    foreach (var weightedPart in weightedParts
                        .Where(wp => wp.Part != null)
                        .OrderBy(wp => wp.Part.GetPath()))
                    {
                        writer.WriteValue(weightedPart.Part.GetPath());
                    }

                    writer.WriteEndArray();
                }
            }
        }

        private static Dictionary<string, string> _ItemTypeMapping = new Dictionary<string, string>()
        {
            {
                "WillowGame.ArtifactDefinition", "Artifact"
                },
            {
                "WillowGame.ClassModDefinition", "ClassMod"
                },
            {
                "WillowGame.GrenadeModDefinition", "GrenadeMod"
                },
            {
                "WillowGame.ShieldDefinition", "Shield"
                },
            {
                "WillowGame.UsableCustomizationItemDefinition", "UsableCustomizationItem"
                },
            {
                "WillowGame.UsableItemDefinition", "UsableItem"
                },
        };
    }
}
