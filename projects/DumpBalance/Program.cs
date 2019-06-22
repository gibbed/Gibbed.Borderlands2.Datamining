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

namespace DumpBalance
{
    internal class Program
    {
        public static readonly MultiSetComparer<string> StringComparer;

        static Program()
        {
            StringComparer = new MultiSetComparer<string>();
        }

        private static void Main(string[] args)
        {
            new Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var inventoryBalanceClass = engine.GetClass("WillowGame.InventoryBalanceDefinition");
            var weaponBalanceClass = engine.GetClass("WillowGame.WeaponBalanceDefinition");
            var missionWeaponBalanceClass = engine.GetClass("WillowGame.MissionWeaponBalanceDefinition");
            var itemBalanceClass = engine.GetClass("WillowGame.ItemBalanceDefinition");
            var classModBalanceClass = engine.GetClass("WillowGame.ClassModBalanceDefinition");
            if (inventoryBalanceClass == null ||
                weaponBalanceClass == null ||
                missionWeaponBalanceClass == null ||
                itemBalanceClass == null ||
                classModBalanceClass == null)
            {
                throw new InvalidOperationException();
            }

            var weaponBalancePartLists = new List<KeyValuePair<string, dynamic>>();
            using (var writer = Dataminer.NewDump("Weapon Balance.json"))
            {
                writer.WriteStartObject();
                var balances = engine.Objects
                    .Where(o => o.IsA(weaponBalanceClass) &&
                                o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balance in balances)
                {
                    var balancePath = (string)balance.GetPath();

                    writer.WritePropertyName(balancePath);
                    writer.WriteStartObject();

                    var baseBalance = balance.BaseDefinition;
                    if (baseBalance != null)
                    {
                        writer.WritePropertyName("base");
                        writer.WriteValue(baseBalance.GetPath());
                    }

                    var typePath = (string)balance.InventoryDefinition?.GetPath();
                    var baseTypePath = (string)balance.BaseDefinition?.InventoryDefinition?.GetPath();
                    if (typePath != null && (baseTypePath == null || typePath != baseTypePath))
                    {
                        writer.WritePropertyName("weapon_type");
                        writer.WriteValue(typePath);
                    }

                    var manufacturers = balance.Manufacturers;
                    if (manufacturers != null && manufacturers.Length > 0)
                    {
                        writer.WritePropertyName("manufacturers");
                        writer.WriteStartArray();
                        foreach (var manufacturer in ((IEnumerable<dynamic>)manufacturers)
                            .Where(imbd => imbd.Manufacturer != null)
                            .OrderBy(imbd => imbd.Manufacturer.GetPath()))
                        {
                            writer.WriteValue(manufacturer.Manufacturer.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (balance.PartListCollection != null)
                    {
                        throw new NotSupportedException();
                    }

                    var weaponPartList = balance.WeaponPartListCollection;
                    if (weaponPartList == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var runtimePartList = balance.RuntimePartListCollection;
                    if (runtimePartList == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var weaponPartListPath = (string)weaponPartList.GetPath();
                    weaponBalancePartLists.Add(new KeyValuePair<string, dynamic>(weaponPartListPath, balance));

                    writer.WritePropertyName("parts");
                    writer.WriteValue(weaponPartListPath);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            using (var writer = Dataminer.NewDump("Weapon Balance Part Lists.json"))
            {
                writer.WriteStartObject();
                foreach (var kv in weaponBalancePartLists)
                {
                    var partListPath = kv.Key;
                    var balance = kv.Value;

                    var partList = balance.RuntimePartListCollection;
                    var baseBalance = balance.BaseDefinition;
                    var basePartList = baseBalance == null ? null : baseBalance.RuntimePartListCollection;

                    PartReplacementMode? mode = null;
                    var bodyPartData = BuildPartList(partList.BodyPartData, basePartList?.BodyPartData, ref mode);
                    var gripPartData = BuildPartList(partList.GripPartData, basePartList?.GripPartData, ref mode);
                    var barrelPartData = BuildPartList(partList.BarrelPartData, basePartList?.BarrelPartData, ref mode);
                    var sightPartData = BuildPartList(partList.SightPartData, basePartList?.SightPartData, ref mode);
                    var stockPartData = BuildPartList(partList.StockPartData, basePartList?.StockPartData, ref mode);
                    var elementalPartData = BuildPartList(partList.ElementalPartData, basePartList?.ElementalPartData, ref mode);
                    var accessory1PartData = BuildPartList(partList.Accessory1PartData, basePartList?.Accessory1PartData, ref mode);
                    var accessory2PartData = BuildPartList(partList.Accessory2PartData, basePartList?.Accessory2PartData, ref mode);
                    var materialPartData = BuildPartList(partList.MaterialPartData, basePartList?.MaterialPartData, ref mode);

                    if (mode == null)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(partListPath);
                    writer.WriteStartObject();

                    writer.WritePropertyName("mode");
                    writer.WriteValue(mode.ToString());

                    var associatedWeaponTypePath = (string)partList.AssociatedWeaponType?.GetPath();
                    var baseAssociatedWeaponTypePath = (string)basePartList?.AssociatedWeaponType?.GetPath();
                    if (associatedWeaponTypePath != null &&
                        (baseAssociatedWeaponTypePath == null || associatedWeaponTypePath != baseAssociatedWeaponTypePath))
                    {
                        writer.WritePropertyName("weapon_type");
                        writer.WriteValue(associatedWeaponTypePath);
                    }

                    WriteStrings(writer, "body", bodyPartData);
                    WriteStrings(writer, "grip", gripPartData);
                    WriteStrings(writer, "barrel", barrelPartData);
                    WriteStrings(writer, "sight", sightPartData);
                    WriteStrings(writer, "stock", stockPartData);
                    WriteStrings(writer, "elemental", elementalPartData);
                    WriteStrings(writer, "accessory1", accessory1PartData);
                    WriteStrings(writer, "accessory2", accessory2PartData);
                    WriteStrings(writer, "material", materialPartData);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            var itemBalancePartLists = new List<KeyValuePair<string, dynamic>>();
            using (var writer = Dataminer.NewDump("Item Balance.json"))
            {
                writer.WriteStartObject();
                var balances = engine.Objects
                    .Where(o => (o.IsA(inventoryBalanceClass) == true ||
                                 o.IsA(itemBalanceClass) == true ||
                                 o.IsA(classModBalanceClass) == true) &&
                                o.GetName().StartsWith("Default__") == false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic balance in balances)
                {
                    var balanceClass = balance.GetClass();
                    if (balanceClass != inventoryBalanceClass &&
                        balanceClass != itemBalanceClass &&
                        balanceClass != classModBalanceClass)
                    {
                        throw new NotSupportedException();
                    }

                    var balancePath = (string)balance.GetPath();

                    writer.WritePropertyName(balancePath);
                    writer.WriteStartObject();

                    var baseBalance = balance.BaseDefinition;
                    if (baseBalance != null)
                    {
                        writer.WritePropertyName("base");
                        writer.WriteValue(baseBalance.GetPath());
                    }

                    var itemPath = (string)balance.InventoryDefinition?.GetPath();
                    var baseItemPath = (string)balance.BaseDefinition?.InventoryDefinition?.GetPath();
                    if (itemPath != null && (baseItemPath == null || itemPath != baseItemPath))
                    {
                        writer.WritePropertyName("item");
                        writer.WriteValue(itemPath);
                    }

                    if (balanceClass == classModBalanceClass &&
                        balance.ClassModDefinitions.Length > 0)
                    {
                        dynamic[] classMods = balance.ClassModDefinitions;
                        writer.WritePropertyName("items");
                        writer.WriteStartArray();
                        foreach (var classMod in classMods.OrderBy(cmd => cmd.GetPath()))
                        {
                            writer.WriteValue(classMod.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    var manufacturers = balance.Manufacturers;
                    if (manufacturers != null &&
                        manufacturers.Length > 0)
                    {
                        writer.WritePropertyName("manufacturers");
                        writer.WriteStartArray();
                        foreach (var manufacturer in ((IEnumerable<dynamic>)manufacturers)
                            .Where(imbd => imbd.Manufacturer != null)
                            .OrderBy(imbd => imbd.Manufacturer.GetPath()))
                        {
                            writer.WriteValue(manufacturer.Manufacturer.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    dynamic itemPartList;
                    string itemPartListPath = null;
                    if (balanceClass == inventoryBalanceClass)
                    {
                        itemPartList = balance.PartListCollection;
                        if (itemPartList != null)
                        {
                            itemPartListPath = (string)itemPartList.GetPath();
                        }
                    }
                    else
                    {
                        if (balance.PartListCollection != null)
                        {
                            throw new InvalidOperationException();
                        }

                        if (balance.ItemPartListCollection == null)
                        {
                            throw new InvalidOperationException();
                        }

                        itemPartList = balance.RuntimePartListCollection;
                        if (itemPartList == null)
                        {
                            throw new InvalidOperationException();
                        }

                        itemPartListPath = (string)balance.ItemPartListCollection.GetPath();
                    }

                    if (itemPartList != null)
                    {
                        itemBalancePartLists.Add(new KeyValuePair<string, dynamic>(itemPartListPath, balance));
                        writer.WritePropertyName("parts");
                        writer.WriteValue(itemPartListPath);
                    }

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            using (var writer = Dataminer.NewDump("Item Balance Part Lists.json"))
            {
                writer.WriteStartObject();
                foreach (var kv in itemBalancePartLists)
                {
                    var partListPath = kv.Key;
                    var balance = kv.Value;
                    var balanceClass = balance.GetClass();
                    var baseBalance = balance.BaseDefinition;

                    PartReplacementMode? mode;
                    dynamic partList;
                    if (balanceClass == inventoryBalanceClass)
                    {
                        partList = balance.PartListCollection;
                        if (partList == null)
                        {
                            throw new InvalidOperationException();
                        }
                        mode = (PartReplacementMode)partList.PartReplacementMode;
                    }
                    else
                    {
                        if (balance.PartListCollection != null)
                        {
                            throw new InvalidOperationException();
                        }

                        if (balance.ItemPartListCollection == null)
                        {
                            throw new InvalidOperationException();
                        }

                        partList = balance.RuntimePartListCollection;
                        if (partList == null)
                        {
                            throw new InvalidOperationException();
                        }
                        mode = (PartReplacementMode)partList.PartReplacementMode;
                    }

                    if (partList.GetClass().Path != "WillowGame.ItemPartListCollectionDefinition")
                    {
                        throw new InvalidOperationException();
                    }

                    var basePartList = baseBalance == null ||
                                       baseBalance.GetClass() == inventoryBalanceClass
                        ? null : baseBalance.RuntimePartListCollection;

                    List<string>
                        alphaPartData, betaPartData, gammaPartData, deltaPartData, epsilonPartData,
                        zetaPartData, etaPartData, thetaPartData, materialPartData;
                    if (basePartList == null)
                    {
                        var associatedItem = partList.AssociatedItem;
                        alphaPartData = BuildPartList(partList.AlphaPartData, associatedItem?.AlphaParts, ref mode);
                        betaPartData = BuildPartList(partList.BetaPartData, associatedItem?.BetaParts, ref mode);
                        gammaPartData = BuildPartList(partList.GammaPartData, associatedItem?.GammaParts, ref mode);
                        deltaPartData = BuildPartList(partList.DeltaPartData, associatedItem?.DeltaParts, ref mode);
                        epsilonPartData = BuildPartList(partList.EpsilonPartData, associatedItem?.EpsilonParts, ref mode);
                        zetaPartData = BuildPartList(partList.ZetaPartData, associatedItem?.ZetaParts, ref mode);
                        etaPartData = BuildPartList(partList.EtaPartData, associatedItem?.EtaParts, ref mode);
                        thetaPartData = BuildPartList(partList.ThetaPartData, associatedItem?.ThetaParts, ref mode);
                        materialPartData = BuildPartList(partList.MaterialPartData, associatedItem?.MaterialParts, ref mode);
                    }
                    else
                    {
                        alphaPartData = BuildPartList(partList.AlphaPartData, basePartList?.AlphaPartData, ref mode);
                        betaPartData = BuildPartList(partList.BetaPartData, basePartList?.BetaPartData, ref mode);
                        gammaPartData = BuildPartList(partList.GammaPartData, basePartList?.GammaPartData, ref mode);
                        deltaPartData = BuildPartList(partList.DeltaPartData, basePartList?.DeltaPartData, ref mode);
                        epsilonPartData = BuildPartList(partList.EpsilonPartData, basePartList?.EpsilonPartData, ref mode);
                        zetaPartData = BuildPartList(partList.ZetaPartData, basePartList?.ZetaPartData, ref mode);
                        etaPartData = BuildPartList(partList.EtaPartData, basePartList?.EtaPartData, ref mode);
                        thetaPartData = BuildPartList(partList.ThetaPartData, basePartList?.ThetaPartData, ref mode);
                        materialPartData = BuildPartList(partList.MaterialPartData, basePartList?.MaterialPartData, ref mode);
                    }

                    if (mode == null)
                    {
                        mode = PartReplacementMode.Additive;
                    }

                    writer.WritePropertyName(partListPath);
                    writer.WriteStartObject();

                    writer.WritePropertyName("mode");
                    writer.WriteValue(mode.ToString());

                    var associatedItemPath = (string)partList.AssociatedItem?.GetPath();
                    var baseAssociatedItemPath = (string)basePartList?.AssociatedItem?.GetPath();
                    if (associatedItemPath != null &&
                        (baseAssociatedItemPath == null || associatedItemPath != baseAssociatedItemPath))
                    {
                        writer.WritePropertyName("item");
                        writer.WriteValue(associatedItemPath);
                    }

                    WriteStrings(writer, "alpha", alphaPartData);
                    WriteStrings(writer, "beta", betaPartData);
                    WriteStrings(writer, "gamma", gammaPartData);
                    WriteStrings(writer, "delta", deltaPartData);
                    WriteStrings(writer, "epsilon", epsilonPartData);
                    WriteStrings(writer, "zeta", zetaPartData);
                    WriteStrings(writer, "eta", etaPartData);
                    WriteStrings(writer, "theta", thetaPartData);
                    WriteStrings(writer, "material", materialPartData);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
        }

        private static List<string> BuildPartList(dynamic data, dynamic baseData, ref PartReplacementMode? mode)
        {
            if ((bool)data.bEnabled == false)
            {
                return null;
            }

            var partPaths = new List<string>();
            foreach (var weightedPart in (dynamic[])data.WeightedParts)
            {
                if (weightedPart.Part == null)
                {
                    partPaths.Add(null);
                }
                else
                {
                    partPaths.Add(weightedPart.Part.GetPath());
                }
            }

            if (baseData == null)
            {
                if (mode == null)
                {
                    mode = PartReplacementMode.Additive;
                }

                return partPaths;
            }

            var baseDataPath = (string)baseData.GetClass().Path;
            if ((baseDataPath == "WillowGame.ItemPartListCollectionDefinition.ItemCustomPartTypeData" ||
                 baseDataPath == "WillowGame.WeaponPartListCollectionDefinition.WeaponCustomPartTypeData") &&
                (bool)baseData.bEnabled == false)
            {
                if (mode == null)
                {
                    mode = PartReplacementMode.Additive;
                }

                return partPaths;
            }

            var basePartPaths = new List<string>();
            foreach (var weightedPart in (dynamic[])baseData.WeightedParts)
            {
                if (weightedPart.Part == null)
                {
                    basePartPaths.Add(null);
                }
                else
                {
                    basePartPaths.Add(weightedPart.Part.GetPath());
                }
            }

            if (mode == PartReplacementMode.Selective ||
                mode == PartReplacementMode.Complete ||
                basePartPaths.Except(partPaths).Any() == true)
            {
                if (mode != PartReplacementMode.Selective &&
                    mode != PartReplacementMode.Complete)
                {
                    mode = PartReplacementMode.Selective;
                }
                return mode == PartReplacementMode.Selective &&
                       StringComparer.Equals(partPaths, basePartPaths) == true
                    ? null
                    : partPaths;
            }

            if (mode != null && mode != PartReplacementMode.Additive)
            {
                throw new InvalidOperationException();
            }

            if (mode == null)
            {
                mode = PartReplacementMode.Additive;
            }

            partPaths = partPaths.Except(basePartPaths).ToList();
            return partPaths.Count == 0 ? null : partPaths;
        }

        private static void WriteStrings(JsonWriter writer, string name, IEnumerable<string> enumerable)
        {
            if (enumerable == null)
            {
                return;
            }

            writer.WritePropertyName(name);
            writer.WriteStartArray();
            foreach (var value in enumerable)
            {
                writer.WriteValue(value);
            }
            writer.WriteEndArray();
        }
    }
}
