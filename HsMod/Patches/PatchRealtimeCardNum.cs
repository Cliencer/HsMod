namespace HsMod
{
    public partial class Patcher
    {
        public class PatchRealtimeCardNum
        {
            //todo: fixme: 显示卡牌数量 或||控制暂时有问题，需要进行delegate委托
            //[HarmonyTranspiler]
            //[HarmonyPatch(typeof(CollectionCardCount), "UpdateVisibility")]
            //public static IEnumerable<CodeInstruction> PatchCollectionCardCountUpdateVisibility(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            //{
            //    List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            //    int num = 0;
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        if (list[i].opcode == OpCodes.Ldc_I4_S && (sbyte)list[i].operand == 10)
            //        {
            //            num = i;
            //            break;
            //        }
            //    }
            //    num--;
            //    if (num > 0)
            //    {
            //        list[num] = new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method);
            //        list[num + 1] = new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsShowCardLargeCountValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());
            //        list[num + 2] = new CodeInstruction(OpCodes.Brfalse_S, list[num + 2].operand);
            //    }
            //    return list;
            //}
        }
    }
}
