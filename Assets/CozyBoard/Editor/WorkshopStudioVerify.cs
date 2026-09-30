using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CozyBoard.Editor {
    public static class WorkshopStudioVerify {
        const string Scene="Assets/CozyBoard/Scenes/Workbench.unity";
        [MenuItem("Cozy Board/Verify 3D paint studio")]
        public static void Run(){
            EditorSceneManager.OpenScene(Scene);
            var controller=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();var game=controller.Game;game.NewOrder();
            var painter=game.Painter;var report=new List<string>();
            void Check(bool test,string message){if(!test)throw new Exception(message);report.Add("PASS "+message);}
            foreach(var part in controller.Items.Where(p=>p.Stage>0).OrderBy(p=>p.Stage).ToArray())controller.PaintAt(part.Stage,controller.Lookup["Case"].transform.TransformPoint(part.Slot));
            var item=controller.Items.First(p=>p.Kind=="keycap"&&p.Fitted&&!p.Label.Contains("Space"));
            var mesh=item.Visual.GetComponent<MeshFilter>().sharedMesh;var bounds=mesh.bounds;
            var side=new Vector3(bounds.center.x,bounds.center.y,bounds.min.z+.002f);
            var opposite=new Vector3(bounds.center.x,bounds.center.y,bounds.max.z-.002f);
            Vector2 sideUV=painter.SurfaceUV(item,side,Vector3.back),backUV=painter.SurfaceUV(item,opposite,Vector3.forward);
            var before=painter.Sample(item,backUV);painter.PaintAtSurface(item,side,Vector3.back,Color.red,.065f);
            var after=painter.Sample(item,sideUV);
            Check(after.r>.8f&&after.g<.3f,"Side wall accepts a direct 3D brush stroke");
            Check(Vector4.Distance(before,painter.Sample(item,backUV))<.01f,"Opposite wall is not painted through the key");
            painter.Undo();Check(Vector4.Distance(before,painter.Sample(item,sideUV))<.01f,"Undo restores side paint");painter.Redo();Check(painter.Sample(item,sideUV).g<.3f,"Redo restores side paint");
            string saved=game.SerializeProgress();game.NewOrder();game.RestoreProgress(saved);Check(painter.Sample(item,sideUV).g<.3f,"Save and restore retain side artwork");
            // Legacy flat images must migrate to the top island without wrapping onto walls.
            var legacy=new Texture2D(8,8);legacy.SetPixels(Enumerable.Repeat(Color.blue,64).ToArray());legacy.Apply();
            painter.Restore(new[]{item.Id},new[]{Convert.ToBase64String(legacy.EncodeToPNG())});UnityEngine.Object.DestroyImmediate(legacy);
            var top=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);var topUV=painter.SurfaceUV(item,top,Vector3.up);
            Check(painter.Sample(item,topUV).b>.9f&&painter.Sample(item,topUV).r<.1f,"Legacy artwork migrates onto the top surface");
            Check(Vector4.Distance(before,painter.Sample(item,sideUV))<.01f,"Legacy artwork leaves new side surfaces clean");
            game.RestoreProgress(saved);
            painter.Edit(item);Check(painter.EditorSurface.texture is RenderTexture,"Studio displays a rendered 3D model");Check(!painter.EditorSurface.material||painter.EditorSurface.material.shader.name!="CozyBoard/RoundedKeyUI","Flat drawing mask is removed");
            painter.ToggleLegend();Check(!painter.LegendIsVisible(item.Id),"Raised legend can be hidden");painter.Undo();Check(painter.LegendIsVisible(item.Id),"Legend visibility supports undo");painter.CloseEditor();
            Check(!ShaderUtil.ShaderHasError(Shader.Find("CozyBoard/KeycapStudio")),"Studio shader compiles");
            painter.ResetPaint();EditorSceneManager.OpenScene(Scene);
            var authored=UnityEngine.Object.FindFirstObjectByType<WorkshopKeyPainter>();authored.StudioShader=Shader.Find("CozyBoard/KeycapStudio");EditorUtility.SetDirty(authored);EditorSceneManager.SaveScene(authored.gameObject.scene);
            Directory.CreateDirectory("Verification");File.WriteAllLines("Verification/3d-paint-checks.txt",report);Debug.Log("COZY_3D_PAINT_VERIFIED "+report.Count);
        }
    }
}
