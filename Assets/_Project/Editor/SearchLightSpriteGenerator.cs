using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 탐색 광원(<see cref="SearchLight"/>)에 꽂을 스프라이트 두 장을 만든다.
/// 그림 프로그램 없이도 되게 코드로 그린다 — 원형 그라데이션(BFS)과 가로로 긴 부드러운 띠(DFS·LS).
///
/// 메뉴: Tools > Search Light > Create Sprites
/// </summary>
public static class SearchLightSpriteGenerator
{
    const string Folder = "Assets/Art/Light";
    const int PixelsPerUnit = 128;

    [MenuItem("Tools/Search Light/Create Sprites")]
    static void Create()
    {
        Directory.CreateDirectory(Folder);

        Save("SearchLight_Circle", 256, 256, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            return Soft(1f - r);
        });

        Save("SearchLight_Line", 256, 64, (u, v) =>
        {
            // 길이 방향은 끝 30% 만 부드럽게, 굵기 방향은 가장자리로 갈수록 옅게
            float along = Soft((1f - Mathf.Abs(u)) / 0.3f);
            float across = Soft(1f - Mathf.Abs(v));
            return along * across;
        });

        AssetDatabase.Refresh();
        Debug.Log($"[SearchLight] {Folder} 에 스프라이트 2장을 만들었다.");
    }

    /// <summary>0~1 을 S자로 부드럽게 만든다. 가장자리가 뚝 끊기지 않게.</summary>
    static float Soft(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>u, v 는 텍스처 중심이 0 이고 가장자리가 ±1.</summary>
    static void Save(string fileName, int width, int height, System.Func<float, float, float> shape)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width * 2f - 1f;
                float v = (y + 0.5f) / height * 2f - 1f;

                float g = shape(u, v);
                texture.SetPixel(x, y, new Color(g, g, g, g));
            }
        }

        string path = $"{Folder}/{fileName}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
