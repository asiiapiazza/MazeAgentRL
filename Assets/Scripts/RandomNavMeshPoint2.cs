using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Get total area of each triangle.
/// Find a random point within that total area.
/// Lookup which triangle that point relates to
/// Find a randiom point which point that triangle
/// This works for all mesh types, and gives fully distributed results.
/// Gist: https://gist.github.com/danieldownes/b1c9bab09cce013cc30a4198bfeda0aa
/// </summary>
public class RandomNavMeshPoint2 : MonoBehaviour
{
    [SerializeField] private MeshCollider meshCollider;

    private Vector3 randomPoint;
    public List<Vector3> debugPoints;

    float[] sizes;
    float[] cumulativeSizes;
    float total = 0;

    private void Start()
    {
        CalcAreas(meshCollider.sharedMesh);
    }

    private void Update()
    {
        for (int i = 0; i < 5000; i++)
            addDebugPoint();
    }

    void OnDrawGizmos()
    {
        foreach (Vector3 debugPoint in debugPoints)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(debugPoint, 0.01f);
        }
    }

    private void addDebugPoint()
    {
        randomPoint = GetRandomPointOnMesh(meshCollider.sharedMesh);
        randomPoint += meshCollider.transform.position;
        debugPoints.Add(randomPoint);
    }

    public void CalcAreas(Mesh mesh)
    {
        sizes = GetTriSizes(mesh.triangles, mesh.vertices);
        cumulativeSizes = new float[sizes.Length];
        total = 0;

        for (int i = 0; i < sizes.Length; i++)
        {
            total += sizes[i];
            cumulativeSizes[i] = total;
        }
    }

    public Vector3 GetRandomPointOnMesh(Mesh mesh)
    {
        float randomsample = Random.value * total;
        int triIndex = -1;

        for (int i = 0; i < sizes.Length; i++)
        {
            if (randomsample <= cumulativeSizes[i])
            {
                triIndex = i;
                break;
            }
        }

        if (triIndex == -1)
            Debug.LogError("triIndex should never be -1");

        Vector3 a = mesh.vertices[mesh.triangles[triIndex * 3]];
        Vector3 b = mesh.vertices[mesh.triangles[triIndex * 3 + 1]];
        Vector3 c = mesh.vertices[mesh.triangles[triIndex * 3 + 2]];

        // Generate random barycentric coordinates
        float r = Random.value;
        float s = Random.value;

        if (r + s >= 1)
        {
            r = 1 - r;
            s = 1 - s;
        }

        // Turn point back to a Vector3
        Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);
        return pointOnMesh;
    }

    public float[] GetTriSizes(int[] tris, Vector3[] verts)
    {
        int triCount = tris.Length / 3;
        float[] sizes = new float[triCount];
        for (int i = 0; i < triCount; i++)
        {
            sizes[i] = .5f * Vector3.Cross(
                verts[tris[i * 3 + 1]] - verts[tris[i * 3]],
                verts[tris[i * 3 + 2]] - verts[tris[i * 3]]).magnitude;
        }
        return sizes;
    }
}