using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
 public static class WorkshopPackingVerify {
  public static void Run(){
   const string scene="Assets/CozyBoard/Scenes/Workbench.unity";
   EditorSceneManager.OpenScene(scene);
   var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();controller.SendMessage("Start");
   var game=controller.Game;game.Experience.Initialize(game);game.VerificationSavePath=Path.GetFullPath("Logs/packing-test-save.json");
   var report=new System.Collections.Generic.List<string>();
   void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);}
   try{
    game.StoryUI.Hide();game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.Shop.Close();
    foreach(string order in new[]{"deniz-macro","mina-keyboard"}){
     Check(game.AcceptOrder(order),"Accept "+order);
     foreach(var item in game.ProductItems.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))controller.Attach(item);
     var board=controller.Lookup["Case"].transform;board.position=new Vector3(0,.21f,0);board.rotation=Quaternion.identity;
     float top=WorkshopCarton.PackedTop(board,.21f);
     var carton=WorkshopCarton.Create(controller.transform,"Packing geometry verification",game.IsMacro?5:9,game.IsMacro?3.8f:4.3f,1.38f,true,top);
     carton.transform.position=Vector3.zero;carton.Wrap.SetActive(true);carton.FoldPaper(1);carton.Lid.localRotation=Quaternion.identity;
     var sheets=carton.Wrap.GetComponentsInChildren<MeshFilter>();
     var paperPoints=sheets.SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
     float actualTop=board.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).Max(r=>r.bounds.max.y);
     Check(Mathf.Abs(top-actualTop)<.001f,order+": measured height matches assembled product");
     Check(paperPoints.Min(p=>p.y)>actualTop+.025f,order+": both folded sheets clear every keycap, including wrinkles");
     Check(carton.Lid.position.y>paperPoints.Max(p=>p.y)+.015f,order+": closed lid clears the folded paper");
     var left=sheets.First(f=>f.name=="Left tissue sheet");var right=sheets.First(f=>f.name=="Right tissue sheet");
     Check(left.GetComponent<MeshRenderer>().bounds.max.x>right.GetComponent<MeshRenderer>().bounds.min.x,order+": folded sheets overlap across the centre");
     board.rotation=Quaternion.Euler(35,20,10);
     Check(Mathf.Abs(WorkshopCarton.PackedTop(board,.21f)-top)<.001f,order+": packing height is independent of inspection rotation");
     board.rotation=Quaternion.identity;Object.DestroyImmediate(carton.gameObject);
     game.ActiveOrder=null;game.ConfigureProduct();game.NewOrder();
    }
    File.WriteAllLines("Logs/packing-checks.txt",report);Debug.Log("COZY_PACKING_VERIFIED "+report.Count);
   }finally{game.VerificationSavePath=null;EditorSceneManager.OpenScene(scene);}
  }
 }
}
