using System;
using System.IO;
using UnityEditor;
namespace CozyBoard.Editor {
 [InitializeOnLoad]
 public static class WorkshopBackgroundBuild {
  static WorkshopBackgroundBuild(){EditorApplication.update+=Tick;}
  static void Tick(){if(!SessionState.GetBool("Cozy.BackgroundBuild",false)||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)return;SessionState.SetBool("Cozy.BackgroundBuild",false);if(SessionState.GetBool("Cozy.WindowsBuild",false)){SessionState.SetBool("Cozy.WindowsBuild",false);BuildWindows();return;}
   try{var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/CozyBoard/Scenes/Workbench.unity"},locationPathName="Builds/macOS/Cozy Board.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});File.WriteAllText("Verification/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);}catch(Exception ex){File.WriteAllText("Verification/build-result.txt",ex.ToString());}
  }
  public static void BuildWindows(){
   const string reportPath="Verification/windows-build-result.txt";File.WriteAllText(reportPath,"Running");
   try{PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=UnityEngine.FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/CozyBoard/Scenes/Workbench.unity"},locationPathName="Builds/Windows/CozyBoard.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
    File.WriteAllText(reportPath,report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize);
    if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded||report.summary.totalErrors>0||!File.Exists("Builds/Windows/CozyBoard.exe"))throw new Exception("Windows build failed validation: executable must exist and report must contain no errors");
   }catch(Exception ex){File.AppendAllText(reportPath,"\n"+ex);throw;}
  }
  public static void RequestWindows(){File.WriteAllText("Verification/windows-build-result.txt","Queued");SessionState.SetBool("Cozy.WindowsBuild",true);SessionState.SetBool("Cozy.BackgroundBuild",true);EditorApplication.QueuePlayerLoopUpdate();}
  public static void Request(){File.WriteAllText("Verification/build-result.txt","Running");SessionState.SetBool("Cozy.BackgroundBuild",true);EditorApplication.QueuePlayerLoopUpdate();}
 }
}
