using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyBoard {
    // Opt-in verification only. Never saves workshop progress or runs in normal games.
    public sealed class WorkshopAtelierSmoke:MonoBehaviour {
        readonly List<string> results=new();string report;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Environment.GetCommandLineArgs().Contains("--cozy-atelier-smoke"))new GameObject("Atelier verification").AddComponent<WorkshopAtelierSmoke>();}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);results.Add("PASS "+message);}
        IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--cozy-atelier-report");report=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.temporaryCachePath,"atelier-smoke.txt");
            yield return null;yield return null;var run=Run();
            while(true){bool more;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception ex){results.Add("FAIL "+ex);File.WriteAllLines(report,results);Application.Quit(1);yield break;}if(!more)break;yield return current;}
            File.WriteAllLines(report,results);Application.Quit(0);
        }
        IEnumerator Run(){
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var game=FindAnyObjectByType<WorkshopGameMode>();game.NewOrder();game.Experience.Continue();
            Check(game.Audio.Music&&game.Audio.Music.name=="WarmFireplace","Windows player loads the new soundtrack");
            Check(game.Painter.StudioWarmed,"Studio shaders are warmed before opening a key");
            foreach(var item in game.Controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))game.Controller.Attach(item);
            game.Refresh();yield return null;
            game.Testing.Begin();yield return null;
            Check(GameObject.Find("Keyboard test card").GetComponentsInChildren<UnityEngine.UI.Button>().Length==62,"Test card exposes 61 keys and its close action");game.Testing.End();
            var cap=game.Controller.Items.First(x=>x.Kind=="keycap"&&x.Label.Contains("Esc"));game.Menu.SelectTool(3);
            var timer=System.Diagnostics.Stopwatch.StartNew();game.Painter.Edit(cap);timer.Stop();results.Add($"INFO Initial studio open call: {timer.Elapsed.TotalMilliseconds:F1} ms");
            Check(game.Painter.Editing&&game.Painter.PreparingSurface,"Studio opens before asynchronous surface preparation finishes");
            double started=Time.realtimeSinceStartupAsDouble;int frames=0;float longestFrame=0;
            while(game.Painter.PreparingSurface){if(Time.realtimeSinceStartupAsDouble-started>12)throw new Exception("Surface preparation timeout");yield return null;frames++;longestFrame=Mathf.Max(longestFrame,Time.unscaledDeltaTime);}
            Check(frames>1,"Windows preparation spans multiple rendered frames");results.Add($"INFO Preparation: {frames} frames, {(Time.realtimeSinceStartupAsDouble-started)*1000:F1} ms elapsed, {longestFrame*1000:F1} ms longest frame");
            var camera=GameObject.Find("Studio camera").GetComponent<Camera>();float before=camera.orthographicSize;
            var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();InputSystem.QueueDeltaStateEvent(mouse.scroll,new Vector2(0,120));yield return null;yield return null;
            Check(camera.orthographicSize<before*.93f,"A wheel notch gives a clearly visible studio zoom step");
            game.Painter.CloseEditor();timer.Restart();game.Painter.Edit(cap);timer.Stop();Check(!game.Painter.PreparingSurface,"Reopening the same key reuses its prepared surface");results.Add($"INFO Cached studio open call: {timer.Elapsed.TotalMilliseconds:F1} ms");game.Painter.CloseEditor();
            WorkshopPaintVerify.Run(game,Check);
            game.Menu.SelectTool(0);game.Testing.Begin();yield return null;
            var ready=game.Controller.Items.First(x=>x.Stage==4&&x.Fitted&&x.Id!="Keycap_00");game.Testing.LooseSwitch=null;game.Press(ready);yield return null;
            Check(game.Testing.Count>0,"Keyboard test records a working key in the Windows player");
        }
    }
}
