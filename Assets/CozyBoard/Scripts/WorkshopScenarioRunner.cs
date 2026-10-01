using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    // Explicitly attached by QA only. Normal game sessions never create this component.
    public sealed class WorkshopScenarioRunner:MonoBehaviour {
        public string Filter="";
        public string ReportPath;
        bool exitAfterRun;
        public bool Done{get;private set;}
        public string CurrentSuite{get;private set;}
        public int Passed{get;private set;}
        public int Failed{get;private set;}
        readonly List<string> results=new();
        string path;int tutorialPref;bool tutorialHadPref;UnityEngine.InputSystem.InputSettings.BackgroundBehavior priorBackground;bool inputSettingsChanged;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){
            var args=Environment.GetCommandLineArgs();if(!args.Contains("--cozy-scenarios"))return;
            var game=FindFirstObjectByType<WorkshopGameMode>();if(game)game.VerificationSavePath=Path.Combine(Application.temporaryCachePath,"adversarial-save.json");
            var runner=new GameObject("Adversarial verification").AddComponent<WorkshopScenarioRunner>();runner.exitAfterRun=true;
            int suite=Array.IndexOf(args,"--cozy-scenario-suite");if(suite>=0&&suite+1<args.Length)runner.Filter=args[suite+1];
            int report=Array.IndexOf(args,"--cozy-scenario-report");if(report>=0&&report+1<args.Length)runner.ReportPath=args[report+1];Application.runInBackground=true;
        }
        void Check(bool condition,string label){results.Add((condition?"PASS ":"FAIL ")+CurrentSuite+": "+label);if(condition)Passed++;else Failed++;Write();}
        void Write(){if(!string.IsNullOrEmpty(path))File.WriteAllLines(path,results);}
        // Batch players have no presented surface; visual QA uses the Editor render capture.
        public static void CaptureScreenshot(string destination){if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(destination);}
        void Log(string message,string stack,LogType type){if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;results.Add("FAIL CONSOLE "+message+"\n"+stack);Failed++;Write();}
        IEnumerator Start(){
            path=string.IsNullOrEmpty(ReportPath)?Path.GetFullPath("Logs/adversarial-"+(string.IsNullOrEmpty(Filter)?"all":Filter)+".txt"):Path.GetFullPath(ReportPath);Directory.CreateDirectory(Path.GetDirectoryName(path));
            priorBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;inputSettingsChanged=true;
            tutorialHadPref=PlayerPrefs.HasKey("CozyBoard.TutorialComplete");tutorialPref=PlayerPrefs.GetInt("CozyBoard.TutorialComplete");Application.logMessageReceived+=Log;
            yield return null;yield return null;var g=FindFirstObjectByType<WorkshopGameMode>();
            if(g==null||string.IsNullOrEmpty(g.VerificationSavePath)){CurrentSuite="safety";Check(false,"An isolated save is required");Done=true;yield break;}
            foreach(string suite in new[]{"Assembly","Transition","Repair","Progression","Pointer","Tools","Mouse"}){
                if(!string.IsNullOrEmpty(Filter)&&!suite.Equals(Filter,StringComparison.OrdinalIgnoreCase))continue;
                CurrentSuite=suite;results.Add("BEGIN "+suite);Write();
                var type=typeof(WorkshopScenarioRunner).Assembly.GetType("CozyBoard.Workshop"+suite+"Scenarios");
                if(type==null){Check(false,"Scenario suite is missing");continue;}
                IEnumerator routine=null;try{routine=(IEnumerator)type.GetMethod("Run").Invoke(null,new object[]{g,(Action<bool,string>)Check});}catch(Exception error){Check(false,"Cannot start suite: "+error);}
                if(routine!=null){
                    var stack=new Stack<IEnumerator>();stack.Push(routine);float deadline=Time.realtimeSinceStartup+240;
                    while(stack.Count>0){
                        bool more=false;object next=null;Exception fault=null;
                        try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception error){fault=error;}
                        if(fault!=null){Check(false,"Unhandled scenario exception: "+fault);break;}
                        if(Time.realtimeSinceStartup>deadline){Check(false,"Suite exceeded four minutes");break;}
                        if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
                    }
                }
                results.Add("END "+suite);Write();
                g.RepairBench.ClosePartInspection();g.Tools.ResetState();g.Experience.EndInspection();g.Experience.CancelPacking();g.Shop.Close();g.StoryUI.Hide();g.Menu.ClosePanels();g.Painter.CloseEditor();g.Testing.End();yield return null;
            }
            CurrentSuite="finished";results.Add("TOTAL PASS "+Passed+" FAIL "+Failed);Write();Done=true;RestorePreferences();Application.logMessageReceived-=Log;if(exitAfterRun)Application.Quit(Failed>0?1:0);
        }
        void RestorePreferences(){if(inputSettingsChanged){UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=priorBackground;inputSettingsChanged=false;}if(tutorialHadPref)PlayerPrefs.SetInt("CozyBoard.TutorialComplete",tutorialPref);else PlayerPrefs.DeleteKey("CozyBoard.TutorialComplete");PlayerPrefs.Save();}
        void OnDestroy(){Application.logMessageReceived-=Log;if(!string.IsNullOrEmpty(path))RestorePreferences();}
    }
}
