using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
 public static class WorkshopVarietyVerify {
  const string Scene="Assets/CozyBoard/Scenes/Workbench.unity";
  public static void VerifyAndBuild(){Run();WorkshopStoryVerify.Run();WorkshopPresentationVerify.Run();BuildWindows();}
  public static void BuildWindows(){
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/VarietyVerification/CozyBoard.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
   if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded||result.summary.totalErrors!=0)throw new Exception("Variety Windows build failed");Debug.Log("COZY_VARIETY_BUILD_SUCCEEDED");
  }
  [MenuItem("Cozy Board/Verify repair and macro pad orders")]
  public static void Run(){
   Directory.CreateDirectory("Logs");EditorSceneManager.OpenScene(Scene);var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();controller.SendMessage("Start");var game=controller.Game;game.Experience.Initialize(game);game.VerificationSavePath=Path.GetFullPath("Logs/variety-verification-save.json");
   var report=new List<string>();void Check(bool value,string name){if(!value)throw new Exception(name);report.Add("PASS "+name);}
   void Fits(Transform root){Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();var rect=text.rectTransform.rect;bool within=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible).All(x=>x.bottomLeft.x>=rect.xMin-1&&x.topRight.x<=rect.xMax+1&&x.bottomLeft.y>=rect.yMin-1&&x.topRight.y<=rect.yMax+1);Check(!text.isTextOverflowing&&within,"Variety text fits: "+text.name);}}
   Vector3 At(WorkshopItem part)=>controller.Lookup["Case"].transform.TransformPoint(part.Slot);
   void Remove(WorkshopItem item){game.Testing.Removed(item);item.Fitted=false;item.transform.SetParent(controller.PartsRoot,true);game.PresentStock();game.Refresh();}
   void Seed(){game.StoryUI.Hide();game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.Shop.Close();}
   try{
    var originalCase=controller.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh;var originalSlot=controller.Lookup["Keycap_01"].Slot;
    Seed();var offers=WorkshopStory.Offers(game.Story);Check(offers.Select(x=>x.Kind).Distinct().Count()==3,"Offer board contains repair, macro pad and keyboard jobs");game.Shop.OpenOrders();Fits(GameObject.Find("Available orders").transform);Capture(controller,"Logs/variety-orders.png");
    Check(game.AcceptOrder("ece-repair"),"Ece's old keyboard repair can be accepted");
    Check(game.IsRepair&&game.Completed&&game.TestCount==3&&!game.Testing.Passed,"Repair arrives assembled and only three neighbouring keys need testing");
    Check(game.Shop.Data.used[0]&&game.Shop.Data.used[2]&&!game.Shop.Data.used[1],"Repair reserves existing body and recovered caps, not a new keyboard kit");
    Fits(game.Menu.OrderPanel.transform);game.Menu.ClosePanels();var faultCap=controller.Lookup[game.Repair.capId];var faultSwitch=controller.Lookup[game.Repair.switchId];
    Check(!game.CanRemoveForRepair(faultCap),"Fault must be diagnosed before removing its cap");game.Testing.Begin();
    foreach(var cap in game.TestKeys)game.Press(cap);Check(game.Repair.diagnosed&&game.Testing.Count==2&&!game.Testing.Passed,"Testing reveals one faulty neighbour and records the diagnosis");
    Check(GameObject.Find("Test keyboard inset").GetComponentsInChildren<UnityEngine.UI.Button>().Length==3,"Repair test shows three pads instead of sixty-one");Fits(GameObject.Find("Keyboard test card").transform);Capture(controller,"Logs/variety-repair-test.png");game.Testing.End();
    int[] inventory=game.Shop.Data.stock.ToArray();int money=game.Shop.Data.credits;Check(game.Shop.Purchase(3)&&game.Shop.Data.credits==money-8&&game.Shop.Data.looseParts[3]==1,"A repair switch can be bought singly for eight Tık");game.Shop.Close();
    Remove(faultCap);string disassembled=game.SerializeProgress();game.RestoreProgress(disassembled);faultCap=controller.Lookup[game.Repair.capId];faultSwitch=controller.Lookup[game.Repair.switchId];
    Check(!faultCap.Fitted&&game.Repair.diagnosed&&game.TestCount==3&&game.TestKeys.Length==3,"Repair reload retains the removed cap, diagnosis and three test targets");
    Remove(faultSwitch);Check(controller.PaintAt(3,At(faultSwitch))&&game.Repair.replaced,"Installing the spare marks the repair as replaced");
    Check(game.Shop.Data.stock.SequenceEqual(inventory)&&game.Shop.Data.looseParts[3]==0,"Repair consumes one loose switch, leaving complete kits untouched");Check(controller.PaintAt(4,At(faultCap)),"Original cap can be put back without another kit");
    game.Testing.Begin();foreach(var cap in game.TestKeys)game.Press(cap);Check(game.Testing.Passed&&game.Testing.Count==3,"Three working neighbour tests finish the repair");game.Testing.End();
    Check(game.FinishDelivery()&&game.LastDelivery.reward==90&&game.LastDelivery.kind=="repair"&&game.Story.Has(WorkshopStory.RepairEvent),"Repair pays ninety Tık and advances Ece's story once");
    Check(WorkshopStory.Offers(game.Story)[0].Id=="ece-repair-repeat","First repair becomes a repeatable short job");game.Shop.Close();
    Check(game.AcceptOrder("deniz-macro"),"Deniz's six-key macro pad can be accepted");
    Check(game.KeyCount==6&&game.PartCount==14&&game.ProductItems.Count(x=>x.Stage>0)==14,"Macro pad needs fourteen parts rather than one hundred twenty-four");
    Check(controller.Lookup["Case"].BoundsSize.x<2&&controller.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh!=originalCase,"Macro pad uses a separate compact rolled-rim mesh");
    Check(controller.Lookup["Keycap_01"].Slot!=originalSlot&&!controller.Lookup["Keycap_07"].Visual.enabled,"Macro pad uses a three-by-two layout and hides inactive keyboard keys");
    Check(!game.CanInstall(controller.Lookup["Keycap_00"]),"Keyboard-only keycap cannot enter a macro pad slot");Fits(game.Menu.OrderPanel.transform);game.Menu.ClosePanels();
    foreach(var item in game.ProductItems.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))Check(controller.PaintAt(item.Stage,At(item)),"Macro assembly accepts "+item.Id);
    Check(game.Completed&&game.Installed==14&&string.IsNullOrEmpty(game.Testing.LooseSwitch),"Macro assembly completes six healthy keys");
    Check(game.Shop.Data.looseParts[3]==55&&game.Shop.Data.looseParts[6]==55&&game.Shop.Data.stock[3]==0&&game.Shop.Data.stock[6]==0,"Only six switches and caps are removed from opened sets");
    game.Testing.Begin();Check(GameObject.Find("Test keyboard inset").GetComponentsInChildren<UnityEngine.UI.Button>().Length==6,"Macro test contains only six pads");GameObject.Find("Macro function 0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Fits(GameObject.Find("Macro function picker").transform);GameObject.Find("Choose Geri al").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();game.MacroFunctions[0]=null;Check(!game.Testing.Check(controller.Lookup["Keycap_01"]),"Unassigned shortcut cannot pass macro testing");
    game.Testing.SetFunction(0,"Bul");Check(!game.Testing.Check(controller.Lookup["Keycap_01"]),"A wrong shortcut does not pass the requested function test");
    for(int i=0;i<6;i++){Check(game.Testing.SetFunction(i,WorkshopGameMode.DesiredFunctions[i]),"Macro shortcut can be configured "+i);game.Press(controller.Lookup[$"Keycap_{i+1:00}"]);}
    Check(game.Testing.Passed&&game.Testing.Count==6&&game.MacroReady,"Six configured shortcut tests complete the macro job");Fits(GameObject.Find("Keyboard test card").transform);Capture(controller,"Logs/variety-macro-test.png");
    game.Testing.SetFunction(0,"Bul");Check(!game.Testing.Passed&&game.Testing.Count==5,"Editing a checked binding invalidates its result");game.Testing.SetFunction(0,"Geri al");game.Press(controller.Lookup["Keycap_01"]);game.Testing.End();game.Tools.Tightened=15;
    var cap1=controller.Lookup["Keycap_01"];var capBounds=cap1.Visual.GetComponent<MeshFilter>().sharedMesh.bounds;var top=new Vector3(capBounds.center.x,capBounds.max.y,capBounds.center.z);
    Check(game.Painter.AddTape(cap1,new WorkshopTapeStrip{Start=top-Vector3.forward*capBounds.size.z,End=top+Vector3.forward*capBounds.size.z,Normal=Vector3.up,Width=.08f}),"Macro cap accepts masking tape");game.Painter.PaintAtSurface(cap1,top+Vector3.right*.12f,Vector3.up,Color.blue,.06f);
    string macro=game.SerializeProgress();game.RestoreProgress(macro);Check(game.IsMacro&&game.Completed&&game.Testing.Passed&&game.Painter.TapeCount(cap1)==1&&game.MacroReady,"Macro reload preserves compact geometry, tape, shortcuts and testing");
    Capture(controller,"Logs/variety-macro-workbench.png");Check(game.FinishDelivery()&&game.LastDelivery.kind=="macro"&&game.Story.Has(WorkshopStory.MacroEvent),"Macro delivery advances Deniz's story");
    Check(controller.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh==originalCase&&controller.Lookup["Keycap_01"].Slot==originalSlot,"Returning from macro pad restores the authored keyboard mesh and slots");
    game.Shop.Close();Check(game.AcceptOrder("mina-keyboard"),"Full keyboard remains selectable beside short jobs");Check(game.KeyCount==61&&game.PartCount==124,"Full keyboard still uses the original counts");
    // Legacy completion promotes the existing workshop without restarting or replaying its introduction.
    game.Story=new WorkshopStory.State();game.ActiveOrder=null;game.NewOrder();foreach(var item in controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))controller.Attach(item);game.Testing.Restore(controller.Items.Where(x=>x.Stage==4).Select(x=>x.Id).ToArray(),null,true);game.Tools.Tightened=15;
    Check(game.FinishDelivery()&&game.Story.Has(WorkshopStory.LegacyEvent)&&game.WaitingForOrder&&game.Story.openingRead,"Finishing a legacy order unlocks varied jobs without replacing the existing workshop");
    File.WriteAllLines("Logs/variety-checks.txt",report);Debug.Log("COZY_VARIETY_VERIFIED "+report.Count);
   }finally{game.VerificationSavePath=null;game.Painter.ResetPaint();EditorSceneManager.OpenScene(Scene);}
  }
  static void Capture(WorkshopController controller,string path){
   controller.Game.Experience.SendMessage("Update");controller.Game.Shop.SendMessage("Update");controller.Game.Menu.ToastLabel.gameObject.SetActive(false);controller.SendMessage("LateUpdate");
   var camera=controller.ViewCamera;var target=new RenderTexture(1920,1080,24){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=16f/9;controller.UpdateCamera();
   foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
   Canvas.ForceUpdateCanvases();var layout=Object.FindFirstObjectByType<WorkshopStatusLayout>();if(layout)layout.Arrange();Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(image);Object.DestroyImmediate(target);
  }
 }
}
