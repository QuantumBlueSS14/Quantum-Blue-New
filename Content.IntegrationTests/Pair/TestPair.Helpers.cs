// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 ElectroJr <leonsfriedrich@gmail.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 MilenVolf <63782763+MilenVolf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tadeo <td12233a@gmail.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: MIT

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Pair;

// Contains misc helper functions to make writing tests easier.
public sealed partial class TestPair
{
    public Task<TestMapData> CreateTestMap(bool initialized = true)
        => CreateTestMap(initialized, "Plating");

    /// <summary>
    /// Loads a test map and returns a <see cref="TestMapData"/> representing it.
    /// </summary>
    /// <param name="testMapPath">The <see cref="ResPath"/> to the test map to load.</param>
    /// <param name="initialized">Whether to initialize the map on load.</param>
    /// <returns>A <see cref="TestMapData"/> representing the loaded map.</returns>
    public async Task<TestMapData> LoadTestMap(ResPath testMapPath, bool initialized = true)
    {
        TestMapData mapData = new();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = initialized };
        var mapLoaderSys = Server.EntMan.System<MapLoaderSystem>();
        var mapSys = Server.System<SharedMapSystem>();

        // Load our test map in and assert that it exists.
        await Server.WaitAssertion(() =>
        {
            Assert.That(mapLoaderSys.TryLoadMap(testMapPath, out var map, out var gridSet, deserializationOptions),
                $"Failed to load map {testMapPath}.");
            Assert.That(gridSet, Is.Not.Empty, "There were no grids loaded from the map!");

            mapData.MapUid = map!.Value.Owner;
            mapData.MapId = map!.Value.Comp.MapId;
            mapData.Grid = gridSet!.First();
            mapData.GridCoords = new EntityCoordinates(mapData.Grid, 0, 0);
            mapData.MapCoords = new MapCoordinates(0, 0, mapData.MapId);
            mapData.Tile = mapSys.GetAllTiles(mapData.Grid.Owner, mapData.Grid.Comp).First();
        });

        await RunTicksSync(10);
        mapData.CMapUid = ToClientUid(mapData.MapUid);
        mapData.CGridUid = ToClientUid(mapData.Grid);
        mapData.CGridCoords = new EntityCoordinates(mapData.CGridUid, 0, 0);

        return mapData;
    }

    /// <summary>
    /// Set a user's antag preferences. Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetAntagPreference(ProtoId<AntagPrototype> id, bool value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is not { } userId)
            return;

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(userId);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        var profile = (HumanoidCharacterProfile)prefs.Characters[0];
        var newProfile = profile.WithAntagPreference(id, value);
        _modifiedProfiles.Add(userId);
        await Server.WaitPost(() => prefMan.SetProfile(userId, 0, newProfile).Wait());
    }

    public Task SetAntagPreferences(HashSet<ProtoId<AntagPrototype>> antagPreferences) // QB edit: add hashset-based antag preference helper expected by MCQ follow-up tests.
    {
        return SetAntagPreferences(Player!, antagPreferences);
    }

    public async Task SetAntagPreferences(ICommonSession player, HashSet<ProtoId<AntagPrototype>> antagPreferences) // QB edit: support setting antag preferences for specific sessions in integration tests.
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(player.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(player.UserId, 0, profile!.WithAntagPreferences(antagPreferences)).Wait();
        });
        await RunTicksSync(5);
    }

    /// <summary>
    /// Set a user's job preferences.  Modified preferences are automatically reset at the end of the test.
    /// </summary>
    public async Task SetJobPriority(ProtoId<JobPrototype> id, JobPriority value, NetUserId? user = null)
    {
        user ??= Client.User!.Value;
        if (user is { } userId)
            await SetJobPriorities(userId, (id, value));
    }

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(params (ProtoId<JobPrototype>, JobPriority)[] priorities)
        => await SetJobPriorities(Client.User!.Value, priorities);

    /// <inheritdoc cref="SetJobPriority"/>
    public async Task SetJobPriorities(NetUserId user, params (ProtoId<JobPrototype>, JobPriority)[] priorities)
    {
        var highCount = priorities.Count(x => x.Item2 == JobPriority.High);
        Assert.That(highCount, Is.LessThanOrEqualTo(1), "Cannot have more than one high priority job");

        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var prefs = prefMan.GetPreferences(user);
        var profile = (HumanoidCharacterProfile)prefs.Characters[0];
        var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(profile.JobPriorities);

        // Automatic preference resetting only resets slot 0.
        Assert.That(prefs.SelectedCharacterIndex, Is.EqualTo(0));

        if (highCount != 0)
        {
            foreach (var (key, priority) in dictionary)
            {
                if (priority == JobPriority.High)
                    dictionary[key] = JobPriority.Medium;
            }
        }

        foreach (var (job, priority) in priorities)
        {
            if (priority == JobPriority.Never)
                dictionary.Remove(job);
            else
                dictionary[job] = priority;
        }

        var newProfile = profile.WithJobPriorities(dictionary);
        _modifiedProfiles.Add(user);
        await Server.WaitPost(() => prefMan.SetProfile(user, 0, newProfile).Wait());
    }

    public async Task SetJobPriorities(ICommonSession player, Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities) // QB edit: add dictionary-based overload used by updated MCQ job tests.
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() => prefMan.SetJobPriorities(player.UserId, jobPriorities).Wait());
        await RunTicksSync(5);
    }

    public Task SetJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities) // QB edit: convenience overload for local player dictionary priorities.
    {
        return SetJobPriorities(Player!, jobPriorities);
    }

    public async Task SetJobPreferences(HashSet<ProtoId<JobPrototype>> jobPreferences) // QB edit: allow tests to explicitly set per-character enabled job list before round start.
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        await Server.WaitPost(() =>
        {
            var profile = prefMan.GetPreferences(Player!.UserId).Characters[0] as HumanoidCharacterProfile;
            prefMan.SetProfile(Player!.UserId, 0, profile!.WithJobPreferences(jobPreferences)).Wait();
        });
        await RunTicksSync(5);
    }

    public Task<IEnumerable<ICommonSession>> AddDummyPlayers(Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities, int count = 1) // QB edit: helper for creating and preconfiguring dummy sessions with job priorities.
    {
        return AddDummyPlayers(jobPriorities, jobPriorities.Keys, count);
    }

    public async Task<IEnumerable<ICommonSession>> AddDummyPlayers(
        Dictionary<ProtoId<JobPrototype>, JobPriority> jobPriorities,
        IEnumerable<ProtoId<JobPrototype>> jobPreferences,
        int count = 1) // QB edit: helper for creating and preconfiguring dummy sessions with explicit preference lists.
    {
        var prefMan = Server.ResolveDependency<IServerPreferencesManager>();
        var dbMan = Server.ResolveDependency<UserDbDataManager>();

        var sessions = await Server.AddDummySessions(count);
        await RunTicksSync(5);
        await Server.WaitPost(() =>
        {
            var tasks = sessions.Select(s =>
            {
                dbMan.WaitLoadComplete(s).Wait();
                var newProfile = HumanoidCharacterProfile.Random().WithJobPreferences(jobPreferences); // QB edit: branch-compatible replacement for missing AsEnabled helper.
                newProfile.Enabled = true; // QB edit
                return Task.WhenAll(
                    prefMan.SetJobPriorities(s.UserId, jobPriorities),
                    prefMan.SetProfile(s.UserId, 0, newProfile));
            });
            Task.WhenAll(tasks).Wait();
        });
        await RunTicksSync(5);

        return sessions;
    }
}
