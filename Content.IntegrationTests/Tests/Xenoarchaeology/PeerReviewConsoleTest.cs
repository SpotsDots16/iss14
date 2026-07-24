using System.Collections.Generic;
using Content.Server.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.IntegrationTests.Tests.Xenoarchaeology;

[TestFixture]
[TestOf(typeof(PeerReviewConsoleSystem))]
public sealed class PeerReviewConsoleTest
{
    [Test]
    public void PublicationDataIsConsumedOldestFirst()
    {
        var component = new PeerReviewConsoleComponent();
        component.Submissions.Add(CreateSubmission("Alice", 2));
        component.Submissions.Add(CreateSubmission("Bob", 3));
        component.Submissions.Add(CreateSubmission("Carol", 5));

        var published = PeerReviewConsoleSystem.TryConsumeData(component, 4, out var contributions);

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.True);
            Assert.That(contributions, Has.Count.EqualTo(2));
            Assert.That(contributions[0].Submission.ResearcherName, Is.EqualTo("Alice"));
            Assert.That(contributions[0].DataUsed, Is.EqualTo(2));
            Assert.That(contributions[1].Submission.ResearcherName, Is.EqualTo("Bob"));
            Assert.That(contributions[1].DataUsed, Is.EqualTo(2));

            Assert.That(component.Submissions, Has.Count.EqualTo(2));
            Assert.That(component.Submissions[0].ResearcherName, Is.EqualTo("Bob"));
            Assert.That(component.Submissions[0].RemainingValue, Is.EqualTo(1));
            Assert.That(component.Submissions[1].ResearcherName, Is.EqualTo("Carol"));
            Assert.That(component.Submissions[1].RemainingValue, Is.EqualTo(5));
            Assert.That(component.StoredValue, Is.EqualTo(6));
        });
    }

    [Test]
    public void InsufficientPublicationDataDoesNotModifyQueue()
    {
        var component = new PeerReviewConsoleComponent();
        component.Submissions.Add(CreateSubmission("Alice", 2));
        component.Submissions.Add(CreateSubmission("Bob", 1));

        var published = PeerReviewConsoleSystem.TryConsumeData(component, 4, out var contributions);

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.False);
            Assert.That(contributions, Is.Empty);
            Assert.That(component.Submissions, Has.Count.EqualTo(2));
            Assert.That(component.Submissions[0].RemainingValue, Is.EqualTo(2));
            Assert.That(component.Submissions[1].RemainingValue, Is.EqualTo(1));
            Assert.That(component.StoredValue, Is.EqualTo(3));
        });
    }

    private static StoredArtifactSubmission CreateSubmission(string researcherName, int value)
    {
        return new StoredArtifactSubmission(
            value,
            "artifact",
            researcherName,
            value * 6250,
            new List<ArtifactResearchNodeData>());
    }
}
