namespace Content.Shared.Chat;

/// <summary>
/// Helper for the job-icon markup used to show a speaker's job icon before their name in radio.
/// </summary>
public static class ChatIconTokens
{
    /// <summary>Builds the job-icon markup for a JobIconPrototype id, to prepend before a speaker's name in radio.</summary>
    public static string JobIconMarkup(string jobIconId)
    {
        // Markup attribute values must be quoted (the parser only accepts quoted strings/numbers/colors).
        return $"[chaticon kind=\"jobicon\" key=\"{jobIconId}\"]";
    }
}
