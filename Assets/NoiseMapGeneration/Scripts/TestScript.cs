using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

    public GenVoxelTools.StandardVoxTerrainGenerateRule rule;

    private float timer = 0;
    private float interv = 0.1f;

    void Update()
    {
        if(timer > interv)
        {
            if (rule != null)
            {
                rule.GenNodes();
                GetComponent<MeshFilter>().mesh = rule.GetDebugMesh();
            }

            timer -= interv;
        }

        timer += Time.deltaTime;
    }
}
