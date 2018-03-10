using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise : MonoBehaviour {

    public int width;
    public int length;
    public int seed;

    public int height = 10;
    public float scale_x = 0.1f;
    public float scale_z = 0.1f;

    private Mesh terrainMesh;
    private Vector3[,] terrain;

	// Use this for initialization
	void Awake () {
        terrain = new Vector3[width+1, length+1];
	}

    private void Start()
    {
        GenTerrain(seed);
        Mesh();
    }


    void OnValidate()
    {
        if (terrain == null) return;

        terrain = new Vector3[width + 1, length + 1];

        GenTerrain(seed);
        Mesh();
    }

    private void GenTerrain(int seed)
    {
        for(int x = 0; x < width + 1; x++)
        {
            for(int z = 0; z < length + 1; z++)
            {
                terrain[x, z] = new Vector3(x, Mathf.PerlinNoise(seed + x * scale_x, seed + z * scale_z) * height * 0.2f + height * 0.8f, z);
            }
        }
    }

    private void Mesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int cube_x = 0; cube_x < width; cube_x++)
        {
            for (int cube_z = 0; cube_z < length; cube_z++)
            {
                int stIndex = vertices.Count;

                // add vertices
                vertices.Add(terrain[cube_x, cube_z]);
                vertices.Add(terrain[cube_x, cube_z + 1]);
                vertices.Add(terrain[cube_x + 1, cube_z + 1]);
                vertices.Add(terrain[cube_x + 1, cube_z]);

                // add triangles
                triangles.Add(stIndex);
                triangles.Add(stIndex + 1);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 3);
                triangles.Add(stIndex);
            }
        }

        terrainMesh = new Mesh();
        terrainMesh.vertices = vertices.ToArray();
        terrainMesh.triangles = triangles.ToArray();

        terrainMesh.RecalculateNormals();
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawMesh(terrainMesh);
    }
}
