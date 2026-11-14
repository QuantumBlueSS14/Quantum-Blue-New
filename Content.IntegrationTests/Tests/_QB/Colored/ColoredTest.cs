#nullable enable
using Content.Server.QB.Colored;
using Content.Shared.QB.Colored;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._QB.Colored;

[TestFixture]
public sealed class ColoredTest
{
    /// <summary>
    /// Checks that a prototype with <see cref="ColoredComponent"/> is tinted on the client,
    /// that recoloring it from the server reaches the client, and that removing the component clears the shader.
    /// </summary>
    [Test]
    public async Task TestColorAppliedOnClient()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var (server, client) = (pair.Server, pair.Client);
        var map = await pair.CreateTestMap();

        EntityUid target = default;
        await server.WaitAssertion(() => target = server.EntMan.Spawn("TestColoredEntity", map.MapCoords));
        await pair.RunTicksSync(5);

        var cTarget = pair.ToClientUid(target);
        var prototypeColor = Color.FromHex("#50c878");

        await client.WaitAssertion(() =>
        {
            var sprite = client.EntMan.GetComponent<SpriteComponent>(cTarget);
            Assert.Multiple(() =>
            {
                Assert.That(sprite[0].Color, Is.EqualTo(prototypeColor));
                Assert.That(((SpriteComponent.Layer) sprite[0]).Shader, Is.Not.Null);
            });
        });

        await server.WaitAssertion(() => server.System<ColoredSystem>().SetColor(target, Color.Red));
        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var sprite = client.EntMan.GetComponent<SpriteComponent>(cTarget);
            Assert.That(sprite[0].Color, Is.EqualTo(Color.Red));
        });

        await server.WaitAssertion(() => server.System<ColoredSystem>().RemoveColor(target));
        await pair.RunTicksSync(5);

        await client.WaitAssertion(() =>
        {
            var sprite = client.EntMan.GetComponent<SpriteComponent>(cTarget);
            Assert.Multiple(() =>
            {
                Assert.That(client.EntMan.HasComponent<ColoredComponent>(cTarget), Is.False);
                Assert.That(((SpriteComponent.Layer) sprite[0]).Shader, Is.Null);
            });
        });

        await pair.CleanReturnAsync();
    }
}
