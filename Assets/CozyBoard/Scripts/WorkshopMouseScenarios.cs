using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CozyBoard {
    // Runs only in the explicit QA harness with an isolated workshop save.
    public static class WorkshopMouseScenarios {
        static IEnumerator Wait(Func<bool> pending,float seconds=8){float end=Time.realtimeSinceStartup+seconds;while(pending()&&Time.realtimeSinceStartup<end)yield return null;}
        static Vector2 Center(RectTransform r){Canvas.ForceUpdateCanvases();return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static IEnumerator Move(Mouse mouse,Vector2 p,MouseButton? button=null,Vector2? scroll=null){var s=new MouseState{position=p,scroll=scroll??Vector2.zero};if(button.HasValue)s=s.WithButton(button.Value);InputSystem.QueueStateEvent(mouse,s);yield return new WaitForSecondsRealtime(.12f);}
        static IEnumerator Click(Mouse mouse,Vector2 p,MouseButton button=MouseButton.Left){yield return Move(mouse,p);yield return Move(mouse,p,button);yield return Move(mouse,p);}
        static bool boardVisible(WorkshopGameMode g)=>g.Controller.Lookup["Case"].gameObject.activeInHierarchy;
        public static IEnumerator Run(WorkshopGameMode g,Action<bool,string> check){
            if(string.IsNullOrEmpty(g.VerificationSavePath))throw new Exception("Mouse QA requires an isolated save");
            var c=g.Controller;Mesh originalCase=null;
            g.StartStory();g.Story.openingRead=true;g.Story.Record(WorkshopStory.FirstEvent);g.Experience.Continue();g.StoryUI.Hide();g.Menu.ClosePanels();g.Shop.Close();yield return null;
            originalCase=c.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh;check(!boardVisible(g)&&!g.Shop.IsOpen,"Idle desk exposes an open laptop instead of a keyboard casing");
            yield return new WaitForSecondsRealtime(.9f);WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>().ReportPath),"idle-native-laptop.png"));yield return new WaitForSecondsRealtime(.2f);
            g.Experience.BeginInspection();check(!g.Experience.Inspecting,"An idle workbench cannot inspect a nonexistent product");
            check(g.AcceptOrder(WorkshopStory.FirstMouse.Id),"Mouse offer is independently selectable");g.Menu.ClosePanels();
            var product=g.MouseProduct;var board=c.Lookup["Case"].transform;Vector3 At(WorkshopItem p)=>board.TransformPoint(p.Slot);
            check(g.IsMouse&&g.PartCount==18&&g.TestCount==8&&g.CurrentStage==1,"Mouse uses its own eighteen parts and eight functional checks");
            check(g.ProductItems.Where(p=>p.Stage>0).All(p=>p.Id.StartsWith("Mouse_")),"No keyboard parts are included in the mouse");
            check(c.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh==product.BaseMesh,"Mouse replaces the keyboard chassis with its contoured base");
            check(!g.CanInstall(c.Lookup["PCB"])&&!g.CanInstall(c.Lookup["Mouse_LeftButton"]),"Keyboard circuitry and premature covers cannot install");
            check(!c.PaintAt(1,At(c.Lookup["Mouse_PCB"])),"No mouse components can be installed before buying a kit");
            int balance=g.Shop.Data.credits;var keyboardStock=(int[])g.Shop.Data.stock.Clone();
            check(g.Shop.PurchaseMouseKit()&&g.Shop.Data.credits==balance-120&&g.Shop.Data.mouseKits==1,"Mouse kit charges exactly 120 Tık");
            check(g.Shop.RefundMouseKit()&&g.Shop.Data.credits==balance&&!g.Shop.RefundMouseKit(),"Only an unopened mouse kit refunds once");
            string path=g.VerificationSavePath,blocked=path+".blocked";File.WriteAllText(blocked,"blocker");g.VerificationSavePath=Path.Combine(blocked,"save.json");
            check(!g.Shop.PurchaseMouseKit()&&g.Shop.Data.credits==balance&&g.Shop.Data.mouseKits==0,"Failed kit save restores credits and stock atomically");g.VerificationSavePath=path;File.Delete(blocked);
            check(g.Shop.PurchaseMouseKit()&&g.Shop.SelectMouseColor(0),"Fresh mouse kit accepts the workshop cream palette");
            check(c.PaintAt(1,At(c.Lookup["Mouse_PCB"]))&&g.CurrentStage==2,"Mouse PCB installs through normal socket placement");
            check(product.State.kitReserved&&g.Shop.Data.mouseKits==0&&g.Shop.Data.stock.SequenceEqual(keyboardStock),"One mouse kit is reserved without consuming keyboard stock");
            check(!g.Shop.RefundMouseKit()&&!g.Shop.SelectMouseColor(2),"Reserved kit cannot refund or silently recolor fitted parts");
            yield return new WaitForSecondsRealtime(.6f);g.Undo();check(g.Installed==0&&product.State.kitReserved&&g.Shop.HasSupply(1),"PCB undo keeps the paid mouse kit available");
            check(c.PaintAt(1,At(c.Lookup["Mouse_PCB"])),"Undone PCB reinstalls without another kit");yield return new WaitForSecondsRealtime(.6f);
            g.SaveProgress();g.LoadProgress();check(g.Installed==1&&product.State.kitReserved&&g.CurrentStage==2,"PCB save restores the mouse identity and reservation");
            foreach(string id in new[]{"Mouse_Sensor","Mouse_Lens","Mouse_Cable"}){check(c.PaintAt(2,At(c.Lookup[id])),id+" fits its dedicated socket");yield return new WaitForSecondsRealtime(.6f);}
            check(g.CurrentStage==3&&!g.Tools.UseScrew(0),"Internal assembly must precede bottom screws");
            foreach(string id in new[]{"Mouse_LeftSwitch","Mouse_RightSwitch","Mouse_SideSwitch_0","Mouse_SideSwitch_1","Mouse_Wheel"}){check(c.PaintAt(3,At(c.Lookup[id])),id+" fits its dedicated socket");yield return new WaitForSecondsRealtime(.6f);}
            check(g.CurrentStage==4&&g.CanInstall(c.Lookup["Mouse_Shell"])&&product.State.tunedMask==3,"Balanced default clicks allow closing the housing without compulsory tuning");
            for(int side=0;side<2;side++){
                check(product.Tune(side)&&product.Busy&&!product.Tune(side),"Click tuning starts once for side "+side);
                g.Shop.Open();g.Experience.ShowMain();g.RepairBench.Visit(true);g.Undo();
                check(!g.Shop.IsOpen&&!g.Experience.MainVisible&&!g.RepairBench.AwayFromProduct&&g.Installed==9,"Click adjustment blocks conflicting screens and undo");
                yield return Wait(()=>product.Busy);check(product.State.clickFeel[side]==2,"Optional click adjustment changes side "+side+" to crisp feel");product.Tune(side);yield return Wait(()=>product.Busy);product.Tune(side);yield return Wait(()=>product.Busy);check(product.State.clickFeel[side]==1,"Optional click cycling returns side "+side+" to balanced feel");
            }
            check(g.CurrentStage==4&&product.State.tunedMask==3,"Two independent adjustments unlock shell assembly");
            foreach(string id in new[]{"Mouse_Shell","Mouse_LeftButton","Mouse_RightButton","Mouse_SideButton_0","Mouse_SideButton_1"}){check(c.PaintAt(4,At(c.Lookup[id])),id+" closes on the continuous exterior");yield return new WaitForSecondsRealtime(.6f);}
            check(product.HardwareComplete&&g.Installed==14&&!product.Tune(0),"Closed housing retains both tuned click mechanisms");
            check(!g.Tools.UseScrew(0)&&!g.CanInstall(c.Lookup["Mouse_Skate_0"]),"Top view cannot fasten bottom screws or unpeeled feet");
            var shell=c.Lookup["Mouse_Shell"];var mesh=shell.Visual.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;float highest=vertices.Max(p=>p.y);int peak=Array.FindIndex(vertices,v=>v.y==highest);
            var point=vertices[peak];g.Painter.PaintAtSurface(shell,point,Vector3.up,new Color(.43f,.56f,.40f),.16f);
            check(g.CanPaint(shell)&&g.CanPaint(c.Lookup["Mouse_LeftButton"])&&!g.CanPaint(c.Lookup["Mouse_PCB"])&&g.Painter.PaintIds.Contains(shell.Id),"Shell and buttons accept painting through the existing surface system");
            g.Menu.SelectTool(3);g.Painter.Edit(shell);yield return Wait(()=>g.Painter.PreparingSurface,20);
            check(g.Painter.Editing&&!g.Painter.PreparingSurface&&!g.Painter.LegendButton.gameObject.activeSelf,"Curved mouse shell opens the paint studio without keyboard lettering controls");g.Painter.CloseEditor();g.Menu.SelectTool(0);
            Vector3 top=board.position;Quaternion rotation=board.rotation;product.ToggleUnderbody();
            check(product.Busy,"Turning the mouse uses an actual flip animation");g.SaveProgress();g.Testing.Begin();g.Undo();check(!g.Testing.Active&&g.Installed==14,"Mid-flip input cannot start tests or remove housing");yield return Wait(()=>product.Busy);
            check(product.State.underbody&&Vector3.Dot(board.up,Vector3.up)<-.99f,"Bottom view exposes the actual underside");
            g.SaveProgress();g.LoadProgress();check(product.State.underbody&&Vector3.Distance(board.position,top+Vector3.up*1.55f)<.002f&&g.Painter.PaintIds.Contains(shell.Id),"Underside reload keeps its pose and painted curved shell without a second lift");
            for(int i=0;i<4;i++){check(g.Tools.TakeScrew(),"Mouse screw "+i+" is taken from the dish");check(g.Tools.UseScrew(i)&&!g.Tools.UseScrew((i+1)%4),"Bottom screw "+i+" starts and excludes concurrent operation");yield return Wait(()=>g.Tools.Busy);check((g.Tools.Tightened&(1<<i))!=0,"Bottom screw "+i+" finishes");}
            check(g.Tools.Tightened==15&&g.Tools.Mounted==15&&g.Tools.LooseScrewCount==0&&g.Tools.Active,"All four mouse screws are physically fastened and the driver is still held");
            WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>().ReportPath),"mouse-native-tools.png"));yield return new WaitForSecondsRealtime(.2f);
            var footPrevious=Mouse.current;var footMouse=InputSystem.AddDevice<Mouse>("CozyBoard skate pointer QA");var footOthers=InputSystem.devices.OfType<Mouse>().Where(x=>x!=footMouse&&x.enabled).ToArray();foreach(var other in footOthers)InputSystem.DisableDevice(other);footMouse.MakeCurrent();
            try{
                int[] targets={3,0,2,1};
                for(int i=0;i<4;i++){
                    var source=g.Stock.Single();var foot=c.Lookup["Mouse_Skate_"+targets[i]];
                    check(source.Stage==5&&!g.CanInstall(source),"Only the next unprepared foot is presented");
                    yield return Click(footMouse,Center((RectTransform)GameObject.Find("Peel next mouse skate").transform));yield return Wait(()=>product.Busy);
                    check(c.Dragged==source&&!g.Tools.Active&&!g.Tools.HoldingScrew,"Actual prepare-foot button peels its backing, puts down the driver and carries one foot");
                    check(!product.Peel(source),"A prepared foot cannot peel twice");
                    yield return Click(footMouse,c.ViewCamera.WorldToScreenPoint(At(foot)));yield return new WaitForSecondsRealtime(.65f);
                    check(foot.Fitted&&!c.Dragged,"Physical foot "+i+" fits a freely chosen empty corner "+targets[i]);
                    check(!g.Tools.UseScrew(targets[i]),"Mounted foot covers its screw "+targets[i]);
                    if(i==1){g.SaveProgress();g.LoadProgress();check(product.State.filmMask==9&&g.Installed==16&&product.State.underbody,"Half-finished feet restore freely chosen corners and their films");yield return new WaitForSecondsRealtime(.2f);}
                }
            }finally{if(footMouse.added)InputSystem.RemoveDevice(footMouse);foreach(var other in footOthers)if(other.added)InputSystem.EnableDevice(other);if(footPrevious!=null&&footPrevious.added)footPrevious.MakeCurrent();}
            check(g.Completed&&g.Installed==18&&g.CurrentStage==6,"All eighteen distinct mouse parts complete the assembly");WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>().ReportPath),"mouse-native-underbody.png"));yield return new WaitForSecondsRealtime(.2f);
            var padMaterials=c.Lookup["Mouse_Skate_0"].Visual.sharedMaterials;check(padMaterials.Length==2&&padMaterials[1].name=="Warm white PTFE glide pads"&&padMaterials[1]!=product.BaseMaterials[0],"Visible PTFE feet use their own light surface and a dark backing instead of the body paint");
            g.SaveProgress();g.LoadProgress();check(g.Completed&&g.Tools.Tightened==15&&product.State.filmMask==15,"Completed mouse persists all feet and screws");
            var previous=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>("CozyBoard mouse QA");var others=InputSystem.devices.OfType<Mouse>().Where(x=>x!=mouse&&x.enabled).ToArray();foreach(var other in others)InputSystem.DisableDevice(other);InputSystem.EnableDevice(mouse);mouse.MakeCurrent();
            try{
                g.Testing.Begin();yield return new WaitForSecondsRealtime(.3f);check(g.Testing.Active&&!product.State.underbody&&Vector3.Distance(board.position,top)<.002f,"Mouse diagnostics open with the original top pose restored");
                var pad=(RectTransform)GameObject.Find("Mouse diagnostic pad").transform;var widget=pad.GetComponent<WorkshopMouseDiagnostics>();var center=Center(pad);
                yield return Click(mouse,center);check(g.Testing.Count==1&&!g.Testing.Passed,"One left click cannot complete all eight diagnostics");
                yield return Click(mouse,center);check(g.Testing.Count==1,"Repeated left click does not inflate functional coverage");
                yield return Click(mouse,center,MouseButton.Right);check(g.Testing.Count==2,"Actual right-button event records a separate diagnostic");
                yield return Move(mouse,center,null,new Vector2(0,120));yield return Move(mouse,center);check((product.State.testMask&4)!=0,"Actual upward wheel event passes upward scrolling [mask="+product.State.testMask+", pointer="+mouse.position.ReadValue()+", center="+center+", enabled="+mouse.enabled+"]");
                yield return Move(mouse,center,null,new Vector2(0,-120));yield return Move(mouse,center);check((product.State.testMask&8)!=0,"Actual downward wheel event passes downward scrolling");
                yield return Move(mouse,Center(widget.Target));yield return Move(mouse,Center(widget.Piece));check((product.State.testMask&16)!=0,"Pointer travel inside the diagnostic pad verifies movement");
                var home=widget.Piece.anchoredPosition;yield return Move(mouse,Center(widget.Piece));yield return Move(mouse,Center(widget.Piece),MouseButton.Left);yield return Move(mouse,center,MouseButton.Left);g.Testing.End();yield return Move(mouse,center);g.Testing.Begin();yield return null;
                check(widget.Piece.anchoredPosition==home,"Closing diagnostics mid-drag restores the test piece");
                yield return Move(mouse,Center(widget.Piece));yield return Move(mouse,Center(widget.Piece),MouseButton.Left);yield return Move(mouse,Center(widget.Target),MouseButton.Left);yield return Move(mouse,Center(widget.Target));
                check(g.Testing.Count==6&&!g.Testing.Passed,"Six basic mouse diagnostics still require both macro buttons");
                yield return Click(mouse,Center((RectTransform)GameObject.Find("Assign mouse macro 0").transform));
                yield return Click(mouse,Center((RectTransform)GameObject.Find("Choose Geri al").transform));
                yield return Click(mouse,Center((RectTransform)GameObject.Find("Assign mouse macro 1").transform));yield return Click(mouse,Center((RectTransform)GameObject.Find("Choose Kaydet").transform));
                check(product.MacrosReady,"Actual assignment buttons set undo and save on the two side buttons");
                yield return Click(mouse,Center((RectTransform)GameObject.Find("Test mouse macro 0").transform));yield return Click(mouse,Center((RectTransform)GameObject.Find("Test mouse macro 1").transform));
                check(g.Testing.Count==8&&g.Testing.Passed,"Mouse diagnostics and two correctly assigned macro buttons complete testing");
                var runner=UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>();string dir=Path.GetDirectoryName(runner.ReportPath);WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(dir,"mouse-native-test.png"));yield return new WaitForSecondsRealtime(.2f);g.Testing.End();
                g.SaveProgress();g.LoadProgress();check(g.Testing.Passed&&g.Testing.Count==8,"All eight distinct functional measurements and macros persist");
                g.Shop.Close();g.Menu.ClosePanels();yield return null;WorkshopScenarioRunner.CaptureScreenshot(Path.Combine(dir,"mouse-native-workbench.png"));yield return new WaitForSecondsRealtime(.2f);
            }finally{if(mouse.added)InputSystem.RemoveDevice(mouse);foreach(var other in others)if(other.added)InputSystem.EnableDevice(other);if(previous!=null&&previous.added)previous.MakeCurrent();}
            product.ToggleUnderbody();yield return Wait(()=>product.Busy);g.Tools.TrySelect(c.Lookup["Tweezers"]);
            check(!g.Tools.TryRemove(c.Lookup["Mouse_Sensor"]),"Sensor removal requires the optical lens to be removed first");
            check(g.Tools.TryRemove(c.Lookup["Mouse_Lens"]),"Underside optical lens can be physically removed");yield return Wait(()=>g.Tools.Busy);
            check(g.Tools.TryRemove(c.Lookup["Mouse_Sensor"]),"Sensor module can be physically pulled through its service opening");yield return Wait(()=>g.Tools.Busy);g.Tools.Deselect();
            check(!g.Completed&&product.TestCount==0&&g.Stock.Single().Id=="Mouse_Sensor","Removing the sensor invalidates all measurements and exposes only the correct replacement");
            g.SaveProgress();g.LoadProgress();check(g.Installed==16&&product.HousingClosed&&product.State.underbody&&g.Tools.Tightened==15,"Disassembled optical module reload keeps housing, covered screws and all feet");
            check(c.PaintAt(2,At(c.Lookup["Mouse_Sensor"]))&&c.PaintAt(2,At(c.Lookup["Mouse_Lens"])),"Reserved sensor and lens reinstall without buying another kit");yield return new WaitForSecondsRealtime(.7f);
            g.Testing.Begin();for(int i=0;i<8;i++)product.RecordTest(i);check(g.Testing.Passed,"A replaced optical module requires a fresh complete test");g.Testing.End();
            balance=g.Shop.Data.credits;g.Deliver();check(g.Experience.PackStep==1,"Tested mouse starts its own fitting carton");g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.95f);g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.95f);
            check(g.Experience.PackStep==3,"Mouse fits in the carton and both tissue sheets close");g.Experience.CancelPacking();check(g.Completed&&g.Testing.Passed&&g.Shop.Data.credits==balance&&Vector3.Distance(board.position,top)<.002f,"Cancelling mouse packing returns the whole product without a payment");
            g.Deliver();for(int step=0;step<3;step++){g.Experience.AdvancePacking();g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.95f);}g.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(1.4f);
            check(g.WaitingForOrder&&g.Story.Has(WorkshopStory.MouseEvent)&&g.LastDelivery.kind=="mouse"&&g.Shop.Data.credits==balance+220,"Balanced mouse shipment pays 220 and unlocks its follow-up order");
            int mail=g.DeliveryMail.Count;check(!g.FinishDelivery()&&g.DeliveryMail.Count==mail&&g.Shop.Data.credits==balance+220,"Mouse shipment cannot duplicate a receipt or payment");g.Shop.Close();g.Menu.ClosePanels();
            check(g.AcceptOrder("mina-keyboard")&&!g.IsMouse&&c.Lookup["Case"].Visual.GetComponent<MeshFilter>().sharedMesh==originalCase&&g.PartCount==124,"Returning from mouse restores the original keyboard model and assembly");
            check(product.Parts.All(p=>!p.Visual.enabled)&&!GameObject.Find("Mouse assembly controls"),"Mouse parts and controls remain hidden during keyboard work");
        }
    }
}
