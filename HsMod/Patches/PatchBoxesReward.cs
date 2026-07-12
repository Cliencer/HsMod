using HarmonyLib;
using static HsMod.PluginConfig;

namespace HsMod
{
    public partial class Patcher
    {
        public class PatchBoxesReward
        {
            //自动点击包裹
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardBoxesDisplay), "RewardPackageOnComplete")]
            public static void PatchRewardPackageOnComplete(RewardBoxesDisplay.RewardBoxData boxData)
            {
                if (isAutoOpenBoxesRewardEnable.Value)
                {
                    boxData.m_RewardPackage.TriggerPress();
                    boxData.m_RewardPackage.TriggerRelease();
                }
            }

            //自动点击完成
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardBoxesDisplay), "OnDoneButtonShown")]
            public static void PatchOnDoneButtonShown(Spell spell, object userData)
            {
                if (isAutoOpenBoxesRewardEnable.Value)
                {
                    RewardBoxesDisplay.Get().m_DoneButton.TriggerPress();
                    RewardBoxesDisplay.Get().m_DoneButton.TriggerRelease();
                }
            }

            //额外奖励
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RewardBoxesDisplay), "OnBonusLootButtonShown")]
            public static void PatchOnBonusLootButtonShown(Spell spell, object userData)
            {
                if (isAutoOpenBoxesRewardEnable.Value)
                {
                    RewardBoxesDisplay.Get().m_BonusLootNextButton.TriggerPress();
                    RewardBoxesDisplay.Get().m_BonusLootNextButton.TriggerRelease();
                }
            }
        }
    }
}
