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

namespace DumpBalance
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

            Directory.CreateDirectory("dumps");

            using (var output = new StreamWriter(Path.Combine("dumps", "Weapon Balance.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var balanceDefinitions = engine.Objects.Where(o => o.IsA(weaponBalanceDefinitionClass) &&
                                                                   o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balanceDefinition in balanceDefinitions)
                {
                    writer.WritePropertyName(balanceDefinition.GetPath());
                    writer.WriteStartObject();

                    var baseDefinition = balanceDefinition.BaseDefinition;
                    if (baseDefinition != null)
                    {
                        writer.WritePropertyName("base");
                        writer.WriteValue(baseDefinition.GetPath());
                    }

                    var inventoryDefinition = balanceDefinition.InventoryDefinition;
                    if (inventoryDefinition != null)
                    {
                        writer.WritePropertyName("type");
                        writer.WriteValue(inventoryDefinition.GetPath());
                    }

                    var manufacturers = balanceDefinition.Manufacturers;
                    if (manufacturers != null &&
                        manufacturers.Length > 0)
                    {
                        writer.WritePropertyName("manufacturers");
                        writer.WriteStartArray();

                        foreach (
                            var manufacturer in
                                ((IEnumerable<dynamic>)manufacturers).Where(imbd => imbd.Manufacturer != null).OrderBy(
                                    imbd => imbd.Manufacturer.GetPath()))
                        {
                            writer.WriteValue(manufacturer.Manufacturer.GetPath());
                        }

                        writer.WriteEndArray();
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
                        writer.WritePropertyName("parts");
                        writer.WriteStartObject();

                        writer.WritePropertyName("mode");
                        writer.WriteValue(((PartReplacementMode)partListCollection.PartReplacementMode).ToString());

                        var associatedWeaponType = partListCollection.AssociatedWeaponType;
                        if (associatedWeaponType != null)
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue(associatedWeaponType.GetPath());
                        }

                        DumpWeaponCustomPartTypeData(writer, "body", partListCollection.BodyPartData);
                        DumpWeaponCustomPartTypeData(writer, "grip", partListCollection.GripPartData);
                        DumpWeaponCustomPartTypeData(writer, "barrel", partListCollection.BarrelPartData);
                        DumpWeaponCustomPartTypeData(writer, "sight", partListCollection.SightPartData);
                        DumpWeaponCustomPartTypeData(writer, "stock", partListCollection.StockPartData);
                        DumpWeaponCustomPartTypeData(writer, "elemental", partListCollection.ElementalPartData);
                        DumpWeaponCustomPartTypeData(writer, "accessory1", partListCollection.Accessory1PartData);
                        DumpWeaponCustomPartTypeData(writer, "accessory2", partListCollection.Accessory2PartData);
                        DumpWeaponCustomPartTypeData(writer, "material", partListCollection.MaterialPartData);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            using (var output = new StreamWriter(Path.Combine("dumps", "Item Balance.json"), false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

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

                    writer.WritePropertyName(balanceDefinition.GetPath());
                    writer.WriteStartObject();

                    var baseDefinition = balanceDefinition.BaseDefinition;
                    if (baseDefinition != null)
                    {
                        writer.WritePropertyName("base");
                        writer.WriteValue(baseDefinition.GetPath());
                    }

                    var inventoryDefinition = balanceDefinition.InventoryDefinition;
                    if (inventoryDefinition != null)
                    {
                        writer.WritePropertyName("type");
                        writer.WriteValue(inventoryDefinition.GetPath());
                    }

                    if (uclass == classModBalanceDefinitionClass &&
                        balanceDefinition.ClassModDefinitions.Length > 0)
                    {
                        dynamic[] classModDefinitions = balanceDefinition.ClassModDefinitions;

                        writer.WritePropertyName("types");
                        writer.WriteStartArray();
                        foreach (var classModDefinition in classModDefinitions.OrderBy(cmd => cmd.GetPath()))
                        {
                            writer.WriteValue(classModDefinition.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    var manufacturers = balanceDefinition.Manufacturers;
                    if (manufacturers != null &&
                        manufacturers.Length > 0)
                    {
                        writer.WritePropertyName("manufacturers");
                        writer.WriteStartArray();
                        foreach (
                            var manufacturer in
                                ((IEnumerable<dynamic>)manufacturers).Where(imbd => imbd.Manufacturer != null).OrderBy(
                                    imbd => imbd.Manufacturer.GetPath()))
                        {
                            writer.WriteValue(manufacturer.Manufacturer.GetPath());
                        }
                        writer.WriteEndArray();
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

                        writer.WritePropertyName("parts");
                        writer.WriteStartObject();

                        writer.WritePropertyName("mode");
                        writer.WriteValue(((PartReplacementMode)partListCollection.PartReplacementMode).ToString());

                        var associatedItem = partListCollection.AssociatedItem;
                        if (associatedItem != null)
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue(associatedItem.GetPath());
                        }

                        DumpItemCustomPartTypeData(writer, "alpha", partListCollection.AlphaPartData);
                        DumpItemCustomPartTypeData(writer, "beta", partListCollection.BetaPartData);
                        DumpItemCustomPartTypeData(writer, "gamma", partListCollection.GammaPartData);
                        DumpItemCustomPartTypeData(writer, "delta", partListCollection.DeltaPartData);
                        DumpItemCustomPartTypeData(writer, "epsilon", partListCollection.EpsilonPartData);
                        DumpItemCustomPartTypeData(writer, "zeta", partListCollection.ZetaPartData);
                        DumpItemCustomPartTypeData(writer, "eta", partListCollection.EtaPartData);
                        DumpItemCustomPartTypeData(writer, "theta", partListCollection.ThetaPartData);
                        DumpItemCustomPartTypeData(writer, "material", partListCollection.MaterialPartData);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }

        private static void DumpWeaponCustomPartTypeData(JsonWriter writer, string name, dynamic customPartTypeData)
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
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var weightedPart in weightedParts)
            {
                writer.WriteValue(weightedPart.Part.GetPath());
            }
            writer.WriteEndArray();
        }

        private static void DumpItemCustomPartTypeData(JsonWriter writer, string name, dynamic customPartTypeData)
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
            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var weightedPart in weightedParts)
            {
                writer.WriteValue(weightedPart.Part.GetPath());
            }
            writer.WriteEndArray();
        }
    }
}
