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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Unreflect.Core;
using Unreflect.Runtime;

namespace DumpAssetLibraryManager
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

                using (var output = new StreamWriter("Asset Library Manager.json", false, Encoding.Unicode))
                {
                    output.WriteLine("{");
                    output.WriteLine("  version: 7,");

                    output.WriteLine("  configs:");
                    output.WriteLine("  {");

                    foreach (dynamic libraryConfig in assLibMan.LibraryConfigs)
                    {
                        output.WriteLine("    \"{0}\":", ((string)libraryConfig.Desc).Replace(" ", ""));
                        output.WriteLine("    {");
                        output.WriteLine("      sublibrary_bits: {0},", libraryConfig.SublibraryBits);
                        output.WriteLine("      asset_bits: {0},", libraryConfig.AssetBits);
                        output.WriteLine("      type: \"{0}\",", ((UnrealClass)libraryConfig.LibraryType).Path);
                        output.WriteLine("    },");
                    }
                    output.WriteLine("  },");

                    output.WriteLine("  sets:");
                    output.WriteLine("  [");

                    foreach (dynamic assetLibrarySet in assLibMan.RuntimeAssetLibraries)
                    {
                        output.WriteLine("    {");
                        output.WriteLine("      id: {0},", assetLibrarySet.Id);
                        output.WriteLine("      libraries:");
                        output.WriteLine("      {");

                        int libraryIndex = 0;
                        foreach (dynamic library in assetLibrarySet.Libraries)
                        {
                            string desc = assLibMan.LibraryConfigs[libraryIndex].Desc;

                            output.WriteLine("        \"{0}\":", desc.Replace(" ", ""));
                            output.WriteLine("        {");

                            output.WriteLine("          type: \"{0}\",", ((UnrealClass)library.LibraryType).Path);

                            output.WriteLine("          sublibraries:");
                            output.WriteLine("          [");

                            if (library.Sublibraries.Length != library.SublibraryLinks.Length)
                            {
                                throw new InvalidOperationException();
                            }

                            int sublibraryIndex = 0;
                            foreach (dynamic sublibrary in library.SublibraryLinks)
                            {
                                output.WriteLine("            {");
                                output.WriteLine("              description: \"{0}\",",
                                                 library.Sublibraries[sublibraryIndex]);

                                if (sublibrary != null)
                                {
                                    output.WriteLine("              package: \"{0}\",", sublibrary.CachedPackageName);
                                }

                                output.WriteLine("              assets:");
                                output.WriteLine("              [");

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
                                        output.WriteLine("                \"{0}\",", path);
                                    }
                                }

                                output.WriteLine("              ],");
                                output.WriteLine("            },");

                                sublibraryIndex++;
                            }

                            output.WriteLine("          ],");
                            output.WriteLine("        },");

                            libraryIndex++;
                        }

                        output.WriteLine("      },");
                        output.WriteLine("    },");
                    }

                    output.WriteLine("  ],");

                    output.WriteLine("}");
                }

                runtime.ResumeThreads();
                runtime.CloseProcess();
            }
        }
    }
}
