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

namespace DumpParts
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

            var weaponParts = new List<dynamic>();
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

                    var partListCollection = balanceDefinition.WeaponPartListCollection;
                    if (partListCollection == null)
                    {
                        throw new InvalidOperationException();
                    }

                    if (partListCollection != null)
                    {
                        AddCustomPartTypeData(weaponParts, partListCollection.BodyPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.GripPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.BarrelPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.SightPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.StockPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.ElementalPartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.Accessory1PartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.Accessory2PartData);
                        AddCustomPartTypeData(weaponParts, partListCollection.MaterialPartData);
                    }
                }
            }

            using (var output = new StreamWriter("Weapon Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (var weaponPart in weaponParts.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    writer.WritePropertyName(weaponPart.GetPath());
                    writer.WriteStartObject();

                    UnrealClass weaponPartClass = weaponPart.GetClass();
                    if (weaponPartClass.Path != "WillowGame.WeaponPartDefinition")
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponPart.TitleList != null &&
                        weaponPart.TitleList.Length > 0)
                    {
                        writer.WritePropertyName("titles");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> titleList = weaponPart.TitleList;
                        foreach (var title in titleList
                            .Where(tp => tp != null)
                            .OrderBy(tp => tp.GetPath()))
                        {
                            writer.WriteValue(title.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (weaponPart.PrefixList != null &&
                        weaponPart.PrefixList.Length > 0)
                    {
                        writer.WritePropertyName("prefixes");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> prefixList = weaponPart.PrefixList;
                        foreach (var prefix in prefixList
                            .Where(pp => pp != null)
                            .OrderBy(pp => pp.GetPath()))
                        {
                            writer.WriteValue(prefix.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    writer.WritePropertyName("type");
                    writer.WriteValue(((WeaponPartType)weaponPart.PartType).ToString());

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            var itemParts = new List<dynamic>();
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

                    var partListCollection = uclass == classModBalanceDefinitionClass
                                                 ? balanceDefinition.ItemPartListCollection
                                                 : balanceDefinition.PartListCollection;
                    if (partListCollection != null)
                    {
                        if (partListCollection.GetClass().Path != "WillowGame.ItemPartListCollectionDefinition")
                        {
                            throw new InvalidOperationException();
                        }

                        AddCustomPartTypeData(itemParts, partListCollection.AlphaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.BetaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.GammaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.DeltaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.EpsilonPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.ZetaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.EtaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.ThetaPartData);
                        AddCustomPartTypeData(itemParts, partListCollection.MaterialPartData);
                    }
                }
            }

            using (var output = new StreamWriter("Item Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (var itemPart in itemParts.Distinct().OrderBy(wp => wp.GetPath()))
                {
                    writer.WritePropertyName(itemPart.GetPath());
                    writer.WriteStartObject();

                    UnrealClass itemPartClass = itemPart.GetClass();
                    if (itemPartClass.Path != "WillowGame.ArtifactPartDefinition" &&
                        itemPartClass.Path != "WillowGame.ClassModPartDefinition" &&
                        itemPartClass.Path != "WillowGame.GrenadeModPartDefinition" &&
                        itemPartClass.Path != "WillowGame.ShieldPartDefinition" &&
                        itemPartClass.Path != "WillowGame.UsableItemPartDefinition")
                    {
                        throw new InvalidOperationException();
                    }
                }

                writer.WriteEndObject();
            }
        }

        private static void AddCustomPartTypeData(List<dynamic> weaponParts, dynamic customPartTypeData)
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
            foreach (var weightedPart in weightedParts)
            {
                weaponParts.Add(weightedPart.Part);
            }
        }
    }
}
