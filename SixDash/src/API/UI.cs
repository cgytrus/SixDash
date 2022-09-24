using System;
using System.Collections.Generic;
using System.Text;

using JetBrains.Annotations;

using TMPro;

using UnityEngine;

namespace SixDash.API;

/// <summary>
/// APIs related to the game's user interface.
/// </summary>
[PublicAPI]
public static class UI {
    /// <summary>
    /// The outline width used commonly across the game's texts.
    /// </summary>
    public const float FontOutlineWidth = 0.304f;

    /// <summary>
    /// The font style used commonly across the game's texts.
    /// </summary>
    public const FontStyles FontStyle = FontStyles.Bold;

    private static readonly List<string> versionTexts = new(4);

    /// <summary>
    /// The font asset used commonly across the game's texts.
    /// </summary>
    /// <seealso cref="fontMaterial"/>
    public static TMP_FontAsset? fontAsset { get; private set; }

    /// <summary>
    /// The font material used commonly across the game's texts.
    /// </summary>
    /// <seealso cref="fontAsset"/>
    public static Material? fontMaterial { get; private set; }

    internal static void Setup() {
        fontAsset = Util.FindResourceOfTypeWithName<TMP_FontAsset>("league-spartan.bold SDF");
        fontMaterial = Util.FindResourceOfTypeWithName<Material>("league-spartan.bold Atlas Material");

        On.MenuButtonScript.Start += (orig, self) => {
            orig(self);

            TMP_Text version = self.transform.parent.Find("Version Text").GetComponent<TMP_Text>();
            version.enableWordWrapping = false;
            StringBuilder textToAppend = new();
            foreach(string text in versionTexts) {
                textToAppend.Append('\n');
                textToAppend.Append(text);
            }
            version.text = $"3Dash {version.text}{textToAppend}";
        };
    }

    /// <summary>
    /// Appends some text to the bottom of the version text seen in the bottom left corner of the main menu.
    /// </summary>
    /// <param name="text">The text to append to the version text.</param>
    public static void AddVersionText(string text) => versionTexts.Add(text);
}
