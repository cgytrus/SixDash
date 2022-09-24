using System;
using System.Collections;

using JetBrains.Annotations;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash.API;

[PublicAPI]
public static class Player {
    public static GameObject? gameObjectInstance { get; private set; }
    public static Transform? transformInstance { get; private set; }
    public static PlayerScript? scriptInstance { get; private set; }

    public static float initialSpeed { get; private set; }

    public static event Action<PlayerScript>? spawn;
    public static event Action<PlayerScript>? death;
    public static event Action<PlayerScript>? wouldDie;
    public static event Action? respawn;

    internal static void Patch() {
        On.PlayerScript.Awake += PlayerScriptAwake;
        On.PlayerScriptEditor.Awake += PlayerScriptEditorAwake;
        On.PlayerScript.Die += PlayerScriptDie;

        On.DeathScript.Update += DeathScriptUpdate;

        World.levelLoading += () => {
            // TODO: get rid of FindObjectOfType if possible
            initialSpeed = Object.FindObjectOfType<PathFollower>().speed;
        };
    }

    private static void PlayerScriptAwake(On.PlayerScript.orig_Awake orig, PlayerScript self) {
        gameObjectInstance = self.gameObject;
        transformInstance = self.transform;
        scriptInstance = self;
        orig(self);
        spawn?.Invoke(self);
        Plugin.StartGlobalCoroutine(ResetMaximumDeltaTimeDelayed());
    }

    private static void PlayerScriptEditorAwake(On.PlayerScriptEditor.orig_Awake orig, PlayerScriptEditor self) {
        gameObjectInstance = self.gameObject;
        transformInstance = self.transform;
        scriptInstance = self;
        orig(self);
        spawn?.Invoke(self);
        Plugin.StartGlobalCoroutine(ResetMaximumDeltaTimeDelayed());
    }

    private static IEnumerator ResetMaximumDeltaTimeDelayed() {
        Time.maximumDeltaTime = 0f;
        // it's 3 frames
        // don't ask why.
        yield return null;
        yield return null;
        yield return null;
        Time.maximumDeltaTime = 0.2f;
    }

    private static void PlayerScriptDie(On.PlayerScript.orig_Die orig, PlayerScript self, bool deathOverride) {
        if(self.dead || self.noDeath && !deathOverride) {
            wouldDie?.Invoke(self);
            return;
        }

        self.dead = true;
        Transform transform = self.transform;
        Object.Destroy(transform.parent.parent.gameObject);
        Object.Instantiate(self.DeathFX, transform.position, transform.rotation);
        if(Music.music) {
            Music.music!.Stop();
            Music.music.time = Music.offset;
        }
        death?.Invoke(self);
    }

    private static void DeathScriptUpdate(On.DeathScript.orig_Update orig, DeathScript self) {
        self.timePassed += Time.deltaTime;
        if(self.timePassed < 1.2f)
            return;

        Object.Destroy(self.gameObject);
        respawn?.Invoke();
    }
}
