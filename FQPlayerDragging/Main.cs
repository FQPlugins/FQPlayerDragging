using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FQPlayerDragging
{
    public class Main : RocketPlugin<Config>
    {
        public Dictionary<ulong, ulong> draggedPlayers;
        public static Main Instance;
        protected override void Load()
        {
            Instance = this;
            draggedPlayers = new Dictionary<ulong, ulong>();
            InteractableVehicle.OnPassengerAdded_Global += InteractableVehicle_OnPassengerAdded_Global;
            InteractableVehicle.OnPassengerRemoved_Global += InteractableVehicle_OnPassengerRemoved_Global;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += UnturnedPlayerEvents_OnPlayerUpdateGesture;
            UnturnedPlayerEvents.OnPlayerDeath += UnturnedPlayerEvents_OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerUpdatePosition += UnturnedPlayerEvents_OnPlayerUpdatePosition;
        }

        private void UnturnedPlayerEvents_OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (draggedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                draggedPlayers.Remove(player.CSteamID.m_SteamID);
            }

            if (draggedPlayers.ContainsValue(player.CSteamID.m_SteamID))
            {
                var drag = draggedPlayers.FirstOrDefault(x => x.Value == player.CSteamID.m_SteamID);
                draggedPlayers.Remove(drag.Key);
            }
        }

        private void UnturnedPlayerEvents_OnPlayerUpdatePosition(UnturnedPlayer player, Vector3 position)
        {
            if (draggedPlayers.TryGetValue(player.CSteamID.m_SteamID, out ulong value))
            {
                var dragged = UnturnedPlayer.FromCSteamID(new CSteamID(value));
                if (Vector3.Distance(player.Position, dragged.Position) >= Configuration.Instance.dragDistance && !player.IsInVehicle) dragged.Teleport(player);
            }
        }

        private void UnturnedPlayerEvents_OnPlayerUpdateGesture(UnturnedPlayer player, UnturnedPlayerEvents.PlayerGesture gesture)
        {
            if (gesture == UnturnedPlayerEvents.PlayerGesture.SurrenderStart && player.HasPermission(Configuration.Instance.dragPermission))
            {
                var patient = UnturnedPlayer.FromSteamPlayer(Provider.clients.FirstOrDefault(x => Vector3.Distance(UnturnedPlayer.FromSteamPlayer(x).Position, player.Position) < Configuration.Instance.dragStartDistance));

                if (patient == null) return;
                if (patient.Player.animator.gesture != EPlayerGesture.ARREST_START) return;
                if (draggedPlayers.ContainsValue(patient.CSteamID.m_SteamID))
                {
                    ChatManager.serverSendMessage(Translate("StoppedDragging"), Color.white, null, player.SteamPlayer(), EChatMode.SAY, Configuration.Instance.serverIcon, true);
                    var x = draggedPlayers.FirstOrDefault(y => y.Value == patient.CSteamID.m_SteamID);
                    draggedPlayers.Remove(x.Key);
                    return;

                }

                if (draggedPlayers.ContainsKey(player.CSteamID.m_SteamID))
                {
                    ChatManager.serverSendMessage(Translate("StartedDragging"), Color.white, null, player.SteamPlayer(), EChatMode.SAY, Configuration.Instance.serverIcon, true);
                    draggedPlayers[player.CSteamID.m_SteamID] = patient.CSteamID.m_SteamID;
                    return;
                }

                ChatManager.serverSendMessage(Translate("StartedDragging"), Color.white, null, player.SteamPlayer(), EChatMode.SAY, Configuration.Instance.serverIcon, true);
                draggedPlayers.Add(player.CSteamID.m_SteamID, patient.CSteamID.m_SteamID);

            }
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (draggedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                draggedPlayers.Remove(player.CSteamID.m_SteamID);
            }

            if (draggedPlayers.ContainsValue(player.CSteamID.m_SteamID))
            {
                var drag = draggedPlayers.FirstOrDefault(x => x.Value == player.CSteamID.m_SteamID);
                draggedPlayers.Remove(drag.Key);
            }
        }

        private void InteractableVehicle_OnPassengerRemoved_Global(InteractableVehicle arg1, int arg2, Player arg3)
        {
            try
            {
                var player = UnturnedPlayer.FromPlayer(arg3);
                if (draggedPlayers.ContainsKey(player.CSteamID.m_SteamID))
                {
                    arg1.forceRemovePlayer(out byte seat, new CSteamID(draggedPlayers[player.CSteamID.m_SteamID]), out var point, out byte angle);
                    var tpPlayer = UnturnedPlayer.FromCSteamID(new CSteamID(draggedPlayers[player.CSteamID.m_SteamID]));
                    tpPlayer.Teleport(player.Position, player.Rotation);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void InteractableVehicle_OnPassengerAdded_Global(InteractableVehicle arg1, int arg2)
        {
            try
            {
                var player = UnturnedPlayer.FromCSteamID((CSteamID)arg1.passengers.FirstOrDefault(x => draggedPlayers.ContainsKey(x.player.playerID.steamID.m_SteamID)).player.playerID.steamID.m_SteamID);

                if (player != null)
                {

                    if (Main.Instance.draggedPlayers.ContainsKey(player.CSteamID.m_SteamID))
                    {
                        Main.Instance.draggedPlayers.TryGetValue(player.CSteamID.m_SteamID, out ulong value);

                        if (arg1.tryAddPlayer(out byte toSeatIndex, UnturnedPlayer.FromCSteamID((CSteamID)value).Player))
                        {
                            VehicleManager.ServerForcePassengerIntoVehicle(UnturnedPlayer.FromCSteamID((CSteamID)value).Player, arg1);
                            arg1.findPlayerSeat((CSteamID)value, out byte fromSeatIndex);
                            arg1.swapPlayer(fromSeatIndex, toSeatIndex);
                        }

                    }

                }
            }
            catch (Exception ex)
            {

            }
        }

        protected override void Unload()
        {
            InteractableVehicle.OnPassengerAdded_Global -= InteractableVehicle_OnPassengerAdded_Global;
            InteractableVehicle.OnPassengerRemoved_Global -= InteractableVehicle_OnPassengerRemoved_Global;
            U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= UnturnedPlayerEvents_OnPlayerUpdateGesture;
            UnturnedPlayerEvents.OnPlayerDeath -= UnturnedPlayerEvents_OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerUpdatePosition -= UnturnedPlayerEvents_OnPlayerUpdatePosition;
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "StartedDragging", "<color=blue>[FQ]</color> Started dragging"},
            { "StoppedDragging", "<color=blue>[FQ]</color> Stopped dragging"},
        };

    }
}
