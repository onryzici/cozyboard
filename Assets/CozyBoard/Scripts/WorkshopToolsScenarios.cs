using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CozyBoard {
 public static class WorkshopToolsScenarios {
  static IEnumerator Wait(Func<bool> pending){float end=Time.realtimeSinceStartup+6;while(pending()&&Time.realtimeSinceStartup<end)yield return null;}
  static IEnumerator Move(Mouse m,Vector2 p,bool pressed=false){var state=new MouseState{position=p};if(pressed)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(m,state);yield return new WaitForSecondsRealtime(.12f);}
  static IEnumerator Click(Mouse m,Vector2 p){yield return Move(m,p);yield return Move(m,p,true);yield return Move(m,p);}
  public static IEnumerator Run(WorkshopGameMode g,Action<bool,string> check){
   if(string.IsNullOrEmpty(g.VerificationSavePath))throw new Exception("Tool QA requires an isolated save");
   g.StoryUI.Hide();g.StartStory();g.Story.openingRead=true;g.Story.Record(WorkshopStory.FirstEvent);g.Experience.Continue();g.Shop.Close();g.Menu.ClosePanels();g.Menu.SelectTool(0);
   check(g.AcceptOrder(WorkshopStory.FirstMacro.Id),"A fresh macro order provides the screw tray fixture");g.Menu.ClosePanels();g.Shop.Close();yield return null;
   var c=g.Controller;var tools=g.Tools;var board=c.Lookup["Case"].transform;var driver=c.Lookup["Screwdriver"];Vector3 home=driver.transform.position;Quaternion rotation=driver.transform.rotation;
   check(Resources.Load<Mesh>("WorkshopHands/GRIP")&&Resources.Load<Mesh>("WorkshopHands/PINCH")&&Resources.Load<Mesh>("WorkshopHands/DRIVER-GRIP"),"Screwdriver power grip, puller grip and screw pinch ship as distinct native meshes");
   check(tools.Mounted==0&&tools.LooseScrewCount==4&&!board.Find("Case screw 0").gameObject.activeSelf,"Four loose screws start in the dish and the product sockets are empty");
   check(!tools.TakeScrew(),"Loose screws cannot install before the internal parts");
   foreach(int stage in new[]{1,2}){var p=c.Lookup[stage==1?"PCB":"Plate"];check(c.PaintAt(stage,board.TransformPoint(p.Slot)),"Internal part "+stage+" installs normally");yield return new WaitForSecondsRealtime(.65f);}
   check(!tools.UseScrew(0)&&!tools.Busy,"An empty socket cannot tighten without taking a screw");
   check(!tools.TakeScrew(),"Top-side pickup asks the player to turn the keyboard over first");Vector3 top=board.position;Quaternion topRotation=board.rotation;
   var prior=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>("CozyBoard glove QA");var others=InputSystem.devices.OfType<Mouse>().Where(x=>x!=mouse&&x.enabled).ToArray();foreach(var other in others)InputSystem.DisableDevice(other);mouse.MakeCurrent();
   try{
    var flip=(RectTransform)GameObject.Find("Turn keyboard underside").transform;Canvas.ForceUpdateCanvases();yield return Click(mouse,RectTransformUtility.WorldToScreenPoint(null,flip.TransformPoint(flip.rect.center)));yield return Wait(()=>tools.Busy);
    check(tools.KeyboardUnderbody&&Vector3.Dot(board.up,Vector3.up)<-.99f,"The actual flip button exposes the keyboard underside");
    check(!g.CanInstall(c.Lookup["Switch_01"])&&g.Stock.Length==0&&!tools.TryRemove(c.Lookup["PCB"]),"Bottom view prevents top assembly, supply pickup and extraction");
    check(board.Find("Case screw 0").localPosition.y<c.Lookup["Case"].BoundsCenter.y&&Vector3.Dot(board.Find("Case screw 0").up,Vector3.up)>.99f,"Bottom screw heads face the player and sit beneath the case");
    var dish=GameObject.Find("Stoneware screw bowl").GetComponent<Collider>();
    yield return Click(mouse,c.ViewCamera.WorldToScreenPoint(dish.bounds.center));
    check(tools.HoldingScrew&&tools.LooseScrewCount==3&&tools.Selected=="Screwdriver"&&tools.HandVisible,"Clicking the physical bowl picks one screw and holds the driver in a glove");
    check(!tools.TakeScrew()&&tools.LooseScrewCount==3,"Repeated pickup cannot take or lose a second screw");
    check(!driver.Hitbox.enabled&&Vector3.Distance(home,driver.transform.position)>.4f,"Held tool leaves its mat and cannot intercept the product raycast");
    var first=driver.transform.position;yield return Move(mouse,c.ViewCamera.WorldToScreenPoint(new Vector3(1,1,0)));yield return new WaitForSecondsRealtime(.25f);
    check(Vector3.Distance(first,driver.transform.position)>.3f,"The glove and held driver follow the pointer across the workbench");
    string dir=Path.GetDirectoryName(UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>().ReportPath);WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(dir,"tools-native-pickup.png"));yield return new WaitForSecondsRealtime(.2f);
    string saved=g.SerializeProgress();g.SaveProgress();g.LoadProgress();
    check(g.Installed==2&&tools.Mounted==0&&tools.LooseScrewCount==4&&!tools.HoldingScrew&&!tools.Active,"Reload returns a carried screw to the bowl without mounting or consuming it");
    check(tools.KeyboardUnderbody&&Vector3.Distance(board.position,top+Vector3.up*1.35f)<.001f,"Bottom-side save restores its original top pose and exactly one lift");
    for(int i=0;i<4;i++){
     yield return Click(mouse,c.ViewCamera.WorldToScreenPoint(dish.bounds.center));check(tools.HoldingScrew,"Physical bowl pickup "+i+" succeeds");
     Vector3 socket=board.Find("Case screw "+i).position;
     yield return Click(mouse,c.ViewCamera.WorldToScreenPoint(socket));check(tools.Busy,"Physical empty-socket click "+i+" starts screw placement and torque");
     if(i==0){check(!tools.TakeScrew(),"A second pickup is rejected while placing a screw");g.Shop.Open();check(!g.Shop.IsOpen,"Laptop cannot interrupt the screw placement hand");}
     yield return Wait(()=>tools.Busy);
     check((tools.Mounted&(1<<i))!=0&&(tools.Tightened&(1<<i))!=0&&tools.HandVisible&&tools.Active&&!tools.HoldingScrew,"Screw "+i+" mounts and tightens while the driver stays in the glove");
     if(i==0){g.SaveProgress();g.LoadProgress();check(tools.Mounted==1&&tools.Tightened==1&&tools.LooseScrewCount==3,"Partial screw save restores one mounted screw and three loose screws");}
    }
    check(tools.LooseScrewCount==0&&!tools.TakeScrew(),"An empty bowl cannot generate extra screws");
    yield return Move(mouse,c.ViewCamera.WorldToScreenPoint(new Vector3(2,1,0)));yield return new WaitForSecondsRealtime(.3f);WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(dir,"tools-native-held.png"));yield return new WaitForSecondsRealtime(.2f);
    check(tools.UseScrew(0),"A mounted screw can still be loosened without another pickup");yield return Wait(()=>tools.Busy);check(tools.Mounted==15&&tools.Tightened==14&&tools.LooseScrewCount==0,"Loosening keeps the screw seated and preserves the tray inventory");
    check(tools.UseScrew(0),"The same seated screw can be retightened");yield return Wait(()=>tools.Busy);
    g.Menu.SettingsButton.onClick.Invoke();yield return null;
    check(!tools.HandVisible&&!tools.Active&&Vector3.Distance(driver.transform.position,home)<.001f&&Quaternion.Angle(driver.transform.rotation,rotation)<.01f,"Opening settings puts the tool back in its exact original mat pose");g.Menu.ClosePanels();
    check(!tools.KeyboardUnderbody&&Vector3.Distance(board.position,top)<.001f&&Quaternion.Angle(board.rotation,topRotation)<.01f,"Opening settings safely restores the keyboard to its original top pose");
    foreach(string id in new[]{"KeyPuller","Tweezers"}){var item=c.Lookup[id];var start=item.transform.position;check(tools.TrySelect(item)&&tools.HandVisible,"The glove can hold "+id);tools.Deselect();check(!tools.HandVisible&&Vector3.Distance(item.transform.position,start)<.001f,"Putting down "+id+" restores its exact desk pose");}
    g.RestoreProgress(saved);check(tools.Mounted==0&&tools.LooseScrewCount==4&&!tools.Active,"Restoring a pre-installation save removes mounted screws and restores the full bowl");
    tools.TakeScrew();g.Menu.SelectTool(1);check(!tools.HoldingScrew&&tools.LooseScrewCount==4&&!tools.HandVisible,"Switching to move mode returns the unused screw and both hands cleanly");g.Menu.SelectTool(0);
    check(!tools.KeyboardUnderbody&&Vector3.Distance(board.position,top)<.001f,"Moving mode restores the top surface before dragging the case");
    var data=JsonUtility.FromJson<WorkshopGameMode.SaveData>(saved);data.screwTrayVersion=0;data.keyboardUnderbody=false;data.tightened=1;g.RestoreProgress(JsonUtility.ToJson(data));
    check(tools.Mounted==15&&tools.Tightened==1&&tools.LooseScrewCount==0,"Older saves retain their already seated four screws without creating duplicates");
    check(!tools.KeyboardUnderbody&&Vector3.Distance(board.position,top)<.001f,"Legacy saves with no underside flag keep their normal top pose");
   }finally{tools.Deselect();if(mouse.added)InputSystem.RemoveDevice(mouse);foreach(var other in others)if(other.added)InputSystem.EnableDevice(other);if(prior!=null&&prior.added)prior.MakeCurrent();}
  }
 }
}
