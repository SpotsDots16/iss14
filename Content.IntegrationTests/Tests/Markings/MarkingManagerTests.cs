using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.IntegrationTests.Tests.Markings;

[TestFixture]
[TestOf(typeof(MarkingManager))]
public sealed class MarkingManagerTests : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: marking
  id: TestChestMarking
  bodyPart: Chest
  markingCategory: Chest
  sprites: [{ sprite: Mobs/Customization/human_hair.rsi, state: afro }]

- type: marking
  id: TestMenOnlyMarking
  bodyPart: Chest
  markingCategory: Chest
  sexRestriction: Male
  sprites: [{ sprite: Mobs/Customization/human_hair.rsi, state: afro }]

- type: marking
  id: TestSpeciesRestrictedMarking
  bodyPart: Chest
  markingCategory: Chest
  speciesRestriction: [ SomeOtherSpecies ]
  sprites: [{ sprite: Mobs/Customization/human_hair.rsi, state: afro }]
";

    [Test]
    public async Task EnsureSexesFiltersRestricted()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var markingManager = server.ResolveDependency<MarkingManager>();

            var markings = new List<Marking>
            {
                new("TestChestMarking", 1),
                new("TestMenOnlyMarking", 1),
            };

            var femaleSet = new MarkingSet(markings, markingManager);
            femaleSet.EnsureSexes(Sex.Female, markingManager);

            Assert.That(femaleSet.TryGetCategory(MarkingCategories.Chest, out var femaleMarkings));
            Assert.That(femaleMarkings, Has.Count.EqualTo(1));
            Assert.That(femaleMarkings![0].MarkingId, Is.EqualTo("TestChestMarking"));

            var maleSet = new MarkingSet(markings, markingManager);
            maleSet.EnsureSexes(Sex.Male, markingManager);

            Assert.That(maleSet.TryGetCategory(MarkingCategories.Chest, out var maleMarkings));
            Assert.That(maleMarkings, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task EnsureSpeciesFiltersRestricted()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var markingManager = server.ResolveDependency<MarkingManager>();

            var set = new MarkingSet(new List<Marking>
            {
                new("TestChestMarking", 1),
                new("TestSpeciesRestrictedMarking", 1),
            }, markingManager);

            set.EnsureSpecies("Human", null, markingManager);

            Assert.That(set.TryGetCategory(MarkingCategories.Chest, out var markings));
            Assert.That(markings, Has.Count.EqualTo(1));
            Assert.That(markings![0].MarkingId, Is.EqualTo("TestChestMarking"));
        });
    }

    [Test]
    public async Task EnsureValidRemovesUnknownAndFixesColors()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var markingManager = server.ResolveDependency<MarkingManager>();

            // Construct with too many colors for the marking's single sprite.
            var set = new MarkingSet(new List<Marking>
            {
                new("TestChestMarking", 3),
            }, markingManager);

            // Sneak in a marking without a prototype, bypassing constructor validation.
            set.Markings[MarkingCategories.Chest].Add(new Marking("ThisMarkingDoesNotExist", 1));

            set.EnsureValid(markingManager);

            Assert.That(set.TryGetCategory(MarkingCategories.Chest, out var markings));
            Assert.That(markings, Has.Count.EqualTo(1), "EnsureValid did not remove the marking without a prototype");
            Assert.That(markings![0].MarkingId, Is.EqualTo("TestChestMarking"));
            Assert.That(markings[0].MarkingColors, Has.Count.EqualTo(1), "EnsureValid did not truncate the color list to the sprite count");
        });
    }

    [Test]
    public async Task ConstructorDropsUnknownMarkings()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var markingManager = server.ResolveDependency<MarkingManager>();

            var set = new MarkingSet(new List<Marking>
            {
                new("TestChestMarking", 1),
                new("ThisMarkingDoesNotExist", 1),
            }, markingManager);

            Assert.That(set.TryGetCategory(MarkingCategories.Chest, out var markings));
            Assert.That(markings, Has.Count.EqualTo(1));
            Assert.That(markings![0].MarkingId, Is.EqualTo("TestChestMarking"));
        });
    }
}
