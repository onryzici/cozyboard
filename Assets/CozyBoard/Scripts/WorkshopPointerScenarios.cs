using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CozyBoard {
    // Queue real Input System events; no Button.onClick or placement calls in this suite.
    public static class WorkshopPointerScenarios {
        static IEnumerator Move(Mouse mouse,Vector2 point,bool held=false){
            var state=new MouseState{position=point};if(held)state=state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse,state);yield return new WaitForSecondsRealtime(.12f);
        }
        static IEnumerator Click(Mouse mouse,Vector2 point){yield return Move(mouse,point);yield return Move(mouse,point,true);yield return Move(mouse,point);}
        static Vector2 Center(Button button){Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));}
        static IEnumerator Key(Keyboard keyboard,Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.1f);}
        static string Hit(Vector2 point){var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();var e=UnityEngine.EventSystems.EventSystem.current;e.RaycastAll(new UnityEngine.EventSystems.PointerEventData(e){position=point},hits);return string.Join(",",hits.Take(3).Select(x=>x.gameObject.name));}
        static IEnumerator Until(Func<bool> ready,float timeout=4){float end=Time.realtimeSinceStartup+timeout;while(!ready()&&Time.realtimeSinceStartup<end)yield return null;}
        static Vector2 StockPoint(WorkshopGameMode game,int stage){var supply=UnityEngine.Object.FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).FirstOrDefault(x=>x.Stage==stage&&x.gameObject.activeInHierarchy);var item=game.SupplyItem(stage);var hit=supply?supply.GetComponent<Collider>():item.GetComponent<Collider>();return game.Controller.ViewCamera.WorldToScreenPoint(hit.bounds.center);}
        public static IEnumerator Run(WorkshopGameMode game,Action<bool,string> check){
            check(!string.IsNullOrEmpty(game.VerificationSavePath),"Pointer scenarios use an isolated save");
            var previousMouse=Mouse.current;var previousKeyboard=Keyboard.current;
            var mouse=InputSystem.AddDevice<Mouse>("CozyBoard pointer QA");var keyboard=InputSystem.AddDevice<Keyboard>("CozyBoard pointer QA keyboard");
            var otherMice=InputSystem.devices.OfType<Mouse>().Where(x=>x!=mouse&&x.enabled).ToArray();foreach(var other in otherMice)InputSystem.DisableDevice(other);mouse.MakeCurrent();
            var c=game.Controller;
            string State()=>" [current="+(Mouse.current==mouse)+", held="+mouse.leftButton.isPressed+", pointer="+mouse.position.ReadValue()+", dragged="+(c.Dragged?c.Dragged.Id:"none")+", ui="+(UnityEngine.EventSystems.EventSystem.current&&UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())+"]";
            try{
                game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();yield return null;
                check(game.AcceptOrder(WorkshopStory.FirstMacro.Id),"Pointer fixture accepts a six-key order");game.Menu.ClosePanels();game.Shop.Close();game.Menu.SelectTool(0);yield return new WaitForSecondsRealtime(.6f);
                var board=c.Lookup["Case"].transform;
                yield return Click(mouse,Center(game.Menu.SettingsButton));check(game.Menu.SettingsPanel.activeSelf,"Real mouse click opens settings");
                yield return Key(keyboard,UnityEngine.InputSystem.Key.Escape);check(!game.Menu.SettingsPanel.activeSelf&&!game.Experience.MainVisible,"Escape closes mouse-opened settings without leaving the desk");
                yield return Click(mouse,Center(game.Menu.OrderButton));check(game.Menu.OrderPanel.activeSelf,"Real mouse click opens order");
                yield return Key(keyboard,UnityEngine.InputSystem.Key.Escape);check(!game.Menu.OrderPanel.activeSelf,"Escape closes mouse-opened order");
                yield return Until(()=>game.Experience.SupplyArrived(1));var pcb=c.Lookup["PCB"];var start=pcb.transform.position;
                yield return Click(mouse,StockPoint(game,1));check(c.Dragged==pcb&&!pcb.Fitted,"Real supply click picks up PCB in click-carry mode");
                yield return Key(keyboard,UnityEngine.InputSystem.Key.Escape);check(!c.Dragged&&!pcb.Fitted&&Vector3.Distance(start,pcb.transform.position)<.01f,"Escape returns mouse-carried PCB to its original supply");
                var outsideSource=StockPoint(game,1);yield return Move(mouse,outsideSource);yield return Move(mouse,outsideSource,true);check(c.Dragged==pcb,"Outside drop fixture picks up PCB"+State());
                var outside=c.ViewCamera.WorldToScreenPoint(new Vector3(-8,.6f,-4));yield return Move(mouse,outside,true);yield return Move(mouse,outside);
                check(!pcb.Fitted&&!c.Dragged,"Dropping a dragged PCB over the laptop returns it to supply without swallowing release"+State());
                for(int stage=1;stage<=2;stage++){
                    yield return Until(()=>game.Experience.SupplyArrived(stage));var item=c.Lookup[stage==1?"PCB":"Plate"];
                    var source=StockPoint(game,stage);var target=c.ViewCamera.WorldToScreenPoint(board.TransformPoint(item.Slot)+Vector3.up*.24f);
                    yield return Move(mouse,source);yield return Move(mouse,source,true);check(c.Dragged==item,"Real mouse starts stage "+stage+" drag");yield return Move(mouse,target,true);yield return Move(mouse,target);
                    check(item.Fitted&&!c.Dragged,"Real drag installs stage "+stage+" (target="+target+", source="+source+")"+State());yield return new WaitForSecondsRealtime(.6f);
                    if(!item.Fitted)yield break;
                }
                for(int stage=3;stage<=4;stage++){
                    yield return Until(()=>game.Experience.SupplyArrived(stage));var source=StockPoint(game,stage);yield return Move(mouse,source);yield return Move(mouse,source,true);
                    check(c.Dragged&&c.Dragged.Stage==stage,"Real mouse picks up stage "+stage+" supply");
                    foreach(var item in game.ProductItems.Where(x=>x.Stage==stage).OrderBy(x=>x.Id))yield return Move(mouse,c.ViewCamera.WorldToScreenPoint(board.TransformPoint(item.Slot)+Vector3.up*.24f),true);
                    yield return Move(mouse,mouse.position.ReadValue());yield return new WaitForSecondsRealtime(.65f);
                    check(game.ProductItems.Count(x=>x.Stage==stage&&x.Fitted)==6&&!c.Dragged,"Holding mouse across six sockets installs exactly six stage "+stage+" parts");
                }
                check(game.Completed&&game.Installed==14,"Real mouse assembly completes all fourteen macro parts");if(!game.Completed)yield break;
                yield return Click(mouse,Center(game.Menu.RotateButton));check(game.Experience.Inspecting,"Real rotate-button click opens inspection");
                yield return Click(mouse,Center(game.Menu.PointerButton));check(!game.Experience.Inspecting,"Real pointer-button click exits inspection");
                game.Testing.Begin();yield return null;
                var binding=GameObject.Find("Macro function 0").GetComponent<Button>();yield return Click(mouse,Center(binding));check(GameObject.Find("Macro function picker")!=null,"Real mouse opens macro function selection");
                var choice=GameObject.Find("Choose "+WorkshopGameMode.DesiredFunctions[0]).GetComponent<Button>();yield return Click(mouse,Center(choice));check(game.MacroFunctions[0]==WorkshopGameMode.DesiredFunctions[0]&&GameObject.Find("Macro function picker")==null,"Real mouse selects and closes the function picker");
                for(int i=0;i<6;i++){
                    // Select every function with the same UI a player uses.
                    binding=GameObject.Find("Macro function "+i).GetComponent<Button>();yield return Click(mouse,Center(binding));choice=GameObject.Find("Choose "+WorkshopGameMode.DesiredFunctions[i]).GetComponent<Button>();yield return Click(mouse,Center(choice));
                    var test=GameObject.Find("Test Keycap_"+(i+1).ToString("00")).GetComponent<Button>();yield return Click(mouse,Center(test));check(game.Testing.Count==i+1,"Real mouse assigns and tests macro key "+(i+1)+" [count="+game.Testing.Count+", binding="+game.MacroFunctions[i]+", measured="+(game.Testing.LastMeasuredKey?game.Testing.LastMeasuredKey.Id:"none")+", hit="+Hit(Center(test))+"]");
                }
                check(game.Testing.Passed,"Six mouse-selected bindings and distinct clicks pass the macro test");
                var runner=UnityEngine.Object.FindFirstObjectByType<WorkshopScenarioRunner>();var snapshot=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(string.IsNullOrEmpty(runner.ReportPath)?"Logs/adversarial-all.txt":runner.ReportPath)),"qa-native-macro.png");WorkshopScenarioRunner.CaptureScreenshot(snapshot);yield return new WaitForSecondsRealtime(.15f);
                yield return Click(mouse,Center(GameObject.Find("Close test").GetComponent<Button>()));check(!game.Testing.Active,"Real close-button click exits the test");
                yield return Key(keyboard,UnityEngine.InputSystem.Key.L);check(game.Shop.IsOpen,"Keyboard laptop shortcut works after a mouse-completed test");yield return Key(keyboard,UnityEngine.InputSystem.Key.Escape);check(!game.Shop.IsOpen&&!game.Experience.MainVisible,"Escape closes laptop as the only active layer");
                yield return Click(mouse,Center(game.Menu.PaintButton));var painted=game.TestKeys[0];yield return Click(mouse,c.ViewCamera.WorldToScreenPoint(painted.Hitbox.bounds.center));
                yield return Until(()=>!game.Painter.PreparingSurface,12);check(game.Painter.Editing&&!game.Painter.PreparingSurface,"Real brush-button and physical key click open the prepared paint studio");
                if(game.Painter.Editing){yield return Click(mouse,Center(game.Painter.DoneButton));check(!game.Painter.Editing,"Real paint completion button returns to the desk");}
            }finally{
                game.Controller.CancelDrag();if(mouse.added)InputSystem.RemoveDevice(mouse);if(keyboard.added)InputSystem.RemoveDevice(keyboard);
                foreach(var other in otherMice)if(other.added)InputSystem.EnableDevice(other);
                if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();if(previousKeyboard!=null&&previousKeyboard.added)previousKeyboard.MakeCurrent();
            }
        }
    }
}
