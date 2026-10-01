using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    // Adversarial repair scenarios. The caller must install an isolated save path first.
    public static class WorkshopRepairScenarios {
        static IEnumerator Settle(Func<bool> pending,float seconds=4){
            float until=Time.realtimeSinceStartup+seconds;
            while(pending()&&Time.realtimeSinceStartup<until)yield return null;
        }
        static IEnumerator Remove(WorkshopGameMode game,WorkshopItem item,string tool,Action<bool,string> check,string label){
            check(game.Tools.TrySelect(game.Controller.Lookup[tool]),label+": select tool");
            check(game.Tools.TryRemove(item),label+": begin removal");
            yield return Settle(()=>game.Tools.Busy);
            check(!game.Tools.Busy&&!item.Fitted,label+": complete removal");game.Tools.Deselect();
        }
        static void Reload(WorkshopGameMode game,Action<bool,string> check,string label){
            check(game.TrySaveQuiet(),label+": isolated save succeeds");game.LoadProgress();
        }
        static bool PinsHiddenWithPart(WorkshopGameMode game,WorkshopItem part){
            var pins=part.transform.Find("New replacement electrical pins");
            return !pins||!pins.gameObject.activeInHierarchy||pins.GetComponentsInChildren<MeshRenderer>().All(x=>!x.enabled);
        }

        public static IEnumerator Run(WorkshopGameMode game,Action<bool,string> check){
            check(!string.IsNullOrEmpty(game.VerificationSavePath),"Repair scenarios use an isolated save");
            if(string.IsNullOrEmpty(game.VerificationSavePath))yield break;
            game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.StoryUI.Hide();game.Shop.OpenOrders();yield return null;
            var offerButton=GameObject.Find("Available orders/Offer 0/Accept offer");var accept=offerButton?offerButton.GetComponent<Button>():null;check(accept!=null,"Repair offer has a real accept button");
            if(accept!=null)accept.onClick.Invoke();else check(game.AcceptOrder(WorkshopStory.FirstRepair.Id),"Repair fixture accepts repair offer");
            yield return null;game.Menu.ClosePanels();game.Shop.Close();game.RepairBench.Visit(true,false);yield return null;
            check(game.IsRepair&&game.Repair!=null&&game.Completed,"Repair begins fully assembled");if(!game.IsRepair||game.Repair==null)yield break;
            var controller=game.Controller;var cap=controller.Lookup[game.Repair.capId];var sw=controller.Lookup[game.Repair.switchId];
            var healthy=game.TestKeys.First(x=>x!=cap);string orderId=game.ActiveOrder.instanceId;
            check(!game.Repair.diagnosed&&!game.Repair.replaced,"Fresh repair has no diagnosis or replacement flags");
            check(!game.RepairBench.OpenPartInspection()&&!game.RepairBench.OpenPartInspection(true),"Neither inspection opens before any part is removed");
            check(!game.RepairBench.OpenReplacementPacket(),"Cannot unbox before inspecting the old part");
            game.Tools.TrySelect(controller.Lookup["KeyPuller"]);
            check(!game.Tools.TryRemove(cap)&&!game.Tools.TryRemove(healthy),"Undiagnosed cap and healthy cap cannot be removed");game.Tools.Deselect();
            game.Deliver();check(!game.Experience.Packing&&!game.Testing.Passed,"Untested faulty repair cannot enter shipping");game.Testing.End();
            game.Testing.Begin();game.Press(healthy);yield return null;
            check(!game.Repair.diagnosed&&game.Testing.Count==1,"One healthy key does not diagnose the faulty key");
            game.RepairBench.Visit(false,false);check(!game.Testing.Active&&!game.RepairBench.InstrumentConnected,"Leaving repair station disconnects active tester");
            check(!game.RepairBench.OpenPartInspection(),"Part inspection cannot open from the assembly station");
            game.Testing.Begin();check(game.RepairBench.Moving&&!game.Testing.Active,"Test requested from the wrong station first travels to the repair desk");
            yield return Settle(()=>game.RepairBench.Moving);check(game.RepairBench.AtRepair&&!game.RepairBench.Moving,"Requested repair return settles");
            game.Testing.Begin();game.Press(cap);game.Press(cap);yield return null;
            check(game.Repair.diagnosed&&game.Testing.HasFailed(cap.Id)&&!game.Testing.Passed,"Repeated failing presses diagnose without passing or duplicating measurements");game.Testing.End();
            Reload(game,check,"Diagnosed assembled reload");check(game.Repair.diagnosed&&!game.Repair.replaced&&sw.Fitted&&cap.Fitted&&game.Testing.LooseSwitch==sw.Id,"Diagnosis and original fault survive reload");
            game.Tools.TrySelect(controller.Lookup["Tweezers"]);check(!game.Tools.TryRemove(sw),"Switch cannot be pulled through its fitted cap");game.Tools.Deselect();
            game.Tools.TrySelect(controller.Lookup["KeyPuller"]);check(!game.Tools.TryRemove(healthy),"Diagnosis never permits removal of a healthy neighbour");
            check(game.Tools.TryRemove(cap),"Faulty cap removal starts");check(!game.Tools.TryRemove(cap),"Double removal input cannot start a second animation");
            check(!game.RepairBench.Visit(false),"Station change is rejected during cap pull");game.Experience.ShowMain();check(!game.Experience.MainVisible,"Home is rejected during cap pull");
            game.Shop.Open();check(!game.Shop.IsOpen,"Shop is rejected during cap pull");game.Testing.Begin();check(!game.Testing.Active,"Tester is rejected during cap pull");
            check(!game.TrySaveQuiet(),"Saving halfway through a physical pull is rejected");yield return Settle(()=>game.Tools.Busy);
            check(!cap.Fitted&&!game.Tools.Busy,"Rapid rejected actions leave cap pull complete");game.Tools.Deselect();
            check(game.CanInstall(cap),"Recovered cap can be put back before continuing repair");
            check(controller.PaintAt(4,controller.Lookup["Case"].transform.TransformPoint(cap.Slot)),"Recovering an accidentally removed cap uses normal placement");yield return new WaitForSecondsRealtime(.6f);
            check(game.Completed&&!game.Testing.Passed&&!game.Repair.replaced,"Putting original cap back cannot falsely repair the switch");
            yield return Remove(game,cap,"KeyPuller",check,"Second cap pull");Reload(game,check,"Cap-only removal reload");
            check(!cap.Fitted&&sw.Fitted&&game.Repair.diagnosed&&game.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==60,"Cap-only reload preserves all sixty healthy caps");
            game.Testing.Begin();check(!game.Testing.Active&&!game.RepairBench.InstrumentConnected,"A keyboard missing its cap cannot start final test");
            check(!game.RepairBench.OpenPartInspection(),"Old-switch inspection stays unavailable while the switch is still fitted");
            yield return Remove(game,sw,"Tweezers",check,"Faulty switch pull");
            check(!game.CanInstall(cap)&&!game.CanInstall(sw),"Cap cannot precede switch, sealed spare cannot be installed");
            check(!game.RepairBench.OpenPartInspection(true),"New-pin inspection cannot inspect a sealed spare");
            Reload(game,check,"Both parts removed reload");check(!cap.Fitted&&!sw.Fitted&&!game.Repair.spareOpened&&!game.Repair.oldSwitchInspected,"Both-part reload does not invent inspections");
            check(game.RepairBench.OpenPartInspection(),"Removed original switch opens inspection");
            game.RepairBench.ClosePartInspection();check(!game.Repair.oldSwitchInspected,"Closing inspection without turning does not approve the old switch");
            check(!game.RepairBench.OpenReplacementPacket(),"Closed but unturned old inspection still blocks unboxing");
            game.RepairBench.OpenPartInspection();game.Experience.ShowMain();check(!game.Experience.MainVisible&&game.RepairBench.DetailOpen,"Home cannot strand an open part inspection");
            game.RepairBench.TurnInspectedSwitch();game.RepairBench.TurnInspectedSwitch();check(game.Repair.oldSwitchInspected,"Turning back does not forget the old underside check");game.RepairBench.ClosePartInspection();
            Reload(game,check,"Old switch inspection reload");check(game.Repair.oldSwitchInspected&&!game.Repair.spareOpened&&!game.Repair.spareChecked,"Only completed old-part inspection survives reload");
            // No-supply fixture, followed exclusively by public purchase actions.
            for(int i=3;i<6;i++){game.Shop.Data.stock[i]=0;game.Shop.Data.looseParts[i]=0;}game.Shop.Data.used[1]=false;game.PresentStock();game.Refresh();
            check(!game.Shop.HasSupply(3)&&!game.RepairBench.OpenReplacementPacket()&&game.Shop.IsOpen,"Without a spare the packet action opens supply catalogue");
            int money=game.Shop.Data.credits;game.Shop.Data.credits=0;check(!game.Shop.Purchase(3)&&!game.Shop.HasSupply(3),"No credits cannot create a spare");game.Shop.Data.credits=money;
            check(game.Shop.Purchase(3)&&game.Shop.Data.looseParts[3]==1,"Repair purchase provides precisely one switch");game.Shop.Close();
            check(game.RepairBench.OpenReplacementPacket()&&game.RepairBench.Unpacking,"First unboxing attempt begins");
            Reload(game,check,"Reload during unboxing animation");
            check(!game.RepairBench.Unpacking&&!game.Repair.spareOpened&&game.Repair.oldSwitchInspected&&!game.CanInstall(sw),"Reload interrupts unfinished unboxing and preserves a sealed non-installable spare");
            yield return new WaitForSecondsRealtime(.55f);
            check(!game.Repair.spareOpened,"Cancelled unboxing coroutine cannot open a later restored repair behind the scenes");
            check(game.RepairBench.OpenReplacementPacket()&&game.RepairBench.Unpacking,"Interrupted unboxing can be retried as an exclusive physical operation");
            check(!game.RepairBench.OpenReplacementPacket(),"Double packet click cannot duplicate unboxing");
            check(!game.RepairBench.Visit(false)&&!game.RepairBench.OpenPartInspection(true)&&!game.RepairBench.UseTestInstrument(),"Unboxing rejects travel, premature inspection, and test instrument");
            game.Experience.ShowMain();check(!game.Experience.MainVisible,"Unboxing rejects home transition");
            game.Shop.Open();check(!game.Shop.IsOpen,"Unboxing rejects the laptop shortcut");game.Shop.Close();
            game.Experience.BeginInspection();check(!game.Experience.Inspecting,"Unboxing rejects the visible whole-product inspection toolbar");game.Experience.EndInspection();
            yield return Settle(()=>game.RepairBench.Unpacking,2);check(game.Repair.spareOpened&&!game.Repair.spareChecked&&!game.RepairBench.Unpacking,"Unboxing ends once and does not automatically approve pins");
            check(!game.CanInstall(sw)&&!controller.PaintAt(3,controller.Lookup["Case"].transform.TransformPoint(sw.Slot)),"Opened but unchecked new switch still cannot be installed");
            check(!game.CanPick(sw)&&game.RepairBench.DetailOpen,"Picking an unchecked spare opens the correct pin inspection");game.RepairBench.ClosePartInspection();
            check(!game.Repair.spareChecked,"Closing new-part inspection without turning keeps pins unchecked");
            Reload(game,check,"Opened packet reload");check(game.Repair.spareOpened&&!game.Repair.spareChecked,"Open packet and unchecked pins remain distinct after reload");
            game.RepairBench.OpenPartInspection(true);game.RepairBench.TurnInspectedSwitch();yield return new WaitForSecondsRealtime(.55f);game.RepairBench.ClosePartInspection();
            Reload(game,check,"New pins approved reload");check(game.Repair.spareChecked&&game.CanInstall(sw)&&game.SupplyItem(3)==sw,"Checked spare remains the only installable repair switch");
            // Refund the last available whole set after opening it: no orphaned electrical pins may remain.
            game.Shop.Data.looseParts[3]=0;game.Shop.Data.stock[3]=1;game.PresentStock();game.Refresh();
            check(game.Shop.Refund(3),"Last unopened supply set can be refunded");game.Shop.Close();yield return null;
            check(!game.Shop.HasSupply(3)&&!game.CanInstall(sw)&&!sw.Visual.enabled,"Refunding the final supply withdraws the loose switch");
            check(PinsHiddenWithPart(game,sw),"Withdrawing the spare hides its electrical pins too");
            check(game.Shop.Purchase(3),"Replacement supply can be repurchased without replaying completed pin inspection");game.Shop.Close();
            game.Testing.Begin();check(!game.Testing.Active,"Incomplete repair still cannot enter test despite all approval flags");
            int creditBefore=game.Shop.Data.credits;
            check(controller.PaintAt(3,controller.Lookup["Case"].transform.TransformPoint(sw.Slot)),"Checked replacement installs");yield return new WaitForSecondsRealtime(.6f);
            check(game.Repair.replaced&&game.Testing.Count==0&&game.Shop.Data.looseParts[3]==0,"Replacement consumes one spare and invalidates every earlier measurement");
            game.Menu.UndoButton.onClick.Invoke();
            check(!sw.Fitted&&game.Repair.replaced&&!game.CanInstall(cap)&&game.RepairInstruction().Contains("switch"),"Undoing the new switch preserves its identity and requests refitting before the cap");
            check(controller.PaintAt(3,controller.Lookup["Case"].transform.TransformPoint(sw.Slot)),"Undone new switch refits without buying another spare");yield return new WaitForSecondsRealtime(.6f);
            check(sw.Fitted&&game.Shop.Data.looseParts[3]==0&&game.Testing.Count==0,"Refitting consumes no second spare and still requires final tests");
            Reload(game,check,"Replacement fitted but cap missing reload");check(sw.Fitted&&!cap.Fitted&&game.Repair.replaced&&game.CanInstall(cap),"Fitted replacement reload restores missing original cap and supply-free recovery");
            check(controller.PaintAt(4,controller.Lookup["Case"].transform.TransformPoint(cap.Slot)),"Recovered original cap completes repair assembly");yield return new WaitForSecondsRealtime(.6f);
            game.Testing.Begin();game.Press(cap);game.Press(cap);yield return null;check(game.Testing.Count==1&&!game.Testing.Passed,"Repeated same-key retest cannot satisfy three different targets");
            game.Testing.End();game.Deliver();check(!game.Experience.Packing&&game.Shop.Data.credits==creditBefore,"Partial final test cannot ship or pay a reward");game.Testing.End();
            Reload(game,check,"One retest reload");check(game.Testing.Count==1&&!game.Testing.Passed,"A partial final test stays partial after reload");
            game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return null;check(game.Testing.Passed&&game.Testing.Count==3,"Three distinct retested targets validate the repaired keyboard");game.Testing.End();
            yield return Remove(game,cap,"KeyPuller",check,"Post-test cap removal");check(!game.Testing.Passed&&game.Testing.Count==2,"Removing a passed cap invalidates that target and delivery");
            check(controller.PaintAt(4,controller.Lookup["Case"].transform.TransformPoint(cap.Slot)),"Passed cap can be refitted without purchasing keycaps");yield return new WaitForSecondsRealtime(.6f);
            check(!game.Testing.Passed&&game.Testing.Count==2,"Refitting a removed cap requires its new retest");
            game.Testing.Begin();game.Press(cap);yield return null;check(game.Testing.Passed,"Single invalidated cap retest restores the complete result");game.Testing.End();
            // Legacy v12 completed repairs predate part-inspection flags.
            var legacy=JsonUtility.FromJson<WorkshopGameMode.SaveData>(game.SerializeProgress());legacy.repair.oldSwitchInspected=false;legacy.repair.spareOpened=false;legacy.repair.spareChecked=false;
            game.RestoreProgress(JsonUtility.ToJson(legacy));check(game.Repair.replaced&&game.Completed&&game.Testing.Passed,"Legacy completed v12 repair remains deliverable without new inspection flags");
            game.Deliver();check(game.Experience.Packing,"Completed legacy repair enters packing");
            game.Experience.CancelPacking();check(!game.Experience.Packing&&controller.Lookup["Case"].transform.position.x>12&&game.RepairBench.AtRepair,"Cancelling repair packing restores the product and view to its repair station");
            game.Deliver();for(int i=0;i<3;i++){game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(.95f);}game.Experience.AdvancePacking();yield return new WaitForSecondsRealtime(2);
            check(game.WaitingForOrder&&game.LastDelivery!=null&&game.LastDelivery.orderId==orderId,"Repair shipment finishes exactly its accepted order");int paid=game.Shop.Data.credits;game.Deliver();check(game.Shop.Data.credits==paid,"Repeated repair delivery cannot pay again");
            check(game.AcceptOrder("ece-repair-repeat"),"Second repair offer can be accepted after shipping");game.Menu.ClosePanels();game.Shop.Close();game.RepairBench.Visit(true,false);yield return null;
            check(game.ActiveOrder.instanceId!=orderId&&game.Repair!=null&&!game.Repair.diagnosed&&!game.Repair.replaced&&!game.Repair.oldSwitchInspected&&!game.Repair.spareOpened&&!game.Repair.spareChecked,"Second repair receives a fresh identity and all fresh part flags");
            check(game.Completed&&game.Testing.Count==0&&game.Testing.LooseSwitch==game.Repair.switchId,"Second repair resets final test and receives its own faulty switch");
            var old=GameObject.Find("Removed faulty switch");check(old==null||!old.activeInHierarchy,"Previous customer's removed switch is absent before second repair diagnosis");
            check(game.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==61&&game.ProductItems.Count(x=>x.Stage==3&&x.Fitted)==61,"Second repair starts with sixty-one intact caps and switches");
            Reload(game,check,"Second repair fresh reload");check(game.Completed&&!game.Repair.diagnosed&&!game.Repair.spareOpened&&game.Testing.Count==0,"Second repair reload cannot inherit the first repair's approvals");
        }
    }
}
