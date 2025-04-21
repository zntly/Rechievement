using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Shared.State;
using UnityEngine;

namespace Rechievement
{
    public class Achievement
    {
        public Achievement() {
            Achievement.allAchievements.Add(this.Name, this);
        }

        public required string Name = "???";

        public required string Description = "???";

        public required Sprite Sprite;

        public Role Role = Role.NONE;

        public Role BToS2Role = Role.NONE;

        public bool Vanilla = true;

        public bool BToS2 = true;

        public static Dictionary<string, Achievement> allAchievements = new Dictionary<string, Achievement>();
    }
}
