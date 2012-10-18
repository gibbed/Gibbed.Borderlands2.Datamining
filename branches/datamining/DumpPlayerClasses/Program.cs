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

namespace DumpPlayerClasses
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var playerClassDefinitionClass = engine.GetClass("WillowGame.PlayerClassDefinition");
            if (playerClassDefinitionClass == null)
            {
                throw new InvalidOperationException();
            }

            using (var output = new StreamWriter("Player Classes.json", false, Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                var playerClassDefinitionClasses = engine.Objects
                    .Where(o => o.IsA(playerClassDefinitionClass) &&
                                o.GetName().StartsWith("Default__") ==
                                false)
                    .OrderBy(o => o.GetPath());
                foreach (dynamic playerClassDefinition in playerClassDefinitionClasses)
                {
                    writer.WritePropertyName(playerClassDefinition.GetPath());
                    writer.WriteStartObject();

                    var characterNameId = playerClassDefinition.CharacterNameId;
                    if (characterNameId == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var characterClassId = characterNameId.CharacterClassId;
                    if (characterClassId == null)
                    {
                        throw new InvalidOperationException();
                    }

                    writer.WritePropertyName("name");
                    writer.WriteValue(characterNameId.LocalizedCharacterName);

                    writer.WritePropertyName("class");
                    writer.WriteValue(characterClassId.LocalizedClassNameNonCaps);

                    writer.WritePropertyName("sort_order");
                    writer.WriteValue(characterNameId.UISortOrder);

                    if (characterClassId.DlcCharacterDef != null)
                    {
                        writer.WritePropertyName("dlc");
                        writer.WriteValue(characterClassId.DlcCharacterDef.GetPath());
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.Flush();
            }
        }
    }
}
