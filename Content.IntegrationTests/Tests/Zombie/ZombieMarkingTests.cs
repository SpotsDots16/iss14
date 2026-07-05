using System.Collections.Generic;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Zombies;
using Content.Shared.Humanoid;
using Content.Shared.Zombies;

namespace Content.IntegrationTests.Tests.Zombie;

[TestOf(typeof(ZombieSystem))]
public sealed class ZombieMarkingTests : InteractionTest
{
    protected override string PlayerPrototype => "MobVulpkanin";

    [Test]
    public async Task ZombificationAppliesAppearance()
    {
        await Server.WaitAssertion(() =>
        {
            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SPlayer);
            var beforeSkin = humanoid.SkinColor;
            var beforeEyes = humanoid.EyeColor;
            var beforeLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(humanoid.CustomBaseLayers);

            var zombie = SEntMan.System<ZombieSystem>();
            zombie.ZombifyEntity(SPlayer);
            var comp = SEntMan.GetComponent<ZombieComponent>(SPlayer);

            Assert.Multiple(() =>
            {
                Assert.That(humanoid.SkinColor, Is.EqualTo(comp.SkinColor), "Zombification did not apply the zombie skin color");
                Assert.That(humanoid.EyeColor, Is.EqualTo(comp.EyeColor), "Zombification did not apply the zombie eye color");
                Assert.That(comp.BeforeZombifiedSkinColor, Is.EqualTo(beforeSkin), "Zombification did not store the pre-zombie skin color");
                Assert.That(comp.BeforeZombifiedEyeColor, Is.EqualTo(beforeEyes), "Zombification did not store the pre-zombie eye color");
                Assert.That(comp.BeforeZombifiedCustomBaseLayers, Is.EqualTo(beforeLayers), "Zombification did not store the pre-zombie custom base layers");
            });
        });
    }

    [Test]
    public async Task ZombificationAppliesBaseLayers()
    {
        await Server.WaitAssertion(() =>
        {
            var zombie = SEntMan.System<ZombieSystem>();
            zombie.ZombifyEntity(SPlayer);

            var humanoid = SEntMan.GetComponent<HumanoidAppearanceComponent>(SPlayer);

            using var scope = Assert.EnterMultipleScope();
            foreach (var layer in ZombieSystem.AdditionalZombieLayers)
            {
                Assert.That(humanoid.CustomBaseLayers, Does.ContainKey(layer), $"Zombification did not override base layer {layer}");
            }
        });
    }
}
