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

namespace DumpAssetLibraryManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WillowDatamining.Dataminer().Run(args, Go);
        }

        private static void Go(Engine engine)
        {
            var globalsClass = engine.GetClass("WillowGame.WillowGlobals");
            if (globalsClass == null)
            {
                throw new InvalidOperationException();
            }

            dynamic globals = engine.Objects.FirstOrDefault(o => o.IsA(globalsClass) &&
                                                                 o.GetName().StartsWith("Default__") == false);
            if (globals == null)
            {
                throw new InvalidOperationException();
            }

            dynamic assLibMan = globals.AssLibMan;
            if (assLibMan == null)
            {
                throw new InvalidOperationException();
            }

            Directory.CreateDirectory("dumps");

            using (
                var output = new StreamWriter(Path.Combine("dumps", "Asset Library Manager.json"),
                                              false,
                                              Encoding.Unicode))
            using (var writer = new JsonTextWriter(output))
            {
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();

                writer.WritePropertyName("version");
                writer.WriteValue(7);

                writer.WritePropertyName("configs");
                writer.WriteStartObject();
                foreach (dynamic libraryConfig in assLibMan.LibraryConfigs)
                {
                    writer.WritePropertyName(((string)libraryConfig.Desc).Replace(" ", ""));
                    writer.WriteStartObject();

                    writer.WritePropertyName("sublibrary_bits");
                    writer.WriteValue(libraryConfig.SublibraryBits);

                    writer.WritePropertyName("asset_bits");
                    writer.WriteValue(libraryConfig.AssetBits);

                    writer.WritePropertyName("type");
                    writer.WriteValue(((UnrealClass)libraryConfig.LibraryType).Path);

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();

                writer.WritePropertyName("sets");
                writer.WriteStartArray();
                foreach (
                    dynamic assetLibrarySet in
                        ((IEnumerable<dynamic>)assLibMan.RuntimeAssetLibraries).OrderBy(ral => ral.Id))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("id");
                    writer.WriteValue(assetLibrarySet.Id);

                    writer.WritePropertyName("libraries");
                    writer.WriteStartObject();

                    int libraryIndex = 0;
                    foreach (dynamic library in assetLibrarySet.Libraries)
                    {
                        string desc = assLibMan.LibraryConfigs[libraryIndex].Desc;

                        writer.WritePropertyName(desc.Replace(" ", ""));
                        writer.WriteStartObject();

                        if (library.LibraryType != null)
                        {
                            writer.WritePropertyName("type");
                            writer.WriteValue(((UnrealClass)library.LibraryType).Path);
                        }

                        writer.WritePropertyName("sublibraries");
                        writer.WriteStartArray();

                        if (library.Sublibraries.Length != library.SublibraryLinks.Length)
                        {
                            throw new InvalidOperationException();
                        }

                        int sublibraryIndex = 0;
                        foreach (dynamic sublibrary in library.SublibraryLinks)
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("description");
                            writer.WriteValue(library.Sublibraries[sublibraryIndex]);

                            if (sublibrary != null)
                            {
                                writer.WritePropertyName("package");
                                writer.WriteValue(sublibrary.CachedPackageName);
                            }

                            writer.WritePropertyName("assets");
                            writer.WriteStartArray();

                            if (sublibrary != null)
                            {
                                var assets = sublibrary.Assets;
                                if (assets.Length != 0)
                                {
                                    throw new NotSupportedException();
                                }

                                var assetPaths = sublibrary.AssetPaths;
                                foreach (var assetPath in assetPaths)
                                {
                                    var parts = new List<string>();
                                    foreach (
                                        var pathComponentName in
                                            ((IEnumerable<string>)assetPath.PathComponentNames).Reverse())
                                    {
                                        if (pathComponentName == "None")
                                        {
                                            break;
                                        }

                                        parts.Add(pathComponentName);
                                    }

                                    parts.Reverse();
                                    var path = string.Join(".", parts.ToArray());

                                    writer.WriteValue(path);
                                }
                            }

                            writer.WriteEndArray();
                            writer.WriteEndObject();

                            sublibraryIndex++;
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();

                        libraryIndex++;
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
        }
    }
}
