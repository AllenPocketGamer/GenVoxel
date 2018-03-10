using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASimpleHeightMapGeneration : MonoBehaviour {

    [Header("Base")]
    [Range(32, 2048)]
    public int size = 256;
    public float zoom = 64;
    public Vector2 offset = new Vector2(0, 0);
    [Range(0, 1)]
    public float seaLevel = 0.1f;
    [Range(1024,4096)]
    public int worldLength = 4096;
    public Color seaColor;
    public Color iceColor;
    public Texture2D biomes;

    [Header("Elevation")]
    [Range(0, 256)]
    public int baseelevation = 128;
    [Range(0, 256)]
    public int decorelevation01 = 64;
    [Range(0, 256)]
    public int decorelevation02 = 32;
    [Range(0, 10)]
    public float pow = 1;
    [Range(0,4)]
    public float frequency = 1;
    [Range(0, 1)]
    public float elevationCoolDown = 0.4f;

    [Header("Moisture")]
    [Range(0, 64)]
    public float moistureFrequency = 4;
    [Range(-50, 50)]
    public float poleTempture = -20;
    [Range(-50,50)]
    public float equatorTempture = 50;

    private Mesh displayMesh;
    private float[,] elevations;

	// Use this for initialization
	void Start () {
        GenTerrain();
	}
    void OnValidate()
    {
        GenTerrain();
    }

    private void GenTerrain()
    {
        GenelevationMap();
        GenDisplayMesh();
        ApplyMesh2Filter();
    }

    private void GenelevationMap()
    {
        elevations = new float[size, size];
        
        for(int x = 0; x < size; x++)
        {
            for(int z = 0; z < size; z++)
            {
                float gx = GetGobalPosition(x,z).x;
                float gz = GetGobalPosition(x, z).y;

                elevations[x,z] = baseelevation * Mathf.PerlinNoise(gx * frequency, gz * frequency)
                    + decorelevation01 * Mathf.PerlinNoise(2 * gx * frequency, 2 * gz * frequency)
                    + decorelevation02 * Mathf.PerlinNoise(4 * gx * frequency, 4 * gz * frequency);

                float temp = elevations[x, z] / (baseelevation + decorelevation01 + decorelevation02);
                temp = Mathf.Pow(temp, pow);
                elevations[x, z] = temp * (baseelevation + decorelevation01 + decorelevation02);
            }
        }
    }
    private float GetMoisture(int lx,int lz)
    {
        float gx = GetGobalPosition(lx, lz).x;
        float gz = GetGobalPosition(lx, lz).y;

        return Mathf.PerlinNoise(moistureFrequency * gx, moistureFrequency * gz);
    }
    private float GetTempture(int lx, int lz)
    {
        float gz = GetGobalPosition(lx, lz).y;

        float lerp = (equatorTempture - poleTempture) / (worldLength / 2);
        float distanceFromEquator = Mathf.Abs(gz - (worldLength / 2));
        float latitudeTempture = equatorTempture - distanceFromEquator * lerp;

        float coolDownTempture = elevationCoolDown * elevations[lx, lz];

        float finalTempture = latitudeTempture - coolDownTempture;

        if (finalTempture < -50) return 0;

        return (finalTempture + 50) / 100;
    }
    // 通过 温度 和 湿度 对地形进行采样
    private Color SampleTerrain(int lx, int lz)
    {
        float seaElevation = (baseelevation + decorelevation01 + decorelevation02) * seaLevel;

        float tempture = GetTempture(lx, lz);
        float moisture = GetMoisture(lx, lz);

        if(elevations[lx,lz] < seaElevation)
        {
            if (tempture < 0.5f) return iceColor;
            else return seaColor;
        }

        int y = (int)(tempture * 512 + 0.5f);
        int x = (int)(moisture * 512 + 0.5f);
        return biomes.GetPixel(x, y);
    }

    private void GenDisplayMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        displayMesh = new Mesh();

        for (int x = 0; x < size - 1; x++)
        {
            for (int z = 0; z < size - 1; z++)
            {
                int stIndex = vertices.Count;

                // add vertices
                vertices.Add(new Vector3(x, elevations[x, z], z));
                vertices.Add(new Vector3(x, elevations[x, z + 1], z + 1));
                vertices.Add(new Vector3(x + 1, elevations[x + 1, z + 1], z + 1));
                vertices.Add(new Vector3(x + 1, elevations[x + 1, z], z));

                // add triangles
                triangles.Add(stIndex);
                triangles.Add(stIndex + 1);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 2);
                triangles.Add(stIndex + 3);
                triangles.Add(stIndex);

                // add colors
                colors.Add(SampleTerrain(x,z));
                colors.Add(SampleTerrain(x,z+1));
                colors.Add(SampleTerrain(x+1,z+1));
                colors.Add(SampleTerrain(x+1,z));
            }
        }
        displayMesh.vertices = vertices.ToArray();
        displayMesh.triangles = triangles.ToArray();
        displayMesh.colors = colors.ToArray();

        displayMesh.RecalculateNormals();
    }

    private void ApplyMesh2Filter()
    {
        GetComponent<MeshFilter>().mesh = displayMesh;
    }

    private Vector2 GetGobalPosition(int lx,int lz)
    {
        float gx = offset.x + (float)(lx / zoom);
        float gz = offset.y + (float)(lz / zoom);

        return new Vector2(gx, gz);
    }

}

