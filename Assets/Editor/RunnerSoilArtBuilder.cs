using System.IO;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Editor
{
    /// <summary>Regenerates derived soil art only; original map textures are never changed.</summary>
    public static class RunnerSoilArtBuilder
    {
        public const string Directory = "Assets/Generated/TrailSoil";

        [MenuItem("Dino Rush/Regenerate Trail Soil Textures")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Directory)) AssetDatabase.CreateFolder("Assets/Generated", "TrailSoil");
            string[] ids = { "forest", "desert", "jungle", "rural" };
            Color32[] colors = { new Color32(19, 48, 49, 255), new Color32(120, 80, 57, 255),
                new Color32(19, 59, 54, 255), new Color32(72, 85, 56, 255) };
            for (int i = 0; i < ids.Length; i++)
            {
                Texture2D texture = RunnerSoilArt.CreateTexture(colors[i], ids[i]);
                string path = $"{Directory}/{ids[i]}_soil.png";
                try { File.WriteAllBytes(path, texture.EncodeToPNG()); }
                finally { Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = RunnerSoilArt.PixelsPerUnit;
                importer.spriteBorder = Vector4.zero;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.TopCenter;
                settings.spritePivot = new Vector2(0.5f, 1f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
