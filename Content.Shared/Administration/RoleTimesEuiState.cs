using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// State for the role time overview admin EUI (<c>roletimes</c> command).
/// Sent from the server with the full set of trackers (roles) and their accumulated time.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleTimesEuiState(
    NetUserId userId,
    string username,
    bool online,
    bool canEdit,
    List<RoleTimeInfo> roles)
    : EuiStateBase
{
    public readonly NetUserId UserId = userId;
    public readonly string Username = username;

    /// <summary>Whether the target player is currently connected. Affects how edits are applied.</summary>
    public readonly bool Online = online;

    /// <summary>Whether the viewing admin is allowed to change times.</summary>
    public readonly bool CanEdit = canEdit;

    public readonly List<RoleTimeInfo> Roles = roles;
}

/// <summary>
/// A single tracker row: the raw tracker id, a friendly display name, the accumulated time, an
/// optional human-readable summary of the play time requirements gating the associated job, and the
/// department it belongs to (for grouping the overview the same way the lobby does).
/// </summary>
[Serializable, NetSerializable]
public struct RoleTimeInfo(
    string tracker,
    string displayName,
    TimeSpan time,
    string? requirement,
    string? departmentName,
    string departmentColor,
    int departmentWeight,
    bool editable = true,
    bool? metRequirements = null)
{
    public string Tracker = tracker;
    public string DisplayName = displayName;
    public TimeSpan Time = time;

    /// <summary>False for informational rows with no backing tracker (e.g. antag requirement rows).</summary>
    public bool Editable = editable;

    /// <summary>
    /// For antag rows: whether the target player currently meets the requirements
    /// (null when not applicable / not computable).
    /// </summary>
    public bool? MetRequirements = metRequirements;

    /// <summary>Pre-formatted "Requires: ..." text, or null if the role has no time requirements.</summary>
    public string? Requirement = requirement;

    /// <summary>Localized department name, or null for trackers with no department (e.g. Overall).</summary>
    public string? DepartmentName = departmentName;

    /// <summary>Department color as a hex string, used for the group header.</summary>
    public string DepartmentColor = departmentColor;

    /// <summary>Department sort weight (higher sorts first); ignored for the catch-all group.</summary>
    public int DepartmentWeight = departmentWeight;
}

/// <summary>
/// Sent from the client to set a tracker to an absolute time value.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleTimesSetMessage(string tracker, TimeSpan time) : EuiMessageBase
{
    public readonly string Tracker = tracker;
    public readonly TimeSpan Time = time;
}
