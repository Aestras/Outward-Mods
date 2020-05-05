﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SideLoader;

namespace Combat_Dummy
{
    public class ModGUI : MonoBehaviour
    {
        private Rect m_rect = new Rect(5, 5, 360, 460);

        private int m_windowPage = 0;
        private string m_newDummyName = "Dummy 1";

        // inspecting dummy
        private DummyCharacter m_dummyCharacter;

        internal void OnGUI()
        {
            var orig = GUI.skin;
            GUI.skin = SideLoader.UI.UIStyles.WindowSkin;
            m_rect = GUI.Window(-5, m_rect, WindowFunction, "Combat Dummy Menu");
            GUI.skin = orig;
        }

        private void WindowFunction(int id)
        {
            GUI.DragWindow(new Rect(60, 0, m_rect.width - 20, 20));

            if (m_windowPage != 0)
            {
                if (GUI.Button(new Rect(3, 3, 55, 20), "< Home"))
                {
                    m_dummyCharacter = null;
                    m_windowPage = 0;
                    return;
                }
            }

            GUILayout.BeginArea(new Rect(3, 25, m_rect.width - 6, m_rect.height - 30), GUI.skin.box);

            if (m_windowPage == 0)
            {
                if (Global.Lobby.PlayersInLobbyCount > 0 && NetworkLevelLoader.Instance.AllPlayerDoneLoading && !NetworkLevelLoader.Instance.InLoading)
                {
                    BoldTitle("Spawn new dummy");

                    GUILayout.BeginHorizontal();

                    GUILayout.Label("Dummy name:", GUILayout.Width(120));
                    m_newDummyName = GUILayout.TextField(m_newDummyName, GUILayout.Width(120));
                    if (GUILayout.Button("Add", GUILayout.Width(40)))
                    {
                        if (string.IsNullOrEmpty(m_newDummyName))
                        {
                            m_newDummyName = "Dummy " + CombatDummyMod.ActiveDummies.Count + 1;
                        }
                        var dummy = CombatDummyMod.AddDummy(m_newDummyName);
                        m_dummyCharacter = dummy;
                        m_windowPage = 1;

                        m_newDummyName = $"Dummy {CombatDummyMod.ActiveDummies.Count + 1}";
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.Space(5);

                    BoldTitle("Active Dummies:");

                    var list = CombatDummyMod.ActiveDummies;

                    if (list.Count < 1)
                    {
                        GUILayout.Label("Spawn a dummy to start...");
                    }
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            var dummy = list[i];
                            if (GUILayout.Button(dummy.Name))
                            {
                                m_dummyCharacter = dummy;
                                m_windowPage = 1;
                            }
                            GUILayout.Label("AI:", GUILayout.Width(20));
                            GUI.color = Color.green;
                            if (GUILayout.Button("Enable", GUILayout.Width(60)))
                            {
                                dummy.SetAIEnabled(true);
                            }
                            GUI.color = Color.red;
                            if (GUILayout.Button("Disable", GUILayout.Width(60)))
                            {
                                dummy.SetAIEnabled(false);
                            }
                            GUI.color = Color.white;
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Load up a character to start...");
                }
            }
            else
            {
                if (m_dummyCharacter == null)
                {
                    m_windowPage = 0;
                }
                else
                {
                    AIButtons();

                    if (!m_dummyCharacter.CharacterExists)
                    {
                        GUILayout.Label("Character has despawned...");
                    }
                    else
                    {
                        EditInspectingDummy();
                    }

                    if (GUILayout.Button("Reset and Apply"))
                    {
                        m_dummyCharacter.SpawnOrReset(m_dummyCharacter.Config);
                    }

                    GUILayout.Space(10);

                    GUI.color = Color.red;
                    if (GUILayout.Button("Destroy Dummy"))
                    {
                        CombatDummyMod.DestroyDummy(m_dummyCharacter);
                        m_dummyCharacter = null;
                        m_windowPage = 0;
                    }
                    GUI.color = Color.white;
                }
            }

            GUILayout.EndArea();
        }

        private void EditInspectingDummy()
        {
            var cfg = m_dummyCharacter.Config;

            BoldTitle("Setup Dummy:");

            IntEdit("Weapon ID", ref cfg.Weapon);

            FactionEdit(cfg);

            FloatEdit("Chance to Dodge", ref cfg.ChanceToDefend);

            BoldTitle("Stats");

            FloatEdit("Health", ref cfg.Health);
            FloatEdit("Impact Res", ref cfg.ImpactResist);
            BoldTitle("Resistances");
            DamageTypesEdit("Resistances", ref cfg.Damage_Resists);
            BoldTitle("Damage Bonus");
            DamageTypesEdit("Damage Bonuses", ref cfg.Damage_Bonus);
        }

        private void AIButtons()
        {
            if (m_dummyCharacter != null && m_dummyCharacter.CharacterExists)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Combat AI:");
                GUI.color = Color.green;
                if (GUILayout.Button("Enable"))
                {
                    m_dummyCharacter.SetAIEnabled(true);
                }
                GUI.color = Color.red;
                if (GUILayout.Button("Disable"))
                {
                    m_dummyCharacter.SetAIEnabled(false);
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
        }

        public void BoldTitle(string label)
        {
            var orig = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label($"<b>{label}</b>");
            GUI.skin.label.alignment = orig;
        }

        public void FloatEdit(string label, ref float stat)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            var input = stat.ToString();
            input = GUILayout.TextField(input);
            if (input != stat.ToString() && float.TryParse(input, out float f))
            {
                stat = f;
            }
            GUILayout.EndHorizontal();
        }

        public void IntEdit(string label, ref int stat)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            var input = stat.ToString();
            input = GUILayout.TextField(input);
            if (input != stat.ToString() && int.TryParse(input, out int i))
            {
                stat = i;
            }
            GUILayout.EndHorizontal();
        }

        public void DamageTypesEdit(string label, ref float[] stats)
        {
            
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            for (int i = 0; i < 6; i++)
            {
                if (i == 3) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }

                GUILayout.Label(((DamageType.Types)i).ToString(), GUILayout.Width(70));

                var input = stats[i].ToString();
                input = GUILayout.TextField(input, GUILayout.Width(35));
                if (input != stats[i].ToString() && float.TryParse(input, out float f))
                {
                    stats[i] = f;
                }

            }
            GUILayout.EndHorizontal();
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        private void FactionEdit(DummyConfig config)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Faction:", GUILayout.Width(100));
            if (GUILayout.Button("<"))
            {
                if ((int)config.Faction > 1)
                {
                    config.Faction -= 1;
                }
            }
            if (GUILayout.Button(">"))
            {
                if ((int)config.Faction < Enum.GetNames(typeof(Character.Factions)).Length - 1)
                {
                    config.Faction += 1;
                }
            }
            GUILayout.Label(config.Faction.ToString(), GUILayout.Width(180));
            GUILayout.EndHorizontal();
        }
    }
}
