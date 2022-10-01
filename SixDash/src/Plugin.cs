using System.Collections;
using System.IO;

using BepInEx;

using SixDash.API;

using UnityEngine;

namespace SixDash;

[BepInPlugin("mod.cgytrus.plugins.sixdash", SixDashPluginInfo.PLUGIN_NAME, SixDashPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BaseUnityPlugin {
    internal static Plugin? instance { get; private set; }

    private void Awake() {
        instance = this;

        Logger.LogInfo("Loading assets");
        AssetBundle bundle = Util.LoadPlatformAssetBundle("6dash");
        World.LoadAssets(bundle);

        Logger.LogInfo("Applying patches");
        World.Patch();
        Player.Patch();
        Music.Patch();
        Checkpoint.Patch();
        Online.Patch();
        Util.ApplyAllPatches();

        Logger.LogInfo("Initializing UI");
        UI.Setup();
        UI.AddVersionText($"6Dash v{SixDashPluginInfo.PLUGIN_VERSION}");
    }

    private void Start() {
        GameObject gizmosCamObj = new("Gizmos Camera");
        DontDestroyOnLoad(gizmosCamObj);
        gizmosCamObj.AddComponent<GizmosCamera>();
    }

    internal static void StartGlobalCoroutine(IEnumerator routine) => instance!.StartCoroutine(routine);
}
