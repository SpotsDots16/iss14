using System;
using System.Globalization;
using Content.Shared.Administration;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.RoleTimes;

/// <summary>
/// A single row in the role time overview: friendly role name, an editable time field, and a Set button.
/// Times are shown and parsed as <c>HH:MM</c>, or a plain number of minutes when typing.
/// </summary>
public sealed class RoleTimeRow : BoxContainer
{
    /// <summary>Raised with the new absolute time when the admin presses Set.</summary>
    public event Action<TimeSpan>? OnSet;

    /// <summary>Width the requirement text wraps at, sized to fit left of the edit field + button.</summary>
    private const float RequirementMaxWidth = 340;

    private readonly string _tracker;
    private readonly string _displayName;
    private readonly LineEdit _edit;

    public RoleTimeRow(RoleTimeInfo info, bool canEdit)
    {
        _tracker = info.Tracker;
        _displayName = info.DisplayName;

        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;
        Margin = new Thickness(0, 1);

        // Left cell: role name on top, optional requirement summary underneath.
        var nameBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        var name = new Label
        {
            Text = info.DisplayName,
            ClipText = true,
        };

        // Surface the raw tracker id when it differs from the friendly name.
        if (info.DisplayName != info.Tracker)
            name.ToolTip = info.Tracker;

        nameBox.AddChild(name);

        if (!string.IsNullOrEmpty(info.Requirement))
        {
            // RichTextLabel with a MaxWidth wraps onto multiple lines instead of overflowing the window.
            var req = new RichTextLabel
            {
                MaxWidth = RequirementMaxWidth,
                Modulate = Color.DarkGray,
            };
            req.SetMessage(FormattedMessage.FromUnformatted(info.Requirement));
            nameBox.AddChild(req);
        }

        AddChild(nameBox);

        // Informational rows (antag requirements) have no backing tracker: no time edit,
        // just whether the player currently qualifies.
        if (!info.Editable)
        {
            _edit = new LineEdit { Visible = false };

            if (info.MetRequirements is { } met)
            {
                AddChild(new Label
                {
                    Text = Loc.GetString(met ? "role-times-antag-met" : "role-times-antag-unmet"),
                    FontColorOverride = met ? Color.FromHex("#3fc47a") : Color.FromHex("#d63c3c"),
                    VerticalAlignment = VAlignment.Center,
                });
            }

            return;
        }

        _edit = new LineEdit
        {
            Text = Format(info.Time),
            MinWidth = 90,
            Editable = canEdit,
            ToolTip = Loc.GetString("role-times-edit-tooltip"),
        };

        var button = new Button
        {
            Text = Loc.GetString("role-times-set-button"),
            Disabled = !canEdit,
            Margin = new Thickness(4, 0, 0, 0),
        };
        button.OnPressed += _ =>
        {
            if (TryParse(_edit.Text, out var ts))
                OnSet?.Invoke(ts);
        };

        AddChild(_edit);
        AddChild(button);
    }

    public bool Matches(string search)
    {
        return _displayName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || _tracker.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string Format(TimeSpan time)
    {
        // Whole hours and minutes, e.g. "12:30".
        return $"{(int) time.TotalHours:00}:{time.Minutes:00}";
    }

    private static bool TryParse(string text, out TimeSpan time)
    {
        time = default;
        text = text.Trim();
        if (string.IsNullOrEmpty(text))
            return false;

        // "HH:MM"
        if (text.Contains(':'))
        {
            var parts = text.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
                || hours < 0 || minutes < 0)
                return false;

            time = new TimeSpan(hours, minutes, 0);
            return true;
        }

        // Plain number of minutes.
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var totalMinutes)
            || totalMinutes < 0)
            return false;

        time = TimeSpan.FromMinutes(totalMinutes);
        return true;
    }
}
