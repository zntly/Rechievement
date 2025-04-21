using System;
using SML;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Server.Shared.Extensions;
using SalemModLoaderUI;

namespace Rechievement
{
    // Token: 0x02000004 RID: 4
    [Mod.SalemMod]
    public class Main
    {
        // Token: 0x06000008 RID: 8 RVA: 0x00002454 File Offset: 0x0000065
        public void Start()
        {
            Console.WriteLine("i'm rechieving it");
            /*AssetBundle assetBundleFromResources = FromAssetBundle.GetAssetBundleFromResources("Rechievement.resources.assetbundles.rechievement", Assembly.GetExecutingAssembly());
            assetBundleFromResources.LoadAllAssets<Texture2D>().ForEach(delegate (Texture2D s)
            {
                Main.Textures.Add(s.name, s);
            });*/
            Settings.SettingsCache.SetValue("Re-earn Achievements", ModSettings.GetBool("Re-earn Achievements", "synapsium.rechievement"));
            Settings.SettingsCache.SetValue("Extra Achievements", ModSettings.GetBool("Extra Achievements", "synapsium.rechievement"));
            Settings.SettingsCache.SetValue("BToS2 Extra Achievements", ModSettings.GetBool("BToS2 Extra Achievements", "synapsium.rechievement"));
        }
    }
}
