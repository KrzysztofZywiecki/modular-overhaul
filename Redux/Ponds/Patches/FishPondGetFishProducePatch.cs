﻿namespace DaLion.Redux.Ponds.Patches;

#region using directives

using System.IO;
using System.Linq;
using System.Reflection;
using DaLion.Redux.Core.Extensions;
using DaLion.Redux.Ponds.Extensions;
using DaLion.Shared.Extensions;
using DaLion.Shared.Extensions.Collections;
using DaLion.Shared.Extensions.Stardew;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using HarmonyPatch = DaLion.Shared.Harmony.HarmonyPatch;

#endregion using directives

[UsedImplicitly]
internal sealed class FishPondGetFishProducePatch : HarmonyPatch
{
    /// <summary>Initializes a new instance of the <see cref="FishPondGetFishProducePatch"/> class.</summary>
    internal FishPondGetFishProducePatch()
    {
        this.Target = this.RequireMethod<FishPond>(nameof(FishPond.GetFishProduce));
    }

    #region harmony patches

    /// <summary>Replace single production with multi-yield production.</summary>
    // ReSharper disable once RedundantAssignment
    [HarmonyPrefix]
    private static bool FishPondGetFishProducePrefix(FishPond __instance, ref SObject? __result, Random? random)
    {
        random ??= new Random(Guid.NewGuid().GetHashCode());

        try
        {
            var held = __instance.DeserializeObjectListData(DataFields.ItemsHeld);
            if (__instance.output.Value is not null)
            {
                held.Add(__instance.output.Value);
            }

            var fish = __instance.GetFishObject();
            __result = null;
            // handle algae, which have no fish pond data
            if (__instance.HasAlgae())
            {
                var algaeStacks = new[] { 0, 0, 0 }; // green, white, seaweed
                var population = __instance.Read<int>(DataFields.GreenAlgaeLivingHere);
                var chance = Utility.Lerp(0.15f, 0.95f, population / (float)__instance.currentOccupants.Value);
                for (var i = 0; i < population; ++i)
                {
                    if (random.NextDouble() < chance)
                    {
                        ++algaeStacks[0];
                    }
                }

                population = __instance.Read<int>(DataFields.WhiteAlgaeLivingHere);
                chance = Utility.Lerp(0.15f, 0.95f, population / (float)__instance.currentOccupants.Value);
                for (var i = 0; i < population; ++i)
                {
                    if (random.NextDouble() < chance)
                    {
                        ++algaeStacks[1];
                    }
                }

                population = __instance.Read<int>(DataFields.SeaweedLivingHere);
                chance = Utility.Lerp(0.15f, 0.95f, population / (float)__instance.currentOccupants.Value);
                for (var i = 0; i < population; ++i)
                {
                    if (random.NextDouble() < chance)
                    {
                        ++algaeStacks[2];
                    }
                }

                if (algaeStacks.Sum() > 0)
                {
                    if (algaeStacks[0] > 0)
                    {
                        held.Add(new SObject(Constants.GreenAlgaeIndex, algaeStacks[0]));
                    }

                    if (algaeStacks[1] > 0)
                    {
                        held.Add(new SObject(Constants.WhiteAlgaeIndex, algaeStacks[1]));
                    }

                    if (algaeStacks[2] > 0)
                    {
                        held.Add(new SObject(Constants.SeaweedIndex, algaeStacks[2]));
                    }

                    __result = __instance.fishType.Value switch
                    {
                        Constants.GreenAlgaeIndex when algaeStacks[0] > 0 => new SObject(
                            Constants.GreenAlgaeIndex,
                            algaeStacks[0]),
                        Constants.WhiteAlgaeIndex when algaeStacks[1] > 0 => new SObject(
                            Constants.WhiteAlgaeIndex,
                            algaeStacks[1]),
                        Constants.SeaweedIndex when algaeStacks[2] > 0 => new SObject(
                            Constants.SeaweedIndex,
                            algaeStacks[2]),
                        _ => null,
                    };

                    if (__result is null)
                    {
                        var max = algaeStacks.ToList().IndexOfMax();
                        __result = max switch
                        {
                            0 => new SObject(Constants.GreenAlgaeIndex, algaeStacks[0]),
                            1 => new SObject(Constants.WhiteAlgaeIndex, algaeStacks[1]),
                            2 => new SObject(Constants.SeaweedIndex, algaeStacks[2]),
                            _ => null,
                        };
                    }
                }

                if (__result is not null)
                {
                    held.Remove(__result);
                }

                var serialized = held.Take(36).Select(p => $"{p.ParentSheetIndex},{p.Stack},0");
                __instance.Write(DataFields.ItemsHeld, string.Join(';', serialized));
                return false; // don't run original logic
            }

            // handle fish pond data
            var fishPondData = __instance.GetFishPondData();
            if (fishPondData is not null)
            {
                held.AddRange(from item in fishPondData.ProducedItems.Where(item =>
                        item.ItemID is not Constants.RoeIndex or Constants.SquidInkIndex &&
                        __instance.currentOccupants.Value >= item.RequiredPopulation &&
                        random.NextDouble() < Utility.Lerp(0.15f, 0.95f, __instance.currentOccupants.Value / 10f) &&
                        random.NextDouble() < item.Chance)
                    let stack = random.Next(item.MinQuantity, item.MaxQuantity + 1)
                    select new SObject(item.ItemID, stack));
            }

            // handle roe or ink
            if (fish.Name == "Coral")
            {
                var algaeStacks = new[] { 0, 0, 0 }; // green, white, seaweed
                var chance = Utils.GetRoeChance(fish.Price, __instance.FishCount - 1);
                for (var i = 0; i < __instance.FishCount; ++i)
                {
                    if (random.NextDouble() < chance)
                    {
                        switch (Utils.ChooseAlgae())
                        {
                            case Constants.GreenAlgaeIndex:
                                ++algaeStacks[0];
                                break;
                            case Constants.WhiteAlgaeIndex:
                                ++algaeStacks[1];
                                break;
                            case Constants.SeaweedIndex:
                                ++algaeStacks[2];
                                break;
                        }
                    }
                }

                if (algaeStacks[0] > 0)
                {
                    held.Add(new SObject(Constants.GreenAlgaeIndex, algaeStacks[0]));
                }

                if (algaeStacks[1] > 0)
                {
                    held.Add(new SObject(Constants.WhiteAlgaeIndex, algaeStacks[1]));
                }

                if (algaeStacks[2] > 0)
                {
                    held.Add(new SObject(Constants.SeaweedIndex, algaeStacks[2]));
                }
            }
            else
            {
                var fishQualities = __instance.Read(
                        DataFields.FishQualities,
                        $"{__instance.FishCount - __instance.Read<int>(DataFields.FamilyLivingHere)},0,0,0")
                    .ParseList<int>();
                if (fishQualities.Count != 4)
                {
                    ThrowHelper.ThrowInvalidDataException("FishQualities data had incorrect number of values.");
                }

                var familyQualities =
                    __instance.Read(DataFields.FamilyQualities, "0,0,0,0").ParseList<int>();
                if (familyQualities.Count != 4)
                {
                    ThrowHelper.ThrowInvalidDataException("FamilyQualities data had incorrect number of values.");
                }

                var totalQualities = fishQualities.Zip(familyQualities, (first, second) => first + second).ToList();
                if (totalQualities.Sum() != __instance.FishCount)
                {
                    ThrowHelper.ThrowInvalidDataException("Quality data had incorrect number of values.");
                }

                var productionChancePerFish = Utils.GetRoeChance(fish.Price, __instance.FishCount - 1);
                var producedRoes = new int[4];
                for (var i = 0; i < 4; ++i)
                {
                    while (totalQualities[i]-- > 0)
                    {
                        if (random.NextDouble() < productionChancePerFish)
                        {
                            ++producedRoes[i];
                        }
                    }
                }

                if (fish.ParentSheetIndex == Constants.SturgeonIndex)
                {
                    for (var i = 0; i < 4; ++i)
                    {
                        producedRoes[i] += random.Next(producedRoes[i]);
                    }
                }

                if (producedRoes.Sum() > 0)
                {
                    var roeIndex = fish.Name.Contains("Squid") ? Constants.SquidInkIndex : Constants.RoeIndex;
                    for (var i = 0; i < 4; ++i)
                    {
                        if (producedRoes[i] > 0)
                        {
                            held.Add(new SObject(roeIndex, producedRoes[i], quality: i == 3 ? 4 : i));
                        }
                    }
                }

                // check for enriched metals
                if (__instance.IsRadioactive())
                {
                    var heldMetals =
                        __instance.Read(DataFields.MetalsHeld)
                            .ParseList<string>(";")
                            .Select(li => li?.ParseTuple<int, int>())
                            .WhereNotNull()
                            .ToList();
                    var readyToHarvest = heldMetals.Where(m => m.Item2 <= 0).ToList();
                    if (readyToHarvest.Count > 0)
                    {
                        held.AddRange(readyToHarvest.Select(m =>
                            m.Item1.IsOre()
                                ? new SObject(Constants.RadioactiveOreIndex, 1)
                                : new SObject(Constants.RadioactiveBarIndex, 1)));
                        heldMetals = heldMetals.Except(readyToHarvest).ToList();
                    }

                    __instance.Write(
                        DataFields.MetalsHeld,
                        string.Join(';', heldMetals.Select(m => string.Join(',', m.Item1, m.Item2))));
                }
            }

            if (held.Count == 0)
            {
                return false; // don't run original logic
            }

            // choose output
            Utility.consolidateStacks(held);
            __result = held.OrderByDescending(h => h.salePrice()).First() as SObject;
            held.Remove(__result!);
            if (held.Count > 0)
            {
                var serialized = held.Take(36).Select(p => $"{p.ParentSheetIndex},{p.Stack},{((SObject)p).Quality}");
                __instance.Write(DataFields.ItemsHeld, string.Join(';', serialized));
            }
            else
            {
                __instance.Write(DataFields.ItemsHeld, null);
            }

            if (__result!.ParentSheetIndex != Constants.RoeIndex)
            {
                return false; // don't run original logic
            }

            var fishIndex = fish.ParentSheetIndex;
            if (fish.IsLegendaryFish() && random.NextDouble() <
                __instance.Read<double>(DataFields.FamilyLivingHere) / __instance.FishCount)
            {
                fishIndex = Utils.ExtendedFamilyPairs[fishIndex];
            }

            var split = Game1.objectInformation[fishIndex].Split('/');
            var c = fishIndex == 698
                ? new Color(61, 55, 42)
                : TailoringMenu.GetDyeColor(new SObject(fishIndex, 1)) ?? Color.Orange;
            var o = new ColoredObject(Constants.RoeIndex, __result.Stack, c);
            o.name = split[0] + " Roe";
            o.preserve.Value = SObject.PreserveType.Roe;
            o.preservedParentSheetIndex.Value = fishIndex;
            o.Price += Convert.ToInt32(split[1]) / 2;
            o.Quality = __result.Quality;
            __result = o;

            return false; // don't run original logic
        }
        catch (InvalidDataException ex)
        {
            Log.W($"{ex}\nThe data will be reset.");
            __instance.Write(DataFields.FishQualities, $"{__instance.FishCount},0,0,0");
            __instance.Write(DataFields.FamilyQualities, null);
            __instance.Write(DataFields.FamilyLivingHere, null);
            return true; // default to original logic
        }
        catch (Exception ex)
        {
            Log.E($"Failed in {MethodBase.GetCurrentMethod()?.Name}:\n{ex}");
            return true; // default to original logic
        }
    }

    #endregion harmony patches
}