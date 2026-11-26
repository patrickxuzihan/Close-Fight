using UnityEditor;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Import settings for Codex art under Resources/Art (art guide: 128 px = 1 m,
    /// pivot at the canvas center). Terrain tiles (64 px = 1 m) repeat; UI images
    /// are plain textures. Applied automatically, so a new delivery only needs to
    /// be copied into the right folder.
    /// </summary>
    sealed class ArtImportSettings : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/CloseFight/Resources/Art/";
        public const float PixelsPerMeter = 128f;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(ArtRoot + "Terrain/") || assetPath.StartsWith(ArtRoot + "UI/") || assetPath.StartsWith(ArtRoot + "Roster/"))
            {
                // Ground and roof tiles repeat across the map; UI and roster images are drawn as plain textures.
                bool tile = assetPath.StartsWith(ArtRoot + "Terrain/");
                importer.textureType = TextureImporterType.Default;
                // UI and roster pictures are drawn with the GuiPicture shader (UiArt.DrawPicture),
                // which decodes sRGB itself: plain IMGUI drawing shows them too bright in this
                // linear-colour project. Terrain tiles go through the normal sRGB path.
                importer.sRGBTexture = tile;
                importer.alphaIsTransparency = !tile;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = tile ? 4 : 1;
                importer.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerMeter;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true; // tanks are drawn far smaller than 128 px/m when zoomed out
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
