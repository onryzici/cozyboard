using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CozyBoard.Editor {
    public static class WorkshopFreeVerify {
        public static void Run() {
            var c=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();c.Initialize();var game=c.Game;game.NewOrder();
            var report=new List<string>();void Check(bool value,string name){if(!value)throw new Exception("FREE ASSEMBLY: "+name);report.Add("PASS "+name);}
            Vector3 At(WorkshopItem p)=>c.Lookup["Case"].transform.TransformPoint(p.Slot);
            Check(game.Installed==0,"Fresh order is empty");
            Check(game.Stock.Select(p=>p.Stage).Distinct().Count()==4,"All four supplies available simultaneously");
            Check(UnityEngine.Object.FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).Length==4,"Four physical open supply packages");
            Check(game.Menu&&game.Menu.SaveButton&&game.Menu.OrderButton&&game.Menu.SettingsButton&&game.Menu.UndoButton,"Painted HUD actions connected");
            Check(!game.TargetMarker.enabled,"No persistent large placement guide");
            // Cap is intentionally placed before PCB, plate, or any switches.
            var cap=c.Lookup["Keycap_47"];
            Check(c.PaintAt(4,At(cap)),"Any category can be installed first");
            Check(cap.Fitted&&game.Installed==1,"Pointer chooses destination key, not an ordered source key");
            Check(!c.PaintAt(4,At(cap)),"A held stroke never fills an occupied socket twice");
            Check(game.Installed==1,"Duplicate stroke consumes no extra pieces");
            game.Undo();Check(!cap.Fitted&&game.Installed==0,"Undo removes exactly the last installed piece");
            var pcb=c.Lookup["PCB"];var pos=pcb.transform.position;
            c.BeginDrag(pcb,c.ViewCamera.WorldToScreenPoint(pos));pcb.transform.position=new Vector3(-9,1,-4);c.FinishDrag();
            Check(!pcb.Fitted&&Vector3.Distance(pos,pcb.transform.position)<.001f,"Outside drop restores source position");
            // Randomized interleaving proves there is no whole-board switch/keycap ordering gate.
            var parts=c.Items.Where(p=>p.Stage>0).OrderBy(p=>p.Id.GetHashCode()).ToArray();
            foreach(var part in parts){Check(c.PaintAt(part.Stage,At(part)),"Place "+part.Id);Check(part.Fitted,"Fitted "+part.Id);}
            Check(game.Completed&&game.Installed==124,"All 124 pieces complete in mixed order");
            game.NewOrder();
            // Sweep a continuous row, including occupied intermediate samples.
            var row=c.Items.Where(p=>p.Kind=="keycap"&&Mathf.Abs(p.Slot.z-.8f)<.01f).OrderBy(p=>p.Slot.x).ToArray();
            for(float x=-3;x<3;x+=.03f)c.PaintAt(3,c.Lookup["Case"].transform.TransformPoint(new Vector3(x,.5f,.8f)));
            Check(c.Items.Count(p=>p.Kind=="switch"&&p.Fitted)==row.Length,"Dense continuous sweep installs exactly one switch per crossed socket");
            var wide=c.Items.First(p=>p.Kind=="keycap"&&p.Label.Contains("Space"));
            var supply=game.SupplyItem(4);c.BeginDrag(supply,c.ViewCamera.WorldToScreenPoint(supply.transform.position));
            supply.transform.position=At(wide);Check(c.SnapCandidate(supply)==wide,"Generic cap adapts to the spacebar destination");c.FinishDrag();
            Check(wide.Fitted&&!supply.Fitted,"Destination mesh and legend are installed, source token returns to stock");
            string saved=game.SerializeProgress();int installed=game.Installed;game.NewOrder();game.RestoreProgress(saved);
            Check(game.Installed==installed&&wide.Fitted,"Save/restore preserves free assembly");
            Check(game.Audio.Keys.Length==8&&game.Audio.Keys.All(a=>a&&a.channels==1),"Eight recorded MechVibes mono switch samples");
            Check(game.Audio.Music&&game.Audio.Music.length>120,"LittleSwitch CC0 lo-fi recording imported");
            Check(game.Audio.Music.loadType==AudioClipLoadType.Streaming,"Long music streams");
            Check(game.Audio.Keys.All(a=>a.loadType==AudioClipLoadType.DecompressOnLoad),"Recorded clicks available without streaming latency");
            Check(PlayerSettings.defaultScreenWidth==3840&&PlayerSettings.defaultScreenHeight==2160,"Native UHD rendering defaults");
            foreach(string shader in new[]{"CozyBoard/Painted","CozyBoard/FlatWorkspace","CozyBoard/PlacementGuide","CozyBoard/PaintedSilhouette"})Check(!ShaderUtil.ShaderHasError(Shader.Find(shader)),"Shader "+shader);
            Check(c.ShadowMaterial.shader.name=="CozyBoard/PaintedSilhouette","Model silhouettes drive offset painted shadows");
            game.NewOrder();
            foreach(var item in c.Items){PrefabUtility.RecordPrefabInstancePropertyModifications(item);PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Visual);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Hitbox);}
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Verification");File.WriteAllLines("Verification/gameplay-checks.txt",report);Debug.Log("COZY_FREE_ASSEMBLY_PASSED "+report.Count);
        }
    }
}
