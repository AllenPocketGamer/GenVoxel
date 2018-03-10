using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GenVoxelTools;

public class AddARadomChunk : MonoBehaviour {

	// Use this for initialization
	void Start () {
        WorldTree wt = new WorldTree("C:\\Users\\AllenPocket\\Desktop\\TestWTFile.wt");

        int px = 32767;
        int pz = 32767;
        int width = 16;
        int length = 16;

        byte[] fill = new byte[16 * 256 * 16];
        for(int i = 0; i < fill.Length; i++)
        {
            fill[i] = 0x01;
        }

        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < length; j++)
            {
                int[] position = new int[]
                {
                    (int)(px + i),
                    (int)(pz + j)
                };
                int uniqueID = _16x256x16VoxChunk.DefaultDecoderFromStPosition2UniqueID(position);
                wt.WriteChunk(new _16x256x16VoxChunk(uniqueID, fill));
            }
        }

        wt.Close();
	}
	

}
