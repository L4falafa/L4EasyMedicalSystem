using Rocket.API;
using Rocket.API.Collections;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Lafalafa.L4EasyMedicalSystem
{

    public class EasyMedic : RocketPlugin<EasyMedicConfig>
    {

        #region load&unload
        protected override void Load()
        {

            instance = this;
            Death = new List<CSteamID>();

            UseableConsumeable.onPerformingAid += UnturnedPlayerEvents_onConsumePerformed;
            DamageTool.damagePlayerRequested += onPlayerDamaged;
            UnturnedPlayerEvents.OnPlayerUpdateStance += UnturnedPlayerEvents_UpdateStance;
            UnturnedPlayerEvents.OnPlayerDead += onPlayerDead;
            UnturnedPlayerEvents.OnPlayerUpdateHealth += onPlayerUpdateHealth;
            EffectManager.onEffectButtonClicked += onButtonClicked;

            Logger.Log("############################", ConsoleColor.Cyan);
            Logger.Log("#         EasyMedic        #", ConsoleColor.Cyan);
            Logger.Log("#       By: Lafalafa       #", ConsoleColor.Cyan);
            Logger.Log("#    discord.gg/eAkMRkv    #", ConsoleColor.Cyan);
            Logger.Log("############################", ConsoleColor.Cyan);



        }


        protected override void Unload()
        {

            UseableConsumeable.onPerformingAid -= UnturnedPlayerEvents_onConsumePerformed;
            UnturnedPlayerEvents.OnPlayerUpdateHealth -= onPlayerUpdateHealth;
            UnturnedPlayerEvents.OnPlayerUpdateStance -= UnturnedPlayerEvents_UpdateStance;
            DamageTool.damagePlayerRequested -= onPlayerDamaged;
            UnturnedPlayerEvents.OnPlayerDead -= onPlayerDead;
            EffectManager.onEffectButtonClicked -= onButtonClicked;

            Logger.Log($"{Assembly.GetName().Name} has been unloaded!", ConsoleColor.Yellow);

        }


        #endregion

        #region events

        private void onPlayerUpdateHealth(UnturnedPlayer player, byte health)
        {


            if (player.GodMode || (health > 0) || Death.Contains(player.CSteamID)) { return; }

            Death.Add(player.CSteamID);
            player.Player.equipment.onEquipRequested += onEquipRequeseted;
            player.Features.GodMode = true;
            player.Bleeding = false;
            //player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ChatManager.serverSendMessage(string.Format(instance.Translate("KNOCKED_OUT").Replace('(', '<').Replace(')', '>')), Color.white, null, player.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
            player.Player.equipment.dequip();
            EffectManager.sendUIEffect(10938, 10939, player.CSteamID, true);
            EffectManager.sendUIEffect(10936, 10937, player.CSteamID, true);
            player.Player.movement.sendPluginSpeedMultiplier(0);
            player.Player.stance.checkStance(EPlayerStance.PRONE, true);


        }
        private void onButtonClicked(Player player, string buttonName)
        {
            UnturnedPlayer uplayer = UnturnedPlayer.FromPlayer(player);

            switch (buttonName)
            {
                case "Suicide_Button":

                    uplayer.Player.movement.sendPluginSpeedMultiplier(1);
                    uplayer.Features.GodMode = false;
                    uplayer.Suicide();
                    break;
                case "CallEMS_Button":
                    UnturnedPlayer uclient;
                    Provider.clients.ForEach(client =>
                    {
                        uclient = UnturnedPlayer.FromSteamPlayer(client);
                        if (uclient.HasPermission("easymedic.revive"))
                        {
                            uclient.Player.quests.askSetMarker(uclient.CSteamID, true, uclient.Position);
                            ChatManager.serverSendMessage(string.Format(instance.Translate("HELP_TO_EMS", uplayer.CharacterName).Replace('(', '<').Replace(')', '>')), Color.white, null, uclient.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
                            ChatManager.serverSendMessage(string.Format(instance.Translate("HELP_TO_VICTIM").Replace('(', '<').Replace(')', '>')), Color.white, null, uplayer.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
                        }

                    });
                    break;
                default:
                    return;

            }



        }

        private void onPlayerDead(UnturnedPlayer player, Vector3 position)
        {

            if (!Death.Contains(player.CSteamID)) return;
            Death.Remove(player.CSteamID);
            player.Player.equipment.onEquipRequested -= onEquipRequeseted;
            player.Player.movement.sendPluginSpeedMultiplier(1);
            EffectManager.askEffectClearByID(10938, player.CSteamID);
            EffectManager.askEffectClearByID(10936, player.CSteamID);
            //player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ChatManager.serverSendMessage(string.Format(instance.Translate("SUCCESSFULLY_DEAD").Replace('(', '<').Replace(')', '>')), Color.white, null, player.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
        }



        private void onPlayerDamaged(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(parameters.player);


            if (Death.Contains(player.CSteamID))
            {
                player.Features.GodMode = false;
                shouldAllow = false;
                player.Damage(1, parameters.direction, EDeathCause.BLEEDING, ELimb.SKULL, parameters.killer);

            }

        }

        private void onEquipRequeseted(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(equipment.player);
            if (Death.Contains(player.CSteamID))
            {

                shouldAllow = false;

            }
        }

        private void UnturnedPlayerEvents_UpdateStance(UnturnedPlayer player, byte stance)
        {


            if (Death.Contains(player.CSteamID))
            {
                player.Player.stance.checkStance(EPlayerStance.PRONE, true);

            }

        }

        private void UnturnedPlayerEvents_onConsumePerformed(Player player, Player victim, ItemConsumeableAsset item, ref bool shouldAllow)
        {
            UnturnedPlayer p;

            UnturnedPlayer v;

            p = UnturnedPlayer.FromPlayer(player);

            v = UnturnedPlayer.FromPlayer(victim);



            if (!p.HasPermission("easymedic.revive") || !Death.Contains(v.CSteamID)) return;

            if (item.id == Configuration.Instance.DesfribilatorId)
            {
                v.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);

                Death.Remove(v.CSteamID);
                v.Player.equipment.onEquipRequested -= onEquipRequeseted;
                v.Player.equipment.dequip();
                v.Player.movement.sendPluginSpeedMultiplier(1);
                EffectManager.askEffectClearByID(10938, p.CSteamID);
                EffectManager.askEffectClearByID(10936, p.CSteamID);
                v.Player.equipment.onEquipRequested -= onEquipRequeseted;
                ChatManager.serverSendMessage(string.Format(instance.Translate("VICTIM_REVIVED").Replace('(', '<').Replace(')', '>')), Color.white, null, v.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
                ChatManager.serverSendMessage(string.Format(instance.Translate("MEDIC_REVIVE").Replace('(', '<').Replace(')', '>')), Color.white, null, v.SteamPlayer(), EChatMode.WELCOME, instance.Configuration.Instance.ImageUrl, true);
            }


        }
        #endregion
        public override TranslationList DefaultTranslations => new TranslationList()
        {
            {"SUCCESSFULLY_DEAD",EasyMedicNameMessage+"(color=#B0ABAA)You died, now you can`t remeber anything!(/color)"},
            {"HELP_TO_EMS",EasyMedicNameMessage+"(color=#B0ABAA){0} needs help, it`s marked on the map(/color)"},
            {"HELP_TO_VICTIM",EasyMedicNameMessage+"(color=#B0ABAA)The EMS were notified, you need wait until he arrive.(/color)"},
            {"KNOCKED_OUT",EasyMedicNameMessage+ "(color=#B0ABAA)You have been knocked out, suicide or wait a medic(/color)"},
            {"VICTIM_REVIVED",EasyMedicNameMessage+"(color=#B0ABAA)A medic revived you, take care.(/color)"},
            {"MEDIC_REVIVE",EasyMedicNameMessage+"(color=#B0ABAA)You save a player, you are the best person!(/color)"}
        };

        public List<CSteamID> Death { get; set; }

        public const string EasyMedicNameMessage = "(color=red)[(/color)(color=white)EasyMedic(/color)(color=red)](/color): ";

        public static EasyMedic instance;

    }
}
