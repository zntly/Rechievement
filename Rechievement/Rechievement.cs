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
using Server.Shared.Extensions;
using System.Runtime.CompilerServices;

namespace Rechievement
{
    public class RechievementData
    {
        public RechievementData() {
            allRechievements.Add(this.Name, this);
        }

        public static Dictionary<string, RechievementData> allRechievements = new Dictionary<string, RechievementData>();

        public string Name = "???";

        public string Description = "???";

        public Sprite Sprite = Utils.NoneSprite;

        public Role Role = Role.NONE;

        public Role BToS2Role = Role.NONE;

        public bool Vanilla = true;

        public bool BToS2 = true;
    }
}
