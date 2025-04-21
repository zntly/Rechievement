using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Achievements;
using Game.DevMenu;
using Rechievement.Patches;
using Server.Shared.State;
using Services;
using UnityEngine;
using TMPro;

using UnityEngine.SocialPlatforms.Impl;

namespace Rechievement
{
    public class Rechievement
    {
        public Rechievement() {
            Rechievement.allRechievements.Add(this.Name, this);
        }

        public string Name = "???";

        public string Description = "???";

        public Sprite Sprite = Utils.GetRoleSprite(Role.NONE);

        public Role Role = Role.NONE;

        public Role BToS2Role = Role.NONE;

        public bool Vanilla = true;

        public bool BToS2 = true;

        public static void ShowAchievement(Rechievement rechievement)
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
            // achievementItem.townPointIcon.sprite = Rechievement.rechievementIcon;
            achievementItem.gameObject.SetActive(true);
            achievementPanel.items.Add(achievementItem);
        }

        public static Dictionary<string, Rechievement> allRechievements = new Dictionary<string, Rechievement>();
    }
}
