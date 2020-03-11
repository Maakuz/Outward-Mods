﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
//using SinAPI;

namespace PvP
{
    public class PvPGUI : MonoBehaviour
    {
        public static PvPGUI Instance;

        private static readonly int WINDOW_ID = 6313531;

        public Rect m_windowRect = Rect.zero;
        public Vector2 scroll = Vector2.zero;

        private Vector2 m_virtualSize = new Vector2(1920, 1080);
        private Vector2 m_currentSize = Vector2.zero;
        public Matrix4x4 m_scaledMatrix;

        public bool showGui = false;
        public int guiPage = 0;
        public bool lastMenuToggle;

        public bool ConfirmingBattleRoyale = false;

        internal void Awake()
        {
            Instance = this;
        }

        internal void Start()
        {
            showGui = PvPGlobal.Instance.settings.Show_Menu_On_Startup;
        }

        internal void Update()
        {
            if (m_currentSize.x != Screen.width || m_currentSize.y != Screen.height)
            {
                m_scaledMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / m_virtualSize.x, Screen.height / m_virtualSize.y, 1));
                m_currentSize = new Vector2(Screen.width, Screen.height);
            }

            if (NetworkLevelLoader.Instance.IsGameplayPaused || Global.Lobby.PlayersInLobbyCount <= 0)
            {
                return;
            }

            MenuMouseFix();
        }

        internal void OnGUI()
        {
            Matrix4x4 orig = GUI.matrix;

            if (PvPGlobal.Instance.settings.Enable_Menu_Scaling)
            {
                GUI.matrix = m_scaledMatrix;
            }

            if (m_windowRect == Rect.zero || m_windowRect == null)
            {
                m_windowRect = new Rect(50, 50, 500, 300);
            }
            else
            {
                if (!ConfirmingBattleRoyale && showGui)
                {
                    m_windowRect = GUI.Window(WINDOW_ID, m_windowRect, DrawWindow, "PvP " + ModLoader.Instance.version.ToString("0.00"));
                }
                if (ConfirmingBattleRoyale || BattleRoyale.Instance.IsGameplayEnding)
                {
                    float x = Screen.width / 2 - 200;
                    float y;
                    if (Global.Lobby.LocalPlayerCount > 1)
                    {
                        y = Screen.height / 4 - 150;
                    }
                    else
                    {
                        y = Screen.height / 2 - 150;
                    }

                    Rect smallRect = new Rect(x, y, 400, 130);
                    if (ConfirmingBattleRoyale)
                    {
                        GUI.Window(WINDOW_ID, smallRect, BattleRoyaleConfirmStart, "Are you sure?");
                    }
                    else if (BattleRoyale.Instance.IsGameplayEnding)
                    {
                        GUI.Window(WINDOW_ID, smallRect, BattleRoyaleGameEnd, "Play again?");
                    }
                }
            }

            if (PvPGlobal.Instance.CurrentGame != PvPGlobal.GameModes.NONE)
            {
                CurrentGameWindow();
            }

            GUI.matrix = orig;
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, m_windowRect.width - 50, 20));

            if (GUI.Button(new Rect(m_windowRect.width - 50, 0, 45, 20), "X"))
            {
                showGui = false;
            }

            GUILayout.BeginArea(new Rect(3, 25, m_windowRect.width - 8, m_windowRect.height - 5));

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Space(150);
            if (guiPage == 0) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Main", GUILayout.Width(100)))
            {
                guiPage = 0;
            }
            if (guiPage == 1) { GUI.color = Color.green; } else { GUI.color = Color.white; }
            if (GUILayout.Button("Settings", GUILayout.Width(100)))
            {
                guiPage = 1;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUI.skin.box.alignment = TextAnchor.UpperLeft;

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(m_windowRect.height - 70));

            switch (guiPage)
            {
                case 0:
                    MainPage();
                    break;
                case 1:
                    SettingsPage();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void MainPage()
        {
            if (Global.Lobby.PlayersInLobbyCount > 0 && !NetworkLevelLoader.Instance.IsGameplayPaused)
            {
                if (!PhotonNetwork.isNonMasterClientInRoom) // only host can start games
                {
                    if (PvPGlobal.Instance.CurrentGame == PvPGlobal.GameModes.NONE && !BattleRoyale.Instance.IsGameplayEnding)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Begin Deathmatch"))
                        {
                            PvPGlobal.Instance.StartGameplay((int)PvPGlobal.GameModes.Deathmatch, "A Deathmatch has begun!");
                        }
                        if (GUILayout.Button("Begin Battle Royale"))
                        {
                            ConfirmingBattleRoyale = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        if (GUILayout.Button("End Gameplay"))
                        {
                            PvPGlobal.Instance.StopGameplay("The host has ended the game.");
                        }
                    }
                }

                GUILayout.Label("Characters: ");

                foreach (PlayerSystem ps in Global.Lobby.PlayersInLobby)
                {
                    if (!ps.ControlledCharacter.Initialized) { continue; }

                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUI.skin.label.wordWrap = false;
                    GUI.color = TeamColors[ps.ControlledCharacter.Faction];

                    string label = ps.ControlledCharacter.Name;
                    if (ps.ControlledCharacter.IsWorldHost) { label += " [Host]"; }
                    else if (ps.ControlledCharacter.IsLocalPlayer) { label += " [Local]"; }
                    else { label += " [Online]"; }
                    GUILayout.Label(label, GUILayout.Width(130));

                    GUILayout.Label("Team: ", GUILayout.Width(40));

                    if (!PhotonNetwork.isNonMasterClientInRoom || ps.ControlledCharacter.IsLocalPlayer)
                    {
                        if (ps.ControlledCharacter.Faction != Character.Factions.Player && PvPGlobal.Instance.CurrentGame == PvPGlobal.GameModes.NONE)
                        {
                            if (GUILayout.Button("<", GUILayout.Width(30)))
                            {
                                var newFaction = (Character.Factions)((int)ps.ControlledCharacter.Faction - 1);
                                PlayerManager.Instance.ChangeFactions(ps.ControlledCharacter, newFaction);
                            }
                        }
                        else { GUILayout.Space(35); }
                    }

                    GUILayout.Label(ps.ControlledCharacter.Faction.ToString(), GUILayout.Width(80));

                    if (!PhotonNetwork.isNonMasterClientInRoom || ps.ControlledCharacter.IsLocalPlayer)
                    {
                        if (ps.ControlledCharacter.Faction != Character.Factions.Golden && PvPGlobal.Instance.CurrentGame == PvPGlobal.GameModes.NONE)
                        {
                            if (GUILayout.Button(">", GUILayout.Width(30)))
                            {
                                var newFaction = (Character.Factions)((int)ps.ControlledCharacter.Faction + 1);
                                PlayerManager.Instance.ChangeFactions(ps.ControlledCharacter, newFaction);
                            }
                        }
                        else { GUILayout.Space(35); }

                        if (ps.ControlledCharacter.IsDead && PvPGlobal.Instance.CurrentGame == PvPGlobal.GameModes.NONE)
                        {
                            if (GUILayout.Button("Resurrect", GUILayout.Width(75)))
                            {
                                PvPGlobal.Instance.SendResurrect(ps.ControlledCharacter);
                            }
                        }
                    }

                    GUI.skin.label.wordWrap = true;
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Load up a character to begin...");
            }
        }

        private void SettingsPage()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            PvPGlobal.Instance.settings.Show_Menu_On_Startup = GUILayout.Toggle(PvPGlobal.Instance.settings.Show_Menu_On_Startup, "Show Menu On Startup");
            PvPGlobal.Instance.settings.Enable_Menu_Scaling = GUILayout.Toggle(PvPGlobal.Instance.settings.Enable_Menu_Scaling, "Enable Menu Scaling");
            GUILayout.Space(15);

            //GUILayout.BeginHorizontal();
            //GUILayout.Label("Online Multiplayer Limit:", GUILayout.Width(140));

            //string s = GUILayout.TextField(global.settings.Multiplayer_Limit.ToString(), GUILayout.Width(40));
            //if (int.TryParse(s, out int i))
            //{
            //    global.settings.Multiplayer_Limit = i;
            //}
            //GUILayout.EndHorizontal();
            //GUILayout.Label("This will only apply when you are the host.");
            //GUILayout.Space(20);
            //GUILayout.Label("If you want to join a game in split mode, you should join the game first, then start split.");
            GUILayout.EndVertical();
        }

        private void BattleRoyaleConfirmStart(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 400, 20));

            GUILayout.BeginArea(new Rect(15, 25, 370, 350));

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            string message = "Are you sure you want to start a Battle Royale?";
            if (SceneManagerHelper.ActiveSceneName != "Monsoon") { message += "\r\n\r\nThis will teleport all players to Monsoon."; }
            GUILayout.Label(message, GUILayout.Width(370));

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("No, go back!"))
            {
                ConfirmingBattleRoyale = false;
            }

            GUILayout.Space(30);

            if (GUILayout.Button("Yes, I'm sure!"))
            {
                ConfirmingBattleRoyale = false;

                if (BattleRoyale.Instance.CheckCanStart())
                {
                    BattleRoyale.Instance.StartBattleRoyale(false);
                    showGui = false;
                }
                else
                {
                    PvPGlobal.Instance.SendUIMessageLocal(CharacterManager.Instance.GetFirstLocalCharacter(), "There are not enough teams to start!");
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void BattleRoyaleGameEnd(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 400, 20));

            GUILayout.BeginArea(new Rect(15, 65, 370, 350));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Again"))
            {
                BattleRoyale.Instance.StartBattleRoyale(true);
                BattleRoyale.Instance.IsGameplayEnding = false;
                showGui = false;
            }

            GUILayout.Space(30);

            if (GUILayout.Button("End Lobby"))
            {
                BattleRoyale.Instance.IsGameplayEnding = false;
                showGui = false;
                if (PhotonNetwork.offlineMode)
                {
                    PvPGlobal.Instance.EndBattleRoyaleRPC();
                }
                else
                {
                    PvPGlobal.Instance.photonView.RPC("EndBattleRoyaleRPC", PhotonTargets.All, new object[0]);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void CurrentGameWindow()
        {
            GUILayout.BeginArea(new Rect(15, 15, 240, Screen.height * 0.7f));

            if (PvPGlobal.Instance.CurrentGame == PvPGlobal.GameModes.BattleRoyale && BattleRoyale.Instance.IsGameplayStarting)
            {
                GUILayout.Label("A Battle Royale is starting...");
            }
            else
            {
                GUI.skin.label.fontSize *= 2;
                TimeSpan t = TimeSpan.FromSeconds(Time.time - PvPGlobal.Instance.GameStartTime);
                GUILayout.Label(t.Minutes.ToString("0") + ":" + t.Seconds.ToString("00"), GUILayout.Height(40));
                GUI.skin.label.fontSize /= 2;

                GUILayout.Label("Current Teams:");

                foreach (KeyValuePair<Character.Factions, List<PlayerSystem>> entry in PvPGlobal.Instance.CurrentPlayers)
                {
                    GUI.color = TeamColors[entry.Key];
                    GUILayout.Label(entry.Key.ToString() + ":");
                    GUI.color = Color.white;

                    foreach (PlayerSystem player in entry.Value)
                    {
                        if (player.ControlledCharacter.IsDead)
                        {
                            GUI.color = Color.black;
                            GUILayout.Label(" - " + player.ControlledCharacter.Name + " (DEAD)");
                        }
                        else
                        {
                            GUILayout.Label(
                            " - " +
                            player.ControlledCharacter.Name +
                            " (" +
                            Math.Round((decimal)player.ControlledCharacter.Stats.CurrentHealth) +
                            " / " +
                            player.ControlledCharacter.Stats.MaxHealth +
                            ")");
                        }
                    }
                }
            }
            
            GUILayout.EndArea();
        }

        private void MenuMouseFix()
        {
            // menu mouse fix
            bool shouldUpdate = false;
            if (!lastMenuToggle && showGui)
            {
                lastMenuToggle = true;
                shouldUpdate = true;
            }
            else if (lastMenuToggle && !showGui)
            {
                lastMenuToggle = false;
                shouldUpdate = true;
            }
            if (shouldUpdate)
            {
                Character c = CharacterManager.Instance.GetFirstLocalCharacter();

                if (c.CharacterUI.PendingDemoCharSelectionScreen is Panel panel)
                {
                    if (lastMenuToggle)
                        panel.Show();
                    else
                        panel.Hide();
                }
                else if (lastMenuToggle)
                {
                    GameObject obj = new GameObject();
                    obj.transform.parent = c.transform;
                    obj.SetActive(true);

                    Panel newPanel = obj.AddComponent<Panel>();
                    At.SetValue(newPanel, typeof(CharacterUI), c.CharacterUI, "PendingDemoCharSelectionScreen");
                    newPanel.Show();
                }
            }
        }

        public Dictionary<Character.Factions, Color> TeamColors = new Dictionary<Character.Factions, Color>()
        {
            { (Character.Factions)0, Color.white },
            { (Character.Factions)1, Color.green },
            { (Character.Factions)2, Color.red * 2.0f },
            { (Character.Factions)3, Color.cyan * Color.grey },
            { (Character.Factions)4, Color.magenta },
            { (Character.Factions)5, new Color(0.8f, 0.3f, 0.3f) },
            { (Character.Factions)6, new Color(0.3f, 0.3f, 1.0f) },
            { (Character.Factions)7, Color.gray },
            { (Character.Factions)8, Color.yellow },
        };
    }
}
