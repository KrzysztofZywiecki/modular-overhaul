﻿using Netcode;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Linq;
using TheLion.Stardew.Common.Extensions;

namespace TheLion.Stardew.Professions.Framework.Events
{
	public delegate void SuperModeEnabledEventHandler();

	public class SuperModeEnabledEvent : BaseEvent
	{
		private const int SHEET_INDEX_OFFSET = 22;

		/// <summary>Hook this event to the event listener.</summary>
		public override void Hook()
		{
			ModEntry.SuperModeEnabled += OnSuperModeEnabled;
		}

		/// <summary>Unhook this event from the event listener.</summary>
		public override void Unhook()
		{
			ModEntry.SuperModeEnabled -= OnSuperModeEnabled;
		}

		/// <summary>Raised when IsSuperModeActive is set to true.</summary>
		public void OnSuperModeEnabled()
		{
			var whichSuperMode = Util.Professions.NameOf(ModEntry.SuperModeIndex);

			// remove bar shake timer
			ModEntry.Subscriber.Unsubscribe(typeof(SuperModeBarShakeTimerUpdateTickedEvent));
			ModEntry.ShouldShakeSuperModeBar = false;

			// fade in overlay
			ModEntry.Subscriber.Subscribe(new SuperModeRenderedWorldEvent(), new SuperModeOverlayFadeInUpdateTickedEvent());

			// play sfx
			if (!ModEntry.SfxLoader.SfxByName.TryGetValue(ModEntry.SuperModeSfx, out var sfx))
				throw new ArgumentException($"Sound asset '{ModEntry.SuperModeSfx}' could not be found.");
			sfx.CreateInstance().Play();

			// add countdown event
			ModEntry.Subscriber.Subscribe(new SuperModeCountdownUpdateTickedEvent());

			// display buff
			var buffID = ModEntry.UniqueID.Hash() + ModEntry.SuperModeIndex + 4;
			var professionIndex = ModEntry.SuperModeIndex;
			var professionName = Util.Professions.NameOf(professionIndex);

			var buff = Game1.buffsDisplay.otherBuffs.FirstOrDefault(p => p.which == buffID);
			if (buff == null)
			{
				Game1.buffsDisplay.addOtherBuff(
					new Buff(0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						0,
						minutesDuration: 1,
						source: "SuperMode",
						displaySource: ModEntry.I18n.Get(professionName.ToLower() + ".superm"))
					{
						which = buffID,
						sheetIndex = professionIndex + SHEET_INDEX_OFFSET,
						glow = ModEntry.SuperModeGlowColor,
						millisecondsDuration = (int)(ModEntry.Config.SuperModeDrainFactor / 60f * ModEntry.SuperModeCounterMax * 1000f),
						description = ModEntry.I18n.Get(professionName.ToLower() + ".supermdesc")
					}
				);
			}

			// notify peers
			ModEntry.Multiplayer.SendMessage(message: ModEntry.SuperModeIndex, messageType: "SuperModeActivated", modIDs: new[] { ModEntry.UniqueID });

			// apply immediate effects
			if (whichSuperMode == "Poacher") DoEnablePoacherSuperMode();
			else if (whichSuperMode == "Piper") DoEnablePiperSuperMode();
		}

		/// <summary>Hide the player from monsters that may have already seen him/her.</summary>
		private void DoEnablePoacherSuperMode()
		{
			foreach (var monster in Game1.currentLocation.characters.OfType<Monster>().Where(m => m.Player.IsLocalPlayer))
			{
				monster.focusedOnFarmers = false;
				switch (monster)
				{
					case DustSpirit dustSpirit:
						ModEntry.Reflection.GetField<bool>(dustSpirit, name: "chargingFarmer").SetValue(false);
						ModEntry.Reflection.GetField<bool>(dustSpirit, name: "seenFarmer").SetValue(false);
						break;
					case AngryRoger angryRoger:
						ModEntry.Reflection.GetField<NetBool>(angryRoger, name: "seenPlayer").GetValue().Set(false);
						break;
					case Bat bat:
						ModEntry.Reflection.GetField<NetBool>(bat, name: "seenPlayer").GetValue().Set(false);
						break;
					case Ghost ghost:
						ModEntry.Reflection.GetField<NetBool>(ghost, name: "seenPlayer").GetValue().Set(false);
						break;
					case RockGolem rockGolem:
						ModEntry.Reflection.GetField<NetBool>(rockGolem, name: "seenPlayer").GetValue().Set(false);
						break;
				}
			}
		}

		/// <summary>Enflate Slimes and apply mutations.</summary>
		private void DoEnablePiperSuperMode()
		{
			foreach (var greenSlime in Game1.currentLocation.characters.OfType<GreenSlime>().Where(slime => slime.Scale < 2f))
			{
				if (Game1.random.NextDouble() <= 0.012 + Game1.player.team.AverageDailyLuck() / 10.0)
				{
					if (Game1.currentLocation is MineShaft && Game1.player.team.SpecialOrderActive("Wizard2")) greenSlime.makePrismatic();
					else greenSlime.hasSpecialItem.Value = true;
				}

				ModEntry.PipedSlimesScales.Add(greenSlime, greenSlime.Scale);
			}

			var bigSlimes = Game1.currentLocation.characters.OfType<BigSlime>().ToList();
			for (int i = bigSlimes.Count - 1; i >= 0; --i)
			{
				bigSlimes[i].Health = 0;
				bigSlimes[i].deathAnimation();
				int toCreate = Game1.random.Next(2, 5);
				while (toCreate-- > 0)
				{
					Game1.currentLocation.characters.Add(new GreenSlime(bigSlimes[i].Position, Game1.CurrentMineLevel));
					var justCreated = Game1.currentLocation.characters[Game1.currentLocation.characters.Count - 1];
					justCreated.setTrajectory((int)(bigSlimes[i].xVelocity / 8 + Game1.random.Next(-2, 3)), (int)(bigSlimes[i].yVelocity / 8 + Game1.random.Next(-2, 3)));
					justCreated.willDestroyObjectsUnderfoot = false;
					justCreated.moveTowardPlayer(4);
					justCreated.Scale = 0.75f + Game1.random.Next(-5, 10) / 100f;
					justCreated.currentLocation = Game1.currentLocation;
				}
			}

			ModEntry.Subscriber.Subscribe(new SlimeInflationUpdateTickedEvent());
		}
	}
}
