using System.Collections;
using System.IO;

using BepInEx;

using SixDash.API;

using UnityEngine;

namespace SixDash;

[BepInPlugin("mod.cgytrus.plugins.sixdash", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static Plugin? instance { get; private set; }

    private void Awake() {
        instance = this;

        Logger.LogInfo("Loading assets");
        AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "6dash"));
        API.World.LoadAssets(bundle);

        Logger.LogInfo("Applying patches");
        API.World.Patch();
        Player.Patch();
        Music.Patch();
        Util.ApplyAllPatches();
    }

    private void Start() {
        GameObject gizmosCamObj = new("Gizmos Camera");
        DontDestroyOnLoad(gizmosCamObj);
        gizmosCamObj.AddComponent<GizmosCamera>();
    }

    internal static void StartGlobalCoroutine(IEnumerator routine) => instance!.StartCoroutine(routine);
}
