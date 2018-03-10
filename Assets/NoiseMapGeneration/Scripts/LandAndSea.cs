using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandAndSea : MonoBehaviour {

    [Header("Base")]
    public int size = 128;
    public float waveLength = 16;
    [Range(0, 1)]
    public float seaLevel = 0.1f;
    [Range(0, 64)]
    public float zoom = 16;

    public Vector2 offset;

    [Header("Land")]
    public int landElevation = 16;
    public Color landColor = Color.yellow;

    [Header("seaElevation")]
    public int seaElevation = 4;
    public Color seaColor = Color.blue;

    private TNode[,] nodes;
    private Mesh displayMesh;

    private MeshFilter meshFilter;

    void OnValidate()
    {
        GenTerrain();
    }

    private void GenTerrain()
    {
        GenElevation();
        AfterWork();
        GenDisplayMesh();
        ApplyDisplayMesh();
    }

    private void GenElevation()
    {
        if (nodes == null) nodes = new TNode[size, size];

        for(int lx = 0; lx < size; lx++)
        {
            for(int lz = 0;lz < size; lz++)
            {
                float gx = GetGPoistion(lx, lz).x;
                float gz = GetGPoistion(lx, lz).y;

                nodes[lx,lz].elevation = Mathf.PerlinNoise(gx / waveLength, gz / waveLength);
            }
        }
    }
    private void AfterWork()
    {
        for(int lx = 0; lx < size; lx++)
        {
            for(int lz = 0;lz < size; lz++)
            {
                if(nodes[lx,lz].elevation <= seaLevel)
                {
                    nodes[lx, lz].elevation = seaElevation;
                    nodes[lx, lz].ttype = TType.SEA;
                }
                else
                {
                    nodes[lx, lz].elevation = landElevation;
                    nodes[lx, lz].ttype = TType.LAND;
                }
            }
        }
    }
    private void GenDisplayMesh()
    {
        if (displayMesh == null)
        {
            displayMesh = new Mesh();
        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        for (int cx = 0; cx < size - 1; cx++)
        {
            for (int cz = 0; cz < size - 1; cz++)
            {
                int stIndex = vertices.Count;

                // add vertex
                vertices.Add(new Vector3(cx, nodes[cx, cz].elevation, cz));
                vertices.Add(new Vector3(cx, nodes[cx, cz + 1].elevation, cz + 1));
                vertices.Add(new Vector3(cx + 1, nodes[cx + 1, cz + 1].elevation, cz + 1));
                vertices.Add(new Vector3(cx + 1, nodes[cx + 1, cz].elevation, cz));

                // add triangles
                triangles.Add(stIndex);
                triangles.Add(stIndex + 1);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 3);
                triangles.Add(stIndex);

                Color color = (nodes[cx, cz].ttype == TType.LAND ? landColor : seaColor);
                colors.Add(color);

                color = (nodes[cx, cz + 1].ttype == TType.LAND ? landColor : seaColor);
                colors.Add(color);

                color = (nodes[cx + 1, cz + 1].ttype == TType.LAND ? landColor : seaColor);
                colors.Add(color);

                color = (nodes[cx + 1, cz].ttype == TType.LAND ? landColor : seaColor);
                colors.Add(color);
            }
        }

        displayMesh.vertices = vertices.ToArray();
        displayMesh.triangles = triangles.ToArray();
        displayMesh.colors = colors.ToArray();

        displayMesh.RecalculateNormals();
    }
    private void ApplyDisplayMesh()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

        meshFilter.mesh = displayMesh;
    }
    private Vector2 GetGPoistion(int lx,int lz)
    {
        float gx = ((float)lx) / zoom + offset.x;
        float gz = ((float)lz) / zoom + offset.y;

        return new Vector2(gx, gz);
    }

    enum TType
    {
        LAND,
        SEA
    }

    struct TNode
    {
        public float elevation;
        public TType ttype;
    }
}
