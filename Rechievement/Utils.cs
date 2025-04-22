using System;
using BetterTOS2;
using Game.Achievements;
using System.Collections.Generic;
using Home.Shared;
using Rechievement.Patches;
using Server.Shared.Extensions;
using Server.Shared.State;
using Services;
using SML;
using UnityEngine;
using Newtonsoft.Json;

namespace Rechievement
{
    // Token: 0x02000006 RID: 6
    public static class Utils
    {
        // Token: 0x06000028 RID: 40
        public static bool BTOS2Exists()
        {
            return ModStates.IsEnabled("curtis.tuba.better.tos2");
        }

        // Token: 0x06000029 RID: 41
        public static bool IsBTOS2()
        {
            bool result;
            try
            {
                result = IsBTOS2Bypass();
            }
            catch
            {
                result = false;
            }
            return result;
        }

        // Token: 0x0600002A RID: 42
        private static bool IsBTOS2Bypass()
        {
            return BTOS2Exists() && BTOSInfo.IS_MODDED;
        }

        // Token: 0x0600002B RID: 43
        public static Sprite GetRoleSprite(this Role role)
        {
            Sprite sprite = NoneSprite;
            try
            {
                sprite = Service.Game.Roles.GetRoleInfo(role).sprite;
            }
            catch { }
            return sprite;
        }

        private static Sprite InternalBToS2Sprite(string str) => BTOSInfo.sprites[str];

        public static Sprite GetBToS2Sprite(string str)
        {
            Sprite sprite = NoneSprite;
            if (Utils.BTOS2Exists())
            {
                try
                {
                    sprite = InternalBToS2Sprite(str);
                }
                catch { }
            }
            return sprite;
        }

        public static Sprite GetAssetBundleSprite(string str)
        {
            Sprite sprite = NoneSprite;
            try
            {
                sprite = AssetBundleSprites[str];
            }
            catch { }
            return sprite;
        }

        public static void ShowRechievement(this Rechievement rechievement)
        {
            if (rechievement == null || AchievementPanelPatch.achievementPanel == null)
                return;
            AchievementPanel achievementPanel = AchievementPanelPatch.achievementPanel;
            achievementPanel.isRunning = true;
            AchievementItem achievementItem = UnityEngine.Object.Instantiate<AchievementItem>(achievementPanel.ItemTemplate, achievementPanel.ItemTemplate.transform.parent);
            achievementItem.multiplier.gameObject.SetActive(false);
            achievementItem.title.SetText(rechievement.Name);
            achievementItem.desciption.SetText(rechievement.Description);
            achievementItem.roleIcon.sprite = rechievement.Sprite;
            achievementItem.townPointIcon.sprite = RechievementSprite;
            achievementItem.gameObject.SetActive(true);
            achievementPanel.items.Add(achievementItem);
        }

        public static void ShowRechievement(string str) => ShowRechievement(Rechievement.allRechievements.GetValue(str, null));

        public static Sprite NoneSprite = Service.Game.Roles.GetRoleInfo(Role.NONE).sprite;

        public static Sprite RechievementSprite;

        public static Dictionary<string, Sprite> AssetBundleSprites = new Dictionary<string, Sprite>();
    }
}
