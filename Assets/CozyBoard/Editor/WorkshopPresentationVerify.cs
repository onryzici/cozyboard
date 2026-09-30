using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
    public static class WorkshopPresentationVerify {
        const string Scene="Assets/CozyBoard/Scenes/Workbench.unity";
        public static void Run(){
            WorkshopAtelierImports.ConfigureAssets();
            EditorSceneManager.OpenScene(Scene);
            var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();controller.SendMessage("Start");
            var game=controller.Game;game.Experience.Initialize(game);game.Experience.Continue();game.Menu.ToastLabel.gameObject.SetActive(false);
            game.Shop.SendMessage("Update");foreach(var mark in Object.FindObjectsByType<WorkshopToolbarSelection>(FindObjectsInactive.Include))mark.SendMessage("Update");
            var report=new List<string>();
            void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add("PASS "+message);}
            Check(WorkshopAtelierStyle.Icons&&WorkshopAtelierStyle.Icons.width==1374,"Shared painted icon atlas imported at original size");
            foreach(var button in new[]{game.Menu.PointerButton,game.Menu.MoveButton,game.Menu.RotateButton,game.Menu.PaintButton,game.Menu.UndoButton,game.Menu.SaveButton,game.Menu.OrderButton,game.Menu.SettingsButton})Check(((RawImage)button.targetGraphic).texture==WorkshopAtelierStyle.Icons,"Shared icon style: "+button.name);
            Check(Resources.Load<AudioClip>("Music/WarmFireplace").length>70,"New piano/guitar soundtrack is available");
            Check(controller.transform.Find("Quiet desk details")&&controller.transform.Find("Quiet desk details").childCount==3,"Three restrained desk decoration groups");
            Check(Mathf.Approximately(WorkshopAtelierStyle.WheelSteps(120),WorkshopAtelierStyle.WheelSteps(1)),"Wheel input normalizes Windows and small-delta devices");
            foreach(var item in controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))controller.Attach(item);
            game.Refresh();game.Experience.SendMessage("Update");controller.SendMessage("LateUpdate");
            var camera=controller.ViewCamera;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=16f/9;controller.UpdateCamera();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)){if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}}
            var status=Object.FindFirstObjectByType<WorkshopStatusLayout>();Canvas.ForceUpdateCanvases();status.Arrange();Canvas.ForceUpdateCanvases();
            var statusRect=(RectTransform)status.transform;Check(statusRect.anchoredPosition.y>112,"Progress sits above the toolbar");
            foreach(var paper in Object.FindObjectsByType<WorkshopPaperGraphic>(FindObjectsInactive.Exclude)){var mesh=paper.canvasRenderer.GetMesh();Check(paper.rectTransform.rect.width>0&&mesh&&mesh.vertexCount>0,"Rounded paper renders: "+paper.transform.parent.name);}
            Capture(camera,target,"Logs/atelier-workbench.png");
            game.Testing.Begin();game.Experience.SendMessage("Update");Canvas.ForceUpdateCanvases();
            var testCard=GameObject.Find("Keyboard test card");var inset=(RectTransform)testCard.transform.Find("Test keyboard inset");
            var pads=inset.GetComponentsInChildren<Button>();Check(pads.Length==61,"Keyboard test has 61 interactive pads");
            foreach(var pad in pads){var r=(RectTransform)pad.transform;Check(r.anchoredPosition.x>=0&&r.anchoredPosition.x+r.rect.width<=inset.rect.width&&-r.anchoredPosition.y+r.rect.height<=inset.rect.height,"Test pad stays inside card: "+pad.name);}
            foreach(var text in testCard.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Test label fits: "+text.name);}
            game.Menu.ToastLabel.gameObject.SetActive(false);Capture(camera,target,"Logs/atelier-testing.png");game.Testing.End();
            var cap=controller.Items.First(x=>x.Kind=="keycap"&&x.Label.Contains("Esc"));
            var original=cap.Visual.GetComponent<MeshFilter>().sharedMesh;int body=Array.FindIndex(cap.Visual.sharedMaterials,m=>m.name.StartsWith("Paint_key_",StringComparison.OrdinalIgnoreCase));
            var surface=new KeycapSurface(original,Mathf.Max(0,body));var preparation=surface.CacheSamplesIncremental();var timer=new System.Diagnostics.Stopwatch();double longest=0,total=0;int slices=0;
            while(true){timer.Restart();bool more=preparation.MoveNext();timer.Stop();longest=Math.Max(longest,timer.Elapsed.TotalMilliseconds);total+=timer.Elapsed.TotalMilliseconds;if(!more)break;slices++;}
            Check(surface.SamplesReady&&slices>1,"Surface preparation yields between work slices");report.Add($"INFO Surface preparation: {total:F1} ms total, {longest:F1} ms longest slice, {slices} yields");surface.ReleaseSamples();Object.DestroyImmediate(surface.Mesh);
            game.Menu.SelectTool(3);game.Painter.Edit(cap);game.Experience.SendMessage("Update");Canvas.ForceUpdateCanvases();
            var palette=(RectTransform)game.Painter.PalettePanel.transform;var tape=(RectTransform)palette.Find("Masking tape");var slider=(RectTransform)game.Painter.BrushSize.transform;
            Check(Mathf.Abs(slider.anchoredPosition.y-tape.anchoredPosition.y)>80,"Tape actions have breathing room above sliders");Check(slider.sizeDelta.x==164,"Brush controls use shorter sliders");
            foreach(var text in palette.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.name=="Tape instructions"||text.transform.parent==tape)Check(!text.isTextOverflowing,"Tape label fits: "+text.name);}
            foreach(var mark in Object.FindObjectsByType<WorkshopToolbarSelection>(FindObjectsInactive.Include))mark.SendMessage("Update");game.Shop.SendMessage("Update");game.Menu.ToastLabel.gameObject.SetActive(false);
            Capture(camera,target,"Logs/atelier-painting.png");game.Painter.CloseEditor();
            WorkshopPaintVerify.Run(game,Check);Check(!ShaderUtil.ShaderHasError(Shader.Find("CozyBoard/KeycapStudio")),"Painting shader compiles");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/atelier-refresh-checks.txt",report);Debug.Log("COZY_ATELIER_VERIFIED "+report.Count);
            game.Painter.ResetPaint();camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);EditorSceneManager.OpenScene(Scene);
        }
        static void Capture(Camera camera,RenderTexture target,string path){
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=previous;
        }
    }
}
