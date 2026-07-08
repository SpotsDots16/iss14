using System;
using System.Text.Json;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server.Administration;

/// <summary>
/// iss14: Admin-editable, persistent configuration for the round-end advertisement broadcast
/// (enabled + message, both CVar-backed). The broadcast itself happens in the GameTicker at round end.
/// Persisted as JSON to the server user-data dir, mirroring <see cref="AutoRestartManager"/>.
/// </summary>
public sealed partial class RoundEndAdManager
{
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly ResPath Dir = new("/round_end_ad_config");
    private static readonly ResPath ConfigFile = new("/round_end_ad_config/config.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private ISawmill _sawmill = default!;

    // Whether the admin window has ever overridden the CVar-backed settings (so we re-apply on boot).
    private bool _settingsPersisted;
    private bool _enabledStore;
    private string _messageStore = "";

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("round_end_ad");
        Load();
    }

    /// <summary>Re-applies persisted setting overrides after the config system is up.</summary>
    public void PostInitialize()
    {
        if (!_settingsPersisted)
            return;

        _cfg.SetCVar(CCVars.ServerRoundEndAdEnabled, _enabledStore);
        _cfg.SetCVar(CCVars.ServerRoundEndAdMessage, _messageStore);
    }

    public bool Enabled => _cfg.GetCVar(CCVars.ServerRoundEndAdEnabled);
    public string Message => _cfg.GetCVar(CCVars.ServerRoundEndAdMessage);

    /// <summary>
    /// The message ready for broadcast: literal "\n" converted to line breaks, trimmed.
    /// Null when the ad is disabled or the message is empty.
    /// </summary>
    public string? GetBroadcastMessage()
    {
        if (!Enabled)
            return null;

        var message = Message.Replace("\\n", "\n").Trim();
        return message.Length != 0 ? message : null;
    }

    public void SetEnabled(bool value)
    {
        _cfg.SetCVar(CCVars.ServerRoundEndAdEnabled, value);
        _enabledStore = value;
        _messageStore = Message;
        MarkPersistedAndSave();
    }

    public void SetMessage(string value)
    {
        _cfg.SetCVar(CCVars.ServerRoundEndAdMessage, value);
        _messageStore = value;
        _enabledStore = Enabled;
        MarkPersistedAndSave();
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
            _sawmill.Error($"Failed to write round end ad config: {e}");
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
            _messageStore = data.Message;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to read round end ad config: {e}");
        }
    }

    private ConfigData BuildData()
    {
        return new ConfigData
        {
            SettingsPersisted = _settingsPersisted,
            Enabled = _enabledStore,
            Message = _messageStore,
        };
    }

    private sealed class ConfigData
    {
        public bool SettingsPersisted { get; set; }
        public bool Enabled { get; set; }
        public string Message { get; set; } = "";
    }
}
