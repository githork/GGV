﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using GunGameV.Shared;

namespace GunGameV.Client
{
    public class Client : BaseScript
    {
        private List<User> users = new List<User>();
        private List<User> usersInMatch = new List<User>();
        private User user;
        private Match currentMatch;
        private Map currentMap;
        private HUD hud;
        private long unixTimestamp = 0;

        public Client()
        {
            Debug.WriteLine("GGV CLIENT");
            hud = new HUD();
        }

        public List<User> Users { get => users; }

        private void SendNuiMessage(string type, object payload)
        {
            dynamic message = new ExpandoObject();
            message.type = type;
            message.payload = payload;

            API.SendNuiMessage(JsonConvert.SerializeObject(message));
        }
        [Tick]
        private async Task OnPlayerReady()
        {
            await Delay(0);

            if (API.NetworkIsSessionStarted())
            {
                TriggerServerEvent("GGV.Setup");
                Tick -= OnPlayerReady;
            }
        }
        [Tick]
        private async Task MatchWatcher()
        {
            if (user != null && currentMatch != null)
            {
                if (user.InMatch)
                {
                    WeaponHash currentWeapon = (WeaponHash) currentMatch.GetWeapon(user.gameStats.Score);

                    if(!Game.PlayerPed.Weapons.HasWeapon(currentWeapon))
                    {
                        Game.PlayerPed.Weapons.RemoveAll();
                        Game.PlayerPed.Weapons.Give(currentWeapon, 250, true, true);
                    }

                    if(Game.PlayerPed.Weapons.Current.Hash != WeaponHash.Unarmed && Game.PlayerPed.Weapons.Current.Hash != currentWeapon)
                    {
                        Game.PlayerPed.Weapons.Remove(Game.PlayerPed.Weapons.Current);
                    }

                    hud.Time = TimeSpan.FromSeconds(currentMatch.EndTime - unixTimestamp).ToString(@"mm\:ss");
                    hud.Draw();
                }
            }
        }
        [EventHandler("GGV.Sync.Users")]
        private void SyncUsers(string jsonUsers)
        {
            users = JsonConvert.DeserializeObject<List<User>>(jsonUsers);
            usersInMatch = users.FindAll(user => user.InMatch == true);
            usersInMatch.Sort((x, y) => y.gameStats.CompareTo(x.gameStats));
            user = users.Find(x => x.ID == API.GetPlayerServerId(Game.Player.Handle).ToString());

            if (user != null)
            {
                if (usersInMatch[0].ID != user.ID) hud.Highscore = usersInMatch[0].gameStats.Score;
                else if (usersInMatch[1] != null) hud.Highscore = usersInMatch[1].gameStats.Score;
                else hud.Highscore = 0;

                hud.Score = user.gameStats.Score;
            }
        }
        [EventHandler("GGV.Sync.Match")]
        private void SyncMatch(string jsonMatch)
        {
            currentMatch = JsonConvert.DeserializeObject<Match>(jsonMatch);
        }
        [EventHandler("GGV.Sync.Time")]
        private void SyncTime(long _unixTimestamp)
        {
            unixTimestamp = _unixTimestamp;
        }
        [EventHandler("GGV.Match.Join")]
        private void JoinMatch()
        {
            if (currentMatch != null) {
                currentMap = new Map(currentMatch.Map);
                Exports["spawnmanager"].setAutoSpawnCallback(new Action(() =>
                {
                    currentMap.Spawn();
                }));
                Exports["spawnmanager"].forceRespawn();
            }
        }
        [EventHandler("GGV.Match.Leave")]
        private void LeaveMatch()
        {
            Exports["spawnmanager"].setAutoSpawnCallback(null);
            Exports["spawnmanager"].forceRespawn();
            currentMap = null;
        }
    }
}