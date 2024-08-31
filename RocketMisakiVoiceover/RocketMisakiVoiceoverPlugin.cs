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
using BaseVoiceoverLib;
using System.Linq;
using Mono.Cecil;

namespace RocketMisakiVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.BaseVoiceoverLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.EnforcerGang.RocketSurvivor", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.KrononConspirator.RocketMisaki", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Schale.RocketMisakiVoiceover", "RocketMisakiVoiceover", "1.2.3")]
    public class RocketMisakiVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyboardShortcut> buttonDust, buttonHurt, buttonTitle, buttonIntro, buttonThanks, buttonMuda, buttonNani, buttonCafe1, buttonCafe4;
        public static ConfigEntry<bool> enableVoicelines;
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;
        public static SurvivorDef rocketSurvivorDef;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RocketMisakiVoiceover.rocketmisakivoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }
            SoundBanks.Init();

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the RocketMisaki Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;

            buttonTitle = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Blue Archive"), KeyboardShortcut.Empty);
            buttonIntro = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Introduction"), KeyboardShortcut.Empty);
            buttonDust = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Dust to Dust"), KeyboardShortcut.Empty);
            buttonCafe1 = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Noisy"), KeyboardShortcut.Empty);
            buttonCafe4 = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Not Interested"), KeyboardShortcut.Empty);
            buttonThanks = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Thanks"), KeyboardShortcut.Empty);
            buttonMuda = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Muda"), KeyboardShortcut.Empty);
            buttonNani = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Nani"), KeyboardShortcut.Empty);
            buttonHurt = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Hurt"), KeyboardShortcut.Empty);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("Misaki"));

            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTitle));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonIntro));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonDust));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonThanks));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMuda));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonNani));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonHurt));
        }

        private void OnLoad()
        {
            BodyIndex rocketIndex = BodyCatalog.FindBodyIndex("RocketSurvivorBody");

            SkinDef misakiSkin = null;
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(rocketIndex);
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "RocketMisakiSkinDef")
                {
                    misakiSkin = skinDef;
                    break;
                }
            }

            if (!misakiSkin)
            {
                Debug.LogError("RocketMisakiVoiceover: Rocket Misaki SkinDef not found. Voicelines will not work!");
            }
            else
            {
                VoiceoverInfo vo = new VoiceoverInfo(typeof(RocketMisakiVoiceoverComponent), misakiSkin, "RocketSurvivorBody");
                vo.selectActions += MisakiSelect;
            }

            RefreshNSE();
        }

        private void MisakiSelect(GameObject mannequinObject)
        {
            if (!enableVoicelines.Value) return;
            bool played = false;

            if (!playedSeasonalVoiceline)
            {
                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                {
                    Util.PlaySound("Play_RocketMisaki_Lobby_Newyear", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 13)
                {
                    Util.PlaySound("Play_RocketMisaki_Lobby_bday", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                {
                    Util.PlaySound("Play_RocketMisaki_Lobby_Halloween", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                {
                    Util.PlaySound("Play_RocketMisaki_Lobby_xmas", mannequinObject);
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
                    Util.PlaySound("Play_RocketMisaki_TitleDrop", mannequinObject);
                }
                else
                {
                    Util.PlaySound("Play_RocketMisaki_Lobby", mannequinObject);
                }
            }
        }

        private void InitNSE()
        {
            RocketMisakiVoiceoverComponent.nseBlock =  RegisterNSE("Play_RocketMisaki_Blocked");
            RocketMisakiVoiceoverComponent.nseEx = RegisterNSE("Play_RocketMisaki_ExSkill");
            RocketMisakiVoiceoverComponent.nseExLevel = RegisterNSE("Play_RocketMisaki_ExSkill_Level");
            RocketMisakiVoiceoverComponent.nseShout = RegisterNSE("Play_RocketMisaki_Shout");
            RocketMisakiVoiceoverComponent.nseDust = RegisterNSE("Play_RocketMisaki_ExSkill_Level_1");
            RocketMisakiVoiceoverComponent.nseHurt = RegisterNSE("Play_RocketMisaki_TakeDamage");
            RocketMisakiVoiceoverComponent.nseTitle = RegisterNSE("Play_RocketMisaki_TitleDrop");
            RocketMisakiVoiceoverComponent.nseIntro = RegisterNSE("Play_RocketMisaki_Intro");
            RocketMisakiVoiceoverComponent.nseThanks = RegisterNSE("Play_RocketMisaki_Thanks");
            RocketMisakiVoiceoverComponent.nseNani = RegisterNSE("Play_RocketMisaki_nani");
            RocketMisakiVoiceoverComponent.nseCafe1 = RegisterNSE("Play_RocketMisaki_Cafe1");
            RocketMisakiVoiceoverComponent.nseCafe4 = RegisterNSE("Play_RocketMisaki_Cafe4");
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        private NetworkSoundEventDef RegisterNSE(string eventName)
        {
            NetworkSoundEventDef nse = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            nse.eventName = eventName;
            Content.networkSoundEventDefs.Add(nse);
            nseList.Add(new NSEInfo(nse));
            return nse;
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