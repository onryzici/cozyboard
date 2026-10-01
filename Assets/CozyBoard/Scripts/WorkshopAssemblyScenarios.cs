using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    public static class WorkshopAssemblyScenarios {
        static IEnumerator Wait(Func<bool> pending,float seconds=5){float end=Time.realtimeSinceStartup+seconds;while(pending()&&Time.realtimeSinceStartup<end)yield return null;}
        public static IEnumerator Run(WorkshopGameMode g,Action<bool,string> check){
            if(string.IsNullOrEmpty(g.VerificationSavePath))throw new Exception("Assembly scenarios require an isolated save");
            g.Experience.NewGame();yield return null;
            check(g.StoryUI.Open&&g.WaitingForOrder,"New game opens the introduction before any order");
            GameObject.Find("Leave notebook").GetComponent<Button>().onClick.Invoke();yield return null;
            check(g.Story.openingRead&&g.Shop.IsOpen,"Reading the introduction opens the first offer");
            GameObject.Find("Available orders/Offer 0/Accept offer").GetComponent<Button>().onClick.Invoke();yield return null;
            check(g.ActiveOrder?.definitionId=="ece-first"&&g.Installed==0,"First keyboard starts with zero installed parts");
            g.Experience.FinishTutorial();g.Menu.ClosePanels();g.Shop.Close();
            var c=g.Controller;Vector3 At(WorkshopItem part)=>c.Lookup["Case"].transform.TransformPoint(part.Slot);
            var cap=c.Lookup["Keycap_00"];var sw=c.Lookup["Switch_00"];var pcb=c.Lookup["PCB"];var plate=c.Lookup["Plate"];
            check(!c.PaintAt(4,At(cap))&&!c.PaintAt(3,At(sw))&&!c.PaintAt(2,At(plate)),"All three later stages reject out-of-order placement");
            check(!g.Tools.UseScrew(-1)&&!g.Tools.UseScrew(4)&&!g.Tools.UseScrew(0),"Invalid screws and screws before internal assembly are rejected");
            yield return new WaitForSecondsRealtime(.65f);
            Vector3 source=pcb.transform.position;Quaternion angle=pcb.transform.rotation;
            c.BeginDrag(pcb,c.ViewCamera.WorldToScreenPoint(source));check(c.Dragged==pcb,"A physical PCB can be picked up");
            c.MoveDrag(c.ViewCamera.WorldToScreenPoint(new Vector3(-9,0,-4)));c.CancelDrag();
            check(!c.Dragged&&!pcb.Fitted&&Vector3.Distance(source,pcb.transform.position)<.001f&&Quaternion.Angle(angle,pcb.transform.rotation)<.01f,"Cancel drag restores the PCB position and orientation");
            c.BeginDrag(pcb,c.ViewCamera.WorldToScreenPoint(source));c.MoveDrag(c.ViewCamera.WorldToScreenPoint(new Vector3(-9,0,-4)));c.FinishDrag();
            check(!pcb.Fitted&&Vector3.Distance(source,pcb.transform.position)<.001f,"Dropping a PCB outside the case returns it to supply");
            c.BeginDrag(pcb,c.ViewCamera.WorldToScreenPoint(source));g.Menu.OrderButton.onClick.Invoke();
            check(!c.Dragged&&g.Menu.OrderPanel.activeSelf&&!pcb.Fitted,"Opening the order while carrying a PCB cancels the held piece");g.Menu.ClosePanels();
            check(c.PaintAt(1,At(pcb))&&g.Installed==1,"PCB installs through the normal placement path");
            check(!c.PaintAt(1,At(pcb))&&g.Installed==1,"Double placement cannot consume a second PCB");
            g.SaveProgress();g.LoadProgress();check(pcb.Fitted&&g.Installed==1&&g.CurrentStage==2,"PCB-only save resumes at the plate stage");
            int reservedStock=g.Shop.Data.stock[g.Shop.Data.selected[0]];g.RestartActiveWork();check(g.Installed==0&&g.Shop.HasSupply(1)&&g.Shop.Data.stock[g.Shop.Data.selected[0]]==reservedStock,"Restart retains the already paid assembly materials");check(c.PaintAt(1,At(pcb)),"Restarted keyboard reinstalls its reserved PCB without another purchase");
            check(c.PaintAt(2,At(plate))&&g.CurrentStage==3,"Plate unlocks switches");yield return new WaitForSecondsRealtime(.6f);
            g.Undo();check(!plate.Fitted&&pcb.Fitted&&g.CurrentStage==2,"Undoing the plate preserves the PCB and restores its stage");
            check(c.PaintAt(2,At(plate)),"A recovered plate can be reinstalled without buying another kit");
            var switches=g.ProductItems.Where(x=>x.Stage==3).OrderBy(x=>x.Id).ToArray();
            for(int i=0;i<switches.Length;i++){
                check(c.PaintAt(3,At(switches[i])),"Full keyboard switch "+i+" installs");
                if(i==29){g.SaveProgress();g.LoadProgress();check(g.ProductItems.Count(x=>x.Stage==3&&x.Fitted)==30&&g.CurrentStage==3,"Thirty-switch save keeps stage and exact part count");}
                if(i%8==0)yield return null;
            }
            check(g.CurrentStage==4&&g.Installed==63,"Sixty-one switches unlock all caps");yield return new WaitForSecondsRealtime(.65f);
            var caps=g.ProductItems.Where(x=>x.Stage==4).OrderBy(x=>x.Id).ToArray();var wide=caps.First(x=>x.Label.Contains("Space"));var token=g.SupplyItem(4);
            var tokenMesh=token.Visual.GetComponent<MeshFilter>().sharedMesh;c.BeginDrag(token,c.ViewCamera.WorldToScreenPoint(token.transform.position));c.MoveDrag(c.ViewCamera.WorldToScreenPoint(At(wide)));
            check(c.SnapCandidate(token)==wide,"A stock cap previews the wide spacebar destination");c.CancelDrag();
            check(!wide.Fitted&&!token.Fitted&&token.Visual.GetComponent<MeshFilter>().sharedMesh==tokenMesh,"Cancelling a wide-key preview restores the original supply mesh");
            for(int i=0;i<caps.Length;i++){
                check(c.PaintAt(4,At(caps[i])),"Full keyboard cap "+i+" installs");
                if(i==29){g.SaveProgress();g.LoadProgress();check(g.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==30&&g.CurrentStage==4,"Thirty-cap save keeps every mounted switch and cap");}
                if(i%8==0)yield return null;
            }
            yield return new WaitForSecondsRealtime(.7f);check(g.Completed&&g.Installed==124,"A full keyboard completes all 124 normal placements");
            check(g.ProductItems.All(x=>x.Visual.transform.localPosition.sqrMagnitude<.00001f),"All installation animations settle at their mounting positions");
            c.BeginDrag(cap,c.ViewCamera.WorldToScreenPoint(cap.transform.position));check(!c.Dragged&&cap.Fitted,"Clicking a fitted cap presses it without removing it");
            g.Testing.Begin();foreach(var key in g.TestKeys)g.Press(key);yield return null;check(g.Testing.Count==61&&g.Testing.Passed,"All sixty-one distinct keys pass the first keyboard test");g.Testing.End();
            check(!g.Tools.TryRemove(cap),"A cap cannot be removed without the correct tool selected");g.Tools.TrySelect(c.Lookup["Tweezers"]);check(!g.Tools.TryRemove(sw),"A switch cannot be pulled through its cap");g.Tools.Deselect();
            g.Tools.TrySelect(c.Lookup["KeyPuller"]);check(g.Tools.TryRemove(cap),"Passed cap can be physically removed");check(!g.Tools.TryRemove(cap),"Repeated pull input cannot create another tool operation");yield return Wait(()=>g.Tools.Busy);g.Tools.Deselect();
            check(!g.Testing.Passed&&g.Testing.Count==60&&!cap.Fitted,"Pulling a tested cap invalidates its test result");
            check(c.PaintAt(4,At(cap)),"Recovered tested cap reinstalls from supply");yield return new WaitForSecondsRealtime(.65f);
            g.Testing.Begin();g.Press(cap);yield return null;check(g.Testing.Passed,"Retesting the replaced cap completes the keyboard again");g.Testing.End();
            g.Deliver();check(g.Tools.Busy&&!g.Experience.Packing,"Delivery before fastening screws begins turning the keyboard over");yield return Wait(()=>g.Tools.Busy);check(g.Tools.KeyboardUnderbody&&g.Tools.Active&&g.Tools.Selected=="Screwdriver","Bottom view selects the required screwdriver after the flip finishes");
            for(int i=0;i<4;i++){check(g.Tools.TakeScrew(),"Case screw "+i+" is taken from the dish");check(g.Tools.UseScrew(i),"Case screw "+i+" starts");check(!g.Tools.UseScrew((i+1)%4),"Screw operation rejects another simultaneous screw");yield return Wait(()=>g.Tools.Busy);check(!g.Tools.Busy&&(g.Tools.Tightened&(1<<i))!=0,"Case screw "+i+" finishes");}
            check(g.Tools.Tightened==15,"All four full-keyboard screws are fastened");g.Tools.Deselect();
            var saved=g.SerializeProgress();var paint=g.Painter;g.Menu.SelectTool(3);paint.Edit(wide);yield return Wait(()=>paint.PreparingSurface,12);check(paint.Editing&&!paint.PreparingSurface,"Spacebar opens its prepared painting studio");
            paint.CloseEditor();WorkshopPaintVerify.Run(g,check);g.RestoreProgress(saved);g.Menu.ClosePanels();
            check(g.Testing.Passed&&g.Tools.Tightened==15,"Returning from paint regression restores tested and fastened keyboard");
            Vector3 deskPose=g.Tools.SavedKeyboardPosition;int credits=g.Shop.Data.credits;g.Deliver();check(g.Experience.PackStep==1&&!g.Tools.KeyboardUnderbody,"Completed keyboard returns to its top pose before entering the carton stage");
            g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.15f);g.Experience.CancelPacking();yield return new WaitForSecondsRealtime(.85f);
            check(!g.Experience.Packing&&g.Completed&&Vector3.Distance(deskPose,c.Lookup["Case"].transform.position)<.001f&&g.Shop.Data.credits==credits,"Cancelling a moving carton step restores pose without payment");
            g.Deliver();for(int step=1;step<=3;step++){g.Experience.AdvancePacking();g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.95f);check(g.Experience.PackStep==step+1,"Double packing input advances stage "+step+" only once");}
            g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(1.3f);check(g.WaitingForOrder&&g.Story.FirstDelivered&&g.LastDelivery!=null&&g.DeliveryMail.Count==1,"Keyboard shipment unlocks new jobs and creates one receipt");
            int paid=g.Shop.Data.credits;g.Deliver();check(g.Shop.Data.credits==paid&&g.DeliveryMail.Count==1,"Repeated shipment cannot duplicate keyboard reward or mail");
        }
    }
}
