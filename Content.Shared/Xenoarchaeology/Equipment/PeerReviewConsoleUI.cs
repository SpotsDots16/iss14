using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment;

public static class PeerReviewConsoleConstants
{
    public const int SmallPublicationCost = 4;
    public const int MediumPublicationCost = 8;
    public const int LargePublicationCost = 16;
}

[Serializable, NetSerializable]
public enum PeerReviewConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum PublicationTier : byte
{
    Small,
    Medium,
    Large,
}

[Serializable, NetSerializable]
public sealed class PeerReviewConsoleUiState : BoundUserInterfaceState
{
    public int StoredValue { get; }

    public PeerReviewConsoleUiState(int storedValue)
    {
        StoredValue = storedValue;
    }
}

[Serializable, NetSerializable]
public sealed class PeerReviewConsolePublishMessage : BoundUserInterfaceMessage
{
    public PublicationTier Tier { get; }

    public PeerReviewConsolePublishMessage(PublicationTier tier)
    {
        Tier = tier;
    }
}
