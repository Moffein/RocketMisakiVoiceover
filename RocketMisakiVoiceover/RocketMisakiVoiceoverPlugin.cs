using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Audio;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RocketMisakiVoiceover.Components;
using RocketMisakiVoiceover.Modules;
using System.Collections.Generic;

namespace RocketMisakiVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.EnforcerGang.RocketSurvivor")]
    [BepInDependency("com.KrononConspirator.RocketMisaki")]
    [BepInPlugin("com.Schale.RocketMisakiVoiceover", "RocketMisakiVoiceover", "1.0.0")]
    public class RocketMisakiVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;
        public static SurvivorDef rocketSurvivorDef;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            BaseVoiceoverComponent.Init();
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RocketMisakiVoiceover.rocketmisakivoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the RocketMisaki Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        private void Start()
        {
            SoundBanks.Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("Misaki"));
        }

        private void OnLoad()
        {
            bool foundSkin = false;
            BodyIndex rocketIndex = BodyCatalog.FindBodyIndex("RocketSurvivorBody");
            SurvivorIndex si = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(rocketIndex);
            rocketSurvivorDef = SurvivorCatalog.GetSurvivorDef(si);
            
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(rocketIndex);
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "RocketMisakiSkinDef")
                {
                    foundSkin = true;
                    RocketMisakiVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("RocketMisakiVoiceover: Rocket Misaki SkinDef not found. Voicelines will not work!");
            }
            else if (rocketSurvivorDef)
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;
                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == rocketSurvivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (true && enableVoicelines.Value && RocketMisakiVoiceoverComponent.requiredSkinDefs.Contains(safe))
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_RocketMisaki_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 13)
                                {
                                    Util.PlaySound("Play_RocketMisaki_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_RocketMisaki_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_RocketMisaki_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }

                                if (played)
                                {
                                    playedSeasonalVoiceline = true;
                                }
                            }
                            if (!played)
                            {
                                if (Util.CheckRoll(5f))
                                {
                                    Util.PlaySound("Play_RocketMisaki_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_RocketMisaki_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };
            }
            RocketMisakiVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");

            //Add NSE here
            nseList.Add(new NSEInfo(RocketMisakiVoiceoverComponent.nseBlock));
            nseList.Add(new NSEInfo(RocketMisakiVoiceoverComponent.nseEx));
            nseList.Add(new NSEInfo(RocketMisakiVoiceoverComponent.nseExLevel));
            RefreshNSE();
        }

        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                if (self.bodyIndex == BodyCatalog.FindBodyIndex("RocketSurvivorBody"))//(RocketMisakiVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex)))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<RocketMisakiVoiceoverComponent>();
                }
            }
        }

        private void InitNSE()
        {
            RocketMisakiVoiceoverComponent.nseBlock = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            RocketMisakiVoiceoverComponent.nseBlock.eventName = "Play_RocketMisaki_Blocked";
            Content.networkSoundEventDefs.Add(RocketMisakiVoiceoverComponent.nseBlock);

            RocketMisakiVoiceoverComponent.nseEx = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            RocketMisakiVoiceoverComponent.nseEx.eventName = "Play_RocketMisaki_ExSkill";
            Content.networkSoundEventDefs.Add(RocketMisakiVoiceoverComponent.nseEx);

            RocketMisakiVoiceoverComponent.nseExLevel = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            RocketMisakiVoiceoverComponent.nseExLevel.eventName = "Play_RocketMisaki_ExSkill_Level";
            Content.networkSoundEventDefs.Add(RocketMisakiVoiceoverComponent.nseExLevel);
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        public static List<NSEInfo> nseList = new List<NSEInfo>();
        public class NSEInfo
        {
            public NetworkSoundEventDef nse;
            public uint akId = 0u;
            public string eventName = string.Empty;

            public NSEInfo(NetworkSoundEventDef source)
            {
                this.nse = source;
                this.akId = source.akId;
                this.eventName = source.eventName;
            }

            private void DisableSound()
            {
                nse.akId = 0u;
                nse.eventName = string.Empty;
            }

            private void EnableSound()
            {
                nse.akId = this.akId;
                nse.eventName = this.eventName;
            }

            public void ValidateParams()
            {
                if (this.akId == 0u) this.akId = nse.akId;
                if (this.eventName == string.Empty) this.eventName = nse.eventName;

                if (!enableVoicelines.Value)
                {
                    DisableSound();
                }
                else
                {
                    EnableSound();
                }
            }
        }
    }
}