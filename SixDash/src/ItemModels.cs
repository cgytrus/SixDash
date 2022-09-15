using System.Collections.Generic;

using UnityEngine;

namespace SixDash;

public static class ItemModels {
    public record struct Face(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3Int direction);
    public record struct Model(Face[] faces);

    private static Face GeneratePlane(Vector3 offset, Vector3Int direction, float width, float height, int detail) {
        Vector3 dir = new Vector3(direction.z, direction.y, direction.x).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);
        int verticesPerRow = detail + 2;
        int trianglesPerRow = detail + 1;
        Vector3[] vertices = new Vector3[verticesPerRow * verticesPerRow];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[trianglesPerRow * trianglesPerRow * 6];
        int i = 0;
        for(int y = 0; y < verticesPerRow; y++) {
            float yNorm = y / (verticesPerRow - 1f);
            for(int x = 0; x < verticesPerRow; x++) {
                float xNorm = x / (verticesPerRow - 1f);
                vertices[i] = rot * new Vector3((xNorm - 0.5f) * width, (yNorm - 0.5f) * height) + offset;
                uvs[i++] = new Vector2(xNorm, yNorm);
            }
        }
        i = 0;
        for(int y = 0; y < trianglesPerRow; y++)
            for(int x = 0; x < trianglesPerRow; x++) {
                triangles[i++] = x + 1 + y * verticesPerRow;
                triangles[i++] = x + 1 + (y + 1) * verticesPerRow;
                triangles[i++] = x + (y + 1) * verticesPerRow;
                triangles[i++] = x + 1 + y * verticesPerRow;
                triangles[i++] = x + (y + 1) * verticesPerRow;
                triangles[i++] = x + y * verticesPerRow;
            }
        return new Face(vertices, triangles, uvs, direction);
    }

    private static Face GenerateTriangle(Vector3 offset, Vector3Int direction, float rotation, float width,
        float height, int detail) {
        Vector3 dir = new Vector3(direction.z, direction.y, direction.x).normalized;
        Quaternion rot = Quaternion.Euler(new Vector3(rotation, 0f)) * Quaternion.LookRotation(dir);
        int verticesPerCol = detail + 2;
        int trianglesPerCol = detail + 1;
        Vector3[] vertices = new Vector3[verticesPerCol * verticesPerCol];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[trianglesPerCol * trianglesPerCol * 6];
        Dictionary<(int, int), int> idk = new();
        int i = 0;
        for(int y = 0; y < verticesPerCol; y++) {
            float yNorm = y / (verticesPerCol - 1f);
            int verticesPerRow = GetVerticesPerRow(y);
            for(int x = 0; x < verticesPerRow; x++) {
                float xNorm = verticesPerRow == 1 ? 0f : x / (verticesPerRow - 1f);
                idk.Add((x, y), i);
                vertices[i] = rot * new Vector3((xNorm - 0.5f) * width, (yNorm - 0.5f) * height) + offset;
                uvs[i++] = new Vector2(xNorm, yNorm);
            }
        }
        i = 0;
        for(int y = 0; y < trianglesPerCol; y++)
            for(int x = 0; x < GetTrianglesPerRow(y); x++) {
                triangles[i++] = idk[(x, y)];
                triangles[i++] = idk[(x + 1, y)];
                triangles[i++] = idk[(x, y + 1)];
                if(x == trianglesPerCol - y - 1)
                    continue;
                triangles[i++] = idk[(x, y + 1)];
                triangles[i++] = idk[(x + 1, y)];
                triangles[i++] = idk[(x + 1, y + 1)];
            }
        return new Face(vertices, triangles, uvs, direction);
        int GetVerticesPerRow(int y) => verticesPerCol - y;
        int GetTrianglesPerRow(int y) => trianglesPerCol - y;
    }

    private static Model[] GenerateFullBlockModels(int count) {
        Model[] models = new Model[count];
        for(int i = 0; i < models.Length; i++)
            models[i] = new Model(new Face[] {
                GeneratePlane(Vector3.forward * 0.5f, Vector3Int.right, 1f, 1f, i),
                GeneratePlane(Vector3.back * 0.5f, Vector3Int.left, 1f, 1f, i),
                GeneratePlane(Vector3.up * 0.5f, Vector3Int.up, 1f, 1f, i),
                GeneratePlane(Vector3.down * 0.5f, Vector3Int.down, 1f, 1f, i),
                GeneratePlane(Vector3.right * 0.5f, Vector3Int.forward, 1f, 1f, i),
                GeneratePlane(Vector3.left * 0.5f, Vector3Int.back, 1f, 1f, i)
            });
        return models;
    }
    private static readonly Model[] fullBlockModels = GenerateFullBlockModels(24);

    private static Model[] GenerateHalfBlockModels(int count) {
        Model[] models = new Model[count];
        for(int i = 0; i < models.Length; i++)
            models[i] = new Model(new Face[] {
                GeneratePlane(Vector3.forward * 0.5f + Vector3.up * 0.25f, Vector3Int.right, 1f, 0.5f, i),
                GeneratePlane(Vector3.back * 0.5f + Vector3.up * 0.25f, Vector3Int.left, 1f, 0.5f, i),
                GeneratePlane(Vector3.up * 0.5f, Vector3Int.up, 1f, 1f, i),
                GeneratePlane(Vector3.zero, Vector3Int.down, 1f, 1f, i),
                GeneratePlane(Vector3.right * 0.5f + Vector3.up * 0.25f, Vector3Int.forward, 1f, 0.5f, i),
                GeneratePlane(Vector3.left * 0.5f + Vector3.up * 0.25f, Vector3Int.back, 1f, 0.5f, i)
            });
        return models;
    }
    private static readonly Model[] halfBlockModels = GenerateHalfBlockModels(24);

    private static Model[] GenerateSlopeModels(int count) {
        Model[] models = new Model[count];
        for(int i = 0; i < models.Length; i++)
            models[i] = new Model(new Face[] {
                GeneratePlane(Vector3.forward * 0.5f, Vector3Int.right, 1f, 1f, i),
                GeneratePlane(Vector3.zero, Vector3Int.up + Vector3Int.left, 1f, Mathf.Sqrt(2f), i),
                GeneratePlane(Vector3.down * 0.5f, Vector3Int.down, 1f, 1f, i),
                GenerateTriangle(Vector3.right * 0.5f, Vector3Int.forward, 0f, 1f, 1f, i),
                GenerateTriangle(Vector3.left * 0.5f, Vector3Int.back, -90f, 1f, 1f, i)
            });
        return models;
    }
    private static readonly Model[] slopeModels = GenerateSlopeModels(24);

    public static readonly IReadOnlyDictionary<string, Model[]> models = new Dictionary<string, Model[]> {
        { "3dash:blocks/normal", fullBlockModels },
        { "3dash:blocks/grid", fullBlockModels },
        { "3dash:blocks/half", halfBlockModels },
        { "3dash:blocks/cosmicGrid", fullBlockModels },
        // TODO: fix outline on slopes
        //{ "3dash:hazards/slope", slopeModels }
    };

    public static readonly ISet<string> transparent = new HashSet<string>
        { "3dash:blocks/grid", "3dash:blocks/cosmicGrid" };
}
