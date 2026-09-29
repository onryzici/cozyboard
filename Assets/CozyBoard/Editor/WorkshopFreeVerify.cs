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
            Check(game.CurrentStage==1&&game.Stock.All(p=>p.Stage==1),"Only the PCB stage is available at the start");
            Check(UnityEngine.Object.FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).Length==4,"Four physical open supply packages");
            Check(game.Menu&&game.Menu.SaveButton&&game.Menu.OrderButton&&game.Menu.SettingsButton&&game.Menu.PaintButton&&game.Menu.UndoButton,"Painted HUD actions connected");
            Check(game.Painter&&game.Painter.PalettePanel&&game.Painter.EditorPanel&&game.Painter.EditorSurface&&game.Painter.ColorField&&game.Painter.HueField&&game.Painter.DoneButton&&game.Painter.LegendButton&&game.Painter.LegendButtonLabel&&game.Painter.SwatchButtons.Length==WorkshopKeyPainter.Palette.Length&&game.Painter.RoundButton&&game.Painter.SquareButton&&game.Painter.AirbrushButton&&game.Painter.SpongeButton&&game.Painter.DryBrushButton&&game.Painter.SplatterButton&&game.Painter.EraserButton&&game.Painter.LineButton&&game.Painter.RectangleButton&&game.Painter.EllipseButton&&game.Painter.RedoButton&&game.Painter.ClearButton&&game.Painter.BrushSize&&game.Painter.Hardness&&game.Painter.Opacity&&game.Painter.Stabilization,"Professional brush, shape and legend studio connected");
            Check(game.GetComponent<WorkshopCursor>()&&game.GetComponent<WorkshopCursor>().Pointer,"Custom workshop cursor connected");
            Check(!game.TargetMarker.enabled,"No persistent large placement guide");
            var cap=c.Lookup["Keycap_47"];
            Check(!c.PaintAt(4,At(cap))&&!cap.Fitted,"Keycaps cannot be installed before the internal assembly");
            Check(!c.PaintAt(3,At(cap)),"Switches cannot be installed before PCB and plate");
            var pcb=c.Lookup["PCB"];var pos=pcb.transform.position;
            c.BeginDrag(pcb,c.ViewCamera.WorldToScreenPoint(pos));pcb.transform.position=new Vector3(-9,1,-4);c.FinishDrag();
            Check(!pcb.Fitted&&Vector3.Distance(pos,pcb.transform.position)<.001f,"Outside drop restores source position");
            Check(c.PaintAt(1,At(pcb))&&game.CurrentStage==2,"PCB unlocks the plate stage");
            var plate=c.Lookup["Plate"];Check(c.PaintAt(2,At(plate))&&game.CurrentStage==3,"Plate unlocks the switch stage");
            Check(!c.PaintAt(4,At(cap)),"Keycaps stay locked while switches remain");
            foreach(var part in c.Items.Where(p=>p.Stage==3).OrderBy(p=>p.Id))Check(c.PaintAt(3,At(part)),"Place "+part.Id);
            Check(game.CurrentStage==4,"All switches unlock the keycap stage");
            foreach(var part in c.Items.Where(p=>p.Stage==4).OrderBy(p=>p.Id))Check(c.PaintAt(4,At(part)),"Place "+part.Id);
            Check(game.Completed&&game.Installed==124,"Ordered assembly completes all 124 pieces");
            game.NewOrder();
            Check(c.PaintAt(1,At(c.Lookup["PCB"]))&&c.PaintAt(2,At(c.Lookup["Plate"])),"Assembly reaches the switch sweep stage");
            var row=c.Items.Where(p=>p.Kind=="keycap"&&Mathf.Abs(p.Slot.z-.8f)<.01f).OrderBy(p=>p.Slot.x).ToArray();
            for(float x=-3;x<3;x+=.03f)c.PaintAt(3,c.Lookup["Case"].transform.TransformPoint(new Vector3(x,.5f,.8f)));
            Check(c.Items.Count(p=>p.Kind=="switch"&&p.Fitted)==row.Length,"Dense continuous sweep installs exactly one switch per crossed socket");
            foreach(var part in c.Items.Where(p=>p.Stage==3&&!p.Fitted).ToArray())Check(c.PaintAt(3,At(part)),"Finish switch "+part.Id);
            var wide=c.Items.First(p=>p.Kind=="keycap"&&p.Label.Contains("Space"));
            var supply=game.SupplyItem(4);c.BeginDrag(supply,c.ViewCamera.WorldToScreenPoint(supply.transform.position));
            supply.transform.position=At(wide);Check(c.SnapCandidate(supply)==wide,"Generic cap adapts to the spacebar destination");c.FinishDrag();
            Check(wide.Fitted&&!supply.Fitted,"Destination mesh and legend are installed, source token returns to stock");
            var customColor=new Color(.95f,.12f,.10f);var unpaintedColor=game.Painter.Sample(wide,new Vector2(.5f,.5f));game.Painter.DrawLine(wide,new Vector2(.2f,.2f),new Vector2(.8f,.8f),customColor);
            var paintedColor=game.Painter.Sample(wide,new Vector2(.5f,.5f));
            Check(game.Painter.HasPaint(wide.Id)&&Vector3.Distance(new Vector3(paintedColor.r,paintedColor.g,paintedColor.b),new Vector3(unpaintedColor.r,unpaintedColor.g,unpaintedColor.b))>.08f,"Installed keycap accepts a custom brush stroke");
            var savedPaintedColor=paintedColor;
            game.Painter.Edit(wide);game.Painter.ClearCurrent();Check(!game.Painter.HasPaint(wide.Id),"Clear key restores the selected keycap");
            game.Painter.Undo();Check(game.Painter.HasPaint(wide.Id),"Paint undo restores cleared artwork");
            game.Painter.Redo();Check(!game.Painter.HasPaint(wide.Id),"Paint redo reapplies the clear action");
            game.Painter.Undo();Check(game.Painter.HasPaint(wide.Id),"Artwork remains editable after undo and redo");
            game.Painter.ToggleLegend();Check(!game.Painter.LegendIsVisible(wide.Id),"Selected key legend can be hidden independently");game.Painter.CloseEditor();
            string saved=game.SerializeProgress();int installed=game.Installed;game.NewOrder();game.RestoreProgress(saved);
            Check(game.Installed==installed&&wide.Fitted,"Save/restore preserves ordered assembly");
            paintedColor=game.Painter.Sample(wide,new Vector2(.5f,.5f));
            Check(game.Painter.HasPaint(wide.Id)&&Vector3.Distance(new Vector3(paintedColor.r,paintedColor.g,paintedColor.b),new Vector3(savedPaintedColor.r,savedPaintedColor.g,savedPaintedColor.b))<.025f,"Save/restore preserves keycap drawing texture");
            Check(!game.Painter.LegendIsVisible(wide.Id),"Save/restore preserves key legend visibility");
            Check(game.Audio.Keys.Length==8&&game.Audio.Keys.All(a=>a&&a.channels==1),"Eight recorded MechVibes mono switch samples");
            Check(game.Audio.Music&&game.Audio.Music.length>120,"LittleSwitch CC0 lo-fi recording imported");
            Check(game.Audio.Music.loadType==AudioClipLoadType.Streaming,"Long music streams");
            Check(game.Audio.Keys.All(a=>a.loadType==AudioClipLoadType.DecompressOnLoad),"Recorded clicks available without streaming latency");
            Check(PlayerSettings.defaultScreenWidth==3840&&PlayerSettings.defaultScreenHeight==2160,"Native UHD rendering defaults");
            foreach(string shader in new[]{"CozyBoard/Painted","CozyBoard/FlatWorkspace","CozyBoard/PlacementGuide","CozyBoard/PaintedSilhouette","CozyBoard/RoundedKeyUI"})Check(!ShaderUtil.ShaderHasError(Shader.Find(shader)),"Shader "+shader);
            Check(c.ShadowMaterial.shader.name=="CozyBoard/PaintedSilhouette","Model silhouettes drive offset painted shadows");
            game.NewOrder();
            foreach(var item in c.Items){PrefabUtility.RecordPrefabInstancePropertyModifications(item);PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Visual);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Hitbox);}
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Verification");File.WriteAllLines("Verification/gameplay-checks.txt",report);Debug.Log("COZY_ORDERED_ASSEMBLY_PASSED "+report.Count);
        }
    }
}
