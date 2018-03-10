using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenVoxelTools {
    [CreateAssetMenu(menuName = "Rule/Standard",fileName = "StandardRule")]
    public class StandardVoxTerrainGenerateRule : VoxTerrainGenerateRule {
        [Header("Base")]
        private readonly int resoultionSize = 16;
        [Range(0.001f,4)]
        public float sampleSize = 64;
        [Range(512,4096)]
        public float worldLength = 1024;
        public Vector2 offset;

        [Header("Sea")]
        [Range(0, 1)]
        public float seaLevel = 0.1f;

        [Header("Elevation")]
        [Range(0, 128)]
        public int baseElevation = 128;
        [Range(0, 64)]
        public int decorElevation01 = 64;
        [Range(0, 32)]
        public int decorElevation02 = 32;
        [Range(0, 10)]
        public float pow = 1;

        [Header("Tempture")]
        [Range(0, 1)]
        public float elevationCoolDown = 0.4f;
        [Range(-50, 50)]
        public float poleTempture = -20f;
        [Range(-50, 50)]
        public float equatorTempture = 50f;

        [Header("Moisture")]
        [Range(0, 64)]
        public float moistureWaveLength = 4;

        [Header("Debug Color")]
        public Color rockColor;
        public Color waterColor;
        public Color iceColor;
        public Color snowColor;
        public Color frozonColor;
        public Color grassColor;
        public Color forestColor;
        public Color jungleColor;
        public Color desertColor;

        private int sum;
        private TNode[,] nodes;

        public override _16x256x16VoxChunk GenerateVoxChunk(int uniqueID)
        {
            int[] position = _16x256x16VoxChunk.DefaultDecoderFromUniqueID2StPosition(uniqueID);
            offset = new Vector2(position[0], position[1]);
            GenNodes();

            byte[] data = new byte[_16x256x16VoxChunk.Count];
            _16x256x16VoxChunk chunk = new _16x256x16VoxChunk(uniqueID, data);

            for(int x = 0;x < _16x256x16VoxChunk.Width; x++)
            {
                for(int z = 0; z < _16x256x16VoxChunk.Length; z++)
                {
                    TNode node = nodes[x, z];
                    int height = (int)node.elevation;
                    byte type = (byte)(node.ttype);

                    for(int y = 0; y < height; y++)
                    {
                        chunk.SetVoxel(x, y, z, type);
                    }
                }
            }

            return chunk; 
        }
        public override Mesh GetDebugMesh()
        {
            if (nodes == null)
            {
                GenNodes();
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();

            Mesh debugMesh = new Mesh();

            for (int x = 0; x < resoultionSize - 1; x++)
            {
                for (int z = 0; z < resoultionSize - 1; z++)
                {
                    int stIndex = vertices.Count;

                    // add vertices
                    vertices.Add(new Vector3(x, nodes[x, z].elevation, z));
                    vertices.Add(new Vector3(x, nodes[x, z + 1].elevation, z + 1));
                    vertices.Add(new Vector3(x + 1, nodes[x + 1, z + 1].elevation, z + 1));
                    vertices.Add(new Vector3(x + 1, nodes[x + 1, z].elevation, z));

                    // add triangles
                    triangles.Add(stIndex);
                    triangles.Add(stIndex + 1);
                    triangles.Add(stIndex + 2);
                    triangles.Add(stIndex + 2);
                    triangles.Add(stIndex + 3);
                    triangles.Add(stIndex);

                    // add colors
                    colors.Add(GetTTypeDebugColor(nodes[x, z].ttype));
                    colors.Add(GetTTypeDebugColor(nodes[x, z + 1].ttype));
                    colors.Add(GetTTypeDebugColor(nodes[x + 1, z + 1].ttype));
                    colors.Add(GetTTypeDebugColor(nodes[x + 1, z].ttype));
                }
            }
            debugMesh.vertices = vertices.ToArray();
            debugMesh.triangles = triangles.ToArray();
            debugMesh.colors = colors.ToArray();
            debugMesh.RecalculateNormals();

            return debugMesh;
        }

        public void GenNodes()
        {
            nodes = new TNode[resoultionSize, resoultionSize];
            sum = baseElevation + decorElevation01 + decorElevation02;

            GenElevation();
            GenBaseBlock();
            GenDecorateBlock();
        }
        private void GenElevation()
        {
            for(int lx = 0;lx < resoultionSize; lx++)
            {
                for(int lz = 0;lz < resoultionSize; lz++)
                {
                    float gx = GetGSamplePosition(lx, lz).x;
                    float gz = GetGSamplePosition(lx, lz).y;

                    float fx = gx / sampleSize;
                    float fz = gz / sampleSize;

                    nodes[lx, lz].elevation = baseElevation * Mathf.PerlinNoise(fx, fz)
                        + decorElevation01 * Mathf.PerlinNoise(2 * fx, 2 * fz)
                        + decorElevation02 * Mathf.PerlinNoise(4 * fx, 4 * fz);

                    float temp = nodes[lx, lz].elevation / sum;
                    nodes[lx, lz].elevation = Mathf.Pow(temp, pow) * sum;
                }
            }
        }
        private void GenBaseBlock()
        {
            float seaElevation = sum * seaLevel;

            for(int lx = 0;lx < resoultionSize; lx++)
            {
                for(int lz = 0;lz < resoultionSize; lz++)
                {
                    if(nodes[lx,lz].elevation < seaElevation)
                    {
                        nodes[lx, lz].ttype = TType.SEA;
                        // Speci
                        nodes[lx, lz].elevation = seaElevation;
                    }
                    else
                    {
                        nodes[lx, lz].ttype = TType.ROCK;
                    }
                }
            }
        }
        private void GenDecorateBlock()
        {
            for(int lx =0;lx < resoultionSize; lx++)
            {
                for(int lz = 0;lz < resoultionSize; lz++)
                {
                    float tempture = GetTempture(lx, lz);
                    float moisture = GetMoisture(lx, lz);

                    TType decorTType = GetDecorateTType(nodes[lx, lz].ttype, tempture, moisture);
                    nodes[lx, lz].ttype = decorTType;
                }
            }
        }

        private float GetMoisture(int lx,int lz)
        {
            float gx = GetGSamplePosition(lx, lz).x;
            float gz = GetGSamplePosition(lx, lz).y;

            return Mathf.PerlinNoise(gx / moistureWaveLength, gz / moistureWaveLength);
        }
        private float GetTempture(int lx,int lz)
        {
            float deltaTempture = ((float)(poleTempture - equatorTempture)) / (worldLength / 2);
            float gz = GetGSamplePosition(lx, lz).y;
            gz %= worldLength;
            float baseTempture = Mathf.Abs(gz - worldLength / 2) * deltaTempture + equatorTempture;

            float decorTempture = elevationCoolDown * nodes[lx, lz].elevation;

            float finalTempture = baseTempture - decorTempture;

            return (finalTempture + 50.0f) / 100;
        }
        private TType GetDecorateTType(TType baseType,float tempture,float moisture)
        {
            if (baseType == TType.SEA)
            {
                if (tempture < 0.5f) return TType.ICE;
                else return TType.WATER;
            }

            if (tempture >= 0 && tempture < 0.5f)
            {
                if (moisture >= 0 && moisture < 0.5f) return TType.FROZENSOIL;
                if (moisture >= 0.5f) return TType.SNOW;
            }
            if (tempture >= 0.5f && tempture < 0.75f)
            {
                if (moisture >= 0 && moisture < 0.25f) return TType.ROCK;
                if (moisture >= 0.25f && moisture < 0.50f) return TType.GRASS;
                if (moisture >= 0.50f && moisture < 1.00f) return TType.FOREST;
            }
            if (tempture >= 0.75f && tempture <= 1.00f)
            {
                if (moisture >= 0 && moisture < 0.25f) return TType.DESERT;
                if (moisture >= 0.25f && moisture < 0.5f) return TType.GRASS;
                if (moisture >= 0.50f && moisture < 0.75f) return TType.FOREST;
            }

            return TType.JUNGLE;
        }
        private Vector2 GetGSamplePosition(int lx,int lz)
        {
            float gx = offset.x + ((float)lx) / resoultionSize;
            float gz = offset.y + ((float)lz) / resoultionSize;

            return new Vector2(gx, gz);
        }

        private Color GetTTypeDebugColor(TType ttype)
        {
            if(ttype == TType.ROCK)
            {
                return rockColor;
            }
            if(ttype == TType.WATER)
            {
                return waterColor;
            }
            if(ttype == TType.ICE)
            {
                return iceColor;
            }
            if(ttype == TType.SNOW)
            {
                return snowColor;
            }
            if(ttype == TType.FROZENSOIL)
            {
                return frozonColor;
            }
            if(ttype == TType.GRASS)
            {
                return grassColor;
            }
            if(ttype == TType.FOREST)
            {
                return forestColor;
            }
            if(ttype == TType.JUNGLE)
            {
                return jungleColor;
            }
            if(ttype == TType.DESERT)
            {
                return desertColor;
            }
            return Color.black;
        }
    }

    enum TType
    {
        ROCK,
        SEA,

        WATER,
        ICE,
        SNOW,
        FROZENSOIL,
        GRASS,
        FOREST,
        JUNGLE,
        DESERT
    }

    struct TNode
    {
        public float elevation;
        public TType ttype;
    }
}
