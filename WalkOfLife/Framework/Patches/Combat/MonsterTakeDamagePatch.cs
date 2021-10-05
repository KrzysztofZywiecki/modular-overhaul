﻿using HarmonyLib;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheLion.Stardew.Common.Extensions;
using TheLion.Stardew.Common.Harmony;

namespace TheLion.Stardew.Professions.Framework.Patches
{
	internal class MonsterTakeDamagePatch : BasePatch
	{
		/// <summary>Construct an instance.</summary>
		internal MonsterTakeDamagePatch()
		{
			Prefix = new HarmonyMethod(GetType(), nameof(MonsterTakeDamagePrefix));
			Postfix = new HarmonyMethod(GetType(), nameof(MonsterTakeDamagePostfix));
		}

		/// <inheritdoc/>
		public override void Apply(Harmony harmony)
		{
			foreach (var targetMethod in TargetMethods())
			{
				try
				{
					Original = targetMethod;
					base.Apply(harmony);
				}
				catch
				{
					continue;
				}
			}
		}

		#region harmony patches

		/// <summary>Patch to add Poacher assassination attempt.</summary>
		[HarmonyPrefix]
		private static bool MonsterTakeDamagePrefix(Monster __instance, ref int __result, ref int ___slideAnimationTimer, int damage, int xTrajectory, int yTrajectory, bool isBomb, Farmer who)
		{
			try
			{
				if (damage <= 0 || isBomb || !ModEntry.IsSuperModeActive ||
					ModEntry.SuperModeIndex != Util.Professions.IndexOf("Poacher") ||
					who.CurrentTool is not MeleeWeapon weapon || weapon.isOnSpecial) return true; // run original logic

				if (__instance is Bug bug && bug.isArmoredBug.Value && !weapon.hasEnchantmentOfType<BugKillerEnchantment>() // skip armored bugs
					|| __instance is LavaCrab && __instance.Sprite.currentFrame % 4 == 0 // skip shelled lava crabs
					|| __instance is RockCrab crab && crab.Sprite.currentFrame % 4 == 0 && !ModEntry.ModHelper.Reflection.GetField<NetBool>(crab, "shellGone").GetValue().Value // skip shelled rock crabs
					|| __instance is LavaLurk lurk && lurk.currentState.Value == LavaLurk.State.Submerged // skip submerged lava lurks
					|| __instance is Spiker // skip spikers
					|| __instance.FacingDirection != who.FacingDirection) // check for backstab
					return true; // run original logic

				___slideAnimationTimer = 0;
				__instance.setTrajectory(xTrajectory / 3, yTrajectory / 3);
				__instance.currentLocation.playSound("crit");
				__instance.Health = 0;
				__instance.deathAnimation();
				__result = 9999;
				return false; // don't run original logic
			}
			catch (Exception ex)
			{
				ModEntry.Log($"Failed in {MethodBase.GetCurrentMethod().Name}:\n{ex}", LogLevel.Error);
				return true; // default to original logic
			}
		}

		/// <summary>Patch to disable Poacher super mode on failed assassination.</summary>
		[HarmonyPostfix]
		private static void MonsterTakeDamagePostfix(Monster __instance, int damage, bool isBomb, Farmer who)
		{
			if (damage <= 0 || isBomb || !ModEntry.IsSuperModeActive || ModEntry.SuperModeIndex != Util.Professions.IndexOf("Poacher") || __instance.Health <= 0)
				return;
			ModEntry.IsSuperModeActive = false;
		}

		#endregion harmony patches

		#region private methods

		[HarmonyTargetMethods]
		private static IEnumerable<MethodBase> TargetMethods()
		{
			var methods = from type in AccessTools.AllTypes()
						  where typeof(Monster).IsAssignableFrom(type) && !type.AnyOf(
							  typeof(HotHead),
							  typeof(LavaLurk),
							  typeof(Leaper),
							  typeof(MetalHead),
							  typeof(Shooter),
							  typeof(ShadowBrute),
							  typeof(Skeleton),
							  typeof(Spiker))
						  select type.MethodNamed("takeDamage",
							  new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(double), typeof(Farmer) });

			return methods.Where(m => m.DeclaringType == m.ReflectedType);
		}

		#endregion private methods
	}
}