using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RendererTexture
{
    private string path;
    private Texture2D t;
    private Color[] cols;
    public readonly int width;
    public readonly int height;
    private bool isSaveable;

    public RendererTexture(int width, int height, string savePath, string picName)
    {
        cols = new Color[width * height];
        this.width = width;
        this.height = height;
        path = Path.Combine(savePath, picName) + ".png";
        isSaveable = true;
    }

    public RendererTexture(Texture2D texture2D)
    {
        t = texture2D;
        cols = t.GetPixels();
        this.width = t.width;
        this.height = t.height;
        isSaveable = false;
    }

    public RendererTexture(string path)
    {
        t = Resources.Load<Texture2D>(path);
        cols = t.GetPixels();
        this.width = t.width;
        this.height = t.height;
        isSaveable = false;
    }

    public Color this[int x, int y]
    {
        get
        {
            if (x > width - 1) x = width - 1;
            if (x < 0) x = 0;
            if (y > height - 1) y = height - 1;
            if (y < 0) y = 0;
            return cols[y * width + x];
        }
        set
        {
            cols[y * width + x] = value;
        }
    }

    public Color this[float x, float y]
    {
        get
        {
            int m = (int)(x * width + 0.49f);
            int n = (int)(y * height + 0.49f);
            return this[m, n];
        }
    }

    public Color GetNearbyPoint(Vector2 uv, Vector2 offset) {
        int m = (int)(uv.x * width - offset.x + 0.49f);
        int n = (int)(uv.y * height - offset.y + 0.49f);
        return this[m,n];
    }

    public void Clear(float r = 0, float g = 0, float b = 0)
    {
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i] = new Color(r, g, b);
        }
    }

    public void Save()
    {
        if (!isSaveable) return;
        Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
        t.SetPixels(cols);
        byte[] bytes = t.EncodeToPNG();
        FileStream file = File.Open(path, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(t);
        t = null;
    }
}
