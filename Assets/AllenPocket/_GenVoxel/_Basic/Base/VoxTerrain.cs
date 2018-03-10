using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenVoxelTools
{
    // The Container of _16x256x16Chunks
    public sealed class VoxTerrain
    {
        public int MaxChunkSize
        {
            get { return maxChunkSize; }
        }

        private int maxChunkSize;
        private _16x256x16VoxChunk[] chunkData;

        public VoxTerrain(int maxChunkSize)
        {
            this.maxChunkSize = maxChunkSize;

            chunkData = new _16x256x16VoxChunk[maxChunkSize];
        }

        public void SetChunk(int index , _16x256x16VoxChunk chunk)
        {
            if (index < 0 || index >= MaxChunkSize) return;

            chunkData[index] = chunk;
        }

        public _16x256x16VoxChunk GetChunk(int index)
        {
            if (index < 0 || index >= MaxChunkSize) return null;

            return chunkData[index];
        }

        public int GetIndexByUniqueID(int uniqueID)
        {
            for(int index = 0; index < maxChunkSize; index++)
            {
                if (chunkData[index] != null && chunkData[index].UniqueID == uniqueID)
                    return index;
            }

            return -1;
        }
    }
}