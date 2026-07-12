using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchDevOptioins
        {
            //进入内部模式（炉石开发者模式）
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Hearthstone.HearthstoneApplication), "GetMode")]
            public static IEnumerable<CodeInstruction> PatchHearthstoneApplicationGetMode(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindLastIndex((CodeInstruction x) => x.opcode == OpCodes.Brtrue);
                if (num > 0)
                {
                    num++;
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    if (list[num].opcode == OpCodes.Ldc_I4_2)
                    {
                        list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                        list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsInternalModeEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                        list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                        list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                        list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsInternalModeEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                        Label label2 = generator.DefineLabel();
                        list.Insert(num, new CodeInstruction(OpCodes.Brtrue_S, label2));
                        num += 3;
                        list.Insert(num, new CodeInstruction(OpCodes.Ldc_I4_1));
                        list[num++].labels.Add(label2);
                        list.Insert(num, new CodeInstruction(OpCodes.Ret));
                    }
                }
                return list;
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Hearthstone.HearthstoneApplication), "Job_InitializeMode", MethodType.Enumerator)]
            public static IEnumerable<CodeInstruction> PatchJob_InitializeMode(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindLastIndex((CodeInstruction x) => x.opcode == OpCodes.Brfalse);
                if (num > 0)
                {
                    object operand = list[num].operand;
                    if (operand is Label label)
                    {
                        Label label2 = generator.DefineLabel();
                        list[num].operand = label2;
                        num += 3;
                        if (list[num].opcode == OpCodes.Ldc_I4_2)
                        {
                            list.Insert(num, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                            list[num++].labels.Add(label2);
                            list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsInternalModeEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                            list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                            list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                            list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsInternalModeEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                            Label label3 = generator.DefineLabel();
                            list.Insert(num, new CodeInstruction(OpCodes.Brtrue_S, label3));
                            num += 2;
                            Label label4 = generator.DefineLabel();
                            list.Insert(num++, new CodeInstruction(OpCodes.Br_S, label4));
                            list.Insert(num, new CodeInstruction(OpCodes.Ldc_I4_1));
                            list[num++].labels.Add(label3);
                            list[num].labels.Add(label4);
                        }
                    }
                }
                return list;
            }




        }
    }
}
