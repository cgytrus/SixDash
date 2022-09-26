using System;
using System.Collections;

using JetBrains.Annotations;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash.API;

/// <summary>
/// APIs related to the player.
/// </summary>
[PublicAPI]
public static class Player {
    /// <summary>
    /// Vanilla respawn time.
    /// </summary>
    public const float VanillaRespawnTime = 1.2f;

    /// <summary>
    /// Instance of the current player's <see cref="GameObject"/>.
    /// </summary>
    public static GameObject? gameObjectInstance { get; private set; }

    /// <summary>
    /// Instance of the current player's <see cref="Transform"/>.
    /// </summary>
    public static Transform? transformInstance { get; private set; }

    /// <summary>
    /// Instance of the current player's <see cref="PlayerScript"/>.
    /// </summary>
    public static PlayerScript? scriptInstance { get; private set; }

    /// <summary>
    /// Instance of the current player's <see cref="PathFollower"/>.
    /// </summary>
    public static PathFollower? pathFollowerInstance { get; private set; }

    /// <summary>
    /// The initial speed of the player in the current level.
    /// </summary>
    public static float initialSpeed { get; private set; }

    /// <summary>
    /// Respawn time of the player on death. Works as a hook (get original value, return modified value).
    /// </summary>
    public static event Func<float, float> respawnTime = orig => orig;

    /// <summary>
    /// Fired when the player is spawned. Includes the initial spawn and all the subsequent respawns.
    /// </summary>
    /// <seealso cref="respawn"/>
    public static event Action<PlayerScript>? spawn;

    /// <summary>
    /// Fired when the player dies.
    /// </summary>
    /// <seealso cref="wouldDie"/>
    public static event Action<PlayerScript>? death;

    /// <summary>
    /// Fired when the player is supposed to die but does not
    /// (for example, if <see cref="PlayerScript.noDeath"/> is set to <b>true</b>).
    /// </summary>
    /// <seealso cref="death"/>
    public static event Action<PlayerScript>? wouldDie;

    /// <summary>
    /// Fired when the player respawns. Does not include the initial spawn, only the subsequent respawns.
    /// </summary>
    /// <seealso cref="spawn"/>
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

        On.PathFollower.Awake += (orig, self) => {
            pathFollowerInstance = self;
            orig(self);
        };
    }

    private static void PlayerScriptAwake(On.PlayerScript.orig_Awake orig, PlayerScript self) {
        gameObjectInstance = self.gameObject;
        transformInstance = self.transform;
        scriptInstance = self;
        orig(self);
        spawn?.Invoke(self);
    }

    private static void PlayerScriptEditorAwake(On.PlayerScriptEditor.orig_Awake orig, PlayerScriptEditor self) {
        gameObjectInstance = self.gameObject;
        transformInstance = self.transform;
        scriptInstance = self;
        orig(self);
        spawn?.Invoke(self);
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
        if(self.timePassed < respawnTime(VanillaRespawnTime))
            return;

        Object.Destroy(self.gameObject);
        respawn?.Invoke();
    }
}
