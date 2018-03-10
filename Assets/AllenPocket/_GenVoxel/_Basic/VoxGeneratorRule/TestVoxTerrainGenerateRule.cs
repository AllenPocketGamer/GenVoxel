using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenVoxelTools
{
    [CreateAssetMenu(menuName ="Rule/Test",fileName = "TestRule")]
    public class TestVoxTerrainGenerateRule : VoxTerrainGenerateRule
    {
        public int groundBaseHeight = 96;
        public int groundDiffHeight = 64;
        public float groundScale = 0.02f;

        public int snowBaseHeight = 80;
        public int snowDiffHeight = 70;
        public float snowScale = 0.02f;
        public int snowDepth = 8;

        public int glassBaseHeight = 100;
        public int glassDiffHeight = 16;
        public float glassScale = 0.02f;
        public int glassDepth = 4;

        private int[] offset;

        public override _16x256x16VoxChunk GenerateVoxChunk(int uniqueID)
        {
            byte[] voxData = new byte[_16x256x16VoxChunk.Count];
            _16x256x16VoxChunk result = new _16x256x16VoxChunk(uniqueID, voxData);

            offset = _16x256x16VoxChunk.DefaultDecoderFromUniqueID2StPosition(uniqueID);

            for(int lx = 0;lx < _16x256x16VoxChunk.Width; lx++)
            {
                for(int lz = 0;lz < _16x256x16VoxChunk.Length; lz++)
                {
                    int groundHeight = GenBaseGround(lx, lz);
                    int SnowLayerHeight = GenSnowLayer(lx, lz);
                    int GlassLayerHeight = GenGlassLayer(lx, lz);

                    for(int h = 0; h < groundHeight; h++)
                    {
                        result.SetVoxel(lx, h, lz, (byte)VoxelType.Ground);
                    }

                    int snowSubGround = SnowLayerHeight - groundHeight;
                    if(snowSubGround > 0 && snowSubGround < snowDepth)
                    {
                        for(int h = SnowLayerHeight; h >= groundHeight; h--)
                        {
                            result.SetVoxel(lx, h, lz, (byte)VoxelType.Snow);
                        }
                    }

                    int glassSubGround = GlassLayerHeight - groundHeight;
                    if(glassSubGround > 0 && glassSubGround < glassDepth)
                    {
                        for(int h = glassSubGround; h >= groundHeight; h--)
                        {
                            result.SetVoxel(lx, h, lz, (byte)VoxelType.Glass); 
                        }
                    }
                }
            }

            return result;
        }
        public override Mesh GetDebugMesh()
        {
            return null;
        }

        private int GenBaseGround(int lx,int lz)
        {
            float sampleX = groundScale * (offset[0] + (float)lx / _16x256x16VoxChunk.Width);
            float sampleZ = groundScale * (offset[1] + (float)lz / _16x256x16VoxChunk.Length);

            return (int)(groundBaseHeight + groundDiffHeight * Mathf.PerlinNoise(sampleX, sampleZ));
        }

        private int GenSnowLayer(int lx, int lz)
        {
            float sampleX = snowScale * (offset[0] + (float)lx / _16x256x16VoxChunk.Width);
            float sampleZ = snowScale * (offset[1] + (float)lz / _16x256x16VoxChunk.Length);

            return (int)(snowBaseHeight + snowDiffHeight * Mathf.PerlinNoise(sampleX, sampleZ));
        }
        private int GenGlassLayer(int lx, int lz)
        {
            float sampleX = glassScale * (offset[0] + (float)lx / _16x256x16VoxChunk.Width);
            float sampleZ = glassScale * (offset[1] + (float)lz / _16x256x16VoxChunk.Length);

            return (int)(glassBaseHeight + glassDiffHeight * Mathf.PerlinNoise(sampleX, sampleZ));
        }

        enum VoxelType
        {
            Ground = 1,
            Snow = 2,
            Glass = 3
        }
    }
}