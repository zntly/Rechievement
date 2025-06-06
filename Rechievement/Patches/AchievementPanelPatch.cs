﻿using HarmonyLib;
using Game.Achievements;
using Server.Shared.State;
using Services;

namespace Rechievement.Patches
{
    [HarmonyPatch(typeof(AchievementPanel), "Start")]
    public class AchievementPanelPatch
    {
        // Token: 0x06000030 RID: 48 RVA: 0x00002389 File Offset: 0x00000589
        [HarmonyPostfix]
        public static void Postfix(AchievementPanel __instance)
        {
            if (!Utils.NoneSprite)
                Utils.NoneSprite = Service.Game.Roles.GetRoleInfo(Role.NONE).sprite;
            achievementPanel = __instance;
        }

        public static AchievementPanel achievementPanel;
    }
}
