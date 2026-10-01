using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
    public static class WorkshopRepairBenchVerify {
        public static void Run(){
            const string scene="Assets/CozyBoard/Scenes/Workbench.unity";
            EditorSceneManager.OpenScene(scene);
            var c=Object.FindFirstObjectByType<WorkshopController>();c.Initialize();c.SendMessage("Start");var g=c.Game;g.Experience.Initialize(g);
            g.VerificationSavePath=Path.GetFullPath("Logs/repair-bench-test-save.json");var checks=new List<string>();
            void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);}
            void Remove(WorkshopItem item){g.Testing.Removed(item);item.Fitted=false;item.transform.SetParent(c.PartsRoot,true);g.PresentStock();g.Refresh();}
            void Capture(string name){g.RepairBench.SendMessage("Update");WorkshopVarietyVerify.Capture(c,"Logs/repair-bench-"+name+".png");}
            try{
                g.StoryUI.Hide();g.StartStory();g.Story.openingRead=true;g.Story.Record(WorkshopStory.FirstEvent);g.Experience.Continue();g.Shop.Close();
                var station=g.RepairBench;var board=c.Lookup["Case"].transform;
                var original=GameObject.Find("FlatWorkspace2D").GetComponent<MeshRenderer>();
                var repairSurface=GameObject.Find("Matching painted repair surface").GetComponent<MeshRenderer>();
                Check(repairSurface.sharedMaterial.shader==original.sharedMaterial.shader,"Both desks use the same authored surface and mat shader");
                Check(repairSurface.sharedMaterial.GetColor("_Mat")==original.sharedMaterial.GetColor("_Mat"),"Repair mat uses the assembly mat colour");
                Check(repairSurface.GetComponent<MeshFilter>().sharedMesh==original.GetComponent<MeshFilter>().sharedMesh,"Both desks share the original surface geometry");
                foreach(string shader in new[]{"CozyBoard/FlatWorkspace","CozyBoard/Painted","CozyBoard/KeycapStudio"})Check(!UnityEditor.ShaderUtil.ShaderHasError(Shader.Find(shader)),"Shader compiles: "+shader);
                Capture("assembly");
                Check(station.Visit(true,false)&&!g.IsRepair,"Empty repair desk can be visited before accepting a job");
                Check(!station.UseTestInstrument()&&!g.Testing.Active&&!station.InstrumentConnected,"Empty instrument reports no connected product instead of silently opening a test");Capture("empty");
                Check(station.Visit(false,false),"Return to assembly desk");
                Check(g.AcceptOrder("ece-repair"),"Accept repair order");g.Menu.ClosePanels();
                Check(board.position.x>12&&!station.AtRepair,"Customer product arrives at repair desk while player stays at assembly desk");
                var pose=board.position;Check(station.Visit(true,false),"Travel to repair desk");
                Check(c.ViewTarget==WorkshopRepairBench.Origin&&board.position==pose,"Travel moves the camera, not the product");
                Check(c.Lookup["KeyPuller"].transform.position.x>12,"Repair tools are on the repair desk");
                Check(!Object.FindObjectsByType<WorkshopSupply>(FindObjectsSortMode.None).Any(),"Repair desk does not use assembly supply cartons");
                Capture("received");
                Check(station.UseTestInstrument()&&station.InstrumentConnected,"Physical test dial starts measurement and connects its USB lead");
                var plug=GameObject.Find("Instrument USB plug").transform;
                Check(Vector3.Distance(plug.position,GameObject.Find("Keyboard USB socket").transform.position+board.forward*.20f)<.001f,"USB lead attaches to the physical keyboard socket");
                var keysByPosition=g.TestKeys.OrderBy(x=>x.Slot.x).ToArray();
                var fault=c.Lookup[g.Repair.capId];var healthy=keysByPosition.First(x=>x!=fault);g.Press(healthy);
                int goodIndex=Array.IndexOf(keysByPosition,healthy);
                Check(GameObject.Find("Three key indicator "+goodIndex).GetComponent<MeshRenderer>().sharedMaterial.name=="Instrument signal light","A measured healthy key lights its corresponding physical indicator green");
                Check(GameObject.Find("Tester status").GetComponent<TMP_Text>().text.Contains("SİNYAL VAR"),"Instrument LCD reports the measured healthy signal");
                g.Press(fault);int faultIndex=Array.IndexOf(keysByPosition,fault);
                Check(GameObject.Find("Three key indicator "+faultIndex).GetComponent<MeshRenderer>().sharedMaterial.name=="Instrument no signal light","Only the measured faulty key lights its physical indicator red");
                var lcd=GameObject.Find("Tester status").GetComponent<TMP_Text>();
                Check(lcd.text.Contains(fault.Label.Replace("Tuş ",""))&&lcd.text.Contains("SİNYAL YOK"),"LCD identifies the actual key with no signal");Capture("instrument-fault");
                foreach(var key in g.TestKeys)g.Press(key);Check(g.Repair.diagnosed&&g.Testing.Count==2,"Test instrument identifies one faulty key");
                Check(!station.UseTestInstrument()&&!g.Testing.Active&&!station.InstrumentConnected,"Pressing the physical dial again stops measurement and disconnects the lead");
                var capItem=c.Lookup[g.Repair.capId];var sw=c.Lookup[g.Repair.switchId];Remove(capItem);Remove(sw);
                Check(!station.UseTestInstrument()&&!g.Testing.Active&&GameObject.Find("Tester status").GetComponent<TMP_Text>().text=="SWITCH EKSİK","Device explains why measurement cannot start with missing parts");
                Check(capItem.Visual.enabled&&capItem.transform.position.x>12,"Recovered cap remains visible in repair tray during switch stage");
                Check(Vector3.Distance(capItem.transform.position,sw.transform.position)>1,"Recovered cap and spare have separate tray compartments");
                Check(g.Shop.Purchase(3),"A single replacement switch can be purchased");g.Shop.Close();
                Check(!g.CanInstall(sw)&&!sw.Visual.enabled,"Purchased spare remains sealed and cannot reuse the removed switch");
                var removed=GameObject.Find("Removed faulty switch");
                Check(removed&&removed.GetComponent<WorkshopItem>()==null&&removed.layer==9,"Removed faulty switch is a separate inspectable object with no installation identity");
                Check(!station.OpenReplacementPacket(),"Packet cannot open before the removed switch is inspected");
                Check(station.OpenPartInspection()&&station.DetailOpen,"Faulty part opens a live close-up inspection");
                station.TurnInspectedSwitch();Check(g.Repair.oldSwitchInspected,"Turning the old switch reveals its actual underside and records inspection");Capture("old-part-inspection");station.ClosePartInspection();
                Check(station.OpenReplacementPacket()&&g.Repair.spareOpened&&sw.Visual.enabled,"Opening the sealed packet reveals a distinct new switch");
                Check(!g.CanInstall(sw),"Fresh switch must have its pins checked before insertion");
                Check(station.OpenPartInspection(true),"Fresh spare opens its own pin inspection");station.TurnInspectedSwitch();Check(g.Repair.spareChecked,"Fresh spare pin inspection is recorded separately");Capture("new-part-inspection");station.ClosePartInspection();
                Check(g.CanInstall(sw),"Only the opened and checked new spare becomes installable");Capture("disassembled");
                string saved=g.SerializeProgress();g.RestoreProgress(saved);
                Check(g.RepairBench.AtRepair&&c.ViewTarget==WorkshopRepairBench.Origin&&!capItem.Fitted&&g.Repair.diagnosed,"Reload returns to repair desk and preserves diagnosis and removed cap");
                Check(g.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==60,"Reload retains all sixty unaffected keycaps while the faulty switch is removed");
                Check(g.Repair.oldSwitchInspected&&g.Repair.spareOpened&&g.Repair.spareChecked&&GameObject.Find("Removed faulty switch"),"Reload preserves old-part separation, unboxing and fresh pin inspection");
                var from=c.ViewCamera.WorldToScreenPoint(sw.transform.position);c.BeginDrag(sw,from);
                c.MoveDrag(c.ViewCamera.WorldToScreenPoint(board.TransformPoint(sw.Slot)));
                Check(c.Dragged==sw&&sw.transform.position.x>12,"Pointer drag stays within repair desk bounds");c.FinishDrag();
                Check(sw.Fitted&&g.Repair.replaced,"Dragged replacement snaps into repaired keyboard");
                Check(GameObject.Find("Removed faulty switch")!=null,"Old faulty switch stays in its dish after the new switch is fitted");
                Check(g.Testing.Count==0,"Replacing the faulty switch clears all old measurements for a fresh three-key retest");
                Check(c.PaintAt(4,board.TransformPoint(capItem.Slot)),"Recovered cap reinstalls from tray");
                station.UseTestInstrument();g.Press(capItem);Check(g.Testing.Count==1&&!g.Testing.Passed,"Testing only the replaced key cannot complete final verification");foreach(var key in g.TestKeys)g.Press(key);Check(g.Testing.Passed,$"Repaired keyboard passes final three-key check (complete={g.Completed}, active={g.Testing.Active}, count={g.Testing.Count}, loose={g.Testing.LooseSwitch}, ready={g.RepairReady}, away={station.AwayFromProduct})");Check(Enumerable.Range(0,3).All(i=>GameObject.Find("Three key indicator "+i).GetComponent<MeshRenderer>().sharedMaterial.name=="Instrument signal light"),"All three physical indicators turn green after final verification");
                Check(GameObject.Find("Tester status").GetComponent<TMP_Text>().text.Contains("TEST TAMAM"),"Physical LCD confirms the completed three-key retest");Capture("instrument-passed");g.Testing.End();
                var prior=board.position;g.Experience.BeginPacking();Check(g.Experience.Packing,"Completed repair enters packaging");g.Experience.CancelPacking();
                Check(board.position==prior&&c.ViewTarget==WorkshopRepairBench.Origin,"Cancelling packaging restores repair station and camera");
                station.Visit(false,false);Check(board.position==prior,"Leaving repair desk keeps customer's keyboard in place");
                station.Visit(true,false);g.Menu.ClosePanels();Capture("complete");
                Check(g.FinishDelivery(),"Repair delivery finishes normally");
                Check(!station.AtRepair&&c.ViewTarget==Vector3.zero&&c.Lookup["KeyPuller"].transform.position.x<12,"Delivery returns to assembly desk and restores tools");
                foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Where(x=>x.transform.IsChildOf(GameObject.Find("Workstation navigation").transform))){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Navigation text fits: "+text.text);}
                File.WriteAllLines("Logs/repair-bench-checks.txt",checks);Debug.Log("COZY_REPAIR_BENCH_VERIFIED "+checks.Count);
            }finally{g.VerificationSavePath=null;EditorSceneManager.OpenScene(scene);}
        }
    }
}
