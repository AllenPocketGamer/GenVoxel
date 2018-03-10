using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyVoxel : MonoBehaviour {

    public GenVoxelTools.GenVoxelManager manager;
    public GameObject wireCube;
    public LayerMask targetLayer;
    public float maxDistance;
    public float cubeSize = 1.0f;
    public Color cubeColor = Color.green;

    public byte addVoxelType = 0x01;

    private Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
    private Vector3 cubeStart;

    private RaycastHit hit;
    private bool isHit;

    void Start()
    {
        UpdateWireCube();
    }

    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, targetLayer))
        {
            cubeStart = manager.SetSelectedVoxel(hit);

            wireCube.transform.position = cubeStart + offset;

            isHit = true;
        }
        else
        {
            wireCube.transform.position = new Vector3(0, 0, 0);

            isHit = false;
        }

        if (Input.GetMouseButtonDown(1) && isHit)
        {
            manager.DeleteVoxel();
        }
        else if (Input.GetMouseButtonDown(0) && isHit)
        {
            manager.AddVoxel(addVoxelType, hit);
        }

        Debug.DrawLine(transform.position, transform.position + transform.forward * maxDistance, Color.red);
    }

    void Onvalidate()
    {
        UpdateWireCube();
    }

    private void UpdateWireCube()
    {
        if (wireCube != null)
        {
            wireCube.GetComponent<MeshRenderer>().material.color = cubeColor;
            wireCube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
        }
    }
}

