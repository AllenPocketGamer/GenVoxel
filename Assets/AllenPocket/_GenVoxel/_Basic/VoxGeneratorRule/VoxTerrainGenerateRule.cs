using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenVoxelTools
{
    public abstract class VoxTerrainGenerateRule : ScriptableObject
    {
        public abstract _16x256x16VoxChunk GenerateVoxChunk(int uniqueID);
        public abstract Mesh GetDebugMesh();
    }
}