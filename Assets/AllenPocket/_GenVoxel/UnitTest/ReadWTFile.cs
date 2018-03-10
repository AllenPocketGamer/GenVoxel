using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GenVoxelTools;

public class ReadWTFile : MonoBehaviour {

	// Use this for initialization
	void Start () {
        WorldTree wt = new WorldTree("C:\\Users\\AllenPocket\\Desktop\\TestWTFile.wt");

        Debug.Log("Chunk Count:" + wt.Count);

        wt.Close();
	}
	
}
