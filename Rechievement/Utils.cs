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
using HarmonyLib;
using BetterTOS2.Observations;

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
        public static bool ApocCheck()
        {
            bool result;
            if (!Utils.IsBTOS2())
                result = (bool)AchievementAdder.necessities.GetValue("Pestilence", false) || (bool)AchievementAdder.necessities.GetValue("War", false) || (bool)AchievementAdder.necessities.GetValue("Famine", false) || (bool)AchievementAdder.necessities.GetValue("Death", false);
            else
                result = Utils.InternalApocCheck();
            return result;
        }
        public static bool CourtCheck()
        {
            if (!Utils.IsBTOS2())
                return false;
            return Utils.InternalCourtCheck();
        }
        private static bool InternalApocCheck()
        {
            return GameObservationsPatch.musicOverrideObservation.Data.apoc;
        }
        private static bool InternalCourtCheck()
        {
            return GameObservationsPatch.musicOverrideObservation.Data.court;
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
                try
                {
                    sprite = InternalBToS2Sprite(str);
                }
                catch { }
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

        public static void ShowRechievement(this RechievementData rechievement)
        {
            if (rechievement == null || AchievementAdder.shown.Contains(rechievement))
                return;
            if (AchievementPanelPatch.achievementPanel == null)
                try
                {
                    AchievementPanelPatch.achievementPanel = GameObject.Find("Hud/AchivementsElementsUI(Clone)/MainCanvasGroup/AchievementPanel").GetComponent<AchievementPanel>();
                }
                catch { }
            if (AchievementPanelPatch.achievementPanel == null)
                return;
            AchievementAdder.shown.Add(rechievement);
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
            Service.Home.AudioService.audioController_.EffectsSource.PlayOneShot(AchievementGet);
        }

        public static void ShowRechievement(string str) => ShowRechievement(RechievementData.allRechievements.GetValue(str, null));

        public static Sprite NoneSprite;

        public static Sprite RechievementSprite;

        public static Dictionary<string, Sprite> AssetBundleSprites = new Dictionary<string, Sprite>();

        public static AudioClip AchievementGet;

        public static Harmony harmonyInstance = new Harmony("synapsium.rechievement.additional");
    }
}
