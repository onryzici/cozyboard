using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
 public sealed class WorkshopVarietySmoke:MonoBehaviour {
  readonly List<string> results=new();string report;WorkshopGameMode game;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){if(Environment.GetCommandLineArgs().Contains("--cozy-variety-smoke"))new GameObject("Variety verification").AddComponent<WorkshopVarietySmoke>();}
  void Check(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);}
  IEnumerator Start(){
   var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--cozy-variety-report");report=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.temporaryCachePath,"variety-smoke.txt");Directory.CreateDirectory(Path.GetDirectoryName(report));
   yield return null;yield return null;var run=Run();
   while(true){bool more;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){results.Add("FAIL "+e);File.WriteAllLines(report,results);Application.Quit(1);yield break;}if(!more)break;yield return current;}
   File.WriteAllLines(report,results);Application.Quit(0);
  }
  void Fits(Transform root){Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();var rect=text.rectTransform.rect;bool within=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible).All(x=>x.bottomLeft.x>=rect.xMin-1&&x.topRight.x<=rect.xMax+1&&x.bottomLeft.y>=rect.yMin-1&&x.topRight.y<=rect.yMax+1);Check(!text.isTextOverflowing&&within,"Runtime variety text fits: "+text.name);}}
  IEnumerator Run(){
   game=FindAnyObjectByType<WorkshopGameMode>();var controller=game.Controller;
   Check(game.SavePath==Path.Combine(Application.temporaryCachePath,"variety-smoke-save.json"),"Variety verification cannot overwrite the user's save");
   game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.Shop.OpenOrders();yield return null;
   Fits(GameObject.Find("Available orders").transform);GameObject.Find("Available orders/Offer 0/Accept offer").GetComponent<Button>().onClick.Invoke();yield return null;
   Check(game.IsRepair&&game.Completed&&game.TestCount==3,"Repair offer button opens an assembled keyboard with three test targets");Fits(game.Menu.OrderPanel.transform);game.Menu.ClosePanels();yield return null;
   GameObject.Find("Visit repair desk").GetComponent<Button>().onClick.Invoke();
   Check(game.RepairBench.Moving&&game.Experience.Blocking,"Station travel blocks product input during the camera movement");
   for(float deadline=Time.realtimeSinceStartup+3;game.RepairBench.Moving&&Time.realtimeSinceStartup<deadline;)yield return null;
   Check(game.RepairBench.AtRepair&&!game.RepairBench.Moving&&controller.ViewTarget==WorkshopRepairBench.Origin,"Smooth camera travel settles on repair workbench");
   yield return null;Fits(GameObject.Find("Repair service note").transform);Fits(GameObject.Find("Workstation navigation").transform);
   var cap=controller.Lookup[game.Repair.capId];var sw=controller.Lookup[game.Repair.switchId];
   game.Tools.TrySelect(controller.Lookup["KeyPuller"]);Check(!game.Tools.TryRemove(cap),"Puller cannot remove the fault before diagnosis");game.Tools.Deselect();game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return null;
   Check(game.Repair.diagnosed&&game.Testing.Count==2,"Real key presses diagnose one faulty neighbour");Fits(GameObject.Find("Keyboard test card").transform);game.Testing.End();
   int money=game.Shop.Data.credits;int kits=game.Shop.Data.stock.Sum();Check(game.Shop.Purchase(3)&&game.Shop.Data.credits==money-8,"Repair catalogue sells one spare switch instead of a full set");game.Shop.Close();
   game.Tools.TrySelect(controller.Lookup["KeyPuller"]);Check(game.Tools.TryRemove(cap),"Keycap puller begins the diagnosed cap removal");
   for(float deadline=Time.realtimeSinceStartup+4;game.Tools.Busy&&Time.realtimeSinceStartup<deadline;)yield return null;
   Check(!game.Tools.Busy&&!cap.Fitted,"Animated cap removal completes");game.Tools.Deselect();game.TrySaveQuiet();game.LoadProgress();
   cap=controller.Lookup[game.Repair.capId];sw=controller.Lookup[game.Repair.switchId];Check(!cap.Fitted&&game.Repair.diagnosed&&game.TestKeys.Length==3,"Mid-repair reload keeps the missing cap and diagnosis");
   game.Tools.TrySelect(controller.Lookup["Tweezers"]);Check(game.Tools.TryRemove(sw),"Switch puller begins removal of the faulty switch");
   for(float deadline=Time.realtimeSinceStartup+4;game.Tools.Busy&&Time.realtimeSinceStartup<deadline;)yield return null;
   Check(!game.Tools.Busy&&!sw.Fitted,"Animated switch removal completes");game.Tools.Deselect();
   Check(cap.Visual.enabled&&cap.transform.position.x>12,"Recovered cap stays visible on repair tray while switch is removed");
   game.TrySaveQuiet();game.LoadProgress();
   Check(game.RepairBench.AtRepair&&game.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==60,"Native repair reload keeps sixty intact caps and the repair station");
   Check(!game.CanInstall(sw),"Native sealed spare cannot reuse the removed switch");
   Check(game.RepairBench.OpenPartInspection(),"Native old switch inspection opens");game.RepairBench.TurnInspectedSwitch();yield return new WaitForSeconds(.55f);Fits(GameObject.Find("Repair part inspection").transform);game.RepairBench.ClosePartInspection();
   Check(game.RepairBench.OpenReplacementPacket(),"Native sealed packet starts opening");yield return new WaitForSeconds(.55f);
   Check(game.Repair.spareOpened&&!game.RepairBench.Unpacking,"Packet opening animation completes before the spare becomes available");
   Check(game.RepairBench.OpenPartInspection(true),"Native fresh switch pin inspection opens");game.RepairBench.TurnInspectedSwitch();yield return new WaitForSeconds(.55f);Fits(GameObject.Find("Repair part inspection").transform);game.RepairBench.ClosePartInspection();
   game.TrySaveQuiet();game.LoadProgress();Check(game.Repair.spareOpened&&game.Repair.spareChecked&&game.Repair.oldSwitchInspected,"Native reload preserves both inspections and the opened spare");
   Check(controller.PaintAt(3,controller.Lookup["Case"].transform.TransformPoint(sw.Slot)),"Replacement switch installs through normal placement");yield return new WaitForSeconds(.75f);
   Check(controller.PaintAt(4,controller.Lookup["Case"].transform.TransformPoint(cap.Slot)),"Recovered cap installs without buying another cap set");yield return new WaitForSeconds(.75f);
   Check(game.RepairReady&&game.Shop.Data.looseParts[3]==0&&game.Shop.Data.stock.Sum()==kits,"Only one purchased spare was consumed during repair");
   game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return null;Check(game.Testing.Passed&&game.Testing.Count==3,"Three retests complete the repair");game.Testing.End();
   game.Deliver();yield return null;for(int step=1;step<=3;step++){game.Experience.AdvancePacking();yield return new WaitForSeconds(.95f);}game.Experience.AdvancePacking();yield return new WaitForSeconds(2);
   Check(game.WaitingForOrder&&game.LastDelivery.kind=="repair"&&game.LastDelivery.reward==90,"Animated repair shipment pays the short-job reward");
   Check(game.LastDelivery.pending&&game.Shop.UnreadMail==1,"Opening new offers does not silently read the repair letter");
   GameObject.Find("Available orders/Offer 1/Accept offer").GetComponent<Button>().onClick.Invoke();yield return null;
   Check(game.IsMacro&&game.KeyCount==6&&game.PartCount==14,"Macro offer button opens the six-key product");Fits(game.Menu.OrderPanel.transform);game.Menu.ClosePanels();
   foreach(var item in game.ProductItems.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))Check(controller.PaintAt(item.Stage,controller.Lookup["Case"].transform.TransformPoint(item.Slot)),"Native macro placement: "+item.Id);yield return new WaitForSeconds(.8f);
   Check(game.Completed&&game.Shop.Data.looseParts[3]==55&&game.Shop.Data.looseParts[6]==55,"Macro assembly uses six parts from each set and keeps the rest");
   Check(controller.Items.Where(x=>x.Stage>0&&!game.BelongsToProduct(x)).All(x=>!x.Visual.enabled&&!x.Hitbox.enabled),"Unused keyboard keys are neither visible nor clickable");
   for(int i=0;i<4;i++){game.Tools.TakeScrew();Check(game.Tools.UseScrew(i),"Compact case screw starts: "+i);for(float deadline=Time.realtimeSinceStartup+4;game.Tools.Busy&&Time.realtimeSinceStartup<deadline;)yield return null;Check(!game.Tools.Busy,"Compact case screw finishes: "+i);}Check(game.Tools.Tightened==15,"All compact case screws fasten at the macro pad corners");
   game.Testing.Begin();yield return null;Check(GameObject.Find("Test keyboard inset").GetComponentsInChildren<Button>().Length==6,"Native macro test contains only six keys");
   for(int i=0;i<6;i++){
    GameObject.Find("Macro function "+i).GetComponent<Button>().onClick.Invoke();yield return null;
    Fits(GameObject.Find("Macro function picker").transform);GameObject.Find("Choose "+WorkshopGameMode.DesiredFunctions[i]).GetComponent<Button>().onClick.Invoke();game.Press(controller.Lookup[$"Keycap_{i+1:00}"]);yield return null;
   }
   Check(game.Testing.Passed&&game.MacroReady&&game.Testing.Count==6,"Six function picker selections and presses validate the requested shortcuts");Fits(GameObject.Find("Keyboard test card").transform);game.Testing.End();
   game.TrySaveQuiet();game.LoadProgress();Check(game.IsMacro&&game.Testing.Passed&&game.MacroReady,"Native reload preserves compact product and six shortcut results");
   game.Deliver();yield return null;var carton=GameObject.Find("Shipping carton").transform;Check(carton.Find("Bottom").localScale.x<6,"Macro pad gets a smaller shipping carton");
   for(int step=1;step<=3;step++){game.Experience.AdvancePacking();yield return new WaitForSeconds(.95f);}game.Experience.AdvancePacking();yield return new WaitForSeconds(2);
   Check(game.WaitingForOrder&&game.LastDelivery.kind=="macro"&&game.Story.Has(WorkshopStory.MacroEvent)&&game.Story.Has(WorkshopStory.RepairEvent),"Macro shipment preserves both character story milestones");
   int paid=game.Shop.Data.credits;game.Deliver();Check(game.Shop.Data.credits==paid,"Repeated delivery cannot duplicate a small-job reward");
   game.Shop.OpenMail();yield return null;Check(GameObject.Find("Subject").GetComponent<TMP_Text>().text.Contains("Altı"),"Macro receipt has a product-specific subject");Fits(GameObject.Find("Customer mailbox").transform);game.Shop.ReadMail(0);Check(GameObject.Find("Subject").GetComponent<TMP_Text>().text.Contains("Eski"),"Repair letter remains archived after macro delivery");game.Shop.Close();
   game.StoryUI.OpenNotebook();yield return null;Fits(GameObject.Find("Notebook paper").transform);game.StoryUI.CloseNotebook();
  }
 }
}
