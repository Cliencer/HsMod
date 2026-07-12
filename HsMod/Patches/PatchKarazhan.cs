using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchKarazhan
        {
            // 尝试修复金币购买卡拉赞bug
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(AdventureWing), "Initialize")]
            public static IEnumerable<CodeInstruction> PatchAdventureWingInitialize(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                list.RemoveRange(201, 9);
                return list;
            }
            // 只保留数字8
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(AdventureWing), "Update")]
            public static IEnumerable PatchAdventureWingUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                for (int i = 0; i <= 112; i++)
                {
                    list[i] = new CodeInstruction(OpCodes.Nop);
                }
                for (int i = 125; i <= 134; i++)
                {
                    list[i] = new CodeInstruction(OpCodes.Nop);
                }
                return list;
            }
        }
    }
}
