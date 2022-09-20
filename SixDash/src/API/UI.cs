using System;
using System.Collections.Generic;
using System.Text;

using JetBrains.Annotations;

using TMPro;

using UnityEngine;

namespace SixDash.API;

[PublicAPI]
public static class UI {
    public const float FontOutlineWidth = 0.304f;
    public const FontStyles FontStyle = FontStyles.Bold;

    private static readonly List<string> versionTexts = new(4);

    public static TMP_FontAsset? fontAsset { get; private set; }
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

    public static void AddVersionText(string text) => versionTexts.Add(text);
}
