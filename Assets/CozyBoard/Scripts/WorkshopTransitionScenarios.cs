using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CozyBoard {
    // Native Play regression scenarios. The caller supplies an isolated save and owns reporting.
    public static class WorkshopTransitionScenarios {
        public static IEnumerator Run(WorkshopGameMode game,Action<bool,string> check){
            check(!string.IsNullOrEmpty(game.VerificationSavePath),"Transition scenarios use an isolated save");
            var previousKeyboard=Keyboard.current;
            var keyboard=InputSystem.AddDevice<Keyboard>("CozyBoard transition verification keyboard");
            try {
                yield return MacroFixture(game,check);
                var controller=game.Controller;
                var board=controller.Lookup["Case"].transform;
                var pcb=controller.Lookup["PCB"];

                // Leaving a click-carried part must restore the original stock object.
                yield return Until(()=>game.Experience.SupplyArrived(1),3);
                var pcbPosition=pcb.transform.position;
                controller.BeginDrag(pcb,controller.ViewCamera.WorldToScreenPoint(pcbPosition));
                check(controller.Dragged==pcb,"A PCB can be picked up for the drag interruption fixture");
                game.Menu.SettingsButton.onClick.Invoke();yield return null;
                check(game.Menu.SettingsPanel.activeSelf&&!controller.Dragged&&!pcb.Fitted&&Near(pcb.transform.position,pcbPosition),"Settings cancels carried PCB without losing or installing it");
                yield return KeyPress(keyboard,Key.Escape);
                check(!game.Menu.SettingsPanel.activeSelf&&!game.Experience.MainVisible,"One Escape closes settings and leaves the workshop open");
                game.Menu.ClosePanels();if(game.Experience.MainVisible)game.Experience.Continue();
                controller.BeginDrag(pcb,controller.ViewCamera.WorldToScreenPoint(pcbPosition));
                game.Shop.Open();yield return null;
                check(game.Shop.IsOpen&&!controller.Dragged&&Near(pcb.transform.position,pcbPosition),"Laptop catalogue safely cancels carried PCB");
                yield return KeyPress(keyboard,Key.Escape);
                check(!game.Shop.IsOpen&&!game.Experience.MainVisible,"One Escape closes laptop without also returning home");
                game.Shop.Close();if(game.Experience.MainVisible)game.Experience.Continue();
                controller.BeginDrag(pcb,controller.ViewCamera.WorldToScreenPoint(pcbPosition));
                game.Experience.ShowMain();yield return null;
                check(game.Experience.MainVisible&&!controller.Dragged&&Near(pcb.transform.position,pcbPosition),"Home restores a carried component to stock");
                game.Shop.Open();check(!game.Shop.IsOpen,"Laptop cannot open behind the main menu");
                game.Experience.Continue();yield return null;

                foreach(var item in game.ProductItems.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))
                    check(controller.PaintAt(item.Stage,board.TransformPoint(item.Slot)),"Transition fixture macro placement: "+item.Id);
                yield return new WaitForSecondsRealtime(.85f);
                check(game.Completed,"Transition fixture assembles a complete macro pad");

                // Mouse release may be swallowed by a modal: never keep a hidden laptop drag alive.
                var laptop=GameObject.Find("Cozy workshop laptop").transform;
                foreach(string destination in new[]{"catalogue","settings","order","home","notebook","desk","pointer","move","paint","inspection","test"}){
                    var start=laptop.position;game.Menu.SelectTool(1);
                    var point=controller.ViewCamera.WorldToScreenPoint(start+Vector3.up*.3f);
                    check(game.Shop.BeginLaptopMove(point),"Laptop drag begins before "+destination);
                    game.Shop.MoveLaptop((Vector2)point+new Vector2(18,16));
                    switch(destination){case "catalogue":game.Shop.Open();break;case "settings":game.Menu.ShowSettings();break;case "order":game.Menu.ShowOrder();break;case "home":game.Experience.ShowMain();break;case "notebook":game.StoryUI.OpenNotebook();break;case "desk":game.RepairBench.Visit(true,false);break;case "pointer":game.Menu.SelectTool(0);break;case "move":game.Menu.SelectTool(1);break;case "paint":game.Menu.SelectTool(3);break;case "inspection":game.Experience.BeginInspection();break;case "test":game.Testing.Begin();break;}
                    yield return null;
                    check(!game.Shop.IsMoving&&Near(laptop.position,start),"Opening "+destination+" cancels laptop drag and restores its position");
                    game.Experience.EndInspection();game.Testing.End();game.Shop.Close();game.StoryUI.Hide();game.Menu.ClosePanels();if(game.Experience.MainVisible)game.Experience.Continue();game.RepairBench.Visit(false,false);game.Menu.SelectTool(0);yield return null;
                }

                var beforeConfirmation=game.SerializeProgress();game.Experience.ShowMain();game.Experience.RequestNewGame();yield return null;
                check(GameObject.Find("New game confirmation")!=null,"New game asks before replacing an existing workshop");
                yield return KeyPress(keyboard,Key.Escape);check(game.Experience.MainVisible&&GameObject.Find("New game confirmation")==null&&game.SerializeProgress()==beforeConfirmation,"Escape cancels new game without changing progress");
                game.Experience.RequestNewGame();Click(game,"Cancel");yield return null;check(game.Experience.MainVisible&&GameObject.Find("New game confirmation")==null&&game.SerializeProgress()==beforeConfirmation,"Cancel button retains the same workshop");
                game.Experience.RequestNewGame();Click(game,"Settings");yield return null;check(game.Menu.SettingsPanel.activeSelf&&GameObject.Find("New game confirmation")==null,"Main settings replaces new game confirmation with one visible layer");
                yield return KeyPress(keyboard,Key.Escape);check(!game.Menu.SettingsPanel.activeSelf&&game.Experience.MainVisible&&game.SerializeProgress()==beforeConfirmation,"One Escape closes main settings and preserves progress");
                game.Experience.RequestNewGame();Click(game,"Continue");yield return null;check(!game.Experience.MainVisible&&GameObject.Find("New game confirmation")==null&&game.SerializeProgress()==beforeConfirmation,"Continue dismisses pending confirmation without starting over");

                // Re-entering inspection and switching toolbar modes must never replace its saved pose.
                var position=board.position;var rotation=board.rotation;
                game.Menu.RotateButton.onClick.Invoke();yield return null;
                check(game.Experience.Inspecting,"Rotate toolbar opens keyboard inspection");
                game.Menu.RotateButton.onClick.Invoke();yield return null;
                game.Experience.EndInspection();yield return null;
                check(Near(board.position,position)&&Quaternion.Angle(board.rotation,rotation)<.1f,"Repeated inspection clicks preserve the original desk pose");
                foreach(var mode in new[]{0,1,3}){
                    game.Experience.BeginInspection();yield return null;
                    (mode==0?game.Menu.PointerButton:mode==1?game.Menu.MoveButton:game.Menu.PaintButton).onClick.Invoke();yield return null;
                    check(!game.Experience.Inspecting&&Near(board.position,position),"Toolbar mode "+mode+" exits inspection and restores the keyboard");
                    game.Menu.SelectTool(0);
                }
                game.Experience.BeginInspection();game.Menu.SettingsButton.onClick.Invoke();yield return null;
                check(game.Menu.SettingsPanel.activeSelf&&!game.Experience.Inspecting&&Near(board.position,position),"Settings replaces inspection and restores its pose");
                game.Menu.ClosePanels();game.Experience.BeginInspection();game.Menu.OrderButton.onClick.Invoke();yield return null;
                check(game.Menu.OrderPanel.activeSelf&&!game.Experience.Inspecting&&Near(board.position,position),"Order card replaces inspection and restores its pose");
                game.Menu.ClosePanels();game.Experience.BeginInspection();game.Shop.Open();yield return null;
                check(game.Shop.IsOpen&&!game.Experience.Inspecting&&Near(board.position,position),"Laptop replaces inspection without moving the product");
                game.Shop.Close();
                game.Experience.BeginInspection();game.Menu.SaveButton.onClick.Invoke();yield return null;
                check(!game.Experience.Inspecting&&Near(board.position,position),"Save restores inspected product before recording the desk pose");

                // The completed keyboard should still allow laptop access after testing ends.
                game.Testing.Begin();yield return null;
                check(game.Testing.Active,"Complete macro pad opens native test");
                Click(game,"Macro function 0");yield return null;check(GameObject.Find("Macro function picker")!=null,"Macro function picker opens above the test");
                yield return KeyPress(keyboard,Key.Escape);check(game.Testing.Active&&GameObject.Find("Macro function picker")==null&&!game.Experience.MainVisible,"First Escape closes only the macro function picker");
                yield return KeyPress(keyboard,Key.Escape);check(!game.Testing.Active&&!game.Experience.MainVisible,"Second Escape closes only the test card");game.Testing.Begin();
                Click(game,"Close test");yield return null;
                yield return KeyPress(keyboard,Key.L);
                check(game.Shop.IsOpen,"L opens laptop after closing the keyboard test");
                game.Shop.Close();game.Testing.Begin();game.Menu.SettingsButton.onClick.Invoke();yield return null;
                check(game.Menu.SettingsPanel.activeSelf&&!game.Testing.Active,"Settings replaces the keyboard test card");
                game.Menu.ClosePanels();game.Testing.Begin();game.Menu.OrderButton.onClick.Invoke();yield return null;
                check(game.Menu.OrderPanel.activeSelf&&!game.Testing.Active,"Order card replaces the keyboard test card");
                game.Menu.ClosePanels();game.Testing.Begin();game.Experience.ShowMain();yield return null;
                check(game.Experience.MainVisible&&!game.Testing.Active,"Home disconnects the keyboard test");
                game.Experience.Continue();yield return null;

                // Painting is an editing operation, not a second layer behind an unrelated modal.
                var cap=game.TestKeys[0];game.Menu.PaintButton.onClick.Invoke();game.Painter.Edit(cap);
                yield return Until(()=>!game.Painter.PreparingSurface,8);
                check(game.Painter.Editing,"Paint toolbar opens a fitted keycap in the studio");
                game.Menu.SettingsButton.onClick.Invoke();yield return null;
                check(game.Menu.SettingsPanel.activeSelf&&!game.Painter.Editing&&!game.Painter.PreparingSurface,"Settings closes the paint studio and its preparation work");
                game.Menu.ClosePanels();game.Menu.SelectTool(3);game.Painter.Edit(cap);yield return null;
                game.Menu.OrderButton.onClick.Invoke();yield return null;
                check(game.Menu.OrderPanel.activeSelf&&!game.Painter.Editing,"Order card closes the paint studio");
                game.Menu.ClosePanels();game.Menu.SelectTool(3);game.Painter.Edit(cap);yield return null;
                bool visited=game.RepairBench.Visit(true);
                check(!visited&&game.Painter.Editing&&!game.RepairBench.Moving,"Desk travel cannot interrupt an active paint studio");
                game.Painter.DoneButton.onClick.Invoke();game.Menu.SelectTool(0);yield return null;
                check(!game.Painter.Editing&&!game.Painter.PreparingSurface,"Paint completion releases editor and surface preparation");

                // Opening the notebook from the laptop must leave only one modal active.
                game.Shop.Open();game.StoryUI.OpenNotebook();yield return null;
                check(game.StoryUI.Open&&!game.Shop.IsOpen,"Notebook replaces laptop catalogue");
                yield return KeyPress(keyboard,Key.L);
                check(!(game.StoryUI.Open&&game.Shop.IsOpen),"L cannot stack laptop and notebook modals");
                game.StoryUI.Hide();game.Shop.Close();game.Menu.ClosePanels();

                // A moving camera and a working tool must retain their own completion state.
                check(game.RepairBench.Visit(true),"Travel to repair desk starts from assembly");
                check(game.RepairBench.Moving&&!game.RepairBench.Visit(false),"Rapid opposite travel request is rejected while camera moves");
                game.Experience.ShowMain();game.Shop.Open();
                check(!game.Experience.MainVisible&&!game.Shop.IsOpen,"Home and laptop cannot interrupt camera travel");
                yield return Until(()=>!game.RepairBench.Moving,3);
                check(game.RepairBench.AtRepair&&Near(controller.ViewTarget,WorkshopRepairBench.Origin),"Camera reaches repair desk after ignored interruptions");
                game.Testing.Begin();
                yield return Until(()=>!game.RepairBench.Moving,3);
                check(!game.RepairBench.AtRepair&&!game.Testing.Active,"Test requested away from the product first returns to assembly");
                game.Testing.Begin();check(game.Testing.Active,"Test opens once the camera reaches the product");game.Testing.End();
                game.Tools.ToggleKeyboardUnderbody();check(game.Tools.Busy,"Keyboard underside flip starts before fastening");game.Shop.Open();game.Menu.SelectTool(1);game.RepairBench.Visit(true);check(!game.Shop.IsOpen&&!game.RepairBench.Moving&&game.Tools.Busy,"Panels, move mode and desk travel cannot interrupt the keyboard flip");yield return Until(()=>!game.Tools.Busy,3);check(game.Tools.KeyboardUnderbody,"Keyboard flip finishes after conflicting input");game.Tools.TrySelect(controller.Lookup["Screwdriver"]);
                game.Tools.TakeScrew();check(game.Tools.UseScrew(0)&&game.Tools.Busy,"Native screw animation starts for interruption scenarios");
                int fittedBeforeUndo=game.Installed;game.Menu.UndoButton.onClick.Invoke();check(game.Installed==fittedBeforeUndo&&game.Tools.Busy,"Undo cannot remove a component during the screwdriver animation");
                int money=game.Shop.Data.credits;
                game.Experience.ShowMain();game.Shop.Open();bool desk=game.RepairBench.Visit(true);game.SaveProgress();
                check(!game.Experience.MainVisible&&!game.Shop.IsOpen&&!desk&&game.Tools.Busy,"Home, laptop, save and travel cannot interrupt a working screwdriver");
                game.Menu.SettingsButton.onClick.Invoke();game.Menu.OrderButton.onClick.Invoke();yield return null;
                check(!game.Menu.SettingsPanel.activeSelf&&!game.Menu.OrderPanel.activeSelf&&game.Tools.Busy,"Settings and order card cannot interrupt a working screwdriver");
                yield return Until(()=>!game.Tools.Busy,4);
                check(!game.Tools.Busy&&game.Tools.Tightened==1&&game.Shop.Data.credits==money,"Rejected interruptions allow the screw to finish exactly once");game.Tools.Deselect();

                // Fixture: the assembly and screw animations are exercised elsewhere; this section
                // seeds only the remaining screws so each packing cancellation can run independently.
                game.Tools.Tightened=15;
                game.Testing.Begin();for(int i=0;i<6;i++){game.Testing.SetFunction(i,WorkshopGameMode.DesiredFunctions[i]);game.Press(controller.Lookup[$"Keycap_{i+1:00}"]);}
                yield return new WaitForSecondsRealtime(.3f);check(game.Testing.Passed,"Packing transition fixture has six validated functions");game.Testing.End();
                for(int target=1;target<=3;target++){
                    position=board.position;rotation=board.rotation;
                    game.Experience.BeginPacking();yield return null;
                    check(game.Experience.Packing&&game.Experience.PackStep==1,"Packing opens before cancellation stage "+target);
                    game.Shop.Open();bool moved=game.RepairBench.Visit(true);
                    check(!game.Shop.IsOpen&&!moved,"Laptop and desk travel are blocked while packing stage "+target);
                    for(int step=1;step<target;step++){game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.9f);}
                    game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.12f);
                    game.Experience.CancelPacking();yield return new WaitForSecondsRealtime(.9f);
                    check(!game.Experience.Packing&&game.Experience.PackStep==0&&Near(board.position,position)&&Quaternion.Angle(board.rotation,rotation)<.1f,"Cancel during packing animation "+target+" restores product and stops later steps");
                    check(game.Completed&&game.Testing.Passed&&game.Tools.Tightened==15,"Packing cancellation "+target+" preserves assembly, tests and screws");
                }
                position=board.position;game.Experience.BeginPacking();game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.9f);
                game.Menu.SaveButton.onClick.Invoke();yield return null;
                check(!game.Experience.Packing&&Near(board.position,position),"Save during packing cancels the parcel and records the original desk pose");
                game.LoadProgress();yield return null;
                check(game.IsMacro&&game.Completed&&game.Testing.Passed&&!game.Experience.Packing&&Near(board.position,position),"Reload after packing save preserves the assembled product without transient parcel state");
                position=board.position;game.Experience.BeginPacking();yield return null;
                for(int step=1;step<=3;step++){game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.9f);}
                check(game.Experience.PackStep==4,"Shipment cancellation fixture reaches final sealed parcel");
                var orderId=game.ActiveOrder.instanceId;money=game.Shop.Data.credits;
                game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.08f);
                game.Experience.CancelPacking();yield return new WaitForSecondsRealtime(1.15f);
                check(!game.WaitingForOrder&&game.ActiveOrder?.instanceId==orderId&&game.Shop.Data.credits==money,"Cancel during shipment curtain prevents delayed delivery and reward");
                check(!game.Experience.Packing&&Near(board.position,position),"Shipment cancellation restores the product to its original desk");
                string savePath=game.VerificationSavePath;string blocker=savePath+".shipment-blocked";System.IO.File.WriteAllText(blocker,"Isolated QA blocker");
                try{
                    game.Experience.BeginPacking();for(int step=1;step<=3;step++){game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.9f);}
                    game.VerificationSavePath=System.IO.Path.Combine(blocker,"save.json");game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(1.15f);
                    check(game.ActiveOrder?.instanceId==orderId&&game.Shop.Data.credits==money&&game.Completed&&game.Testing.Passed,"Failed shipment save preserves the same tested product and wallet");
                    check(!game.Experience.Packing&&Near(board.position,position),"Failed shipment save restores the original desk pose");
                }finally{game.VerificationSavePath=savePath;System.IO.File.Delete(blocker);}

                // Repeat cancellation using a real isolated repair order and its sealed spare.
                yield return RepairFixture(game,check);
                game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return new WaitForSecondsRealtime(.3f);game.Testing.End();
                check(game.Repair.diagnosed,"Repair transition fixture diagnoses the fault");
                check(game.Shop.Purchase(3),"Repair transition fixture purchases a single spare");game.Shop.Close();
                game.Tools.TrySelect(controller.Lookup["KeyPuller"]);check(game.Tools.TryRemove(controller.Lookup[game.Repair.capId]),"Repair transition fixture removes the cap");yield return Until(()=>!game.Tools.Busy,4);game.Tools.Deselect();
                game.Tools.TrySelect(controller.Lookup["Tweezers"]);check(game.Tools.TryRemove(controller.Lookup[game.Repair.switchId]),"Repair transition fixture removes the fault");yield return Until(()=>!game.Tools.Busy,4);game.Tools.Deselect();
                check(game.RepairBench.OpenPartInspection(),"Old-part detail opens for modal tests");yield return null;
                game.Experience.ShowMain();check(!game.Experience.MainVisible&&game.RepairBench.DetailOpen,"Home cannot interrupt part detail");
                yield return KeyPress(keyboard,Key.Escape);
                check(!game.RepairBench.DetailOpen&&!game.Experience.MainVisible,"One Escape closes part detail and leaves repair desk open");
                game.RepairBench.ClosePartInspection();if(game.Experience.MainVisible)game.Experience.Continue();
                check(game.RepairBench.OpenPartInspection(),"Old-part detail can reopen after Escape");game.RepairBench.TurnInspectedSwitch();yield return new WaitForSecondsRealtime(.55f);game.RepairBench.ClosePartInspection();
                check(game.RepairBench.OpenReplacementPacket()&&game.RepairBench.Unpacking,"Sealed spare begins a native unpacking animation");
                yield return KeyPress(keyboard,Key.L);
                game.Experience.ShowMain();bool leaving=game.RepairBench.Visit(false);
                check(!game.Shop.IsOpen&&!game.Experience.MainVisible&&!leaving,"Laptop, home and desk travel cannot interrupt opening the spare");
                yield return Until(()=>!game.RepairBench.Unpacking,3);
                check(game.Repair.spareOpened&&!game.RepairBench.Unpacking,"Spare opening completes after ignored interruptions");
                check(game.RepairBench.OpenPartInspection(true),"New-part detail opens after packet animation");game.RepairBench.ClosePartInspection();
                game.TrySaveQuiet();game.LoadProgress();yield return null;
                check(game.Repair.spareOpened&&game.Repair.oldSwitchInspected&&!game.Repair.spareChecked,"Closing unchecked new-part detail preserves its required pin check across reload");
                check(game.RepairBench.AtRepair&&!game.RepairBench.DetailOpen&&!game.RepairBench.Unpacking,"Reload closes transient repair UI and restores the repair station");
            } finally {
                if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();
            }
        }
        static IEnumerator MacroFixture(WorkshopGameMode game,Action<bool,string> check){
            game.StoryUI.Hide();game.Shop.Close();game.Menu.ClosePanels();game.Experience.EndInspection();game.Experience.CancelPacking();
            game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();yield return null;
            check(game.AcceptOrder(WorkshopStory.FirstMacro.Id),"Transition fixture accepts a macro order");game.Menu.ClosePanels();game.Shop.Close();yield return null;
        }
        static IEnumerator RepairFixture(WorkshopGameMode game,Action<bool,string> check){
            game.StoryUI.Hide();game.Shop.Close();game.Menu.ClosePanels();game.Experience.EndInspection();game.Experience.CancelPacking();
            game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();yield return null;
            check(game.AcceptOrder(WorkshopStory.FirstRepair.Id),"Transition fixture accepts a repair order");game.Menu.ClosePanels();game.Shop.Close();
            game.RepairBench.Visit(true);yield return Until(()=>!game.RepairBench.Moving,3);
            check(game.RepairBench.AtRepair&&game.Completed,"Repair transition fixture arrives at assembled repair product");
        }
        static IEnumerator Until(Func<bool> condition,float seconds){for(float deadline=Time.realtimeSinceStartup+seconds;!condition()&&Time.realtimeSinceStartup<deadline;)yield return null;}
        static IEnumerator KeyPress(Keyboard keyboard,Key key){
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        static void Click(WorkshopGameMode game,string name){
            var button=game.Menu.OrderPanel.transform.root.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);
            if(!button)throw new Exception("Transition button missing: "+name);button.onClick.Invoke();
        }
        static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.015f;
    }
}
