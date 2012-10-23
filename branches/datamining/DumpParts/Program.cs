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
            var weaponPartDefinitionClass = engine.GetClass("WillowGame.WeaponPartDefinition");
            var itemPartDefinitionClass = engine.GetClass("WillowGame.ItemPartDefinition");
            var artifactPartDefinitionClass = engine.GetClass("WillowGame.ArtifactPartDefinition");
            var classModPartDefinitionClass = engine.GetClass("WillowGame.ClassModPartDefinition");
            var equipableItemPartDefinitionClass = engine.GetClass("WillowGame.EquipableItemPartDefinition");
            var grenadeModPartDefinitionClass = engine.GetClass("WillowGame.GrenadeModPartDefinition");
            var missionItemPartDefinitionClass = engine.GetClass("WillowGame.MissionItemPartDefinition");
            var shieldPartDefinitionClass = engine.GetClass("WillowGame.ShieldPartDefinition");
            var usableItemPartDefinitionClass = engine.GetClass("WillowGame.UsableItemPartDefinition");

            if (weaponPartDefinitionClass == null ||
                itemPartDefinitionClass == null ||
                artifactPartDefinitionClass == null ||
                classModPartDefinitionClass == null ||
                equipableItemPartDefinitionClass == null ||
                grenadeModPartDefinitionClass == null ||
                missionItemPartDefinitionClass == null ||
                shieldPartDefinitionClass == null ||
                usableItemPartDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            var weaponNamePartDefinitionClass = engine.GetClass("WillowGame.WeaponNamePartDefinition");
            var itemNamePartDefinitionClass = engine.GetClass("WillowGame.ItemNamePartDefinition");
            if (weaponNamePartDefinitionClass == null ||
                itemNamePartDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            var weaponParts = engine.Objects
                .Where(o => o.IsA(weaponPartDefinitionClass) == true && o.GetName().StartsWith("Default__") == false)
                .Distinct()
                .OrderBy(o => o.GetPath());
            using (var output = new StreamWriter("Weapon Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (dynamic weaponPart in weaponParts)
                {
                    UnrealClass uclass = weaponPart.GetClass();
                    if (uclass != weaponPartDefinitionClass)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(weaponPart.GetPath());
                    writer.WriteStartObject();

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

            var weaponNameParts = engine.Objects
                .Where(o => o.IsA(weaponNamePartDefinitionClass) == true && o.GetName().StartsWith("Default__") == false)
                .Distinct()
                .OrderBy(o => o.GetPath());
            using (var output = new StreamWriter("Weapon Name Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (dynamic weaponNamePart in weaponNameParts)
                {
                    UnrealClass uclass = weaponNamePart.GetClass();
                    if (uclass != weaponNamePartDefinitionClass)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(weaponNamePart.GetPath());
                    writer.WriteStartObject();

                    if (weaponNamePart.AttributeSlotEffects != null &&
                        weaponNamePart.AttributeSlotEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.AttributeSlotUpgrades != null &&
                        weaponNamePart.AttributeSlotUpgrades.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.ExternalAttributeEffects != null &&
                        weaponNamePart.ExternalAttributeEffects.Length > 0)
                    {
                        IEnumerable<dynamic> externalAttributeEffects = weaponNamePart.ExternalAttributeEffects;
                        foreach (var externalAttributeEffect in externalAttributeEffects)
                        {
                            if (externalAttributeEffect.AttributeToModify != null &&
                                externalAttributeEffect.AttributeToModify.GetPath() !=
                                "GD_Shields.Attributes.Attr_LawEquipped")
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }

                    if (weaponNamePart.WeaponAttributeEffects != null &&
                        weaponNamePart.WeaponAttributeEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.ZoomExternalAttributeEffects != null &&
                        weaponNamePart.ZoomExternalAttributeEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.ZoomWeaponAttributeEffects != null &&
                        weaponNamePart.ZoomWeaponAttributeEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.WeaponCardAttributes != null &&
                        weaponNamePart.WeaponCardAttributes.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.CustomPresentations != null &&
                        weaponNamePart.CustomPresentations.Length > 0)
                    {
                        IEnumerable<dynamic> customPresentations = weaponNamePart.CustomPresentations;
                        foreach (var customPresentation in customPresentations)
                        {
                            if (string.IsNullOrEmpty((string)customPresentation.Suffix) == false)
                            {
                                throw new InvalidOperationException();
                            }

                            if (string.IsNullOrEmpty((string)customPresentation.Prefix) == false)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }

                    if (weaponNamePart.TitleList != null &&
                        weaponNamePart.TitleList.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (weaponNamePart.PrefixList != null &&
                        weaponNamePart.PrefixList.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    var type = (WeaponPartType)weaponNamePart.PartType;
                    if (type != WeaponPartType.Body)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((bool)weaponNamePart.bNameIsUnique != false)
                    {
                        writer.WritePropertyName("unique");
                        writer.WriteValue((bool)weaponNamePart.bNameIsUnique);
                    }

                    string partName = weaponNamePart.PartName;
                    if (string.IsNullOrEmpty(partName) == false)
                    {
                        writer.WritePropertyName("name");
                        writer.WriteValue(partName);
                    }

                    if (weaponNamePart.Expressions != null &&
                        weaponNamePart.Expressions.Length > 0)
                    {
                        /*
                        writer.WritePropertyName("expressions");
                        writer.WriteStartArray();

                        foreach (var expression in weaponNamePart.Expressions)
                        {
                            writer.WriteStartObject();

                            if (expression.AttributeOperand1 != null)
                            {
                                writer.WritePropertyName("operand1_attribute");
                                writer.WriteValue(expression.AttributeOperand1.GetPath());
                            }

                            var comparisonOperator = (ComparisonOperator)expression.ComparisonOperator;
                            writer.WritePropertyName("comparison_operator");
                            writer.WriteValue(comparisonOperator.ToString());

                            var operand2Usage = (OperandUsage)expression.Operand2Usage;
                            writer.WritePropertyName("operand2_usage");
                            writer.WriteValue(operand2Usage.ToString());

                            if (expression.AttributeOperand2 != null)
                            {
                                writer.WritePropertyName("operand2_attribute");
                                writer.WriteValue(expression.AttributeOperand2.GetPath());
                            }

                            float constantOperand2 = expression.ConstantOperand2;
                            if (constantOperand2.Equals(0.0f) == false)
                            {
                                writer.WritePropertyName("operand2_constant");
                                writer.WriteValue(constantOperand2);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        */
                    }

                    if (weaponNamePart.MinExpLevelRequirement != 1)
                    {
                        /*
                        writer.WritePropertyName("min_exp_level_required");
                        writer.WriteValue(weaponNamePart.MinExpLevelRequirement);
                        */
                    }

                    if (weaponNamePart.MaxExpLevelRequirement != 100)
                    {
                        /*
                        writer.WritePropertyName("max_exp_level_required");
                        writer.WriteValue(weaponNamePart.MaxExpLevelRequirement);
                        */
                    }

                    /*
                    writer.WritePropertyName("priority");
                    writer.WriteValue(weaponNamePart.Priority);
                    */

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            var itemParts = engine.Objects
                .Where(o => (o.IsA(itemPartDefinitionClass) == true ||
                             o.IsA(artifactPartDefinitionClass) == true ||
                             o.IsA(classModPartDefinitionClass) == true ||
                             o.IsA(equipableItemPartDefinitionClass) == true ||
                             o.IsA(grenadeModPartDefinitionClass) == true ||
                             o.IsA(missionItemPartDefinitionClass) == true ||
                             o.IsA(shieldPartDefinitionClass) == true ||
                             o.IsA(usableItemPartDefinitionClass) == true) &&
                            o.GetName().StartsWith("Default__") == false)
                .Distinct()
                .OrderBy(o => o.GetPath());
            using (var output = new StreamWriter("Item Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (dynamic itemPart in itemParts)
                {
                    UnrealClass uclass = itemPart.GetClass();
                    if (uclass != artifactPartDefinitionClass &&
                        uclass != classModPartDefinitionClass &&
                        uclass != grenadeModPartDefinitionClass &&
                        uclass != shieldPartDefinitionClass &&
                        uclass != usableItemPartDefinitionClass)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(itemPart.GetPath());
                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue(((ItemPartType)itemPart.PartType).ToString());

                    if (itemPart.TitleList != null &&
                        itemPart.TitleList.Length > 0)
                    {
                        writer.WritePropertyName("titles");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> titleList = itemPart.TitleList;
                        foreach (var title in titleList
                            .Where(tp => tp != null)
                            .OrderBy(tp => tp.GetPath()))
                        {
                            writer.WriteValue(title.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    if (itemPart.PrefixList != null &&
                        itemPart.PrefixList.Length > 0)
                    {
                        writer.WritePropertyName("prefixes");
                        writer.WriteStartArray();
                        IEnumerable<dynamic> prefixList = itemPart.PrefixList;
                        foreach (var prefix in prefixList
                            .Where(pp => pp != null)
                            .OrderBy(pp => pp.GetPath()))
                        {
                            writer.WriteValue(prefix.GetPath());
                        }
                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            var itemNameParts = engine.Objects
                .Where(o => o.IsA(itemNamePartDefinitionClass) == true && o.GetName().StartsWith("Default__") == false)
                .Distinct()
                .OrderBy(o => o.GetPath());
            using (var output = new StreamWriter("Item Name Parts.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                foreach (dynamic itemNamePart in itemNameParts)
                {
                    UnrealClass uclass = itemNamePart.GetClass();
                    if (uclass != itemNamePartDefinitionClass)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName(itemNamePart.GetPath());
                    writer.WriteStartObject();

                    if (itemNamePart.AttributeSlotEffects != null &&
                        itemNamePart.AttributeSlotEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (itemNamePart.AttributeSlotUpgrades != null &&
                        itemNamePart.AttributeSlotUpgrades.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (itemNamePart.ExternalAttributeEffects != null &&
                        itemNamePart.ExternalAttributeEffects.Length > 0)
                    {
                        IEnumerable<dynamic> externalAttributeEffects = itemNamePart.ExternalAttributeEffects;
                        foreach (var externalAttributeEffect in externalAttributeEffects)
                        {
                            if (externalAttributeEffect.AttributeToModify != null &&
                                externalAttributeEffect.AttributeToModify.GetPath() !=
                                "GD_Shields.Attributes.Attr_LawEquipped")
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }

                    if (itemNamePart.ItemAttributeEffects != null &&
                        itemNamePart.ItemAttributeEffects.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (itemNamePart.ItemCardAttributes != null &&
                        itemNamePart.ItemCardAttributes.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (itemNamePart.CustomPresentations != null &&
                        itemNamePart.CustomPresentations.Length > 0)
                    {
                        IEnumerable<dynamic> customPresentations = itemNamePart.CustomPresentations;
                        foreach (var customPresentation in customPresentations)
                        {
                            if (string.IsNullOrEmpty((string)customPresentation.Suffix) == false)
                            {
                                throw new InvalidOperationException();
                            }

                            if (string.IsNullOrEmpty((string)customPresentation.Prefix) == false)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }

                    if (itemNamePart.TitleList != null &&
                        itemNamePart.TitleList.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (itemNamePart.PrefixList != null &&
                        itemNamePart.PrefixList.Length > 0)
                    {
                        throw new InvalidOperationException();
                    }

                    var type = (ItemPartType)itemNamePart.PartType;
                    if (type != ItemPartType.Alpha)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((bool)itemNamePart.bNameIsUnique != false)
                    {
                        writer.WritePropertyName("unique");
                        writer.WriteValue((bool)itemNamePart.bNameIsUnique);
                    }

                    string partName = itemNamePart.PartName;
                    if (string.IsNullOrEmpty(partName) == false)
                    {
                        writer.WritePropertyName("name");
                        writer.WriteValue(partName);
                    }

                    if (itemNamePart.Expressions != null &&
                        itemNamePart.Expressions.Length > 0)
                    {
                        /*
                        writer.WritePropertyName("expressions");
                        writer.WriteStartArray();

                        foreach (var expression in itemNamePart.Expressions)
                        {
                            writer.WriteStartObject();

                            if (expression.AttributeOperand1 != null)
                            {
                                writer.WritePropertyName("operand1_attribute");
                                writer.WriteValue(expression.AttributeOperand1.GetPath());
                            }

                            var comparisonOperator = (ComparisonOperator)expression.ComparisonOperator;
                            writer.WritePropertyName("comparison_operator");
                            writer.WriteValue(comparisonOperator.ToString());

                            var operand2Usage = (OperandUsage)expression.Operand2Usage;
                            writer.WritePropertyName("operand2_usage");
                            writer.WriteValue(operand2Usage.ToString());

                            if (expression.AttributeOperand2 != null)
                            {
                                writer.WritePropertyName("operand2_attribute");
                                writer.WriteValue(expression.AttributeOperand2.GetPath());
                            }

                            float constantOperand2 = expression.ConstantOperand2;
                            if (constantOperand2.Equals(0.0f) == false)
                            {
                                writer.WritePropertyName("operand2_constant");
                                writer.WriteValue(constantOperand2);
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        */
                    }

                    if (itemNamePart.MinExpLevelRequirement != 1)
                    {
                        /*
                        writer.WritePropertyName("min_exp_level_required");
                        writer.WriteValue(itemNamePart.MinExpLevelRequirement);
                        */
                    }

                    if (itemNamePart.MaxExpLevelRequirement != 100)
                    {
                        /*
                        writer.WritePropertyName("max_exp_level_required");
                        writer.WriteValue(itemNamePart.MaxExpLevelRequirement);
                        */
                    }

                    /*
                    writer.WritePropertyName("priority");
                    writer.WriteValue(itemNamePart.Priority);
                    */

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }
    }
}
