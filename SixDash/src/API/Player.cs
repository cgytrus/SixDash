using System;
using System.Collections;

using HarmonyLib;

using JetBrains.Annotations;

using MonoMod.RuntimeDetour;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash.API;

[PublicAPI]
public static class Player {
    public static float initialSpeed { get; private set; }
    public static AudioSource? music { get; private set; }
    public static float musicOffset { get; private set; }

    public static event Action<PlayerScript>? playerSpawn;
    public static event Action<PlayerScript>? playerDeath;
    public static event Action<PlayerScript>? playerWouldDie;
    public static event Action? playerRespawn;
    public static event Action<CheckpointScript>? checkpointPlace;
    public static event Action? checkpointRemove;

    internal static void Patch() {
        On.PlayerScript.Awake += PlayerScriptAwake;
        On.PlayerScriptEditor.Awake += PlayerScriptEditorAwake;
        On.PlayerScript.Die += PlayerScriptDie;

        On.DeathScript.Update += DeathScriptUpdate;

        new Hook(AccessTools.Method(typeof(CheckpointScript), "Awake"),
            (Action<CheckpointScript> orig, CheckpointScript self) => {
                orig(self);
                checkpointPlace?.Invoke(self);
            }).Apply();
        On.PauseMenuManager.DeleteCheckpoint += (orig, self) => {
            orig(self);
            checkpointRemove?.Invoke();
        };

        World.levelLoading += () => {
            // TODO: get rid of FindObjectOfType if possible
            initialSpeed = Object.FindObjectOfType<PathFollower>().speed;
        };
    }

    private static void PlayerScriptAwake(On.PlayerScript.orig_Awake orig, PlayerScript self) {
        PlayerAwake();
        orig(self);
        playerSpawn?.Invoke(self);
        Plugin.StartGlobalCoroutine(ResetMaximumDeltaTimeDelayed());
    }

    private static void PlayerScriptEditorAwake(On.PlayerScriptEditor.orig_Awake orig, PlayerScriptEditor self) {
        PlayerAwake();
        orig(self);
        playerSpawn?.Invoke(self);
        Plugin.StartGlobalCoroutine(ResetMaximumDeltaTimeDelayed());
    }

    private static void PlayerAwake() {
        bool firstTime = !music;
        GameObject musicObj = GameObject.FindGameObjectWithTag("Music");
        music = musicObj ? musicObj.GetComponent<AudioSource>() : null;
        if(firstTime && music)
            musicOffset = music!.time == 0f ? LevelEditor.songStartTime / 1000f : music.time;
    }

    private static IEnumerator ResetMaximumDeltaTimeDelayed() {
        float saved = Time.maximumDeltaTime;
        Time.maximumDeltaTime = 0f;
        // it's 3 frames
        // don't ask why.
        yield return null;
        yield return null;
        yield return null;
        Time.maximumDeltaTime = saved;
    }

    private static void PlayerScriptDie(On.PlayerScript.orig_Die orig, PlayerScript self, bool deathOverride) {
        if(self.dead || self.noDeath && !deathOverride) {
            playerWouldDie?.Invoke(self);
            return;
        }

        self.dead = true;
        Transform transform = self.transform;
        Object.Destroy(transform.parent.parent.gameObject);
        Object.Instantiate(self.DeathFX, transform.position, transform.rotation);
        if(music) {
            music!.Stop();
            music.time = musicOffset;
        }
        playerDeath?.Invoke(self);
    }

    private static void DeathScriptUpdate(On.DeathScript.orig_Update orig, DeathScript self) {
        self.timePassed += Time.deltaTime;
        if(self.timePassed < 1.2f)
            return;

        Object.Destroy(self.gameObject);
        playerRespawn?.Invoke();
    }
}
