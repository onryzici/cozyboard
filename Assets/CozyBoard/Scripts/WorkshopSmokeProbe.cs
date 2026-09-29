using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CozyBoard {
    // Opt-in standalone regression runner; does nothing in a normal player session.
    public sealed class WorkshopSmokeProbe:MonoBehaviour {
        readonly List<string> results=new();
        string report;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Environment.GetCommandLineArgs().Contains("--cozy-smoke"))new GameObject("BackgroundSmokeProbe").AddComponent<WorkshopSmokeProbe>();}
        void Check(bool value,string text){if(!value)throw new Exception(text);results.Add("PASS "+text);}
        IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--cozy-report");report=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.temporaryCachePath,"cozy-runtime-smoke.txt");
            yield return null;yield return null;
            var routine=Run();
            while(true){bool more;object current=null;try{more=routine.MoveNext();if(more)current=routine.Current;}catch(Exception e){results.Add("FAIL "+e);File.WriteAllLines(report,results);Debug.LogError(e);Application.Quit(1);yield break;}if(!more)break;yield return current;}
            File.WriteAllLines(report,results);Debug.Log("COZY_RUNTIME_SMOKE_PASSED "+results.Count);Application.Quit(0);
        }
        IEnumerator Run(){
            var c=FindFirstObjectByType<WorkshopController>();var game=c.Game;game.NewOrder();game.Menu.ClosePanels();
            game.Audio.MusicSource.mute=true;game.Audio.EffectsSource.mute=true;
            var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();
            void Pointer(Vector3 world,bool down){var p=c.ViewCamera.WorldToScreenPoint(world);var state=new MouseState{position=new Vector2(p.x,p.y)};if(down)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
            Check(!c.PaintAt(4,c.Lookup["Case"].transform.TransformPoint(c.Lookup["Keycap_00"].Slot)),"Runtime blocks keycaps before internal parts");
            Check(c.PaintAt(1,c.Lookup["Case"].transform.TransformPoint(c.Lookup["PCB"].Slot)),"Runtime installs PCB first");
            Check(c.PaintAt(2,c.Lookup["Case"].transform.TransformPoint(c.Lookup["Plate"].Slot)),"Runtime installs plate second");
            var box=FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).First(b=>b.Stage==3);
            Physics.SyncTransforms();Pointer(box.transform.position+Vector3.up*.25f,false);yield return new WaitForSeconds(.05f);
            Pointer(box.transform.position+Vector3.up*.25f,true);yield return new WaitForSeconds(.05f);
            Check(c.Dragged&&c.Dragged.Stage==3,"Mouse picks a switch from its physical box");
            var row=c.Items.Where(p=>p.Kind=="keycap"&&Mathf.Abs(p.Slot.z-.8f)<.01f).OrderBy(p=>p.Slot.x).ToArray();
            foreach(var cap in row){Pointer(c.Lookup["Case"].transform.TransformPoint(cap.Slot)+Vector3.up*.05f,true);yield return new WaitForSeconds(.12f);}
            Pointer(c.Lookup["Case"].transform.TransformPoint(row.Last().Slot),false);yield return new WaitForSeconds(.1f);
            Check(c.Items.Count(p=>p.Kind=="switch"&&p.Fitted)>=row.Length-1,"Held mouse stroke places successive switches");
            Check(!c.Dragged,"Releasing a stroke returns the spare held piece");
            foreach(var part in c.Items.Where(p=>p.Stage==3&&!p.Fitted).ToArray())c.PaintAt(3,c.Lookup["Case"].transform.TransformPoint(part.Slot));
            box=FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).First(b=>b.Stage==4);
            Pointer(box.transform.position+Vector3.up*.25f,false);yield return new WaitForSeconds(.04f);Pointer(box.transform.position+Vector3.up*.25f,true);yield return new WaitForSeconds(.05f);
            Check(c.Dragged&&c.Dragged.Stage==4,"Mouse picks a keycap after completing all switches");
            foreach(var cap in row.Take(3)){Pointer(c.Lookup["Case"].transform.TransformPoint(cap.Slot)+Vector3.up*.05f,true);yield return new WaitForSeconds(.13f);}
            Pointer(c.Lookup["Case"].transform.TransformPoint(row[2].Slot),false);yield return new WaitForSeconds(.7f);
            Check(row.Take(3).All(p=>p.Fitted),"Swept keycaps acquire the target letters");
            int count=game.Installed;game.Menu.UndoButton.onClick.Invoke();Check(game.Installed==count-1,"Painted undo button reverses one placement");
            game.Menu.OrderButton.onClick.Invoke();Check(game.Menu.InputBlocked&&game.Menu.OrderPanel.activeSelf,"Order icon opens the readable order card");game.Menu.CloseOrder.onClick.Invoke();Check(!game.Menu.InputBlocked,"Order close button restores interaction");
            game.Menu.SettingsButton.onClick.Invoke();Check(game.Menu.SettingsPanel.activeSelf,"Settings icon opens sound controls");game.Menu.CloseSettings.onClick.Invoke();
            foreach(var part in c.Items.Where(p=>p.Stage>0&&!p.Fitted).OrderBy(p=>p.Stage).ToArray())c.PaintAt(part.Stage,c.Lookup["Case"].transform.TransformPoint(part.Slot));
            yield return new WaitForSeconds(1);
            Check(game.Completed,"Runtime ordered assembly completes");
            Check(c.Items.All(p=>p.Visual.transform.localPosition.sqrMagnitude<.00001f),"Installation animations settle exactly at rest");
            string json=game.SerializeProgress();game.NewOrder();game.RestoreProgress(json);Check(game.Completed,"Runtime save data restores all placed parts");
            Check(game.Audio.Keys.All(a=>a&&a.loadState==AudioDataLoadState.Loaded),"Recorded clicks are loaded in the player");
        }
    }
}
