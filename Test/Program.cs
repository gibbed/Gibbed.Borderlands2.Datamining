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
using System.Linq;
using Gibbed.Unreflect.Core;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            /*
            var globalsDefinitionClass = engine.GetClass("WillowGame.GlobalsDefinition");
            var willowGlobalsClass = engine.GetClass("WillowGame.WillowGlobals");
            if (globalsDefinitionClass == null ||
                willowGlobalsClass == null)
            {
                throw new InvalidOperationException();
            }

            var globalsDefinitions = engine.Objects
                    .Where(o => o.IsA(globalsDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath())
                    .ToArray();
            
            dynamic globalsDefinition = globalsDefinitions.FirstOrDefault();

            var willowGlobals = engine.Objects
                    .Where(o => o.IsA(willowGlobalsClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath())
                    .ToArray();

            dynamic willowGlobal = willowGlobals.FirstOrDefault();

            dynamic[] butt = willowGlobal.KnownCurrencies;

            var generalSkillPointsPerLevelUp = globalsDefinition.GeneralSkillPointsPerLevelUp;
            var generalSkillPointsTotalForCurrentLevel = globalsDefinition.GeneralSkillPointsTotalForCurrentLevel;
            var specialistSkillPointsPerLevelUp = globalsDefinition.SpecialistSkillPointsPerLevelUp;
            var costToResetSkillPoints = globalsDefinition.CostToResetSkillPoints;
            var rarityLevelColors = globalsDefinition.RarityLevelColors;
            */

            var customizationDefinitionClass = engine.GetClass("WillowGame.CustomizationDefinition");
            var customizationDefinitions = engine.Objects
                .Where(o => o.IsA(customizationDefinitionClass) &&
                            o.GetName().StartsWith("Default__") ==
                            false)
                .OrderBy(o => o.GetPath())
                .ToArray();

            foreach (dynamic customizationDefinition in customizationDefinitions)
            {
                customizationDefinition.DlcCustomizationSetDef = null;
            }
        }
    }
}
