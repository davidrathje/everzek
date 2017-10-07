using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class TestMesh : MonoBehaviour
{

    public Transform cube;
    //change these params in the inspector on textscene
    public string GamePath = @"D:\games\everquest\EQ Vanilla UF\";
    public string ArchiveName = "snd1.pfs";
    public bool LoadFirstSound = false;
    public string SoundToLoad = "boatbell.wav";

    //Loads the first bmp found in the pack, by default uses befallen.s3d
    void Start()
    {
        Debug.Log("Loading s3d");
        var archive = EQArchive.Load(GamePath + ArchiveName);
        Debug.Log("File count: " + archive.Files.Count);

        string extension = "";

        AudioSource asource = cube.GetComponent<AudioSource>();
        //Vector3[] newVertices;
        //Vector2[] newUV;
        //int[] newTriangles;

        Mesh mesh = new Mesh();
        cube.GetComponent<MeshFilter>().mesh = CreateMesh(1, 2);        
        //mesh.vertices = newVertices;
        //mesh.uv = newUV;
        //mesh.triangles = newTriangles;

        return;

        foreach (var file in archive.Files)
        {


            if (!LoadFirstSound)
            {
                if (file.Key.ToLower() == SoundToLoad.ToLower())
                {
                    byte[] contents = file.Value.GetContents();
                    return;
                }
                continue;
            }

            extension = System.IO.Path.GetExtension(file.Key);
            if (extension == ".wav")
            {
                byte[] contents = file.Value.GetContents();
                return;
            }
            continue;
            //Debug.Log(file.Key);            
        }
        Debug.Log("Done");
    }
    Mesh CreateMesh(float width, float height)
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        m.vertices = new Vector3[] {
         new Vector3(-width, -height, 0.01f),
         new Vector3(width, -height, 0.01f),
         new Vector3(width, height, 0.01f),
         new Vector3(-width, height, 0.01f)
     };
        m.uv = new Vector2[] {
         new Vector2 (0, 0),
         new Vector2 (0, 1),
         new Vector2(1, 1),
         new Vector2 (1, 0)
     };
        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();

        return m;
    }
}
