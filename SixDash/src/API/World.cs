using System;
using System.Collections.Generic;

using HarmonyLib;

using JetBrains.Annotations;

using Mono.Cecil.Cil;

using MonoMod.Cil;
using MonoMod.RuntimeDetour;

using PathCreation;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace SixDash.API;

[PublicAPI]
public static class World {
    private const int ChunkWidth = 128;

    public static event Action? levelLoading;
    public static event Action? levelLoaded;
    public static event Action<string, Vector3Int, int, GameObject?>? itemLoaded;
    public static event Action? levelUpdate;

    public const float ColorChangeSpeed = 1.8f;
    public record struct ColorChangerData(float startDistance, float endDistance, Color color);

    public static float levelTime => DistanceToTime(PathFollower.distanceTravelled);

    public static IReadOnlyDictionary<int, Chunk> levelChunks => chunks;
    public static IReadOnlyList<ItemScript> levelItems => items;
    public static IReadOnlyList<OrbScript> levelOrbs => orbs;
    public static IReadOnlyList<PadScript> levelPads => pads;
    public static IReadOnlyList<PortalScript> levelPortals => portals;
    public static IReadOnlyList<ColorChangerData> levelColorChangers => colorChangers;

    private static readonly Dictionary<int, Chunk> chunks = new();
    private static readonly List<ItemScript> items = new();
    private static readonly List<OrbScript> orbs = new();
    private static readonly List<PadScript> pads = new();
    private static readonly List<PortalScript> portals = new();
    private static readonly List<ColorChangerData> colorChangers = new();

    private static readonly Dictionary<string, Material> wallMaterials = new();
    private static readonly HashSet<Material> uniqueMaterials = new();
    private static readonly Dictionary<string, Outline> outlines = new();
    private static bool _preprocessedItems;

    private static Shader? _shader;
    private static readonly int renderMinProp = Shader.PropertyToID("_RenderMin");
    private static readonly int renderMaxProp = Shader.PropertyToID("_RenderMax");

    internal static void LoadAssets(AssetBundle bundle) {
        _shader = bundle.LoadAsset<Shader>("Assets/6Dash/StandardLevelFade.shader");
    }

    internal static void Patch() {
        On.WorldGenerator.Awake += (orig, self) => {
            orig(self);
            LoadLevel(self.myarrays, self.itemPrefabs, true, self.pathFollower.pathCreator.path,
                self.pathFollower.endOfPathInstruction);
        };
        On.WorldGeneratorEditor.Awake += (orig, self) => {
            orig(self);
            LoadLevel(self.myarrays, self.editor.items, false, self.pathFollower.pathCreator.path,
                self.pathFollower.endOfPathInstruction);
        };

        On.WorldGenerator.Update += (_, self) => UpdateLevel(self.renderDistance);
        On.WorldGeneratorEditor.Update += (_, self) => UpdateLevel(self.renderDistance);

        new Hook(AccessTools.Method(typeof(WorldGenerator), "FixedUpdate"),
            (Action<WorldGenerator> _, WorldGenerator self) => FixedUpdateLevel(self.renderDistance)).Apply();
        new Hook(AccessTools.Method(typeof(WorldGeneratorEditor), "FixedUpdate"),
                (Action<WorldGeneratorEditor> _, WorldGeneratorEditor self) => FixedUpdateLevel(self.renderDistance))
            .Apply();

        Player.playerSpawn += _ => ResetRenderIndex();

        IL.FlatEditor.Update += il => {
            ILCursor cursor = new(il);
            while(cursor.TryGotoNext(code => code.MatchStfld<ItemScript>(nameof(ItemScript.dead)))) {
                cursor.Index -= 2;
                cursor.RemoveRange(3);
                cursor.Emit(OpCodes.Call,
                    AccessTools.Method(typeof(Object), nameof(Object.Destroy), new[] { typeof(Object) }));
            }
        };
    }

    public static float TimeToDistance(float time) {
        float currentTime = 0f;
        float speed = Player.initialSpeed;
        float distance = 0f;
        foreach(PortalScript portal in levelPortals) {
            if(portal.portalFunction != PortalScript.PortalFunction.Speed)
                continue;
            float portalTime = currentTime + (portal.myDist - distance) / speed;
            if(portalTime > time)
                continue;
            currentTime = portalTime;
            distance = portal.myDist;
            speed = portal.speed;
        }
        return distance + (time - currentTime) * speed;
    }

    public static float DistanceToTime(float distance) {
        float currentDistance = 0f;
        float speed = Player.initialSpeed;
        float time = 0f;
        foreach(PortalScript portal in levelPortals) {
            if(portal.portalFunction != PortalScript.PortalFunction.Speed || portal.myDist > distance)
                continue;
            time += (portal.myDist - currentDistance) / speed;
            currentDistance = portal.myDist;
            speed = portal.speed;
        }
        return time + (distance - currentDistance) / speed;
    }

    private static void ResetRenderIndex() {
        foreach(Chunk chunk in chunks.Values)
            chunk.ResetRenderIndex();
    }

    private static void LoadLevel(int[,] levelData, IReadOnlyList<GameObject> itemPrefabs, bool official,
        VertexPath path, EndOfPathInstruction endOfPath) {
        levelLoading?.Invoke();

        chunks.Clear();
        items.Clear();
        orbs.Clear();
        pads.Clear();
        portals.Clear();
        colorChangers.Clear();

        if(!_preprocessedItems)
            PreprocessItems(itemPrefabs, official);

        for(int i = 0; i < levelData.GetLength(0); i++) {
            int id = levelData[i, 0];
            Vector3Int position = new(levelData[i, 1], levelData[i, 2], levelData[i, 3]);
            int rotation = levelData[i, 4];
            GameObject prefab = itemPrefabs[id];
            LoadItem(prefab, path, endOfPath, id, position, rotation, official);
        }

        for(int i = 0; i < colorChangers.Count; i++) {
            ColorChangerData data = colorChangers[i];
            data.endDistance = TimeToDistance(DistanceToTime(data.startDistance) + 1f / ColorChangeSpeed);
            colorChangers[i] = data;
        }

        foreach(Chunk chunk in chunks.Values) {
            chunk.UpdateItemAnimationPositions();
            chunk.UpdateMeshes();
        }

        // why is this not in level dataaaaaaaa
        ItemScript.deletionDistance = SceneManager.GetActiveScene().name switch {
            "Shadow Siege" => 75f,
            "Cosmic Growl" => 70f,
            _ => 50f
        };

        levelLoaded?.Invoke();
    }

    private static void PreprocessItems(IReadOnlyList<GameObject> itemPrefabs, bool official) {
        _preprocessedItems = true;
        for(int i = 0; i < itemPrefabs.Count; i++) {
            GameObject prefab = itemPrefabs[i];
            string id = ItemIds.Get(official, i);
            PreprocessItem(prefab, id);
        }
    }

    private static void PreprocessItem(GameObject prefab, string id) {
        foreach(MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>()) {
            if(!renderer)
                continue;
            foreach(Material material in renderer.materials)
                PreprocessMaterial(id, material);
        }
        if(outlines.ContainsKey(id))
            return;
        Outline outline = prefab.GetComponentInChildren<Outline>();
        if(outline)
            outlines.Add(id, outline);
    }

    private static void PreprocessMaterial(string id, Material material) {
        if(!uniqueMaterials.Add(material))
            return;
        material.enableInstancing = true;
        if(!ItemModels.models.ContainsKey(id))
            return;
        // setting a shader resets render queue hhhhhh
        int renderQueue = material.renderQueue;
        material = new Material(material) {
            shader = _shader,
            renderQueue = renderQueue
        };
        wallMaterials.Add(id, material);
    }

    private static void LoadItem(GameObject prefab, VertexPath path, EndOfPathInstruction endOfPath, int id,
        Vector3Int position, int rotation, bool official) {
        string idStr = ItemIds.Get(official, id);
        GameObject obj = SetItem(path, endOfPath, idStr, position, rotation, prefab);
        items.Add(obj.GetComponent<ItemScript>());

        // TODO: get rid of GetComponentInChildren

        OrbScript orb = obj.GetComponentInChildren<OrbScript>();
        if(orb)
            orbs.Add(orb);

        PadScript pad = obj.GetComponentInChildren<PadScript>();
        if(pad) {
            pad.myAngle = rotation;
            pads.Add(pad);
        }

        PortalScript portal = obj.GetComponentInChildren<PortalScript>();
        if(portal) {
            portal.myDist = position.x;
            portals.Add(portal);
        }

        if(official) {
            ColorChanger colorChanger = obj.GetComponent<ColorChanger>();
            if(colorChanger) {
                colorChanger.myColor = Color.HSVToRGB(Mathf.Abs(rotation / 360f), 0.7f, position.y / 18f);
                colorChanger.myDistance = position.x;
                colorChangers.Add(new ColorChangerData(position.x, -1f, colorChanger.myColor));
            }
        }
        else {
            ColorChangerEditor colorChanger = obj.GetComponent<ColorChangerEditor>();
            if(colorChanger) {
                colorChanger.myColor = Color.HSVToRGB(rotation / 360f % 1f, 0.7f,
                    Mathf.Min(1f, (position.y + 10f) / 10f) * 0.9f);
                colorChanger.myDistance = position.x;
                colorChanger.myY = position.y;
                colorChangers.Add(new ColorChangerData(position.x, -1f, colorChanger.myColor));
            }
        }

        itemLoaded?.Invoke(idStr, position, rotation, obj);
    }

    private static GameObject SetItem(VertexPath path, EndOfPathInstruction endOfPath, string id, Vector3Int position,
        int rotation, GameObject prefab) {
        int chunkPos = position.x / ChunkWidth;
        if(chunks.TryGetValue(chunkPos, out Chunk? chunk))
            return chunk.SetItem(id, position, rotation, prefab);
        chunk = new Chunk(path, endOfPath, wallMaterials, outlines);
        chunks.Add(chunkPos, chunk);
        return chunk.SetItem(id, position, rotation, prefab);
    }

    private static void UpdateLevel(float renderDistance) {
        float renderMin = PathFollower.distanceTravelled - ItemScript.deletionDistance;
        float renderMax = PathFollower.distanceTravelled + renderDistance;
        foreach(Material material in wallMaterials.Values) {
            material.SetFloat(renderMinProp, renderMin);
            material.SetFloat(renderMaxProp, renderMax);
        }
        levelUpdate?.Invoke();
    }

    private static void FixedUpdateLevel(float renderDistance) {
        float renderMin = PathFollower.distanceTravelled - ItemScript.deletionDistance;
        float renderMax = PathFollower.distanceTravelled + renderDistance;
        foreach(Chunk chunk in chunks.Values)
            chunk.FixedUpdate(renderMin, renderMax);
    }
}
