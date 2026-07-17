using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchRealtimeCardNum
        {
            // 收藏卡牌数量显示：UpdateVisibility 里普通与试用两处都有 count < 10 的判断，
            // 超过 10 张只显示 9+。把两处阈值抬高后始终走真实数量分支。
            // 补丁由 isShowCardLargeCount 动态加载/卸载，无需在 IL 内再判断配置。
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CollectionCardCount), "UpdateVisibility")]
            public static IEnumerable<CodeInstruction> PatchCollectionCardCountUpdateVisibility(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    // 就地修改指令，保留原有跳转标签
                    if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte value && value == 10)
                    {
                        instruction.opcode = OpCodes.Ldc_I4;
                        instruction.operand = int.MaxValue;
                    }
                    yield return instruction;
                }
            }
        }
    }
}
