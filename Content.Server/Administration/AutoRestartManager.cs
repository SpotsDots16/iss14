using System;
using System.Globalization;
using System.Text.Json;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server.Administration;

/// <summary>
/// iss14: Admin-editable, persistent configuration for the scheduled daily server restart
/// (enabled + time of day, both CVar-backed). The actual restart scheduling lives in
/// <see cref="Content.Server.ServerUpdates.ServerUpdateManager"/>, which watches the CVars.
/// Persisted as JSON to the server user-data dir, mirroring <see cref="NewLifeManager"/>.
/// </summary>
public sealed partial class AutoRestartManager
{
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly ResPath Dir = new("/auto_restart_config");
    private static readonly ResPath ConfigFile = new("/auto_restart_config/config.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private ISawmill _sawmill = default!;

    // Whether the admin window has ever overridden the CVar-backed settings (so we re-apply on boot).
    private bool _settingsPersisted;
    private bool _enabledStore;
    private string _timeStore = "03:00";

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("auto_restart");
        Load();
    }

    /// <summary>Re-applies persisted setting overrides after the config system is up.</summary>
    public void PostInitialize()
    {
        if (!_settingsPersisted)
            return;

        _cfg.SetCVar(CCVars.ServerAutoRestartEnabled, _enabledStore);
        _cfg.SetCVar(CCVars.ServerAutoRestartTime, _timeStore);
    }

    public bool Enabled => _cfg.GetCVar(CCVars.ServerAutoRestartEnabled);
    public string Time => _cfg.GetCVar(CCVars.ServerAutoRestartTime);

    public void SetEnabled(bool value)
    {
        _cfg.SetCVar(CCVars.ServerAutoRestartEnabled, value);
        _enabledStore = value;
        _timeStore = Time;
        MarkPersistedAndSave();
    }

    /// <summary>Sets the restart time of day. Rejects strings that aren't valid 24h "HH:mm".</summary>
    public bool SetTime(string value)
    {
        if (!TryParseTimeOfDay(value, out var parsed))
            return false;

        var normalized = parsed.ToString(@"hh\:mm");
        _cfg.SetCVar(CCVars.ServerAutoRestartTime, normalized);
        _timeStore = normalized;
        _enabledStore = Enabled;
        MarkPersistedAndSave();
        return true;
    }

    /// <summary>Parses a 24h "HH:mm" (or "H:mm") time-of-day string.</summary>
    public static bool TryParseTimeOfDay(string text, out TimeSpan time)
    {
        time = default;
        var formats = new[] { @"hh\:mm", @"h\:mm" };
        foreach (var format in formats)
        {
            if (TimeSpan.TryParseExact(text.Trim(), format, CultureInfo.InvariantCulture, out time))
                return time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
        }

        return false;
    }

    private void MarkPersistedAndSave()
    {
        _settingsPersisted = true;
        Save();
    }

    private void Save()
    {
        try
        {
            _res.UserData.CreateDir(Dir);
            using var writer = _res.UserData.OpenWriteText(ConfigFile);
            writer.Write(JsonSerializer.Serialize(BuildData(), JsonOpts));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to write auto restart config: {e}");
        }
    }

    private void Load()
    {
        if (!_res.UserData.Exists(ConfigFile))
            return;

        try
        {
            using var reader = _res.UserData.OpenText(ConfigFile);
            var data = JsonSerializer.Deserialize<ConfigData>(reader.ReadToEnd(), JsonOpts);
            if (data == null)
                return;

            _settingsPersisted = data.SettingsPersisted;
            _enabledStore = data.Enabled;
            _timeStore = data.Time;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to read auto restart config: {e}");
        }
    }

    private ConfigData BuildData()
    {
        return new ConfigData
        {
            SettingsPersisted = _settingsPersisted,
            Enabled = _enabledStore,
            Time = _timeStore,
        };
    }

    private sealed class ConfigData
    {
        public bool SettingsPersisted { get; set; }
        public bool Enabled { get; set; }
        public string Time { get; set; } = "03:00";
    }
}
