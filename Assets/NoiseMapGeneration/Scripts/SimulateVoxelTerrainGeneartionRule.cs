using System.Collections.Generic;
using UnityEngine;

public class SimulateVoxelTerrainGeneartionRule : MonoBehaviour
{

    [Header("Base")]
    [Range(32, 2048)]
    public int size = 128;
    [Range(1, 64)]
    public int zoom = 16;
    [Range(0, 10)]
    public float pow = 1;
    [Range(0, 64)]
    public float scale = 1;
    public Vector2 offset;
    public Color rockColor;
    public Color soilColor;

    [Header("Rock")]
    [Range(0, 256)]
    public int baseRockElevation;
    [Range(0, 256)]
    public int decorRockElevation01;
    [Range(0, 256)]
    public int decorRockElevation02;

    [Header("Soil")]
    [Range(0, 1)]
    public float soilMinelevation;
    [Range(0, 1)]
    public float soilMaxElevation;
    [Range(0, 1)]
    public float soilDepth;

    private Mesh displayMesh;
    private TNode[,] nodes;

    private MeshFilter meshfilter;

    void Start()
    {
        GenTerrain();
    }

    void OnValidate()
    {
        if(soilMinelevation > soilMaxElevation)
        {
            soilMinelevation = soilMaxElevation;
        }
        if(soilDepth+soilMinelevation > soilMaxElevation)
        {
            soilDepth = soilMaxElevation - soilMinelevation;
        }

        GenTerrain();
    }

    private void GenTerrain()
    {
        GenElevations();
        GenSoils();
        GenDisplayMesh();
        ApplyMesh2Filter();
    }

    private void GenElevations()
    {
        int sum = baseRockElevation + decorRockElevation01 + decorRockElevation02;

        nodes = new TNode[size, size];

        for(int lx = 0; lx < size; lx++)
        {
            for(int lz = 0;lz < size; lz++)
            {
                float gx = GetGlobalPosition(lx, lz).x;
                float gz = GetGlobalPosition(lx, lz).y;

                nodes[lx, lz].elevation = baseRockElevation * Mathf.PerlinNoise(scale * gx, scale * gz)
                    + decorRockElevation01 * Mathf.PerlinNoise(2 * scale * gx, 2 * scale * gz)
                    + decorRockElevation02 * Mathf.PerlinNoise(4 * scale * gx, 4 * scale * gz);

                float proportion = ((float)(nodes[lx, lz].elevation)) / sum;
                nodes[lx, lz].elevation = Mathf.Pow(proportion, pow) * sum;

                nodes[lx, lz].ttype = TType.rock;
            }
        }
    }
    private void GenSoils()
    {
        int sum = baseRockElevation + decorRockElevation01 + decorRockElevation02;

        for(int lx = 0; lx < size; lx++)
        {
            for(int lz = 0; lz < size; lz++)
            {
                float elevation = nodes[lx, lz].elevation;
                if(elevation < soilMaxElevation * sum && elevation > soilMinelevation * sum)
                {
                    nodes[lx, lz].elevation = soilMaxElevation * sum;
                    nodes[lx, lz].ttype = TType.soil;
                }
            }
        }
    }
    private void GenDisplayMesh()
    {
        if(displayMesh == null)
        {
            displayMesh = new Mesh();
        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        for(int cx = 0;cx < size - 1; cx++)
        {
            for(int cz = 0;cz < size - 1; cz++)
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

                Color color = (nodes[cx, cz].ttype == TType.rock ? rockColor : soilColor);
                colors.Add(color);

                color = (nodes[cx, cz + 1].ttype == TType.rock ? rockColor : soilColor);
                colors.Add(color);

                color = (nodes[cx + 1, cz + 1].ttype == TType.rock ? rockColor : soilColor);
                colors.Add(color);

                color = (nodes[cx + 1, cz].ttype == TType.rock ? rockColor : soilColor);
                colors.Add(color);
            }
        }

        displayMesh.vertices = vertices.ToArray();
        displayMesh.triangles = triangles.ToArray();
        displayMesh.colors = colors.ToArray();

        displayMesh.RecalculateNormals();
    }
    private void ApplyMesh2Filter()
    {
        if(meshfilter == null)
        {
            meshfilter = GetComponent<MeshFilter>();
        }

        meshfilter.mesh = displayMesh;
    }

    private Vector2 GetGlobalPosition(int lx,int lz)
    {
        float gx = (float)((float)lx / zoom) + offset.x;
        float gz = (float)((float)lz / zoom) + offset.y;

        return new Vector2(gx, gz);
    }
}

public struct TNode
{
    public float elevation;
    public TType ttype;
}
public enum TType
{
    rock,
    soil
}
