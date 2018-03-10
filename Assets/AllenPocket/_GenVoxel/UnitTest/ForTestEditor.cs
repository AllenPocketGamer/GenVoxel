using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[ExecuteInEditMode]
public class ForTestEditor : MonoBehaviour {

    public int test = 10;

    void Awake()
    {
        Debug.Log("Awake");
    }

    void OnEnable()
    {
        Debug.Log("OnEnable");
    }

    void OnDisable()
    {
        Debug.Log("OnDisable");
    }

    void Reset()
    {
        Debug.Log("Reset");
    }

    void OnApplicationQuit()
    {
        Debug.Log("Quit");    
    }

    void OnValidate()
    {
        Debug.Log("Validate");    
    }

    void OnDestroy()
    {
        Debug.Log("OnDestroy");

        FileStream file = File.Open("C:\\Users\\AllenPocket\\Desktop\\Log.txt", FileMode.Open, FileAccess.Write);

        BinaryWriter bw = new BinaryWriter(file);

        bw.Write("OnDestroy");

        bw.Close();

        file.Close();
    }
}

