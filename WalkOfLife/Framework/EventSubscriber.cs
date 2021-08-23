﻿using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using TheLion.Stardew.Common.Extensions;
using TheLion.Stardew.Professions.Framework.Events;
using TheLion.Stardew.Professions.Framework.Extensions;

namespace TheLion.Stardew.Professions.Framework
{
	/// <summary>Manages dynamic subscribing and unsubscribing of events for modded professions.</summary>
	internal class EventSubscriber
	{
		internal IEnumerable<string> SubscribedEvents => _subscribed.Select(e => e.GetType().Name);

		private readonly List<BaseEvent> _subscribed = new();

		private static readonly Dictionary<int, List<BaseEvent>> EventsByProfession = new()
		{
			{ Util.Professions.IndexOf("Artisan"), new() { new ArtisanDayEndingEvent() } },
			{ Util.Professions.IndexOf("Conservationist"), new() { new ConservationistDayEndingEvent(), new ConservationistDayStartedEvent() } },
			{ Util.Professions.IndexOf("Piper"), new() { new PiperWarpedEvent() } },
			{ Util.Professions.IndexOf("Prospector"), new() { new ProspectorHuntDayStartedEvent(), new ProspectorWarpedEvent(), new TrackerButtonsChangedEvent() } },
			{ Util.Professions.IndexOf("Scavenger"), new() { new ScavengerHuntDayStartedEvent(), new ScavengerWarpedEvent(), new TrackerButtonsChangedEvent() } },
			{ Util.Professions.IndexOf("Spelunker"), new() { new SpelunkerWarpedEvent() } }
		};

		/// <summary>Construct an instance.</summary>
		internal EventSubscriber()
		{
			// hook static events
			SubscribeStaticEvents();
		}

		/// <summary>Subscribe new events to the event listener.</summary>
		/// <param name="events">Events to be subscribed.</param>
		internal void Subscribe(params BaseEvent[] events)
		{
			foreach (var e in events)
			{
				if (_subscribed.ContainsType(e.GetType()))
				{
					ModEntry.Log($"Farmer already subscribed to {e.GetType().Name}.", LogLevel.Trace);
				}
				else
				{
					e.Hook();
					_subscribed.Add(e);
					ModEntry.Log($"Subscribed to {e.GetType().Name}.", LogLevel.Trace);
				}
			}
		}

		/// <summary>Unsubscribe events from the event listener.</summary>
		/// <param name="eventTypes">The event types to be unsubscribed.</param>
		internal void Unsubscribe(params Type[] eventTypes)
		{
			foreach (var type in eventTypes)
			{
				if (_subscribed.RemoveType(type, out var removed))
				{
					removed.Unhook();
					ModEntry.Log($"Unsubscribed from {type.Name}.", LogLevel.Trace);
				}
				else
				{
					ModEntry.Log($"Farmer not subscribed to {type.Name}.", LogLevel.Trace);
				}
			}
		}

		/// <summary>Subscribe the event listener to events required for basic mod function.</summary>
		internal void SubscribeStaticEvents()
		{
			ModEntry.Log("Subscribing static events...", LogLevel.Trace);
			Subscribe(new StaticLevelChangedEvent(), new StaticReturnedToTitleEvent(), new StaticSaveLoadedEvent(), new StaticSuperModeRegisteredEvent());

			if (!ModEntry.ModRegistry.IsLoaded("alphablackwolf.skillPrestige") && !ModEntry.ModRegistry.IsLoaded("cantorsdust.AllProfessions"))
				return;

			ModEntry.Log("Skill Prestige or All Professions mod detected. Subscribing additional fail-safe event.", LogLevel.Trace);
			Subscribe(new StaticDayStartedEvent());
		}

		/// <summary>Subscribe the event listener to all events required by the local player's current professions.</summary>
		internal void SubscribeEventsForLocalPlayer()
		{
			ModEntry.Log($"Subscribing dynamic events for farmer {Game1.player.Name}...", LogLevel.Trace);
			foreach (var professionIndex in Game1.player.professions) SubscribeEventsForProfession(professionIndex);
			ModEntry.Log("Done subscribing player events.", LogLevel.Trace);
		}

		/// <summary>Subscribe the event listener to all events required by the local player's current professions.</summary>
		internal void UnsubscribeLocalPlayerEvents()
		{
			ModEntry.Log($"Unsubscribing player dynamic events...", LogLevel.Trace);
			List<Type> toRemove = new();
			for (var i = 4; i < _subscribed.Count; ++i) toRemove.Add(_subscribed[i].GetType());
			Unsubscribe(toRemove.ToArray());
			ModEntry.Log("Done unsubscribing player events.", LogLevel.Trace);
		}

		/// <summary>Subscribe the event listener to all events required by a specific profession.</summary>
		/// <param name="whichProfession">The profession index.</param>
		internal void SubscribeEventsForProfession(int whichProfession)
		{
			if (!EventsByProfession.TryGetValue(whichProfession, out var events)) return;

			if (whichProfession == Util.Professions.IndexOf("Artisan") && ModEntry.Data.ReadField<uint>("ArtisanAwardLevel") >= 5) return;

			ModEntry.Log($"Subscribing to {Util.Professions.NameOf(whichProfession)} profession events...", LogLevel.Trace);
			foreach (var e in events) Subscribe(e);
			ModEntry.Log("Done subscribing profession events.", LogLevel.Trace);
		}

		/// <summary>Unsubscribe the event listener from all events required by a specific profession.</summary>
		/// <param name="whichProfession">The profession index.</param>
		internal void UnsubscribeProfessionEvents(int whichProfession)
		{
			if (!EventsByProfession.TryGetValue(whichProfession, out var events)) return;

			List<BaseEvent> except = new();
			if (Util.Professions.NameOf(whichProfession) == "Prospector" && Game1.player.HasProfession("Scavenger") ||
			Util.Professions.NameOf(whichProfession) == "Scavenger" && Game1.player.HasProfession("Prospector"))
				except.Add(new TrackerButtonsChangedEvent());

			ModEntry.Log($"Unsubscribing from {Util.Professions.NameOf(whichProfession)} profession events...", LogLevel.Trace);
			foreach (var e in events.Except(except)) Unsubscribe(e.GetType());
			ModEntry.Log("Done unsubscribing profession events.", LogLevel.Trace);
		}

		/// <summary>Subscribe the event listener to all events required for super mode functionality.</summary>
		internal void SubscribeSuperModeEvents()
		{
			Subscribe(
				new SuperModeButtonsChangedEvent(),
				new SuperModeCounterFilledEvent(),
				new SuperModeCounterRaisedAboveZeroEvent(),
				new SuperModeCounterReturnedToZeroEvent(),
				new SuperModeDisabledEvent(),
				new SuperModeEnabledEvent(),
				new SuperModeKeyHeldLongEnoughEvent(),
				new SuperModeWarpedEvent()
			);

			if (Game1.currentLocation.AnyOfType(typeof(MineShaft), typeof(Woods), typeof(SlimeHutch), typeof(VolcanoDungeon)) || ModEntry.SuperModeCounter > 0)
				ModEntry.Subscriber.Subscribe(new SuperModeBarRenderedHudEvent());
		}

		/// <summary>Unsubscribe the event listener from all events related to super mode functionality.</summary>
		internal void UnsubscribeSuperModeEvents()
		{
			Unsubscribe(
				typeof(SuperModeBarRenderedHudEvent),
				typeof(SuperModeBuffsDisplayUpdateTickedEvent),
				typeof(SuperModeButtonsChangedEvent),
				typeof(SuperModeCounterFilledEvent),
				typeof(SuperModeCounterRaisedAboveZeroEvent),
				typeof(SuperModeCounterReturnedToZeroEvent),
				typeof(SuperModeDisabledEvent),
				typeof(SuperModeEnabledEvent),
				typeof(SuperModeKeyHeldLongEnoughEvent),
				typeof(SuperModeWarpedEvent)
			);
		}

		/// <summary>Check if any events that should be subscribed are missing and if so subscribe those events.</summary>
		internal void SubscribeMissingEvents()
		{
			ModEntry.Log("Checking for missing profession events...", LogLevel.Trace);
			foreach (var professionIndex in Game1.player.professions)
			{
				if (!EventsByProfession.TryGetValue(professionIndex, out var events)) continue;
				foreach (var e in events.Except(_subscribed)) Subscribe(e);
			}
			ModEntry.Log("Done subscribing to missing events.", LogLevel.Trace);
		}

		/// <summary>Check if there are rogue events still subscribed and remove them.</summary>
		internal void CleanUpRogueEvents()
		{
			ModEntry.Log("Checking for rogue profession events...", LogLevel.Trace);
			foreach (var e in _subscribed
				.Where(e => Util.Professions.IndexByName.Contains(e.Prefix()) && !Game1.player.HasProfession(e.Prefix()) ||
							e.Prefix().Equals("Tracker") && !(Game1.player.HasProfession("Prospector") || Game1.player.HasProfession("Scavenger")) ||
							e.Prefix().Equals("SuperMode") && !Game1.player.HasAnyOfProfessions("Brute", "Hunter", "Piper", "Desperado"))
				.Reverse()) Unsubscribe(e.GetType());
			ModEntry.Log("Done cleaning up rogue events.", LogLevel.Trace);
		}

		/// <summary>Whether the event listener is subscribed to a given event type.</summary>
		/// <param name="eventType">The event type to check.</param>
		internal bool IsSubscribed(Type eventType)
		{
			return _subscribed.ContainsType(eventType);
		}
	}
}