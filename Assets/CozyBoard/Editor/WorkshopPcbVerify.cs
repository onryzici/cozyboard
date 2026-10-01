using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
 public static class WorkshopPcbVerify {
  public static void Run(){
   const string scene="Assets/CozyBoard/Scenes/Workbench.unity";
   EditorSceneManager.OpenScene(scene);
   var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();controller.SendMessage("Start");
   var game=controller.Game;game.Experience.Initialize(game);game.VerificationSavePath=Path.GetFullPath("Logs/pcb-test-save.json");
   var report=new System.Collections.Generic.List<string>();
   void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);}
   try{
    var pcb=controller.Lookup["PCB"];var source=pcb.Visual.GetComponent<MeshFilter>().sharedMesh;var sourceBounds=pcb.BoundsSize;
    var slot=controller.Lookup["Switch_01"].Slot;var materials=pcb.Visual.sharedMaterials;
    Capture(source,materials,"Logs/pcb-keyboard.png");
    game.StoryUI.Hide();game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.Shop.Close();
    Check(game.AcceptOrder("deniz-macro"),"Accept six-key order");
    var compact=pcb.Visual.GetComponent<MeshFilter>().sharedMesh;
    Check(compact.subMeshCount==materials.Length,"PCB retains one submesh per authored material");
    for(int sub=0;sub<compact.subMeshCount;sub++)Check(compact.GetIndexCount(sub)>0,"Visible PCB material: "+materials[sub].name);
    foreach(string material in new[]{"Paint_copper","Paint_trace","Paint_silk"}){
     int sub=Array.FindIndex(materials,m=>m.name==material);var vertices=source.vertices;var indices=source.GetTriangles(sub);int count=0;
     bool Inside(Vector3 p)=>Mathf.Abs(p.x-slot.x)<.20f&&Mathf.Abs(p.z-slot.z)<.175f;
     for(int i=0;i<indices.Length;i+=3)if(Inside(vertices[indices[i]])&&Inside(vertices[indices[i+1]])&&Inside(vertices[indices[i+2]]))count+=3;
     Check(count>0&&compact.GetIndexCount(sub)==6*count,"Exactly six authored footprints: "+material);
     var points=compact.vertices;var tris=compact.GetTriangles(sub);
     for(int key=1;key<=6;key++){var target=controller.Lookup[$"Switch_{key:00}"].Slot;Check(tris.Any(i=>Mathf.Abs(points[i].x-target.x)<.20f&&Mathf.Abs(points[i].z-target.z)<.20f),material+" aligns with switch "+key);}
    }
    Check(compact.bounds.size.x<=1.571f&&compact.bounds.size.z<=1.101f&&compact.bounds.max.y<.105f,"Circuitry fits compact case below plate");
    Check(pcb.Hitbox.size==compact.bounds.size,"Picking bounds follow detailed PCB");
    Capture(compact,materials,"Logs/pcb-macro.png");
    game.ActiveOrder=null;game.ConfigureProduct();
    Check(pcb.Visual.GetComponent<MeshFilter>().sharedMesh==source&&pcb.BoundsSize==sourceBounds,"Full keyboard restores original PCB mesh and bounds");
    File.WriteAllLines("Logs/pcb-checks.txt",report);Debug.Log("COZY_PCB_VERIFIED "+report.Count);
   }finally{game.VerificationSavePath=null;EditorSceneManager.OpenScene(scene);}
  }
  static void Capture(Mesh mesh,Material[] materials,string path){
   var root=new GameObject("PCB isolated preview");root.layer=31;root.transform.position=new Vector3(100,0,100);
   root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterials=materials;
   var cameraObject=new GameObject("PCB preview camera");var camera=cameraObject.AddComponent<Camera>();
   camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.83f,.80f,.71f);camera.orthographic=true;
   camera.orthographicSize=Mathf.Max(mesh.bounds.size.z*.75f,mesh.bounds.size.x*.36f);camera.transform.position=root.transform.position+new Vector3(0,5,-2);camera.transform.LookAt(root.transform.position);
   var target=new RenderTexture(1400,900,24){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=1400f/900;camera.Render();
   var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;
   camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(root);
  }
 }
}
