// FlipNormals.cs
// Attach to a GameObject with a MeshFilter and MeshRenderer.
// Inverts normals and reverses triangle winding so faces render from the inside.
// Can run in editor (context menu) or at runtime (Awake).
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FlipNormals : MonoBehaviour
{
    // If true, operation happens on Awake (runtime). Otherwise use the context menu.
    public bool flipOnAwake = false;

    void Awake()
    {
        if (flipOnAwake)
            Flip();
    }

    // Call this via component context menu (right-click component) or from script
    [ContextMenu("Flip Mesh Normals")]
    public void Flip()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("FlipNormals: No mesh found.");
            return;
        }

        // Duplicate the mesh instance so we don't modify shared asset unexpectedly
        Mesh mesh = Instantiate(mf.sharedMesh);
        mesh.name = mf.sharedMesh.name + "_inverted";

        // Flip normals
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];
        mesh.normals = normals;

        // Reverse triangles for each submesh
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] tris = mesh.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                // swap 0 and 1 to reverse winding
                int tmp = tris[i];
                tris[i] = tris[i + 1];
                tris[i + 1] = tmp;
            }
            mesh.SetTriangles(tris, sub);
        }

        // Optional: recalc bounds/normals/tangents if needed
        mesh.RecalculateBounds();
        // mesh.RecalculateNormals(); // do NOT recalc normals since we intentionally negated them

        mf.sharedMesh = mesh;
        Debug.Log("FlipNormals: Mesh inverted on " + gameObject.name);
    }
}