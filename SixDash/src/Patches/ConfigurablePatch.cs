using System;
using System.Linq;

using BepInEx.Configuration;

using JetBrains.Annotations;

namespace SixDash.Patches;

/// <summary>
/// Represents a simple patch that can be toggled using BepInEx's config API.
/// </summary>
[UsedImplicitly]
public abstract class ConfigurablePatch : IPatch {
    /// <summary>
    /// Indicates whether the patch is enabled.
    /// </summary>
    [UsedImplicitly]
    protected virtual bool enabled { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="config">The <see cref="BepInEx.BaseUnityPlugin.Config"/> of the current plugin.</param>
    /// <param name="section">The section the config entry will be bound to.</param>
    /// <param name="key">The key the config entry will be bound to.
    /// <see cref="object.GetType"/>.<see cref="Type.FullName"/> if null.</param>
    /// <param name="defaultValue">Whether the patch is enabled by default.</param>
    /// <param name="description">The description of the config entry.</param>
    /// <exception cref="InvalidOperationException">Thrown when a config entry in the same section
    /// and with the same key already exists.</exception>
    protected ConfigurablePatch(ConfigFile config, string section, string? key, bool defaultValue,
        string? description) {
        if(string.IsNullOrWhiteSpace(key))
            key = GetType().FullName;
        if(config.Select(conf => conf.Key).Any(def => def.Section == section && def.Key == key))
            throw new InvalidOperationException("Tried loading a patch which is already loaded.");

        ConfigEntry<bool> configEntry = config.Bind(section, key, defaultValue, description ?? "");
        // TODO: make not virtual or do smth about this idk
        enabled = configEntry.Value;
        configEntry.SettingChanged += (_, _) => { enabled = configEntry.Value; };
    }

    /// <inheritdoc cref="IPatch.Apply"/>
    public abstract void Apply();
}
