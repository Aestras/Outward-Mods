﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace NecromancerSkills
{
    public class SummonSkeleton : Effect // Inherits from the game's "Effect" class, so it works with those systems automatically.
    {
		//public static readonly float HealthCost = 0.10f; // 10% of current health

		// Setup (called from SkillManager init)
		#region Summon Skill Setup
		public static void SetupSummon()
		{
			var resurrect = ResourcesPrefabManager.Instance.GetItemPrefab(8890103) as Skill;

			// destroy the existing skills, but keep the rest (VFX / Sound).
			DestroyImmediate(resurrect.transform.Find("Lightning").gameObject);
			DestroyImmediate(resurrect.transform.Find("SummonSoul").gameObject);

			var effects = new GameObject("Effects");
			effects.transform.parent = resurrect.transform;
			effects.AddComponent<SummonSkeleton>();

			// setup custom blade visuals
			var blade = ResourcesPrefabManager.Instance.GetItemPrefab(2598500) as Weapon;
			if (blade.VisualPrefab.transform.Find("Weapon3DVisual").GetComponent<MeshRenderer>() is MeshRenderer mesh)
			{
				mesh.material.color = new Color(-0.5f, 1.5f, -0.5f);
			}
		}

		#endregion


		protected override bool TryTriggerConditions() // The game checks this before it activates the effect. Note that any mana/stamina/item costs will already be consumed.
		{
			float healthcost = ModBase.settings.Summon_HealthCost * this.m_affectedCharacter.Stats.MaxHealth;
			// check player has enough HP
			if (this.m_affectedCharacter.Stats.CurrentHealth - healthcost <= 0)
			{
				this.m_affectedCharacter.CharacterUI.ShowInfoNotification("You do not have enough health to do that!");
				// refund the cooldown and costs
				if (this.ParentItem is Skill skill)
				{
					skill.ResetCoolDown();
					m_affectedCharacter.Stats.SetMana(m_affectedCharacter.Stats.CurrentMana + skill.ManaCost);
				}
				return false;
			}
			else // caster has enough HP
			{
				return true;
			}
		}

		protected override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            if (SummonManager.Instance == null) { return; }

			bool armyOfDeathLearned = _affectedCharacter.Inventory.SkillKnowledge.IsItemLearned(8890108);

			int MaxSummons = armyOfDeathLearned ? ModBase.settings.Summon_MaxSummons_WithArmyOfDeath : ModBase.settings.Summon_MaxSummons_NoArmyOfDeath;

			if (SummonManager.Instance.SummonedCharacters.ContainsKey(_affectedCharacter.UID))
			{
				var list = SummonManager.Instance.SummonedCharacters[_affectedCharacter.UID];

				if (list.Count() == MaxSummons)
				{
					// kill weakest current summon
					try { SummonManager.Instance.FindWeakestSummon(_affectedCharacter.UID).GetComponent<Character>().Stats.ReceiveDamage(999f); } 
					catch { }
				}
			}

			// custom health cost for casting
			_affectedCharacter.Stats.UseBurntHealth = ModBase.settings.Summon_BurnsHealth;
			float healthcost = ModBase.settings.Summon_HealthCost * _affectedCharacter.Stats.MaxHealth;
			_affectedCharacter.Stats.ReceiveDamage(healthcost);
			_affectedCharacter.Stats.UseBurntHealth = true;

			// only host should do this
			if (!PhotonNetwork.isNonMasterClientInRoom)
			{
				var uid = UID.Generate().ToString();
				int sceneViewID = PhotonNetwork.AllocateSceneViewID();

				bool insidePlagueAura = PlagueAuraProximityCondition.IsInsidePlagueAura(_affectedCharacter.transform.position);

				// this function will handle RPC through the in-game systems, no need to RPC call it.
				SummonManager.Instance.SummonSpawn(_affectedCharacter, uid, sceneViewID, insidePlagueAura);

				// this function is for setting things up locally for all characters.
				RPCManager.Instance.photonView.RPC("SendSummonSpawn", PhotonTargets.All, new object[]
				{
					_affectedCharacter.UID.ToString(), 
					uid, 
					sceneViewID,
				    insidePlagueAura
				});
			}
		}
	}
}
