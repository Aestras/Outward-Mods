﻿using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SharedModConfig;
using Partiality.Modloader;

namespace MPLimitRemover
{
    public class ModBase : PartialityMod
    {
        public string ID = "Custom Player Limit";
        public double version = 2.22;

        public ModBase()
        {
            this.ModID = ID;
            this.Version = version.ToString("0.00");
            this.author = "Sinai";
        }

        public static PlayerLimitRemover limitRemover;

        public override void OnEnable()
        {
            base.OnEnable();

            GameObject obj = new GameObject();
            GameObject.DontDestroyOnLoad(obj);
            limitRemover = obj.AddComponent<PlayerLimitRemover>();
        }
    }

    public class Settings
    {
        public static string PlayerLimit = "PlayerLimit";
    }

    public class PlayerLimitRemover : MonoBehaviour
    {
        public ModConfig config;

        internal void Awake()
        {
            // fixes
            On.PauseMenu.Show += ShowPatch;
            On.PauseMenu.Update +=UpdatePatch;
            On.RestingMenu.UpdatePanel += RestingPanelPatch;
        }

        internal void Start()
        {
            config = SetupConfig();
            config.Register();
        }

        internal void Update()
        {
            if (Global.Lobby.PlayersInLobbyCount < 1 || NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                return;
            }

            var limitInt = (int)(float)config.GetValue(Settings.PlayerLimit);

            // handle custom multiplayer limit 
            if (PhotonNetwork.inRoom && PhotonNetwork.isMasterClient)
            {
                // if the room limit is not set to our custom value, do that.
                if (PhotonNetwork.room.maxPlayers != limitInt)
                {
                    PhotonNetwork.room.maxPlayers = limitInt;
                }

                // not sure if this is necessary
                if (!PhotonNetwork.room.open && PhotonNetwork.room.playerCount < limitInt)
                {
                    PhotonNetwork.room.open = true;
                }
            }
        }

        // resting panel fix
        private void RestingPanelPatch(On.RestingMenu.orig_UpdatePanel orig, RestingMenu self)
        {
            At.Call(self, "RefreshSkylinePosition", new object[0]);

            int num = 0;
            bool flag = true;
            bool flag2 = true;

            var m_otherPlayerUIDs = At.GetValue(typeof(RestingMenu), self, "m_otherPlayerUIDs") as List<UID>;

            if (Global.Lobby.PlayersInLobbyCount - 1 != m_otherPlayerUIDs.Count)
            {
                self.InitPlayerCursors();
                flag = false;
                flag2 = false;
            }
            else
            {
                var m_sldOtherPlayerCursors = At.GetValue(typeof(RestingMenu), self, "m_sldOtherPlayerCursors") as Slider[];

                for (int i = 0; i < m_otherPlayerUIDs.Count; i++)
                {
                    Character characterFromPlayer = CharacterManager.Instance.GetCharacterFromPlayer(m_otherPlayerUIDs[i]);
                    if (characterFromPlayer != null)
                    {
                        if (CharacterManager.Instance.RestingPlayerUIDs.Contains(characterFromPlayer.UID))
                        {
                            flag2 &= characterFromPlayer.CharacterResting.DonePreparingRest;
                        }
                        else
                        {
                            flag = false;
                        }

                        if (m_sldOtherPlayerCursors.Length - 1 >= i)
                        {
                            m_sldOtherPlayerCursors[i].value = (float)characterFromPlayer.CharacterResting.TotalRestTime;
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                }
            }

            for (int j = 0; j < SplitScreenManager.Instance.LocalPlayerCount; j++)
            {
                flag &= (SplitScreenManager.Instance.LocalPlayers[j].AssignedCharacter != null);
            }
            flag2 = (flag2 && flag);

            var m_restingCanvasGroup = At.GetValue(typeof(RestingMenu), self, "m_restingCanvasGroup") as CanvasGroup;
            var m_waitingForOthers = At.GetValue(typeof(RestingMenu), self, "m_waitingForOthers") as Transform;
            var m_waitingText = At.GetValue(typeof(RestingMenu), self, "m_waitingText") as Text;

            m_restingCanvasGroup.interactable = (flag && !(self as UIElement).LocalCharacter.CharacterResting.DonePreparingRest);
            if (m_waitingForOthers)
            {
                if (m_waitingForOthers.gameObject.activeSelf == m_restingCanvasGroup.interactable)
                {
                    m_waitingForOthers.gameObject.SetActive(!m_restingCanvasGroup.interactable);
                }
                if (m_waitingText && m_waitingForOthers.gameObject.activeSelf)
                {
                    m_waitingText.text = LocalizationManager.Instance.GetLoc((!flag2) ? "Sleep_Title_Waiting" : "Rest_Title_Resting");
                }
            }

            var m_restingActivityDisplays = At.GetValue(typeof(RestingMenu), self, "m_restingActivityDisplays") as RestingActivityDisplay[];
            var ActiveActivities = At.GetValue(typeof(RestingMenu), self, "ActiveActivities") as RestingActivity.ActivityTypes[];

            for (int k = 0; k < m_restingActivityDisplays.Length; k++)
            {
                num += m_restingActivityDisplays[k].AssignedTime;
            }
            for (int l = 0; l < m_restingActivityDisplays.Length; l++)
            {
                if (ActiveActivities[l] != RestingActivity.ActivityTypes.Guard || CharacterManager.Instance.BaseAmbushProbability > 0)
                {
                    m_restingActivityDisplays[l].MaxValue = 24 - (num - m_restingActivityDisplays[l].AssignedTime);
                }
                else
                {
                    m_restingActivityDisplays[l].MaxValue = 0;
                }
            }

            var m_sldLocalPlayerCursor = At.GetValue(typeof(RestingMenu), self, "m_sldLocalPlayerCursor") as Slider;

            if (m_sldLocalPlayerCursor)
            {
                m_sldLocalPlayerCursor.value = (float)num;
            }

            var m_lastTotalRestTime = (int)At.GetValue(typeof(RestingMenu), self, "m_lastTotalRestTime");

            bool flag3 = false;
            if (m_lastTotalRestTime != num)
            {
                flag3 = true;
                m_lastTotalRestTime = num;
                At.SetValue(m_lastTotalRestTime, typeof(RestingMenu), self, "m_lastTotalRestTime");
                self.OnConfirmTimeSelection(num);
            }

            var m_tryRest = (bool)At.GetValue(typeof(RestingMenu), self, "m_tryRest");

            At.Call(self, "RefreshOverviews", new object[] { (flag3 && !m_tryRest) });
        }

        /*
         * -------- PAUSE MENU HOOKS CREDIT TO ASHNAL AND FAEDAR --------
        */

        // fix pause menu 1
        public static void ShowPatch(On.PauseMenu.orig_Show orig, PauseMenu self)
        {
            orig(self);
            Button onlineButton = typeof(PauseMenu).GetField("m_btnToggleNetwork", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self) as Button;

            //Due to spawning bugs, only allow disconnect if you are the master, or if you are a client with no splitscreen, force splitscreen to quit before disconnect
            if (PhotonNetwork.isMasterClient || SplitScreenManager.Instance.LocalPlayerCount == 1)
            {
                onlineButton.interactable = true;
            }

            SetSplitButtonInteractable(self);

            //If this is used with a second splitscreen player both players load in missing inventory. Very BAD. Disabled for now.
            //Button findMatchButton = typeof(PauseMenu).GetField("m_btnFindMatch", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Button;
            //findMatchButton.interactable = PhotonNetwork.offlineMode;
        }

        // fix pause menu 2
        //for some reason the update function also forces the split button interactable, so we have to override it here too
        public static void UpdatePatch(On.PauseMenu.orig_Update orignal, PauseMenu instance)
        {
            orignal(instance);
            SetSplitButtonInteractable(instance);
        }

        public static void SetSplitButtonInteractable(PauseMenu instance)
        {
            //Debug.Log("isMasterClient: " + PhotonNetwork.isMasterClient);
            Button splitButton = typeof(PauseMenu).GetField("m_btnSplit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance) as Button;
            if (!PhotonNetwork.isMasterClient || !PhotonNetwork.isNonMasterClientInRoom)
            {
                splitButton.interactable = true;
            }
        }

        // =========== settings =============

        private ModConfig SetupConfig()
        {
            var newConfig = new ModConfig
            {
                ModName = "MP Limit Remover",
                SettingsVersion = 1.0,
                Settings = new List<BBSetting>
                {
                    new FloatSetting
                    {
                        Name = Settings.PlayerLimit,
                        Description = "Max number of Players in room (when you are host)",
                        DefaultValue = 4.0f,
                        RoundTo = 0,
                        MinValue = 1f,
                        MaxValue = 20f,
                        ShowPercent = false
                    }
                }
            };

            return newConfig;
        }
    }
}
