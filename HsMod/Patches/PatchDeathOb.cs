using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchDeathOb
        {
            private static readonly MethodInfo registerCreateGameListenerInfo = typeof(GameState).GetMethod("RegisterCreateGameListener", BindingFlags.Instance | BindingFlags.Public, null, new Type[2]
                                                                        {
                                                                        typeof(GameState.CreateGameCallback),
                                                                        typeof(object)
                                                                        }, null);

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(FriendMgr), "OnSceneLoaded")]
            public static IEnumerable<CodeInstruction> PatchOnSceneLoaded(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindLastIndex((CodeInstruction x) => x.opcode == OpCodes.Ldftn && (x.operand as MethodInfo).Name == "OnGameOver");
                if (num > 0)
                {
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldftn, new Action<GameState.CreateGamePhase, object>(FriendMgrPatch.OnGameCreated).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Newobj, typeof(GameState.CreateGameCallback).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[2]
                    {
                    typeof(object),
                    typeof(IntPtr)
                    }, null)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldnull));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, registerCreateGameListenerInfo));
                    list.Insert(num++, new CodeInstruction(OpCodes.Pop));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldloc_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0));
                }
                return list;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ZoneHand), "GetCardScale")]
            public static IEnumerable<CodeInstruction> PatchZoneHandGetCardScale(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindIndex((CodeInstruction x) => x.opcode == OpCodes.Ldfld && (x.operand as FieldInfo).Name == "enemyHand");
                if (num > 0)
                {
                    num++;
                    object operand = list[num++].operand;
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsMoveEnemyCardsEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldfld, typeof(ZoneHand).GetField("m_controller", BindingFlags.Instance | BindingFlags.NonPublic)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(Player).GetMethod("IsRevealed", BindingFlags.Instance | BindingFlags.Public)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldc_R4, 0.41f));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldc_R4, 0.085f));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldc_R4, 0.41f));
                    list.Insert(num++, new CodeInstruction(OpCodes.Newobj, typeof(Vector3).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[3]
                    {
                    typeof(float),
                    typeof(float),
                    typeof(float)
                    }, null)));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ret));
                    list.Insert(num, new CodeInstruction(OpCodes.Br_S, operand));
                }
                return list;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ZoneHand), "GetCardRotation", new Type[]
            {
                    typeof(int),
                    typeof(int)
            })]
            public static IEnumerable<CodeInstruction> PatchZoneHandGetCardRotation(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> list = new List<CodeInstruction>(instructions);
                int num = list.FindLastIndex((CodeInstruction x) => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "IsRevealed");
                if (num > 0)
                {
                    num++;
                    object operand = list[num++].operand;
                    Label label = generator.DefineLabel();
                    list[num].labels.Add(label);
                    list.Insert(num++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    list.Insert(num++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsMoveEnemyCardsEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    list.Insert(num++, new CodeInstruction(OpCodes.Brfalse_S, label));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldloc_0));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_1));
                    list.Insert(num++, new CodeInstruction(OpCodes.Conv_R4));
                    list.Insert(num++, new CodeInstruction(OpCodes.Mul));
                    list.Insert(num++, new CodeInstruction(OpCodes.Ldloc_1));
                    list.Insert(num++, new CodeInstruction(OpCodes.Add));
                    list.Insert(num++, new CodeInstruction(OpCodes.Stloc_3));
                    list.Insert(num, new CodeInstruction(OpCodes.Br_S, operand));
                }
                return list;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ZoneHand), "GetCardPosition", new Type[]
            {
                typeof(int),
                typeof(int)
            })]
            public static IEnumerable<CodeInstruction> PatchZoneHandGetCardPosition(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> newInstructions = new List<CodeInstruction>(instructions);
                int index = newInstructions.FindLastIndex(x => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "IsRevealed");
                if (index > 0)
                {
                    index++;
                    var l1 = newInstructions[index++].operand;
                    var l2 = generator.DefineLabel();
                    var l3 = generator.DefineLabel();
                    var l4 = generator.DefineLabel();
                    newInstructions[index].labels.Add(l2);
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Call, new Func<ConfigValue>(ConfigValue.Get).Method));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Callvirt, typeof(ConfigValue).GetProperty("IsMoveEnemyCardsEnableValue", BindingFlags.Instance | BindingFlags.Public).GetGetMethod()));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Brfalse_S, l2));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldflda, typeof(ZoneHand).GetField("m_centerOfHand", BindingFlags.NonPublic | BindingFlags.Instance)));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldfld, typeof(Vector3).GetField("z", BindingFlags.Public | BindingFlags.Instance)));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldarg_1));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldloc_0));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldc_I4_2));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Div));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Sub));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Call, ((Func<int, int>)Mathf.Abs).Method));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Conv_R4));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldc_R4, 2f));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Call, ((Func<float, float, float>)Mathf.Pow).Method));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldc_I4_4));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldloc_0));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Mul));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Conv_R4));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Div));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldloc_1));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Brtrue_S, l3));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldc_R4, 0f));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Br_S, l4));
                    newInstructions.Insert(index, new CodeInstruction(OpCodes.Ldc_R4, 1f));
                    newInstructions[index++].labels.Add(l3);
                    newInstructions.Insert(index, new CodeInstruction(OpCodes.Mul));
                    newInstructions[index++].labels.Add(l4);
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Sub));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldloc_S, (byte)6));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Sub));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Ldc_R4, 0.6f));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Sub));
                    newInstructions.Insert(index++, new CodeInstruction(OpCodes.Stloc_S, (byte)5));
                    newInstructions.Insert(index, new CodeInstruction(OpCodes.Br_S, l1));
                }
                return newInstructions;
            }
        }
    }
}
