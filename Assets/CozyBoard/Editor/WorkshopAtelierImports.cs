using UnityEditor;
using UnityEngine;
namespace CozyBoard.Editor {
    public sealed class WorkshopAtelierImports:AssetPostprocessor {
        void OnPreprocessTexture(){if(assetPath!="Assets/Resources/UI/AtelierIcons.png")return;Configure((TextureImporter)assetImporter);}
        static void Configure(TextureImporter importer){importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;}
        void OnPreprocessAudio(){if(assetPath!="Assets/Resources/Music/WarmFireplace.ogg")return;Configure((AudioImporter)assetImporter);}
        static void Configure(AudioImporter importer){var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;importer.loadInBackground=true;}
        public static void ConfigureAssets(){
            var texture=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/UI/AtelierIcons.png");Configure(texture);texture.SaveAndReimport();
            var audio=(AudioImporter)AssetImporter.GetAtPath("Assets/Resources/Music/WarmFireplace.ogg");Configure(audio);audio.SaveAndReimport();
        }
    }
}
