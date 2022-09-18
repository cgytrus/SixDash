using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using PathCreation;

using SixDash.API;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace SixDash;

[PublicAPI]
public class Chunk {
    public const float OutAnimSpeed = 1.8f;
    public const float InAnimSpeed = 7.7f;

    public const float OutAnimTime = 1f / OutAnimSpeed;
    public const float InAnimTime = 1f / InAnimSpeed;

    [PublicAPI]
    public class ItemInfo {
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

        public string id { get; }
        public Vector3Int position { get; }
        public int rotation { get; }
        public Vector3 worldPosition { get; }
        public Quaternion worldRotation { get; }

        public float time { get; private set; } = -1f;
        public float outAnimationEnd { get; private set; } = -1f;
        public float inAnimationEnd { get; private set; } = -1f;

        public ItemModels.Model? model { get; }
        public Quaternion pathRotation { get; }
        private readonly Quaternion _dirRot;

        private readonly VertexPath _path;
        private readonly EndOfPathInstruction _endOfPath;

        public void CacheAnimationValues() {
            time = World.DistanceToTime(position.x);
            outAnimationEnd = World.TimeToDistance(time + OutAnimTime);
            inAnimationEnd = World.TimeToDistance(time + InAnimTime);
        }

        public Vector3 OffsetToPathSpace(Vector3 pos) {
            Vector3 rotatedVertex = pathRotation * pos;
            Vector3 offset = new(rotatedVertex.z, rotatedVertex.y, rotatedVertex.x);
            return offset;
        }

        public Vector3 VertexToPathSpace(Vector3 vertex) => position + OffsetToPathSpace(vertex);

        public Vector3 VertexToWorldSpace(Vector3 vertex) {
            Vector3 pathPosition = position + OffsetToPathSpace(vertex);
            Vector3 worldPos = pathPosition.x < 0f || pathPosition.x > _path.length ?
                worldPosition + worldRotation * vertex : PathSpaceToWorldSpace(pathPosition).Item1;
            return worldPos;
        }

        public Vector3Int DirectionToWorldSpaceInt(Vector3Int direction) {
            Vector3 rotDir = DirectionToWorldSpace(direction);
            return new Vector3Int((int)rotDir.x, (int)rotDir.y, (int)rotDir.z);
        }

        public Vector3 DirectionToWorldSpace(Vector3Int direction) => _dirRot * direction;

        public (Vector3, Quaternion) PathSpaceToWorldSpace(Vector3 position) {
            Quaternion pathRotation = Quaternion.Euler(_path.GetRotationAtDistance(position.x, _endOfPath).eulerAngles +
                new Vector3(0f, 0f, 90f));
            Vector3 pathPosition = _path.GetPointAtDistance(position.x, _endOfPath) +
                pathRotation * new Vector3(position.z, position.y, 0f);
            return (pathPosition, pathRotation);
        }

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

    public bool active { get; private set; } = true;
    public IReadOnlyDictionary<Vector3Int, ItemInfo> items => _items;
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

    public void UpdateItemAnimationPositions() {
        _maxRenderX = float.NegativeInfinity;
        _minRenderX = float.PositiveInfinity;
        foreach(ItemInfo item in _items.Values) {
            item.CacheAnimationValues();
            _minRenderX = Mathf.Min(_minRenderX, item.position.x);
            _maxRenderX = Mathf.Max(_maxRenderX, item.inAnimationEnd);
        }
    }

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
                //vertices.Add(item.worldPosition + item.worldRotation * vertex);
                vertices.Add(item.VertexToWorldSpace(vertex));
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
