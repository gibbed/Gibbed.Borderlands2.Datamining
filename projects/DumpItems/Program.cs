/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
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
using System.Linq;
using Gibbed.Unreflect.Core;
using Newtonsoft.Json;
using Dataminer = Borderlands2Datamining.Dataminer;

namespace DumpItems
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new Dataminer().Run(args, Go);
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
                var balanceDefinitions = engine.Objects
                    .Where(o => (o.IsA(inventoryBalanceDefinitionClass) ||
                                 o.IsA(weaponBalanceDefinitionClass)) &&
                                o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balanceDefinition in balanceDefinitions)
                {
                    if (balanceDefinition.InventoryDefinition != null &&
                        balanceDefinition.InventoryDefinition.GetClass().Path == "WillowGame.WeaponTypeDefinition")
                    {
                        weaponTypes.Add(balanceDefinition.InventoryDefinition);
                    }

                    if (balanceDefinition.GetClass() == weaponBalanceDefinitionClass)
                    {
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
                            if (partListCollection.AssociatedWeaponType != null)
                            {
                                weaponTypes.Add(partListCollection.AssociatedWeaponType);
                            }
                        }
                    }
                }
            }

            var weaponPartLists = new List<dynamic>();
            using (var writer = Dataminer.NewDump("Weapon Types.json"))
            {
                writer.WriteStartObject();
                foreach (var weaponType in weaponTypes.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    writer.WritePropertyName(weaponType.GetPath());
                    writer.WriteStartObject();

                    UnrealClass weaponPartClass = weaponType.GetClass();
                    if (weaponPartClass.Path != "WillowGame.WeaponTypeDefinition" &&
                        weaponPartClass.Path != "WillowGame.BuzzaxeWeaponTypeDefinition")
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

                    WritePartListReference(writer, "body_parts", weaponType.BodyParts, weaponPartLists);
                    WritePartListReference(writer, "grip_parts", weaponType.GripParts, weaponPartLists);
                    WritePartListReference(writer, "barrel_parts", weaponType.BarrelParts, weaponPartLists);
                    WritePartListReference(writer, "sight_parts", weaponType.SightParts, weaponPartLists);
                    WritePartListReference(writer, "stock_parts", weaponType.StockParts, weaponPartLists);
                    WritePartListReference(writer, "elemental_parts", weaponType.ElementalParts, weaponPartLists);
                    WritePartListReference(writer, "accessory1_parts", weaponType.Accessory1Parts, weaponPartLists);
                    WritePartListReference(writer, "accessory2_parts", weaponType.Accessory2Parts, weaponPartLists);
                    WritePartListReference(writer, "material_parts", weaponType.MaterialParts, weaponPartLists);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            using (var writer = Dataminer.NewDump("Weapon Part Lists.json"))
            {
                writer.WriteStartObject();
                foreach (var partList in weaponPartLists.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    WritePartList(writer, partList.GetPath(), partList);
                }
                writer.WriteEndObject();
            }

            var items = new List<dynamic>();
            {
                var balances = engine.Objects
                    .Where(o => (o.IsA(inventoryBalanceDefinitionClass) ||
                                 o.IsA(itemBalanceDefinitionClass) ||
                                 o.IsA(classModBalanceDefinitionClass)) &&
                                o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balance in balances)
                {
                    var balanceClass = balance.GetClass();
                    if (balanceClass != inventoryBalanceDefinitionClass &&
                        balanceClass != itemBalanceDefinitionClass &&
                        balanceClass != classModBalanceDefinitionClass)
                    {
                        throw new NotSupportedException();
                    }

                    if (balanceClass == classModBalanceDefinitionClass &&
                        balance.ClassModDefinitions.Length > 0)
                    {
                        IEnumerable<dynamic> classModDefinitions = balance.ClassModDefinitions;
                        items.AddRange(classModDefinitions.Where(cmd => cmd != null));
                    }

                    if (balance.InventoryDefinition != null &&
                        balance.InventoryDefinition.GetClass().Path != "WillowGame.WeaponTypeDefinition")
                    {
                        items.Add(balance.InventoryDefinition);
                    }

                    var partListCollection = balanceClass == classModBalanceDefinitionClass
                                                 ? balance.ItemPartListCollection
                                                 : balance.PartListCollection;
                    if (partListCollection != null)
                    {
                        if (partListCollection.GetClass().Path != "WillowGame.ItemPartListCollectionDefinition")
                        {
                            throw new InvalidOperationException();
                        }

                        if (partListCollection.AssociatedItem != null)
                        {
                            items.Add(partListCollection.AssociatedItem);
                        }
                    }
                }
            }

            var itemPartLists = new List<dynamic>();
            using (var writer = Dataminer.NewDump("Items.json"))
            {
                writer.WriteStartObject();
                foreach (var item in items.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    UnrealClass itemClass = item.GetClass();

                    if (itemClass.Path != "WillowGame.UsableItemDefinition" &&
                        itemClass.Path != "WillowGame.ArtifactDefinition" &&
                        itemClass.Path != "WillowGame.UsableCustomizationItemDefinition" &&
                        itemClass.Path != "WillowGame.ClassModDefinition" &&
                        itemClass.Path != "WillowGame.GrenadeModDefinition" &&
                        itemClass.Path != "WillowGame.ShieldDefinition" &&
                        itemClass.Path != "WillowGame.MissionItemDefinition" &&
                        itemClass.Path != "WillowGame.CrossDLCClassModDefinition")
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(item.GetPath());
                    writer.WriteStartObject();

                    var itemName = (string)item.ItemName;
                    if (string.IsNullOrEmpty(itemName) == false &&
                        itemName != "None")
                    {
                        writer.WritePropertyName("name");
                        writer.WriteValue(itemName);
                    }
                    else if (itemClass.Path == "WillowGame.UsableCustomizationItemDefinition")
                    {
                        var customizationDef = item.CustomizationDef;
                        if (string.IsNullOrEmpty((string)customizationDef.CustomizationName) == false)
                        {
                            writer.WritePropertyName("name");
                            writer.WriteValue(customizationDef.CustomizationName);
                        }
                    }

                    if ((bool)item.bItemNameIsFullName == true)
                    {
                        writer.WritePropertyName("has_full_name");
                        writer.WriteValue(item.bItemNameIsFullName);
                    }

                    writer.WritePropertyName("type");
                    writer.WriteValue(_ItemTypeMapping[itemClass.Path]);

                    if (item.TitleList != null &&
                        item.TitleList.Length > 0)
                    {
                        writer.WritePropertyName("titles");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> titleList = item.TitleList;
                        foreach (var title in titleList
                            .Where(tp => tp != null)
                            .OrderBy(tp => tp.GetPath()))
                        {
                            writer.WriteValue(title.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (item.PrefixList != null &&
                        item.PrefixList.Length > 0)
                    {
                        writer.WritePropertyName("prefixes");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> prefixList = item.PrefixList;
                        foreach (var prefix in prefixList
                            .Where(pp => pp != null)
                            .OrderBy(pp => pp.GetPath()))
                        {
                            writer.WriteValue(prefix.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    WritePartListReference(writer, "alpha_parts", item.AlphaParts, itemPartLists);
                    WritePartListReference(writer, "beta_parts", item.BetaParts, itemPartLists);
                    WritePartListReference(writer, "gamma_parts", item.GammaParts, itemPartLists);
                    WritePartListReference(writer, "delta_parts", item.DeltaParts, itemPartLists);
                    WritePartListReference(writer, "epsilon_parts", item.EpsilonParts, itemPartLists);
                    WritePartListReference(writer, "zeta_parts", item.ZetaParts, itemPartLists);
                    WritePartListReference(writer, "eta_parts", item.EtaParts, itemPartLists);
                    WritePartListReference(writer, "theta_parts", item.ThetaParts, itemPartLists);
                    WritePartListReference(writer, "material_parts", item.MaterialParts, itemPartLists);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            using (var writer = Dataminer.NewDump("Item Part Lists.json"))
            {
                writer.WriteStartObject();
                foreach (var partList in itemPartLists.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    WritePartList(writer, partList.GetPath(), partList);
                }
                writer.WriteEndObject();
            }
        }

        private static void WritePartListReference(JsonWriter writer, string name, dynamic partList, List<dynamic> partLists)
        {
            if (partList != null)
            {
                partLists.Add(partList);
                writer.WritePropertyName(name);
                writer.WriteValue(partList.GetPath());
            }
        }

        private static void WritePartList(JsonWriter writer, string name, dynamic data)
        {
            if (data == null)
            {
                return;
            }

            var weightedParts = ((IEnumerable<dynamic>)data.WeightedParts).ToArray();
            if (weightedParts.Length == 0)
            {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var weightedPartPath in weightedParts
                .Where(wp => wp.Part != null)
                .Select(wp => (string)wp.Part.GetPath())
                .OrderBy(wpp => wpp))
            {
                writer.WriteValue(weightedPartPath);
            }
            writer.WriteEndArray();
        }

        private static Dictionary<string, string> _ItemTypeMapping = new Dictionary<string, string>()
        {
            { "WillowGame.ArtifactDefinition", "Artifact" },
            { "WillowGame.ClassModDefinition", "ClassMod" },
            { "WillowGame.GrenadeModDefinition", "GrenadeMod" },
            { "WillowGame.MissionItemDefinition", "MissionItem" },
            { "WillowGame.ShieldDefinition", "Shield" },
            { "WillowGame.UsableCustomizationItemDefinition", "UsableCustomizationItem" },
            { "WillowGame.UsableItemDefinition", "UsableItem" },
            { "WillowGame.CrossDLCClassModDefinition", "CrossDLCClassMod" },
        };
    }
}
