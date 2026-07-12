using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchBattlegrounds
        {
            //闭了,鲍勃!
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(TB_BaconShop), "PlayBobLineWithoutText", MethodType.Enumerator)]
            public static IEnumerable<CodeInstruction> PatchPlayBobLineWithoutText(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Stfld && (x.operand as FieldInfo).Name == "<>1__state");
                if (num > 0)
                {
                    num++;
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsShutUpBobEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldc_I4_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ret));
                }
                return list;
            }
            [HarmonyPostfix, HarmonyPatch(typeof(TB_BaconShop), "GetBobActor")]
            public static void PatchGetBobActor(ref Actor __result)
            {
                if (__result != null && HsMod.ConfigValue.Get().IsShutUpBobEnableValue && GameMgr.Get().IsBattlegrounds())
                {
                    __result.ActivateSpellBirthState(SpellType.HERO_EMOTE_SILENCED);
                }
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(TB_BaconShop), "HandleGameOverWithTiming", MethodType.Enumerator)]
            public static IEnumerable<CodeInstruction> PatchHandleGameOverWithTiming(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "UpdateLayout");
                if (num > 0)
                {
                    num++;
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsShutUpBobEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldc_I4_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ret));
                }
                return list;
            }


            //解锁酒馆礼遇
            [HarmonyPrefix]
            [HarmonyPatch(typeof(MulliganManager), "ToggleHoldState", new Type[] { typeof(Card) })]
            public static void PatchToggleHoldState(ref Card toggleCard)
            {
                if (!isBgsSeasonTicketUnlock.Value) return;
                var entity = toggleCard?.GetEntity();
                if (entity != null)
                {
                    entity.SetTag(GAME_TAG.BACON_LOCKED_MULLIGAN_HERO, false);
                }

            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TB_BaconShop), "ConfigureLockedMulliganCardActor")]
            public static bool PatchConfigureLockedMulliganCardActor(Actor actor, bool shown)
            {
                if (isBgsSeasonTicketUnlock.Value)
                {
                    return false;
                }
                return true;
            }
            //快速战斗 - 理论上可以用于所有模式 现只应用于酒馆战旗或佣兵战纪的ai
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(SpellController), "OnProcessTaskList")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void AttackSpellControllerBase(AttackSpellController instance) {; }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(AttackSpellController), "OnProcessTaskList")]
            public static bool PatchAttackSpellControllerOnProcessTaskList(AttackSpellController __instance)
            {
                if (ConfigValue.Get().IsQuickModeEnableValue)
                {
                    AttackSpellControllerBase(__instance);
                    return false;
                }
                else return true;
            }
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(SpellController), "OnProcessTaskList")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void DeathSpellControllerBase(DeathSpellController instance) {; }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(DeathSpellController), "OnProcessTaskList")]
            public static bool PatchDeathSpellControllerOnProcessTaskList(DeathSpellController __instance)
            {
                if (ConfigValue.Get().IsQuickModeEnableValue)
                {
                    DeathSpellControllerBase(__instance);
                    return false;
                }
                else return true;
            }
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(SpellController), "OnProcessTaskList")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void TriggerSpellControllerBase(TriggerSpellController instance) {; }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TriggerSpellController), "OnProcessTaskList")]
            public static bool PatchTriggerSpellControllerOnProcessTaskList(TriggerSpellController __instance)
            {
                if (ConfigValue.Get().IsQuickModeEnableValue)
                {
                    TriggerSpellControllerBase(__instance);
                    return false;
                }
                else return true;
            }
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(SpellController), "OnProcessTaskList")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SubSpellControllerBase(SubSpellController instance) {; }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SubSpellController), "OnProcessTaskList")]
            public static bool PatchSubSpellControllerOnProcessTaskList(SubSpellController __instance)
            {
                //if (isQuickModeEnable.Value && GameMgr.Get().IsBattlegrounds())
                if (ConfigValue.Get().IsQuickModeEnableValue)
                {
                    SubSpellControllerBase(__instance);
                    return false;
                }
                else return true;
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(SideQuestSpellController), "OnProcessTaskList")]
            public static IEnumerable PatchSubSideQuestSpellControllerOnProcessTaskList(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "SetSecretTriggered");

                if (num > 0)
                {
                    Label label = generator.DefineLabel();
                    list[++num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsQuickModeEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, typeof(SpellController).GetMethod("OnProcessTaskList", BindingFlags.Instance | BindingFlags.NonPublic)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ret));
                }
                return list;
            }
            [HarmonyPrefix, HarmonyPatch(typeof(GameState), "IsUsingFastActorTriggers")]
            public static bool PatchGameStateIsUsingFastActorTriggers(ref bool __result)
            {
                if (ConfigValue.Get().IsQuickModeEnableValue)
                {
                    __result = true;
                    return false;
                }
                else return true;
            }


        }
    }
}
