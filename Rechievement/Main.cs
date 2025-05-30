﻿using System;
using SML;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Server.Shared.Extensions;
using SalemModLoaderUI;
using UnityEngine.UIElements;
using Home.Shared;
using Services;
using UnityEngine.SceneManagement;
using static XMLShop;
using System.Diagnostics;
using Rechievement.Patches;

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
            AssetBundle assetBundleFromResources = FromAssetBundle.GetAssetBundleFromResources("Rechievement.resources.assetbundles.rechievement", Assembly.GetExecutingAssembly());
            assetBundleFromResources.LoadAllAssets<Texture2D>().ForEach(delegate (Texture2D texture2D)
            {
                Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(texture2D.width / 2, texture2D.height / 2));
                if (texture2D.name == "Rechievement")
                    Utils.RechievementSprite = sprite;
                Utils.AssetBundleSprites.Add(texture2D.name, sprite);
            });
            assetBundleFromResources.LoadAllAssets<AudioClip>().ForEach(delegate (AudioClip audio)
            {
                if (audio.name == "AchievementGet")
                    Utils.AchievementGet = audio;
            });
            Service.Home.ApplicationService.OnSceneLoaded += delegate (SceneType sceneType, LoadSceneMode loadSceneMode)
            {
                if (Leo.IsHomeScene() || Leo.IsLoginScene())
                {
                    AchievementAdder.ClearCoroutines();
                    AchievementAdder.ClearPatches();
                    AchievementAdder.IdentityPatch = null;
                    AchievementAdder.necessities.Clear();
                    AchievementAdder.shown.Clear();
                    AchievementAdder.processed.Clear();
                }
            };
        }
    }
}
