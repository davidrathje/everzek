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
    public string ArchiveName = "airplane.s3d";
    public bool LoadFirstMesh = true;
    public string MeshToLoad = "airplane.wld";

    //Loads the first bmp found in the pack, by default uses befallen.s3d
    void Start()
    {
        Debug.Log("Loading s3d");
        var archive = EQArchive.Load(GamePath + ArchiveName);
        Debug.Log("File count: " + archive.Files.Count);

        string extension = "";
        
        //Vector3[] newVertices;
        //Vector2[] newUV;
        //int[] newTriangles;

        Mesh mesh = new Mesh();
        cube.GetComponent<MeshFilter>().mesh = CreateMesh(1, 2);
        //mesh.vertices = newVertices;
        //mesh.uv = newUV;
        //mesh.triangles = newTriangles;
        

        foreach (var file in archive.Files)
        {        
//            Debug.Log(file.Key);
            if (!LoadFirstMesh)
            {
                if (file.Key.ToLower() == MeshToLoad.ToLower())
                {
                    byte[] contents = file.Value.GetContents();
                    return;
                }
                continue;
            }

            extension = System.IO.Path.GetExtension(file.Key);
            if (extension == ".wld")
            {
                Debug.Log("Loading " + file.Key);
                byte[] contents = file.Value.GetContents();

                try
                {
                    Wld wld = new Wld(contents);
                } catch (Exception e)
                {
                    Debug.LogError("Failed to load World: " + e.Message);
                    return;
                }
                
                return;
            }
            continue;    
        }
        Debug.Log("Done");
    }




    //just a test function on creating mesh in unity
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
