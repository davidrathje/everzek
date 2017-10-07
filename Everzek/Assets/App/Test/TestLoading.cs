using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLoading : MonoBehaviour {

    public Transform cube;
    //change these params in the inspector on textscene
    public string GamePath = @"D:\games\everquest\EQ Vanilla UF\";
    public string ArchiveName = "befallen.s3d";
    public bool LoadFirstTexture = false;
    public string TextureToLoad = "cloud.bmp";

    //Loads the first bmp found in the pack, by default uses befallen.s3d
    void Start () {

        MeshRenderer mesh;
        if (cube == null)
        {
            Debug.Log("Cube not set");
            return;
        }

        mesh = cube.GetComponent<MeshRenderer>();
        if (mesh == null)
        {
            Debug.Log("No mesh set");
            return;
        }
        
        Debug.Log("Loading s3d");
        var archive = EQArchive.Load(GamePath);
        Debug.Log("File count: " + archive.Files.Count);

        string extension = "";
        
        foreach (var file in archive.Files)
        {
            if (!LoadFirstTexture)
            {
                if (file.Key.ToLower() == TextureToLoad.ToLower())
                {
                    byte[] contents = file.Value.GetContents();
                    Texture2D tex = new Texture2D(256, 256, TextureFormat.DXT1, false);
                    tex.LoadRawTextureData(contents);
                    tex.Apply();
                    mesh.material.mainTexture = tex;
                    Debug.Log("Loaded " + TextureToLoad);
                    return;
                }
                continue;
            }

            extension = System.IO.Path.GetExtension(file.Key);
            if (extension == ".bmp")
            {
                Debug.Log("Bmp file: "+ file.Key);
                byte[] contents = file.Value.GetContents();
                //todo: identify dimensions etc so i don't hardcore to 256x256
                //Debug.Log("Size: " + contents.Length);
                //Debug.Log("Size Header: "+ (contents[0] + contents[1] + contents[2] + contents[3]));
                //Debug.Log("Width: " + contents[4]+", Height: "+contents[5]);
                Texture2D tex = new Texture2D(256, 256, TextureFormat.DXT1, false); 
                tex.LoadRawTextureData(contents);
                tex.Apply();
                mesh.material.mainTexture = tex;
                return;
            }
            continue;            
            //Debug.Log(file.Key);            
        }
        Debug.Log("Done");
	}
}
