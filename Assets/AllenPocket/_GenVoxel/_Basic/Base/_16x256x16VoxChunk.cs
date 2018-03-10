using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenVoxelTools
{
    public sealed class _16x256x16VoxChunk
    {
        public static readonly int Width = 16;
        public static readonly int Height = 256;
        public static readonly int Length = 16;
        public static readonly int Count = 16 * 256 * 16;
        public static readonly byte EmptyType = 0x00;

        public int[] Position
        {
            get { return new int[] { x, z }; }
            set
            {
                x = value[0];
                z = value[1];
            }
        }
        // UniqueID is an unique id to mark the chunk.
        // UniqueID Always has Big relationship with chunk position. 
        public int UniqueID
        {
            get { return uniqueID; }
        }
        public bool IsRefresh
        {
            get { return isRefresh; }
            set
            {
                isRefresh = value;
            }
        }
        public byte[] VoxData
        {
            get { return voxData.Clone() as byte[]; }
            set
            {
                if(value.Length == Count)
                {
                    voxData = value.Clone() as byte[];
                }
            }
        }

        private byte[] voxData;
        private int x;
        private int z;
        private int uniqueID;
        private bool isRefresh;

        public static int[] DefaultDecoderFromUniqueID2StPosition(int uniqueID)
        {
            int z = (uniqueID >> 16) & 0x0000ffff;
            int x = uniqueID & 0x0000ffff;

            return new int[] { x, z };
        }
        public static int DefaultDecoderFromStPosition2UniqueID(int[] position)
        {
            int uniqueID = position[0] | (position[1] << 16);

            return uniqueID;
        }

        public _16x256x16VoxChunk(int uniqueID,byte[] voxData)
        {
            this.uniqueID = uniqueID;

            this.Position = DefaultDecoderFromUniqueID2StPosition(uniqueID);

            this.voxData = voxData;

            isRefresh = false;
        }
        public _16x256x16VoxChunk(int[] position,byte[] voxData)
        {
            this.Position = position;
            this.uniqueID = DefaultDecoderFromStPosition2UniqueID(position);

            this.VoxData = voxData;

            isRefresh = false;
        }

        public void SetVoxel(int x,int y,int z, byte value)
        {
            int index = GetIndex(x, y, z);
            if (index == -1) return;

            voxData[index] = value;
            
            isRefresh = true;
        }

        public byte GetVoxel(int x,int y ,int z)
        {
            int index = GetIndex(x, y, z);
            if (index == -1)
            {
                return 0x00;
            }

            return voxData[index];
        }

        private int GetIndex(int x,int y,int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Length)
                return -1;

            return x + z * Width + y * Width * Length;
        }
    }
}