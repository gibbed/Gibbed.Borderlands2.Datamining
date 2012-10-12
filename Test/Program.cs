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
using System.Diagnostics;
using System.Linq;
using Gibbed.Unreflect.Core;
using Gibbed.Unreflect.Runtime;

namespace Test
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

                dynamic artifact = engine.GetObject("GD_Artifacts.A_Item_Unique.Artifact_Terramorphous");
                dynamic[] attributeSlotEffects = artifact.AttributeSlotEffects;

                foreach (dynamic attributeSlotEffect in attributeSlotEffects)
                {
                    Console.WriteLine(attributeSlotEffect.SlotName);

                    dynamic attributeToModify = attributeSlotEffect.AttributeToModify;
                    Console.WriteLine("  modify = {0}", attributeToModify);
                }

                runtime.ResumeThreads();
                runtime.CloseProcess();
            }
        }
    }
}
