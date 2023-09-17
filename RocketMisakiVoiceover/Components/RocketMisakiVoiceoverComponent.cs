using System.Collections.Generic;
using BaseVoiceoverLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RocketMisakiVoiceover.Components
{
    public class RocketMisakiVoiceoverComponent : BaseVoiceoverComponent
    {
        public static NetworkSoundEventDef nseBlock;
        public static NetworkSoundEventDef nseEx;
        public static NetworkSoundEventDef nseExLevel;

        private float levelCooldown = 0f;
        private float blockedCooldown = 0f;
        private float lowHealthCooldown = 0f;
        private float secondaryCooldown = 0f;
        private float specialCooldown = 0f;
        private bool acquiredScepter = false;

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(scepterIndex) > 0) acquiredScepter = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (specialCooldown > 0f) specialCooldown -= Time.fixedDeltaTime;
            if (secondaryCooldown > 0f) secondaryCooldown -= Time.fixedDeltaTime;
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
            if (blockedCooldown > 0f) blockedCooldown -= Time.fixedDeltaTime;
            if (lowHealthCooldown > 0f) lowHealthCooldown -= Time.fixedDeltaTime;
        }

        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseBlock, 0.8f, false);
            if (played) blockedCooldown = 30f;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_RocketMisaki_Defeat", 4f, true);
        }

        public override void PlayHurt(float percentHPLost)
        {
            if (percentHPLost >= 0.1f)
            {
                TryPlaySound("Play_RocketMisaki_TakeDamage", 0f, false);
            }
        }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = TryPlaySound("Play_RocketMisaki_LevelUp", 5.7f, false);
            if (played) levelCooldown = 60f;
        }

        public override void PlayLowHealth()
        {
            if (lowHealthCooldown > 0f) return;
            if (TryPlaySound("Play_RocketMisaki_LowHealth", 1.2f, false)) lowHealthCooldown = 60f;
        }

        public override void PlaySecondaryAuthority(GenericSkill skill)
        {
            if (secondaryCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseEx, 1.46f, false);
            if (played) secondaryCooldown = 10f;
        }

        public override void PlaySpawn()
        {
            TryPlaySound("Play_RocketMisaki_Spawn", 2.4f, true);
        }

        public override void PlaySpecialAuthority(GenericSkill skill)
        {
            if (specialCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseExLevel, 1.85f, false);
            if (played) specialCooldown = 10f;
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_RocketMisaki_Victory", 3.3f, false);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_RocketMisaki_TeleporterStart", 3f, false);
        }

        public override void PlayVictory()
        {
            TryPlaySound("Play_RocketMisaki_Memorial4", 12.5f, true);
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (scepterIndex != ItemIndex.None && itemIndex == scepterIndex)
            {
                PlayAcquireScepter();
            }
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (id == DLC1Content.Items.MoreMissile)
                {
                    PlayAcquireICBM();
                }
                else if (id == RoR2Content.Items.Squid)
                {
                    PlayBadItem();
                }
                else if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_RocketMisaki_AcquireScepter", 21.5f, true);
            acquiredScepter = true;
        }

        public void PlayAcquireICBM()
        {
            TryPlaySound("Play_RocketMisaki_CommonSkill", 1.5f, false);
        }

        public void PlayBadItem()
        {
            TryPlaySound("Play_RocketMisaki_nani", 0.5f, false);
        }

        public void PlayAcquireLegendary()
        {
            if (Util.CheckRoll(50f))
            {
                TryPlaySound("Play_RocketMisaki_Relationship_Short", 3f, false);
            }
            else
            {
                TryPlaySound("Play_RocketMisaki_Relationship", 7f, false);
            }
        }
    }
}
