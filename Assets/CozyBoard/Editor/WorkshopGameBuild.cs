using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using TMPro;
namespace CozyBoard.Editor {
    public static class WorkshopGameBuild {
        const string Root="Assets/CozyBoard",ScenePath=Root+"/Scenes/Workbench.unity";
        [MenuItem("Cozy Board/Apply flat workspace game update")]
        public static void Configure() {
            EditorSceneManager.OpenScene(ScenePath);
            var controller=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();
            var game=controller.GetComponent<WorkshopGameMode>();if(!game)game=controller.gameObject.AddComponent<WorkshopGameMode>();
            controller.Game=game;game.Controller=controller;
            var audio=controller.GetComponent<WorkshopAudio>();if(!audio)audio=controller.gameObject.AddComponent<WorkshopAudio>();game.Audio=audio;
            ConfigureAudio(audio);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/FlatWorkspace.mat");
            if(!material){material=new Material(Shader.Find("CozyBoard/FlatWorkspace"));AssetDatabase.CreateAsset(material,Root+"/Materials/FlatWorkspace.mat");}
            material.SetColor("_Surface",new Color(.76f,.48f,.29f));material.SetColor("_Mat",new Color(.50f,.61f,.60f));EditorUtility.SetDirty(material);
            var background=GameObject.Find("Desk_Rug_Chair_2D")??GameObject.Find("FlatWorkspace2D");background.name="FlatWorkspace2D";background.GetComponent<MeshRenderer>().sharedMaterial=material;
            controller.Pitch=85;
            foreach(var button in controller.LayerButtons)button.gameObject.SetActive(false);
            controller.AssembleButton.gameObject.SetActive(false);controller.ResetButton.gameObject.SetActive(false);controller.StatusLabel.gameObject.SetActive(false);
            BuildUI(game);
            NormalizeSlider(game.MusicSlider);NormalizeSlider(game.EffectsSlider);
            var meter=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Data/MeterSprite.asset");
            if(!meter) {
                var pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();pixel.name="MeterPixel";
                AssetDatabase.CreateAsset(pixel,Root+"/Data/MeterPixel.asset");
                meter=Sprite.Create(pixel,new Rect(0,0,1,1),Vector2.one*.5f);meter.name="MeterSprite";AssetDatabase.CreateAsset(meter,Root+"/Data/MeterSprite.asset");
            }
            game.ProgressFill.sprite=meter;
            WorkshopArtRevision.Apply(controller);
            WorkshopFreeBuild.Apply(controller);
            if(!game.TargetMarker) {
                var marker=new GameObject("PlacementGuide");marker.transform.SetParent(controller.transform,false);marker.AddComponent<MeshFilter>().sharedMesh=controller.GroundQuad;
                game.TargetMarker=marker.AddComponent<MeshRenderer>();
                var guide=new Material(Shader.Find("CozyBoard/PlacementGuide"));AssetDatabase.CreateAsset(guide,Root+"/Materials/PlacementGuide.mat");game.TargetMarker.sharedMaterial=guide;
            }
            PlayerSettings.defaultScreenWidth=3840;PlayerSettings.defaultScreenHeight=2160;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.macRetinaSupport=true;
            var pipeline=(QualitySettings.renderPipeline??UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if(pipeline){pipeline.renderScale=1;pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);}
            controller.ViewCamera.allowMSAA=true;
            game.NewOrder();Save(controller);
            Verify();
            Debug.Log("COZY_GAME_CONFIGURED_4K");
        }
        static void ConfigureAudio(WorkshopAudio audio) {
            foreach(var path in Directory.GetFiles(Root+"/Audio","*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".wav")||p.EndsWith(".mp3"))) {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);bool music=path.Contains("lofi_loop")||path.Contains("ChillLofi");
                var settings=importer.defaultSampleSettings;
                settings.loadType=music?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=music?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                settings.quality=.8f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.forceToMono=!music;importer.loadInBackground=music;importer.SaveAndReimport();
            }
            AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+name+".wav");
            audio.Keys=Enumerable.Range(1,8).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Recorded/Linear-"+i.ToString("00")+".wav")).ToArray();
            audio.Music=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Recorded/ChillLofi.mp3");audio.Pickup=audio.Keys[0];audio.Snap=audio.Keys[1];audio.Place=audio.Keys[2];audio.Complete=audio.Keys[3];
            if(!audio.MusicSource){var go=new GameObject("LofiMusic");go.transform.SetParent(audio.transform);audio.MusicSource=go.AddComponent<AudioSource>();}
            if(!audio.EffectsSource){var go=new GameObject("KeyboardEffects");go.transform.SetParent(audio.transform);audio.EffectsSource=go.AddComponent<AudioSource>();}
            audio.MusicSource.playOnAwake=false;audio.MusicSource.loop=true;audio.MusicSource.spatialBlend=0;audio.MusicSource.volume=.32f;audio.MusicSource.clip=audio.Music;
            audio.EffectsSource.playOnAwake=false;audio.EffectsSource.spatialBlend=0;audio.EffectsSource.volume=.8f;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size) {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string value,int size,Vector2 anchor,Vector2 position,Vector2 dimensions) {
            var rect=Rect(name,parent,anchor,position,dimensions);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.color=new Color(.25f,.20f,.19f);text.raycastTarget=false;return text;
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string value,Vector2 position) {
            var rect=Rect(name,parent,Vector2.one,position,new Vector2(165,42));var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.85f,.88f,.82f,.97f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var label=Text("Label",rect,font,value,18,new Vector2(.5f,.5f),Vector2.zero,new Vector2(165,42));label.alignment=TextAlignmentOptions.Center;
            return button;
        }
        static UnityEngine.UI.Slider Slider(string name,Transform parent,TMP_FontAsset font,Vector2 position,float value) {
            var rect=Rect(name,parent,Vector2.one,position,new Vector2(190,28));
            var label=Text(name+"Label",rect,font,name,16,new Vector2(0,.5f),new Vector2(-78,0),new Vector2(72,28));label.alignment=TextAlignmentOptions.Left;
            var track=Rect("Track",rect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,5));track.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.32f,.28f,.27f,.55f);
            var fill=Rect("Fill",rect,new Vector2(0,.5f),Vector2.zero,new Vector2(190,5));fill.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.94f,.89f,.77f);
            var handle=Rect("Handle",rect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(15,20));var image=handle.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.33f,.44f,.41f);
            var slider=rect.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=image;slider.minValue=0;slider.maxValue=1;slider.value=value;
            return slider;
        }
        static void NormalizeSlider(UnityEngine.UI.Slider slider) {
            var root=slider.transform;
            var fillArea=root.Find("FillArea") as RectTransform;
            if(!fillArea)fillArea=Rect("FillArea",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,5));
            slider.fillRect.SetParent(fillArea,false);slider.fillRect.anchorMin=Vector2.zero;slider.fillRect.anchorMax=Vector2.one;slider.fillRect.sizeDelta=Vector2.zero;slider.fillRect.anchoredPosition=Vector2.zero;
            var handleArea=root.Find("HandleArea") as RectTransform;
            if(!handleArea)handleArea=Rect("HandleArea",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(190,20));
            slider.handleRect.SetParent(handleArea,false);slider.handleRect.sizeDelta=new Vector2(14,0);slider.handleRect.anchoredPosition=Vector2.zero;
            var value=slider.value;slider.value=0;slider.value=value;
        }
        static void BuildUI(WorkshopGameMode game) {
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var existing=canvas.transform.Find("GameHUD");
            if(existing){game.Objective=existing.Find("Objective").GetComponent<TMP_Text>();return;}
            var hud=new GameObject("GameHUD",typeof(RectTransform)).GetComponent<RectTransform>();hud.SetParent(canvas.transform,false);hud.anchorMin=Vector2.zero;hud.anchorMax=Vector2.one;hud.offsetMin=hud.offsetMax=Vector2.zero;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Data/WorkshopFont.asset");font.TryAddCharacters("çğıöşüÇĞİÖŞÜ’ ·",out _);
            game.Objective=Text("Objective",hud,font,"",25,new Vector2(.5f,1),new Vector2(0,-25),new Vector2(750,40));game.Objective.alignment=TextAlignmentOptions.Center;
            game.Progress=Text("Progress",hud,font,"",18,new Vector2(.5f,1),new Vector2(0,-67),new Vector2(400,25));game.Progress.alignment=TextAlignmentOptions.Center;
            var progress=Rect("ProgressTrack",hud,new Vector2(.5f,1),new Vector2(0,-99),new Vector2(290,7));progress.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.35f,.30f,.28f,.18f);
            var fill=Rect("ProgressFill",progress,new Vector2(.5f,.5f),Vector2.zero,new Vector2(290,7));game.ProgressFill=fill.gameObject.AddComponent<UnityEngine.UI.Image>();game.ProgressFill.color=new Color(.38f,.55f,.44f);game.ProgressFill.type=UnityEngine.UI.Image.Type.Filled;game.ProgressFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            // Image fill requires a sprite; the built-in white sprite keeps this resolution independent.
            game.ProgressFill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            game.StockLabel=Text("StockLabel",hud,font,"",20,new Vector2(.5f,0),new Vector2(0,115),new Vector2(700,32));game.StockLabel.alignment=TextAlignmentOptions.Center;
            game.CompletionLabel=Text("Completion",hud,font,"",22,new Vector2(.5f,0),new Vector2(0,150),new Vector2(950,64));game.CompletionLabel.alignment=TextAlignmentOptions.Center;
            game.TestButton=Button("TestKeyboard",hud,font,"Tuşları dene",new Vector2(-25,-25));game.NewOrderButton=Button("NewOrder",hud,font,"Yeni sipariş",new Vector2(-25,-78));
            game.MusicSlider=Slider("Müzik",hud,font,new Vector2(-25,-146),.32f);game.EffectsSlider=Slider("Efekt",hud,font,new Vector2(-25,-186),.8f);
            Text("Quality",hud,font,"4K  ·  COZY BOARD",15,new Vector2(0,0),new Vector2(30,76),new Vector2(250,25));
        }
        static void Save(WorkshopController controller) {
            foreach(var item in controller.Items) {
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Visual);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Hitbox);
            }
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);EditorSceneManager.SaveScene(controller.gameObject.scene,ScenePath);AssetDatabase.SaveAssets();
        }
        [MenuItem("Cozy Board/Verify assembly game")]
        public static void Verify() { WorkshopFreeVerify.Run(); }
        public static void BuildMac() {
            EditorSceneManager.OpenScene(ScenePath);Verify();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/macOS/Cozy Board.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText("Verification/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Game build failed");
        }
    }
}
