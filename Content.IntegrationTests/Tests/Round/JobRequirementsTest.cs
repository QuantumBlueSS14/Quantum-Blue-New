using System.Collections.Generic;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Round;

[TestFixture]
public sealed class JobRequirementsTest
{
    private const string MapId = "JobRequirementsTestMap";
    private static readonly ProtoId<JobPrototype> Passenger = "Passenger";

    [TestPrototypes]
    private const string Prototypes = """
        - type: playTimeTracker
          id: PlayTimeDummySeniorCitizen

        - type: playTimeTracker
          id: PlayTimeDummyTwenties

        - type: job
          id: SeniorCitizen
          playTimeTracker: PlayTimeDummySeniorCitizen
          requirements:
          - !type:AgeRequirement
            requiredAge: 65

        - type: job
          id: Twenties
          playTimeTracker: PlayTimeDummyTwenties
          requirements:
          - !type:AgeRequirement
            requiredAge: 20
          - !type:AgeRequirement
            requiredAge: 29
            inverted: true

        - type: gameMap
          id: JobRequirementsTestMap
          mapName: JobRequirementsTestMap
          mapPath: /Maps/Test/empty.yml
          minPlayers: 0
          stations:
            Empty:
              mapNameTemplate: JobRequirementsTestMap
              stationProto: StandardNanotrasenStation
              components:
                - type: StationJobs
                  availableJobs:
                    SeniorCitizen: [ -1, -1 ]
                    Passenger: [ -1, -1 ]
                    Twenties: [ -1, -1 ]
        """;

    private static void AssertJob(TestPair pair, ProtoId<JobPrototype> job, NetUserId? user = null, bool isAntag = false)
    {
        var jobSys = pair.Server.System<SharedJobSystem>();
        var mindSys = pair.Server.System<MindSystem>();
        var roleSys = pair.Server.System<RoleSystem>();
        var ticker = pair.Server.System<GameTicker>();

        user ??= pair.Client.User!.Value;

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));
        Assert.That(ticker.PlayerGameStatuses[user.Value], Is.EqualTo(PlayerGameStatus.JoinedGame));

        var uid = pair.Server.PlayerMan.SessionsDict.GetValueOrDefault(user.Value)?.AttachedEntity;
        Assert.That(pair.Server.EntMan.EntityExists(uid));

        var mind = mindSys.GetMind(uid!.Value);
        Assert.That(pair.Server.EntMan.EntityExists(mind));
        Assert.That(jobSys.MindTryGetJobId(mind, out var actualJob));
        Assert.That(actualJob, Is.EqualTo(job));
        Assert.That(roleSys.MindIsAntagonist(mind), Is.EqualTo(isAntag));
    }

    private static HumanoidCharacterProfile MutateProfile(
        HumanoidCharacterProfile profile,
        int age,
        IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> priorities)
    {
        var next = profile
            .WithAge(age)
            .WithJobPriorities(priorities)
            .WithPreferenceUnavailable(PreferenceUnavailableMode.StayInLobby);
        next.Enabled = true;
        return next;
    }

    [Test]
    [TestCase(75, "SeniorCitizen")]
    [TestCase(20, "SeniorCitizen", false)]
    [TestCase(19, "Twenties", false)]
    [TestCase(20, "Twenties")]
    [TestCase(29, "Twenties")]
    [TestCase(30, "Twenties", false)]
    public async Task AgeRequirementsTest(int age, string wantedJob, bool expectedJob = true)
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true,
        });

        try
        {
            pair.Server.CfgMan.SetCVar(CCVars.GameMap, MapId);
            pair.Server.CfgMan.SetCVar(CCVars.GameRoleTimers, true);

            var ticker = pair.Server.System<GameTicker>();
            var prefMan = pair.Server.ResolveDependency<IServerPreferencesManager>();

            await pair.ReallyBeIdle();

            var initialProfile = (HumanoidCharacterProfile) prefMan.GetPreferences(pair.Client.User!.Value).Characters[0];
            Assert.That(initialProfile, Is.Not.Null);

            var jobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                { wantedJob, JobPriority.High },
                { Passenger, JobPriority.Low },
            };

            var profileForRun = MutateProfile(initialProfile, age, jobPriorities);
            await pair.Server.WaitPost(() => prefMan.SetProfile(pair.Client.User!.Value, 0, profileForRun).Wait());
            await pair.SetJobPriorities(jobPriorities);

            await pair.ReallyBeIdle();

            var savedProfile = (HumanoidCharacterProfile) prefMan.GetPreferences(pair.Client.User!.Value).Characters[0];
            Assert.That(savedProfile.Age, Is.EqualTo(age));
            Assert.That(savedProfile.JobPriorities.ContainsKey(wantedJob), Is.True);

            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.NotReadyToPlay));
            ticker.ToggleReadyAll(true);
            Assert.That(ticker.PlayerGameStatuses[pair.Client.User!.Value], Is.EqualTo(PlayerGameStatus.ReadyToPlay));

            await pair.Server.WaitPost(() => ticker.StartRound());
            await pair.RunTicksSync(10);

            AssertJob(pair, expectedJob ? wantedJob : Passenger);

            await pair.Server.WaitPost(() => ticker.RestartRound());
        }
        finally
        {
            await pair.CleanReturnAsync();
        }
    }
}
