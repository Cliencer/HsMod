using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchDeckShareCode
        {
            //移除卡组代码检测
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManagerDisplay), "IsValidHeroClassesForCollectionDeck")]
            public static bool PatchIsValidHeroClassesForCollectionDeck(ref List<TAG_CLASS> heroClasses, ref CollectionDeck deck, ref bool __result)
            {
                if (isBypassDeckShareCodeCheckEnable.Value)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CollectionManagerDisplay), "CanPasteShareableDeck", new Type[] { typeof(ShareableDeck), typeof(string) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
            public static bool PatchCanPasteShareableDeck(ShareableDeck shareableDeck, out string alertMessage, ref bool __result)
            {
                alertMessage = string.Empty;
                if (isBypassDeckShareCodeCheckEnable.Value)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ShareableDeck), "DeserializeFromVersion_1")]
            public static IEnumerable<CodeInstruction> PatchDeserializeFromVersion_1(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                if (isBypassDeckShareCodeCheckEnable.Value == false)
                {
                    return instructions;
                }
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Ldloc_1);
                num += 2;
                list[num++] = new CodeInstruction(OpCodes.Nop);
                list[num++] = new CodeInstruction(OpCodes.Nop);
                num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "IsHeroSkin");
                if (num > 0)
                {
                    num++; // brture.s
                    list[num++] = new CodeInstruction(OpCodes.Nop);
                    list[num++] = new CodeInstruction(OpCodes.Nop);
                }
                return list;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(DeckRuleset), "EntityIgnoresRuleset")]
            public static bool PatchEntityIgnoresRuleset(ref EntityDef def, ref bool __result)
            {
                if (isBypassDeckShareCodeCheckEnable.Value)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
