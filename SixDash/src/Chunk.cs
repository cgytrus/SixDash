using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using PathCreation;

using SixDash.API;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace SixDash;

/// <summary>
/// Represents a single chunk of items in a level.
/// </summary>
[PublicAPI]
public class Chunk {
    /// <summary>
    /// Speed of the item out animation.
    /// </summary>
    public const float OutAnimSpeed = 1.8f;
    /// <summary>
    /// Speed of the item in animation.
    /// </summary>
    public const float InAnimSpeed = 7.7f;

    /// <summary>
    /// Time of the item out animation.
    /// </summary>
    public const float OutAnimTime = 1f / OutAnimSpeed;
    /// <summary>
    /// Time of the item in animation.
    /// </summary>
    public const float InAnimTime = 1f / InAnimSpeed;

    /// <summary>
    /// Represents some information about an item.
    /// </summary>
    [PublicAPI]
    public class ItemInfo {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">Item ID.</param>
        /// <param name="position">Item position.</param>
        /// <param name="rotation">Item rotation.</param>
        /// <param name="path">The path the item is on.</param>
        /// <param name="endOfPath">End of path instruction.</param>
        public ItemInfo(string id, Vector3Int position, int rotation, VertexPath path, EndOfPathInstruction endOfPath) {
            this.id = id;
            this.position = position;
            this.rotation = rotation;

            _path = path;
            _endOfPath = endOfPath;

            (worldPosition, worldRotation) = PathSpaceToWorldSpace(position);

            Vector3 currPathRot = _path.GetRotationAtDistance(position.x, _endOfPath).eulerAngles;
            Vector3 prevPathRot = _path.GetRotationAtDistance(position.x - 1, _endOfPath).eulerAngles;
            float pathEulerDiff = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(prevPathRot.x, currPathRot.x)),
                Mathf.Abs(Mathf.DeltaAngle(prevPathRot.y, currPathRot.y)),
                Mathf.Abs(Mathf.DeltaAngle(prevPathRot.z, currPathRot.z)));

            pathRotation = Quaternion.Euler(-rotation, 0f, 0f);
            _dirRot = Quaternion.Euler(0, 0, rotation);

            worldRotation *= pathRotation;

            int modelIndex = (int)(pathEulerDiff / 15f);
            model = ItemModels.models.TryGetValue(id, out ItemModels.Model[] models) && modelIndex >= 0 &&
                modelIndex < models.Length ? models[modelIndex] : null;
        }

        /// <summary>
        /// Item ID.
        /// </summary>
        public string id { get; }
        /// <summary>
        /// Item position.
        /// </summary>
        public Vector3Int position { get; }
        /// <summary>
        /// Item rotation.
        /// </summary>
        public int rotation { get; }
        /// <summary>
        /// Item world position.
        /// </summary>
        public Vector3 worldPosition { get; }
        /// <summary>
        /// Item world rotation.
        /// </summary>
        public Quaternion worldRotation { get; }

        /// <summary>
        /// The time in seconds at which the player's distance and the item's distance will match.
        /// </summary>
        public float time { get; private set; } = -1f;
        /// <summary>
        /// The distance at which the out animation of the item ends, based on renderMin.
        /// </summary>
        public float outAnimationEnd { get; private set; } = -1f;
        /// <summary>
        /// The distance at which the in animation of the item ends, based on renderMax.
        /// </summary>
        public float inAnimationEnd { get; private set; } = -1f;

        /// <summary>
        /// Item model.
        /// </summary>
        public ItemModels.Model? model { get; }
        /// <summary>
        /// Item rotation in its path's space.
        /// </summary>
        public Quaternion pathRotation { get; }
        private readonly Quaternion _dirRot;

        private readonly VertexPath _path;
        private readonly EndOfPathInstruction _endOfPath;

        internal void CacheAnimationValues() {
            time = World.DistanceToTime(position.x);
            outAnimationEnd = World.TimeToDistance(time + OutAnimTime);
            inAnimationEnd = World.TimeToDistance(time + InAnimTime);
        }

        /// <summary>
        /// Rotates an offset from this item's position to its path's space.
        /// </summary>
        /// <param name="pos">The offset in item's local space.</param>
        /// <returns>The offset in the item's path space.</returns>
        public Vector3 OffsetToPathSpace(Vector3 pos) {
            Vector3 rotatedVertex = pathRotation * pos;
            Vector3 offset = new(rotatedVertex.z, rotatedVertex.y, rotatedVertex.x);
            return offset;
        }

        /// <summary>
        /// Converts vertex position to path space.
        /// </summary>
        /// <param name="vertex">The vertex position.</param>
        /// <returns>The vertex position in path space.</returns>
        public Vector3 VertexToPathSpace(Vector3 vertex) => position + OffsetToPathSpace(vertex);

        /// <summary>
        /// Converts vertex position to world space.
        /// </summary>
        /// <param name="vertex">The vertex position.</param>
        /// <returns>The vertex position in world space.</returns>
        public Vector3 VertexToWorldSpace(Vector3 vertex) {
            Vector3 pathPosition = position + OffsetToPathSpace(vertex);
            Vector3 worldPos = pathPosition.x < 0f || pathPosition.x > _path.length ?
                worldPosition + worldRotation * vertex : PathSpaceToWorldSpace(pathPosition).Item1;
            return worldPos;
        }

        /// <summary>
        /// Converts an integer direction from local to world space.
        /// </summary>
        /// <param name="direction">The direction in local space.</param>
        /// <returns>The direction in world space.</returns>
        public Vector3Int DirectionToWorldSpaceInt(Vector3Int direction) {
            Vector3 rotDir = DirectionToWorldSpace(direction);
            return new Vector3Int((int)rotDir.x, (int)rotDir.y, (int)rotDir.z);
        }

        /// <summary>
        /// Converts a direction from local to world space.
        /// </summary>
        /// <param name="direction">The direction in local space.</param>
        /// <returns>The direction in world space.</returns>
        public Vector3 DirectionToWorldSpace(Vector3Int direction) => _dirRot * direction;

        /// <summary>
        /// Converts a position in path space to world space.
        /// </summary>
        /// <param name="position">The position in path space.</param>
        /// <returns>The position in world space.</returns>
        public (Vector3, Quaternion) PathSpaceToWorldSpace(Vector3 position) {
            Quaternion pathRotation = Quaternion.Euler(_path.GetRotationAtDistance(position.x, _endOfPath).eulerAngles +
                new Vector3(0f, 0f, 90f));
            Vector3 pathPosition = _path.GetPointAtDistance(position.x, _endOfPath) +
                pathRotation * new Vector3(position.z, position.y, 0f);
            return (pathPosition, pathRotation);
        }

        /// <summary>
        /// Checks whether the <paramref name="face"/> of an <paramref name="item"/>
        /// can be culled against this item's model.
        /// </summary>
        /// <param name="item">The item the face is on.</param>
        /// <param name="face">The face to check.</param>
        /// <returns>Whether the face can be culled against this item.</returns>
        public bool CanCullFaceAgainstModel(ItemInfo item, ItemModels.Face face) {
            if(!model.HasValue)
                return false;
            ItemInfo self = this;
            return model.Value.faces.Any(selfFace =>
                self.CanCullFaceAgainstFace(item, face, selfFace));
        }

        private bool CanCullFaceAgainstFace(ItemInfo leftInfo, ItemModels.Face left, ItemModels.Face right) {
            ItemInfo rightInfo = this;
            IEnumerable<Vector3> leftVert = left.vertices.Select(leftInfo.VertexToPathSpace);
            IEnumerable<Vector3> rightVert = right.vertices.Select(rightInfo.VertexToPathSpace);
            Vector3 rightMin = new(rightVert.Min(v => v.x), rightVert.Min(v => v.y), rightVert.Min(v => v.z));
            Vector3 rightMax = new(rightVert.Max(v => v.x), rightVert.Max(v => v.y), rightVert.Max(v => v.z));
            bool allLeftInsideRight = leftVert.All(v => v.x >= rightMin.x - 0.1f && v.x <= rightMax.x + 0.1f &&
                v.y >= rightMin.y - 0.1f && v.y <= rightMax.y + 0.1f && v.z >= rightMin.z - 0.1f && v.z <= rightMax.z + 0.1f);
            return allLeftInsideRight &&
                leftInfo.DirectionToWorldSpace(left.direction) == -rightInfo.DirectionToWorldSpace(right.direction);
        }
    }

    /// <summary>
    /// Indicates whether the chunk is active.
    /// </summary>
    public bool active { get; private set; } = true;

    /// <summary>
    /// All items in this chunk.
    /// </summary>
    public IReadOnlyDictionary<Vector3Int, ItemInfo> items => _items;

    /// <summary>
    /// All items' <see cref="GameObject"/>s, <see cref="Transform"/>s and
    /// whether they should not stop rendering when going out of render distance in this chunk.
    /// </summary>
    public IReadOnlyList<(ItemInfo, GameObject, Transform, bool)> itemObjects => _itemObjects;

    private readonly Dictionary<Vector3Int, ItemInfo> _items = new();
    private readonly GameObject? _parentObj;
    private readonly Transform? _parent;
    private readonly List<(ItemInfo, GameObject, Transform, bool)> _itemObjects = new();
    private readonly Dictionary<string, Mesh> _meshes = new();

    private readonly VertexPath _path;
    private readonly EndOfPathInstruction _endOfPath;

    private float _maxRenderX;
    private float _minRenderX;
    private int _outRenderIndex;
    private int _inRenderIndex;

    internal Chunk(VertexPath path, EndOfPathInstruction endOfPath, IReadOnlyDictionary<string, Material> materials,
        IReadOnlyDictionary<string, Outline> outlines) {
        _path = path;
        _endOfPath = endOfPath;

        _parentObj = new GameObject("Chunk");
        _parent = _parentObj.transform;

        CreateMeshes(materials, outlines);
    }

    private void CreateMeshes(IReadOnlyDictionary<string, Material> materials,
        IReadOnlyDictionary<string, Outline> outlines) {
        foreach(string id in ItemModels.models.Keys) {
            GameObject obj = new($"{id} mesh");
            obj.transform.SetParent(_parent);

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;

            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = materials[id];
            meshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;

            if(outlines.TryGetValue(id, out Outline outlinePrefab)) {
                Outline outline = obj.AddComponent<Outline>();
                outline.OutlineColor = outlinePrefab.OutlineColor;
                outline.OutlineMode = outlinePrefab.OutlineMode;
                outline.OutlineWidth = outlinePrefab.OutlineWidth;
            }

            _meshes.Add(id, mesh);
        }
    }

    /// <summary>
    /// Sets an item in this chunk.
    /// </summary>
    /// <param name="id">Item ID.</param>
    /// <param name="position">Item position.</param>
    /// <param name="rotation">Item rotation.</param>
    /// <param name="prefab">Item prefab.</param>
    /// <returns>The created item's <see cref="GameObject"/></returns>
    /// <seealso cref="UpdateItemAnimationPositions"/>
    /// <seealso cref="UpdateMeshes"/>
    public GameObject SetItem(string id, Vector3Int position, int rotation, GameObject prefab) {
        ItemInfo info = new(id, position, rotation, _path, _endOfPath);
        _items[position] = info;
        GameObject? obj = prefab ? Object.Instantiate(prefab, info.worldPosition, info.worldRotation, _parent) : null;
        if(!obj)
            return obj!;
        _itemObjects.Add((info, obj!, obj!.transform, obj.CompareTag("Finish")));
        if(!ItemModels.models.ContainsKey(id))
            return obj;
        foreach(MeshFilter meshFilter in obj.GetComponentsInChildren<MeshFilter>())
            Object.Destroy(meshFilter);
        foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            Object.Destroy(renderer);
        foreach(Outline outline in obj.GetComponentsInChildren<Outline>())
            Object.Destroy(outline);
        return obj;
    }

    /// <summary>
    /// Updates the information about item animations. Should be called when you're done adding items to the chunk.
    /// </summary>
    /// <seealso cref="SetItem"/>
    public void UpdateItemAnimationPositions() {
        _maxRenderX = float.NegativeInfinity;
        _minRenderX = float.PositiveInfinity;
        foreach(ItemInfo item in _items.Values) {
            item.CacheAnimationValues();
            _minRenderX = Mathf.Min(_minRenderX, item.position.x);
            _maxRenderX = Mathf.Max(_maxRenderX, item.inAnimationEnd);
        }
    }

    /// <summary>
    /// Updates the meshes of blocks in this chunk. Should be called when you're done adding items to the chunk.
    /// </summary>
    /// <seealso cref="SetItem"/>
    public void UpdateMeshes() {
        foreach(KeyValuePair<string, Mesh> pair in _meshes)
            UpdateMesh(pair.Key, pair.Value);
    }

    private void UpdateMesh(string id, Mesh mesh) {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();
        List<Vector2> fakeUvs = new();
        List<Color> colors = new();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach(KeyValuePair<Vector3Int, ItemInfo> item in _items) {
            if(item.Value.id != id)
                continue;
            GenerateMesh(vertices, triangles, uvs, fakeUvs, colors, item.Key, item.Value);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, false);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(3, fakeUvs);
        mesh.SetColors(colors);

        mesh.Optimize();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
    }

    private void GenerateMesh(ICollection<Vector3> vertices, ICollection<int> triangles, List<Vector2> uvs,
        ICollection<Vector2> fakeUvs, ICollection<Color> colors, Vector3Int position, ItemInfo item) {
        if(!item.model.HasValue)
            return;
        // """"color""""
        Color color = new(item.worldPosition.x, item.worldPosition.y, item.worldPosition.z, position.x);
        Vector2 fakeUv = new(item.outAnimationEnd, item.inAnimationEnd);
        foreach(ItemModels.Face face in item.model.Value.faces) {
            if(CanCull(item, face))
                continue;

            int trianglesOffset = vertices.Count;

            foreach(Vector3 vertex in face.vertices) {
                // TODO: find a better way to fix this
                // offset vertically to fix noticeable z-fighting
                vertices.Add(item.VertexToWorldSpace(vertex) + new Vector3(0f, position.x % 100f / 10000f, 0f));
                colors.Add(color);
                fakeUvs.Add(fakeUv);
            }

            foreach(int triangle in face.triangles)
                triangles.Add(triangle + trianglesOffset);

            uvs.AddRange(face.uvs);
        }
    }

    private bool CanCull(ItemInfo item, ItemModels.Face face) {
        Vector3Int direction = item.DirectionToWorldSpaceInt(face.direction);
        Vector3Int checkPos = item.position + direction;
        if(!_items.TryGetValue(checkPos, out ItemInfo neighbor) ||
            !ItemModels.models.ContainsKey(neighbor.id) ||
            // only cull transparent blocks with the same blocks
            (ItemModels.transparent.Contains(item.id) || ItemModels.transparent.Contains(neighbor.id)) &&
            item.id != neighbor.id)
            return false;
        return neighbor.CanCullFaceAgainstModel(item, face);
    }

    internal void FixedUpdate(float renderMin, float renderMax) {
        if(renderMax < _minRenderX || renderMin > _maxRenderX) {
            SetInactive();
            return;
        }
        SetActive();
        ProcessRenderMin(renderMin);
        ProcessRenderMax(renderMax);
    }

    private void ProcessRenderMin(float renderMin) {
        for(int i = _outRenderIndex; i < _itemObjects.Count; i++) {
            (ItemInfo item, GameObject obj, Transform transform, bool dontStopRendering) = _itemObjects[i];
            if(!dontStopRendering && item.outAnimationEnd < renderMin) {
                _outRenderIndex = i + 1;
                obj.SetActive(false);
                continue;
            }
            if(renderMin < item.position.x)
                break;
            transform.localScale = ScaleOut(renderMin, item.outAnimationEnd, item.position.x);
        }
    }

    /// <summary>
    /// Applies the out animation to a scale.
    /// </summary>
    /// <param name="renderMin">Minimum render distance.</param>
    /// <param name="animationEnd">The end of the animation relative to <paramref name="renderMin"/>.</param>
    /// <param name="posX">The X position of the item.</param>
    /// <returns>The scale.</returns>
    public static Vector3 ScaleOut(float renderMin, float animationEnd, float posX) {
        float length = animationEnd - posX;
        float t = (renderMin - posX) / length;
        t = Util.ApplyEasing(t, Util.Easing.Exponential, Util.EasingMode.Out, 0f);
        return Vector3.Lerp(Vector3.one, Vector3.zero, t);
    }

    private void ProcessRenderMax(float renderMax) {
        for(int i = _inRenderIndex; i < _itemObjects.Count; i++) {
            (ItemInfo item, GameObject obj, Transform transform, bool _) = _itemObjects[i];
            if(renderMax <= item.position.x)
                break;
            if(item.inAnimationEnd < renderMax) {
                _inRenderIndex = i + 1;
                obj.SetActive(true);
                transform.localScale = Vector3.one;
                continue;
            }
            obj.SetActive(true);
            transform.localScale = ScaleIn(renderMax, item.inAnimationEnd, item.position.x);
        }
    }

    /// <summary>
    /// Applies the in animation to a scale.
    /// </summary>
    /// <param name="renderMax">Maximum render distance.</param>
    /// <param name="animationEnd">The end of the animation relative to <paramref name="renderMax"/>.</param>
    /// <param name="posX">The X position of the item.</param>
    /// <returns>The scale.</returns>
    public static Vector3 ScaleIn(float renderMax, float animationEnd, float posX) {
        float length = animationEnd - posX;
        float t = (renderMax - posX) / length;
        t = Util.ApplyEasing(t, Util.Easing.Exponential, Util.EasingMode.Out, 0f);
        return Vector3.Lerp(Vector3.zero, Vector3.one, t);
    }

    internal void ResetRenderIndex() {
        foreach((_, GameObject obj, Transform transform, _) in _itemObjects) {
            if(obj)
                obj.SetActive(false);
            if(transform)
                transform.localScale = Vector3.one;
        }
        _outRenderIndex = 0;
        _inRenderIndex = 0;
    }

    private void SetActive() {
        if(active)
            return;
        active = true;
        if(_parentObj)
            _parentObj!.SetActive(true);
    }

    private void SetInactive() {
        if(!active)
            return;
        active = false;
        if(_parentObj)
            _parentObj!.SetActive(false);
    }
}
