using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.Maps;
using Content.Shared.Power.Components;
using Content.Shared.NodeContainer;
using Robust.Shared.EntitySerialization;

namespace Content.IntegrationTests.Tests.Power;

[Explicit]
public sealed class StationPowerTests
{
    /// <summary>
    /// How long the station should be able to survive on stored power if nothing is changed from round start.
    /// </summary>
    private const float MinimumPowerDurationSeconds = 10 * 60;

    private static readonly string[] GameMaps =
    {
        // WIZDEN PROTOTYPES, ONES COMMENTED OUT ARE IN ignoredPrototypes.yml:
        //"Bagel",
        //"Box",
        //"Elkridge",
        //"Fland",
        //"Marathon",
        //"Oasis",
        //"Packed",
        //"Plasma",
        //"Relic",
        //"Snowball",
        //"Reach",
        //"Exo",

        // IMP PROTOTYPES:
        // "AmberImp",
        // "BagelImp",
        // "Banana",
        // "Barratry",
        // "Bedlam",
        // "Boat",
        // "BoxImp",
        //"CentCommImp",
        // "Cluster",
        // "CogImp",
        // "CoreImp",
        //"E1M1",
        // "ElkridgeImp",
        // "GateImp",
        // "Hummingbird",
        // "Lilboat",
        // "MarathonImp",
        // "OasisImp",
        // "PackedImp",
        // "PlasmaImp",
        // "ReachImp",
        // "SalternImp",
        // "Submarine",
        // "TrainImp",
        // "Union",
        // "Xeno",
        // "Pathway",
        // "Whisper",
        // "Monarch",

        // DEROTATED:
        //"Eclipse",
        //"Luna",
        //"Refsdal",
        //"reHash",
        //"RelicImp",
        //"Skimmer",

        // QB PROTOTYPES:
        "AsteriskQB",
        "ByoinQB",
        "ChibiQB",
        "Dash",
        "ElkridgeQB",
        "Foundry",
        "PebbleQB",
    };

    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestStationStartingPowerWindow(string mapProtoId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
        });
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var ticker = entMan.System<GameTicker>();
        var batterySys = entMan.System<BatterySystem>();

        // Load the map
        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            ticker.LoadGameMap(mapProto, out var mapId, opts);
        });

        // Let powernet set up
        await server.WaitRunTicks(1);

        // Find the power network with the greatest stored charge in its batteries.
        // This keeps backup SMESes out of the calculation.
        var networks = new Dictionary<PowerState.Network, float>();
        var batteryQuery = entMan.EntityQueryEnumerator<PowerNetworkBatteryComponent, BatteryComponent, NodeContainerComponent>();
        while (batteryQuery.MoveNext(out var uid, out _, out var battery, out var nodeContainer))
        {
            if (!nodeContainer.Nodes.TryGetValue("output", out var node))
                continue;
            if (node.NodeGroup is not IBasePowerNet group)
                continue;
            networks.TryGetValue(group.NetworkNode, out var charge);
            var currentCharge = batterySys.GetCharge((uid, battery));
            networks[group.NetworkNode] = charge + currentCharge;
        }
        var totalStartingCharge = networks.MaxBy(n => n.Value).Value;

        // Find how much charge all the APC-connected devices would like to use per second.
        var totalAPCLoad = 0f;
        var receiverQuery = entMan.EntityQueryEnumerator<ApcPowerReceiverComponent>();
        while (receiverQuery.MoveNext(out _, out var receiver))
        {
            totalAPCLoad += receiver.Load;
        }

        var estimatedDuration = totalStartingCharge / totalAPCLoad;
        var requiredStoredPower = totalAPCLoad * MinimumPowerDurationSeconds;
        Assert.Multiple(() =>
        {
            Assert.That(estimatedDuration, Is.GreaterThanOrEqualTo(MinimumPowerDurationSeconds),
                $"Initial power for {mapProtoId} does not last long enough! Needs at least {MinimumPowerDurationSeconds}s " +
                $"but estimated to last only {estimatedDuration}s!");
            Assert.That(totalStartingCharge, Is.GreaterThanOrEqualTo(requiredStoredPower),
                $"Needs at least {requiredStoredPower - totalStartingCharge} more stored power!");
        });


        await pair.CleanReturnAsync();
    }

    // // IMP ADD- 2nd test to catch variable power loads
    // [Test, TestCaseSource(nameof(GameMaps))]
    // public async Task ImpTestApcLoad(string mapProtoId)
    // {
    //     await using var pair = await PoolManager.GetServerClient(new PoolSettings
    //     {
    //         Dirty = true,
    //     });
    //     var server = pair.Server;
    //
    //     var entMan = server.EntMan;
    //     var protoMan = server.ProtoMan;
    //     var ticker = entMan.System<GameTicker>();
    //     var xform = entMan.System<TransformSystem>();
    //
    //     // Load the map
    //     await server.WaitAssertion(() =>
    //     {
    //         Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
    //         var opts = DeserializationOptions.Default with { InitializeMaps = true };
    //         ticker.LoadGameMap(mapProto, out var mapId, opts);
    //     });
    //
    //     // Usually 30s is enough to trip things
    //     await pair.RunSeconds(30);
    //
    //     // Check that no APCs are overloaded
    //     var apcQuery = entMan.EntityQueryEnumerator<ApcComponent>();
    //     Assert.Multiple(() =>
    //     {
    //         while (apcQuery.MoveNext(out var uid, out var apc))
    //         {
    //             if (xform.TryGetMapOrGridCoordinates(uid, out var coord))
    //             {
    //                 Assert.That(!apc.TripFlag,
    //                         $"APC {uid} on {mapProtoId} ({coord.Value.X}, {coord.Value.Y}) is overloaded");
    //             }
    //             else
    //             {
    //                 Assert.That(!apc.TripFlag,
    //                         $"APC {uid} on {mapProtoId} is overloaded");
    //             }
    //         }
    //     });
    //
    //     await pair.CleanReturnAsync();
    // }

}
