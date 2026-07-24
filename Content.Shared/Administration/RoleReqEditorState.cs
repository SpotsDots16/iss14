using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>The kind of a play time requirement, for the role requirement editor.</summary>
public enum RoleReqKind : byte
{
    /// <summary>Overall playtime requirement.</summary>
    Overall,

    /// <summary>Time in a specific role/tracker.</summary>
    Role,

    /// <summary>Cumulative time across a department.</summary>
    Department,

    /// <summary>A requirement type the editor does not understand (shown read-only, removable).</summary>
    Other,
}

/// <summary>One requirement row for a job in the editor.</summary>
[Serializable, NetSerializable]
public struct RoleReqEntry(
    int index,
    RoleReqKind kind,
    string target,
    TimeSpan time,
    bool inverted,
    bool editable,
    string display)
{
    /// <summary>Index within the job's requirement list; used to address edits/removals.</summary>
    public int Index = index;
    public RoleReqKind Kind = kind;

    /// <summary>Tracker id (Role) or department id (Department); empty otherwise.</summary>
    public string Target = target;
    public TimeSpan Time = time;
    public bool Inverted = inverted;

    /// <summary>False for non-time requirement types — shown read-only but still removable.</summary>
    public bool Editable = editable;

    /// <summary>Human-readable summary of the requirement.</summary>
    public string Display = display;
}

/// <summary>A job and its (effective) requirements in the editor, grouped by department.</summary>
[Serializable, NetSerializable]
public struct RoleReqJobInfo(
    string jobId,
    string jobName,
    bool overridden,
    string? departmentName,
    string departmentColor,
    int departmentWeight,
    List<RoleReqEntry> requirements,
    bool isAntag = false)
{
    public string JobId = jobId;
    public string JobName = jobName;

    /// <summary>True when this entry is an antag role rather than a station job.</summary>
    public bool IsAntag = isAntag;

    /// <summary>True if this job currently has a custom override (vs. its YAML defaults).</summary>
    public bool Overridden = overridden;
    public string? DepartmentName = departmentName;
    public string DepartmentColor = departmentColor;
    public int DepartmentWeight = departmentWeight;
    public List<RoleReqEntry> Requirements = requirements;
}

/// <summary>State for the role requirement editor EUI (<c>roletimereqs</c> command).</summary>
[Serializable, NetSerializable]
public sealed class RoleReqEditorState(
    bool canEdit,
    bool roleTimersEnabled,
    bool overridesEnabled,
    List<string> profiles,
    List<RoleReqJobInfo> jobs)
    : EuiStateBase
{
    public readonly bool CanEdit = canEdit;

    /// <summary>The master <c>game.role_timers</c> switch.</summary>
    public readonly bool RoleTimersEnabled = roleTimersEnabled;

    /// <summary>Whether custom overrides are currently applied.</summary>
    public readonly bool OverridesEnabled = overridesEnabled;

    public readonly List<string> Profiles = profiles;
    public readonly List<RoleReqJobInfo> Jobs = jobs;
}

// ---- Client -> server messages ----

[Serializable, NetSerializable]
public sealed class RoleReqSetTimersEnabledMessage(bool value) : EuiMessageBase
{
    public readonly bool Value = value;
}

[Serializable, NetSerializable]
public sealed class RoleReqSetOverridesEnabledMessage(bool value) : EuiMessageBase
{
    public readonly bool Value = value;
}

[Serializable, NetSerializable]
public sealed class RoleReqEditTimeMessage(string jobId, int index, TimeSpan time, bool isAntag = false) : EuiMessageBase
{
    public readonly string JobId = jobId;
    public readonly bool IsAntag = isAntag;
    public readonly int Index = index;
    public readonly TimeSpan Time = time;
}

[Serializable, NetSerializable]
public sealed class RoleReqSetInvertedMessage(string jobId, int index, bool inverted, bool isAntag = false) : EuiMessageBase
{
    public readonly string JobId = jobId;
    public readonly bool IsAntag = isAntag;
    public readonly int Index = index;
    public readonly bool Inverted = inverted;
}

[Serializable, NetSerializable]
public sealed class RoleReqRemoveMessage(string jobId, int index, bool isAntag = false) : EuiMessageBase
{
    public readonly string JobId = jobId;
    public readonly bool IsAntag = isAntag;
    public readonly int Index = index;
}

[Serializable, NetSerializable]
public sealed class RoleReqAddMessage(string jobId, RoleReqKind kind, string target, TimeSpan time, bool inverted, bool isAntag = false)
    : EuiMessageBase
{
    public readonly string JobId = jobId;
    public readonly bool IsAntag = isAntag;
    public readonly RoleReqKind Kind = kind;
    public readonly string Target = target;
    public readonly TimeSpan Time = time;
    public readonly bool Inverted = inverted;
}

[Serializable, NetSerializable]
public sealed class RoleReqResetJobMessage(string jobId, bool isAntag = false) : EuiMessageBase
{
    public readonly string JobId = jobId;
    public readonly bool IsAntag = isAntag;
}

[Serializable, NetSerializable]
public sealed class RoleReqSaveProfileMessage(string name) : EuiMessageBase
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class RoleReqLoadProfileMessage(string name) : EuiMessageBase
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class RoleReqDeleteProfileMessage(string name) : EuiMessageBase
{
    public readonly string Name = name;
}

/// <summary>Imports the requirement_overrides.yml setup (the active jobRequirementOverride prototype).</summary>
[Serializable, NetSerializable]
public sealed class RoleReqImportPrototypeMessage : EuiMessageBase;
