using UnityEditor;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Import settings for Codex art under Resources/Art (art guide: 128 px = 1 m,
    /// pivot at the canvas center). Applied automatically, so a new delivery only
    /// needs to be copied into the right folder.
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
