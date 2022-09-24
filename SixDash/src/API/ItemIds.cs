using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace SixDash.API;

/// <summary>
/// Provides items IDs of items.
/// </summary>
[PublicAPI]
public static class ItemIds {
    private static readonly IReadOnlyDictionary<string, int> official = new Dictionary<string, int> {
        { "3dash:blocks/normal", 0 },
        { "3dash:hazards/spike", 1 },
        { "3dash:blocks/grid", 2 },
        { "3dash:blocks/half", 3 },
        { "3dash:hazards/slope", 4 },
        { "3dash:decoration/chain", 5 },
        { "3dash:hazards/spikeball", 6 },
        { "3dash:hazards/sawblade", 7 },
        { "3dash:hazards/spikeGroup", 8 },
        { "3dash:pads/yellow", 9 },
        { "3dash:pads/cyan", 10 },
        { "3dash:pads/magenta", 11 },
        { "3dash:pads/red", 12 },
        { "3dash:orbs/yellow", 13 },
        { "3dash:orbs/cyan", 14 },
        { "3dash:orbs/magenta", 15 },
        { "3dash:orbs/red", 16 },
        { "3dash:orbs/green", 17 },
        { "3dash:orbs/black", 18 },
        { "3dash:portals/mode/cube", 19 },
        { "3dash:portals/mode/ship", 20 }, // called rocket in 3Dash wtf
        { "3dash:portals/mode/wave", 21 },
        { "3dash:portals/mode/hedron", 22 }, // ball alternative
        { "3dash:portals/gravity/normal", 23 },
        { "3dash:portals/gravity/reverse", 24 },
        { "3dash:portals/speed/normal", 25 },
        { "3dash:portals/speed/fast", 26 },
        { "3dash:portals/speed/super", 27 },
        { "3dash:portals/size/normal", 28 },
        { "3dash:portals/size/small", 29 },
        { "3dash:triggers/finish", 30 },
        { "3dash:triggers/color", 31 },
        { "3dash:portals/mode/ufo", 32 },
        { "3dash:blocks/cosmicGrid", 33 },
        { "3dash:hazards/cosmicSpike", 34 },
        { "3dash:decoration/cosmicChain", 35 }
    };

    private static readonly IReadOnlyDictionary<int, string> officialInverse =
        official.ToDictionary(pair => pair.Value, pair => pair.Key);

    private static readonly IReadOnlyDictionary<string, int> custom = new Dictionary<string, int> {
        { "3dash:blocks/normal", 0 },
        { "3dash:blocks/grid", 1 },
        { "3dash:blocks/half", 2 },
        { "3dash:hazards/spike", 3 },
        { "3dash:hazards/spikeGroup", 4 },
        { "3dash:hazards/sawblade", 5 },
        { "3dash:hazards/spikeball", 6 },
        { "3dash:hazards/slope", 7 },
        { "3dash:pads/magenta", 8 },
        { "3dash:pads/yellow", 9 },
        { "3dash:pads/red", 10 },
        { "3dash:pads/cyan", 11 },
        { "3dash:orbs/magenta", 12 },
        { "3dash:orbs/yellow", 13 },
        { "3dash:orbs/red", 14 },
        { "3dash:orbs/cyan", 15 },
        { "3dash:orbs/green", 16 },
        { "3dash:orbs/black", 17 },
        { "3dash:portals/gravity/normal", 18 },
        { "3dash:portals/gravity/reverse", 19 },
        { "3dash:portals/mode/cube", 20 },
        { "3dash:portals/mode/ship", 21 }, // called rocket in 3Dash wtf
        { "3dash:portals/mode/wave", 22 },
        { "3dash:portals/mode/ufo", 23 },
        { "3dash:portals/mode/hedron", 24 }, // ball alternative
        { "3dash:portals/size/normal", 25 },
        { "3dash:portals/size/small", 26 },
        { "3dash:portals/speed/normal", 27 },
        { "3dash:portals/speed/fast", 28 },
        { "3dash:portals/speed/super", 29 },
        { "3dash:decoration/chain", 30 },
        { "3dash:triggers/color", 31 },
        { "3dash:triggers/finish", 32 },
        { "3dash:blocks/cosmicGrid", 33 },
        { "3dash:hazards/cosmicSpike", 34 },
        { "3dash:decoration/cosmicChain", 35 }
    };

    private static readonly IReadOnlyDictionary<int, string> customInverse =
        custom.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>
    /// Gets the numeric ID of an item from its string ID.
    /// </summary>
    /// <param name="official">Whether the item is in an official level.</param>
    /// <param name="id">The string ID of the item.</param>
    /// <returns>The numeric ID of the item.</returns>
    /// <seealso cref="Get(bool, int)"/>
    public static int Get(bool official, string id) => official ? GetOfficial(id) : GetCustom(id);

    /// <summary>
    /// Gets the string ID of an item from its numeric ID.
    /// </summary>
    /// <param name="official">Whether the item is in an official level.</param>
    /// <param name="id">The numeric ID of the item.</param>
    /// <returns>The string ID of the item.</returns>
    /// <seealso cref="Get(bool, string)"/>
    public static string Get(bool official, int id) => official ? GetOfficial(id) : GetCustom(id);

    /// <summary>
    /// Gets the numeric ID of an item in an official level from its string ID.
    /// </summary>
    /// <param name="id">The string ID of the item.</param>
    /// <returns>The numeric ID of the item.</returns>
    /// <seealso cref="GetOfficial(int)"/>
    public static int GetOfficial(string id) => official[id];

    /// <summary>
    /// Gets the string ID of an item in an official level from its numeric ID.
    /// </summary>
    /// <param name="id">The numeric ID of the item.</param>
    /// <returns>The string ID of the item.</returns>
    /// <seealso cref="GetOfficial(string)"/>
    public static string GetOfficial(int id) => officialInverse[id];

    /// <summary>
    /// Gets the numeric ID of an item in a custom level from its string ID.
    /// </summary>
    /// <param name="id">The string ID of the item.</param>
    /// <returns>The numeric ID of the item.</returns>
    /// <seealso cref="GetCustom(int)"/>
    public static int GetCustom(string id) => custom[id];

    /// <summary>
    /// Gets the string ID of an item in a custom level from its numeric ID.
    /// </summary>
    /// <param name="id">The numeric ID of the item.</param>
    /// <returns>The string ID of the item.</returns>
    /// <seealso cref="GetCustom(string)"/>
    public static string GetCustom(int id) => customInverse[id];
}
