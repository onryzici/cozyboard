using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CozyBoard.Editor {
 public static class WorkshopExperienceBuild {
  [MenuItem("Cozy Board/Configure cozy experience")]
  public static void Configure(){
   var game=Object.FindFirstObjectByType<WorkshopGameMode>();
   var experience=game.GetComponent<WorkshopExperience>();if(!experience)experience=game.gameObject.AddComponent<WorkshopExperience>();
   var shop=game.GetComponent<WorkshopShop>();if(!shop)shop=game.gameObject.AddComponent<WorkshopShop>();game.Shop=shop;shop.Game=game;shop.CoinArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CozyBoard/Art/UI/TikCoin.png");EditorUtility.SetDirty(shop);
   var workshopTools=game.GetComponent<WorkshopTools>();if(!workshopTools)workshopTools=game.gameObject.AddComponent<WorkshopTools>();game.Tools=workshopTools;workshopTools.Game=game;EditorUtility.SetDirty(workshopTools);
   game.Experience=experience;experience.Game=game;experience.MenuArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CozyBoard/Art/UI/MenuAtelier.png");experience.MenuTitleFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/CozyBoard/Data/CozyFredoka.asset");
   var menuImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/CozyBoard/Art/UI/MenuAtelier.png");menuImporter.maxTextureSize=2048;menuImporter.mipmapEnabled=false;menuImporter.textureCompression=TextureImporterCompression.Uncompressed;menuImporter.SaveAndReimport();
   foreach(var name in new[]{"WorkshopGuide","BrushTools","TikCoin"}){string p="Assets/CozyBoard/Art/UI/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();}
   experience.PackingPaper=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CozyBoard/Art/UI/PackingPaper.png");
   ConfigureFont();
   experience.GuideArt=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CozyBoard/Art/UI/WorkshopGuide.png");experience.ToolIcons=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CozyBoard/Art/UI/BrushTools.png");
   var camera=Object.FindFirstObjectByType<WorkshopController>().ViewCamera;camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline){pipeline.supportsHDR=true;EditorUtility.SetDirty(pipeline);}
   const string path="Assets/CozyBoard/Data/CozyGlow.asset";
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
   if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
   bloom.threshold.Override(.85f);bloom.intensity.Override(.12f);bloom.scatter.Override(.45f);bloom.highQualityFiltering.Override(true);
   var go=GameObject.Find("Cozy soft light");if(!go)go=new GameObject("Cozy soft light");var volume=go.GetComponent<Volume>();if(!volume)volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
   var supplies=game.Controller?game.Controller.transform.Find("SupplyPackages"):Object.FindFirstObjectByType<WorkshopController>().transform.Find("SupplyPackages");if(supplies){foreach(Transform box in supplies){foreach(Transform old in box.Cast<Transform>().ToArray())Object.DestroyImmediate(old.gameObject);box.gameObject.SetActive(false);}}
   EditorUtility.SetDirty(profile);EditorUtility.SetDirty(game);EditorUtility.SetDirty(experience);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  }
  public static void ConfigureFont(){
   const string path="Assets/CozyBoard/Data/WorkshopHandwritten.asset";
   var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
   if(!font){font=TMPro.TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/CozyBoard/Fonts/PatrickHand-Regular.ttf"),80,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,TMPro.AtlasPopulationMode.Dynamic,true);font.name="Workshop handwritten Turkish";AssetDatabase.CreateAsset(font,path);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);}
   if(!font.HasCharacters("çğıöşüÇĞİÖŞÜ")){font.atlasPopulationMode=TMPro.AtlasPopulationMode.Dynamic;
   font.TryAddCharacters("çğıöşüÇĞİÖŞÜ",out string missing);if(!string.IsNullOrEmpty(missing))throw new System.Exception("Missing Turkish glyphs: "+missing);
   string chars="";for(int i=32;i<384;i++)chars+=(char)i;font.TryAddCharacters(chars+"→×·°–—…",out _);font.atlasPopulationMode=TMPro.AtlasPopulationMode.Static;}
   font.atlasPopulationMode=TMPro.AtlasPopulationMode.Static;
   foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)){text.font=font;EditorUtility.SetDirty(text);}
   EditorUtility.SetDirty(font);EditorUtility.SetDirty(font.material);foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);
  }
 }
}
