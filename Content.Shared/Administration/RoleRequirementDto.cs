using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Content.Shared.Roles;

namespace Content.Shared.Administration;

/// <summary>
/// A flat, non-polymorphic representation of a play time requirement, used for persisting and replicating the
/// role requirement override without the fragility of polymorphic YAML serialization. Covers the three
/// time-based requirement types the editor supports.
/// </summary>
public sealed class RoleRequirementDto
{
    public string Kind { get; set; } = "";
    public string Target { get; set; } = "";
    public double Seconds { get; set; }
    public bool Inverted { get; set; }

    public const string KindOverall = "overall";
    public const string KindRole = "role";
    public const string KindDepartment = "department";

    /// <summary>Converts a requirement to a DTO, or null for types that can't be represented (dropped).</summary>
    public static RoleRequirementDto? FromRequirement(JobRequirement req)
    {
        switch (req)
        {
            case OverallPlaytimeRequirement o:
                return new RoleRequirementDto { Kind = KindOverall, Seconds = o.Time.TotalSeconds, Inverted = o.Inverted };
            case RoleTimeRequirement r:
                return new RoleRequirementDto { Kind = KindRole, Target = r.Role.Id, Seconds = r.Time.TotalSeconds, Inverted = r.Inverted };
            case DepartmentTimeRequirement d:
                return new RoleRequirementDto { Kind = KindDepartment, Target = d.Department.Id, Seconds = d.Time.TotalSeconds, Inverted = d.Inverted };
            default:
                return null;
        }
    }

    /// <summary>Rebuilds a requirement from a DTO, or null if the kind is unknown.</summary>
    public JobRequirement? ToRequirement()
    {
        var time = TimeSpan.FromSeconds(Seconds);
        switch (Kind)
        {
            case KindOverall:
                return new OverallPlaytimeRequirement { Time = time, Inverted = Inverted };
            case KindRole:
                return new RoleTimeRequirement { Role = Target, Time = time, Inverted = Inverted };
            case KindDepartment:
                return new DepartmentTimeRequirement { Department = Target, Time = time, Inverted = Inverted };
            default:
                return null;
        }
    }

    // ---- Replication format ----
    // A tiny line-based text format used for the server->client override message. Hand-rolled (not JSON) so it
    // stays within the client's type-check sandbox (System.Text.Json is not allowed there). Job ids, kinds and
    // targets are all alphanumeric prototype ids, so space delimiting is safe.

    public static string Serialize(
        Dictionary<string, List<RoleRequirementDto>> jobs,
        Dictionary<string, List<RoleRequirementDto>>? antags = null)
    {
        var sb = new StringBuilder();
        AppendRoles(sb, "JOB ", jobs);
        if (antags != null)
            AppendRoles(sb, "ANTAG ", antags);

        return sb.ToString();
    }

    private static void AppendRoles(StringBuilder sb, string prefix, Dictionary<string, List<RoleRequirementDto>> roles)
    {
        foreach (var (roleId, list) in roles)
        {
            sb.Append(prefix).Append(roleId).Append('\n');
            foreach (var dto in list)
            {
                sb.Append("REQ ")
                    .Append(dto.Kind).Append(' ')
                    .Append(dto.Target.Length == 0 ? "_" : dto.Target).Append(' ')
                    .Append(dto.Seconds.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(dto.Inverted ? '1' : '0').Append('\n');
            }
        }
    }

    public static (Dictionary<string, List<RoleRequirementDto>> Jobs, Dictionary<string, List<RoleRequirementDto>> Antags) Deserialize(string data)
    {
        var jobs = new Dictionary<string, List<RoleRequirementDto>>();
        var antags = new Dictionary<string, List<RoleRequirementDto>>();
        List<RoleRequirementDto>? current = null;

        foreach (var line in data.Split('\n'))
        {
            if (line.StartsWith("JOB "))
            {
                current = new List<RoleRequirementDto>();
                jobs[line[4..]] = current;
            }
            else if (line.StartsWith("ANTAG "))
            {
                current = new List<RoleRequirementDto>();
                antags[line[6..]] = current;
            }
            else if (line.StartsWith("REQ ") && current != null)
            {
                var parts = line[4..].Split(' ');
                if (parts.Length != 4
                    || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                    continue;

                current.Add(new RoleRequirementDto
                {
                    Kind = parts[0],
                    Target = parts[1] == "_" ? "" : parts[1],
                    Seconds = seconds,
                    Inverted = parts[3] == "1",
                });
            }
        }

        return (jobs, antags);
    }
}
