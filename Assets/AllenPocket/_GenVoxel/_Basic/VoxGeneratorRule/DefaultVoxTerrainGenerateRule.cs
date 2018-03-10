using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GenVoxelTools {
    [CreateAssetMenu(menuName = "Rule/Default", fileName = "DefaultRule")]
    public class DefaultVoxTerrainGenerateRule : VoxTerrainGenerateRule
    {
        // 山脉层
        // 平原层
        public int baseHeight = 96;
        public int gapHeight = 32;
        public float plainScale = 0.02f;

        public override _16x256x16VoxChunk GenerateVoxChunk(int uniqueID)
        {
            byte[] voxData = new byte[_16x256x16VoxChunk.Count];
            _16x256x16VoxChunk res = new _16x256x16VoxChunk(uniqueID, voxData);

            for(int x = 0; x < _16x256x16VoxChunk.Width; x++)
            {
                for(int z = 0; z < _16x256x16VoxChunk.Length; z++)
                {
                    int plainHeight = (int)(PlainSample(uniqueID, x, z) + 0.5f);

                    for(int y = 0; y < plainHeight; y++)
                    {
                        res.SetVoxel(x, y, z, 0x02);
                    }
                }
            }
            return res;
        }
        public override Mesh GetDebugMesh()
        {
            return null;
        }

        private float PlainSample(int uniqueID,int x,int z)
        {
            int[] position = _16x256x16VoxChunk.DefaultDecoderFromUniqueID2StPosition(uniqueID);

            float sample_x = plainScale * (position[0] + (float)x / _16x256x16VoxChunk.Width);
            float sample_z = plainScale * (position[1] + (float)z / _16x256x16VoxChunk.Length);

            return baseHeight + gapHeight * Mathf.PerlinNoise(sample_x, sample_z);
        }
    }
}
