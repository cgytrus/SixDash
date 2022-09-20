using System;
using System.Collections;
using System.Reflection;

using HarmonyLib;

using JetBrains.Annotations;

using SixDash.API;
using SixDash.Patches;

using UnityEngine;

using Object = UnityEngine.Object;

namespace SixDash.Enhancements;

[UsedImplicitly]
internal class NoReloadOnRespawn : IPatch {
    private static GameObject? _playerPrefab;

    private static GameObject? _deadCamera;
    private static GameObject? _latestPlayer;

    private static readonly FieldInfo savedOrbsField = AccessTools.Field(typeof(CheckpointScript), "savedOrbs");
    private static readonly FieldInfo savedPadsField = AccessTools.Field(typeof(CheckpointScript), "savedPads");
    private static readonly FieldInfo savedPortalsField = AccessTools.Field(typeof(CheckpointScript), "savedPortals");

    public void Apply() {
        Player.playerSpawn += self => {
            if(!_playerPrefab)
                InitializePlayerPrefab(self);
            LoadCheckpoint(self);
        };

        Player.playerDeath += self => {
            _deadCamera = Object.Instantiate(self.cam, self.cam.transform.position, self.cam.transform.rotation);
            _latestPlayer = Object.Instantiate(_playerPrefab);
        };

        Player.playerRespawn += () => {
            if(_deadCamera)
                Object.Destroy(_deadCamera);
            if(_latestPlayer)
                _latestPlayer!.SetActive(true);
        };

        On.ColorChangerEditor.Update += ColorChangerEditorUpdate;
        On.EffectSphereScript.Update += EffectSphereScriptUpdate;

        On.LevelManager.Update += (_, self) => {
            Color.RGBToHSV(DistanceToColor(self.levelColor, PathFollower.distanceTravelled), out float h, out float s, out float v);
            v *= self.valueMultiplier;
            Color color = Color.HSVToRGB(h, s, v);
            if(LevelEditor.backgroundId != 2)
                self.SetSkyboxColor(color);
            self.groundMat.color = color;
            if(self.SkyMat2)
                self.SetStarsColor(Color.HSVToRGB(h, s, v * 0.4f));
        };

        Player.checkpointPlace += SaveCheckpoint;
    }

    private static void InitializePlayerPrefab(Component self) {
        Transform rootTransform = self.transform.parent.parent;
        GameObject root = rootTransform.gameObject;
        root.SetActive(false);
        _playerPrefab = Object.Instantiate(root, rootTransform.position, rootTransform.rotation);
        root.SetActive(true);
    }

    private static void ColorChangerEditorUpdate(On.ColorChangerEditor.orig_Update orig, ColorChangerEditor self) {
        if(!self.gfx.activeSelf)
            return;
        if(self.gameObject.TryGetComponent(out FlatItem flatItem))
            self.myColor = self.GetColorAtValues(flatItem.y, flatItem.angle);
        self.spriteRenderer.color = self.myColor;
    }

    private static void EffectSphereScriptUpdate(On.EffectSphereScript.orig_Update orig, EffectSphereScript self) {
        self.transform.localScale -= Vector3.one * (Time.deltaTime * 3f);
        if(self.transform.localScale.x > 0f)
            return;
        self.transform.localScale = Vector3.one * 2f;
        self.gameObject.SetActive(false);
    }

    private static Color DistanceToColor(Color startColor, float distance) {
        Color prevColor = startColor;
        Color color = startColor;
        float t = 1f;
        foreach(World.ColorChangerData colorChanger in World.levelColorChangers) {
            if(colorChanger.startDistance > distance)
                continue;
            prevColor = color;
            color = colorChanger.color;
            float length = colorChanger.endDistance - colorChanger.startDistance;
            t = (distance - colorChanger.startDistance) / length;
        }
        return Color.Lerp(prevColor, color, t);
    }

    private static void LoadCheckpoint(PlayerScript player) {
        GameObject? recentCheckpoint = PauseMenuManager.inPracticeMode ? PlayerScript.GetRecentCheckpoint() : null;
        CheckpointScript? checkpoint = recentCheckpoint ? recentCheckpoint!.GetComponent<CheckpointScript>() : null;

        if(checkpoint) {
            Animator animator = Object.FindObjectOfType<Animator>();
            player.StartCoroutine(PlayCameraAnimationDelayed(animator, checkpoint!.savedCameraTime));
        }

        float[]? savedOrbs = checkpoint ? (float[])savedOrbsField.GetValue(checkpoint) : null;
        float[]? savedPads = checkpoint ? (float[])savedPadsField.GetValue(checkpoint) : null;
        bool[]? savedPortals = checkpoint ? (bool[])savedPortalsField.GetValue(checkpoint) : null;

        for(int i = 0; i < World.levelOrbs.Count; i++) {
            OrbScript orb = World.levelOrbs[i];
            orb.cooldown = savedOrbs is null ? 0f : savedOrbs[i];
            orb.activated = orb.cooldown > 0f;
            orb.onHit.SetActive(orb.activated);
            orb.onHit.GetComponentInChildren<EffectSphereScript>(true).gameObject.SetActive(!orb.activated);
        }
        for(int i = 0; i < World.levelPads.Count; i++) {
            PadScript pad = World.levelPads[i];
            pad.cooldown = savedPads is null ? 0f : savedPads[i];
            pad.activated = pad.cooldown > 0f;
            pad.onHit.SetActive(pad.activated);
        }
        for(int i = 0; i < World.levelPortals.Count; i++) {
            PortalScript portal = World.levelPortals[i];
            portal.activated = savedPortals is not null && savedPortals[i];
            portal.onHit.SetActive(portal.activated);
        }
    }

    private static IEnumerator PlayCameraAnimationDelayed(Animator animator, float time) {
        yield return null;
        yield return null;
        animator.Play(0, 0, time);
        animator.Update(0.001f);
    }

    private static void SaveCheckpoint(CheckpointScript self) {
        float[] savedOrbs = new float[World.levelOrbs.Count];
        float[] savedPads = new float[World.levelPads.Count];
        bool[] savedPortals = new bool[World.levelPortals.Count];

        for(int i = 0; i < World.levelOrbs.Count; i++)
            savedOrbs[i] = World.levelOrbs[i].cooldown;
        for(int i = 0; i < World.levelPads.Count; i++)
            savedPads[i] = World.levelPads[i].cooldown;
        for(int i = 0; i < World.levelPortals.Count; i++)
            savedPortals[i] = World.levelPortals[i].activated;

        savedOrbsField.SetValue(self, savedOrbs);
        savedPadsField.SetValue(self, savedPads);
        savedPortalsField.SetValue(self, savedPortals);
    }
}
