using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour {

    
    public Texture2D cursor;
    public Canvas canvas;

    public string GamePath = @"D:\games\everquest\EQ Vanilla UF\";
    public string ArchiveName = "befallen.s3d";
    public bool LoadFirstTexture = false;
    public string TextureToLoad = "cloud.bmp";


    // Use this for initialization
    void Start () {
        
        Cursor.SetCursor(cursor,Vector2.zero, CursorMode.ForceSoftware);        

        var obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_01.tga", new Vector3(-256, 128));
        obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_02.tga", new Vector3(-2, 128));
        obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_03.tga", new Vector3(252, 128));
        obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_04.tga", new Vector3(-256, -127));
        obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_05.tga", new Vector3(-2, -127));
        obj = LoadTGA(GamePath + @"uifiles\default\EQLS_WndBorder_06.tga", new Vector3(252, -127));
        Debug.Log(obj);
        var bg1 = new GameObject("bg1", typeof(RectTransform));
        bg1.AddComponent<CanvasRenderer>();
        var text = bg1.AddComponent<Text>();
        text.text = "SERVER SELECT";
        text.fontSize = 14;
        text.color = Color.yellow;
        text.rectTransform.position = new Vector3(-21.2f, 218.9f);
        bg1.transform.SetParent(canvas.transform);
        //todo: set font
    }
	
    GameObject LoadTGA(string path, Vector3 position)
    {
        var bg1 = new GameObject("bg1", typeof(RectTransform));
        var rect = bg1.GetComponent<RectTransform>();
        
        bg1.transform.SetParent(canvas.transform);
        bg1.AddComponent<CanvasRenderer>();
        var image = bg1.AddComponent<Image>();

        Texture2D tex = TGALoader.LoadTGA(path);
        bg1.GetComponent<RectTransform>().sizeDelta = new Vector2(tex.width, tex.height);
        Sprite spr = Sprite.Create(tex, Rect.MinMaxRect(0, 0, tex.width, tex.height), Vector2.zero);
        image.sprite = spr;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);

        rect.localPosition = position;
        return bg1;
    }
}
