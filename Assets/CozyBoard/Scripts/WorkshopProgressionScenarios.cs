using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    // Explicitly driven by the isolated QA runner; never starts itself or chooses a save path.
    public static class WorkshopProgressionScenarios {
        public static IEnumerator Run(WorkshopGameMode game,Action<bool,string> check){
            check(!string.IsNullOrEmpty(game.VerificationSavePath),"Progression scenarios require an isolated save path");
            var controller=game.Controller;var shop=game.Shop;string isolated=game.VerificationSavePath;
            game.StartStory();game.Story.openingRead=true;game.Story.Record(WorkshopStory.FirstEvent);game.Experience.Continue();game.StoryUI.Hide();shop.Close();game.Menu.ClosePanels();
            yield return null;
            check(game.WaitingForOrder&&game.Installed==0,"Idle story exposes no installable product");
            string idle=game.SerializeProgress();
            check(!game.AcceptOrder("missing-order")&&game.SerializeProgress()==idle,"Unknown offer is rejected without changing inventory or story");
            check(!game.CanInstall(controller.Lookup["PCB"])&&game.SupplyItem(1)==null,"Waiting for an order cannot consume a PCB");
            check(game.AcceptOrder("mina-keyboard"),"Keyboard order can be selected after first story milestone");
            string firstId=game.ActiveOrder.instanceId;game.Menu.ClosePanels();
            check(!game.AcceptOrder("ece-repair")&&!game.AcceptOrder("deniz-macro")&&game.ActiveOrder.instanceId==firstId,"An accepted empty workbench cannot be replaced by another offer");
            int balance=shop.Data.credits;
            check(shop.Purchase(5)&&shop.Data.credits==balance-85&&game.Audio.SwitchVoice==2,"Clicky purchase charges the catalogue price and changes the sound profile");
            check(shop.Purchase(8)&&shop.Data.selected[2]==8,"Cap variant purchase selects the new material before assembly");
            balance=shop.Data.credits;
            check(shop.Refund(3)&&shop.Data.credits==balance+60&&shop.Data.stock[3]==0,"Unopened unused switch kit refunds exactly once");
            check(!shop.Refund(3),"Repeated kit refund cannot create money");
            int oldBalance=shop.Data.credits;shop.Data.credits=0;int oldStock=shop.Data.stock[4];
            check(!shop.Purchase(4)&&shop.Data.stock[4]==oldStock&&shop.Data.credits==0,"Insufficient funds do not create a kit or negative balance");shop.Data.credits=oldBalance;
            check(!shop.Purchase(-1)&&!shop.Purchase(9)&&!shop.Select(-1)&&!shop.Refund(9),"Catalogue boundaries reject invalid product identifiers");
            yield return Assemble(game,check,"First full keyboard");
            check(!shop.Select(4)&&shop.Data.selected[1]==5,"Installed switches lock the voice and selected variant");
            check(shop.Data.stock[5]==0&&shop.Data.stock[8]==0&&shop.Data.used.All(x=>x),"Full keyboard consumes one PCB kit, switch kit and cap kit");
            game.Tools.Tightened=15;
            yield return TestAll(game,check,"First full keyboard");
            check(!game.AcceptOrder("ece-repair")&&game.ActiveOrder.instanceId==firstId,"Completed but unshipped work cannot be replaced by a repair offer");
            var board=controller.Lookup["Case"].transform;board.rotation=Quaternion.Euler(0,4,0);game.SaveProgress();game.LoadProgress();
            check(game.ActiveOrder.instanceId==firstId&&game.Completed&&game.Testing.Passed&&Mathf.Abs(Mathf.DeltaAngle(board.eulerAngles.y,4))<.1f,"Reload keeps identity, completed product, tests and board pose");
            check(shop.Data.selected[1]==5&&game.Audio.SwitchVoice==2,"Reload restores the installed switch sound profile");

            // A failed atomic delivery must not pay, advance the chapter, or erase the work.
            string blocked=isolated+".blocked";File.WriteAllText(blocked,"QA save-path blocker");int beforeMail=game.DeliveryMail.Count;int beforeOrder=game.OrderNumber;balance=shop.Data.credits;
            game.VerificationSavePath=Path.Combine(blocked,"save.json");bool failedDelivery=game.FinishDelivery();game.VerificationSavePath=isolated;File.Delete(blocked);
            check(!failedDelivery&&game.Completed&&game.ActiveOrder?.instanceId==firstId&&shop.Data.credits==balance&&game.OrderNumber==beforeOrder&&game.DeliveryMail.Count==beforeMail,"Failed save rolls delivery, credits, order identity and mail back together");
            yield return Ship(game,check,"First full keyboard");
            check(game.WaitingForOrder&&shop.Data.credits==balance+300&&game.LastDelivery.customerId=="mina"&&game.LastDelivery.matches==2,"Matching full keyboard pays 300 once and preserves the chosen customer");
            check(game.LastDelivery.pending&&shop.UnreadMail==1,"New offer list keeps the shipment letter unread");
            balance=shop.Data.credits;int mailCount=game.DeliveryMail.Count;
            check(!game.FinishDelivery()&&game.DeliveryMail.Count==mailCount&&shop.Data.credits==balance,"Duplicate shipment cannot produce another receipt or reward");

            check(game.AcceptOrder("ece-repair"),"Campaign can transition from a full keyboard to repair");game.Menu.ClosePanels();
            check(game.IsRepair&&game.Completed&&game.Repair!=null&&game.MacroFunctions==null&&game.TestCount==3,"Repair creates an assembled keyboard without inheriting macro bindings");
            check(!game.AcceptOrder("deniz-macro"),"Assembled incoming repair still counts as an active order");
            game.RepairBench.Visit(true);yield return WaitUntil(()=>!game.RepairBench.Moving,3,check,"Repair station settles for progression fixture");
            var cap=controller.Lookup[game.Repair.capId];var sw=controller.Lookup[game.Repair.switchId];game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return null;game.Testing.End();
            check(game.Repair.diagnosed&&game.Testing.Count==2,"Repair diagnosis produces exactly two working keys and one fault");
            balance=shop.Data.credits;int allKits=shop.Data.stock.Sum();int previousSpare=shop.Data.looseParts[3];
            check(shop.Purchase(3)&&shop.Data.credits==balance-8&&shop.Data.stock.Sum()==allKits&&shop.Data.looseParts[3]==previousSpare+1,"Repair catalogue sells one eight-credit switch without manufacturing a full kit");
            game.Menu.ClosePanels();shop.Close();game.Tools.TrySelect(controller.Lookup["KeyPuller"]);check(game.Tools.TryRemove(cap),"Campaign repair starts cap extraction");yield return WaitUntil(()=>!game.Tools.Busy,4,check,"Campaign cap extraction finishes");
            game.Tools.TrySelect(controller.Lookup["Tweezers"]);check(game.Tools.TryRemove(sw),"Campaign repair starts switch extraction");yield return WaitUntil(()=>!game.Tools.Busy,4,check,"Campaign switch extraction finishes");game.Tools.Deselect();
            check(game.RepairBench.OpenPartInspection(),"Campaign inspects the old part");game.RepairBench.TurnInspectedSwitch();game.RepairBench.ClosePartInspection();
            check(game.RepairBench.OpenReplacementPacket(),"Campaign opens the sealed replacement");yield return WaitUntil(()=>!game.RepairBench.Unpacking,2,check,"Campaign spare package finishes opening");
            check(game.RepairBench.OpenPartInspection(true),"Campaign inspects the new spare pins");game.RepairBench.TurnInspectedSwitch();game.RepairBench.ClosePartInspection();
            check(controller.PaintAt(3,board.TransformPoint(sw.Slot)),"Campaign installs the inspected replacement");yield return new WaitForSeconds(.8f);
            check(controller.PaintAt(4,board.TransformPoint(cap.Slot)),"Campaign restores the saved customer cap");yield return new WaitForSeconds(.8f);
            check(shop.Data.looseParts[3]==previousSpare&&shop.Data.stock.Sum()==allKits,"Repair consumes exactly its purchased single switch");
            yield return TestAll(game,check,"Campaign repair");balance=shop.Data.credits;
            yield return Ship(game,check,"Campaign repair");
            check(shop.Data.credits==balance+90&&game.Story.Has(WorkshopStory.RepairEvent)&&game.DeliveryMail.Count==2,"Repair reward and chapter event coexist with the archived keyboard receipt");

            check(game.AcceptOrder("deniz-macro"),"Campaign can transition from repair to macro pad");game.Menu.ClosePanels();
            check(game.IsMacro&&game.Installed==0&&game.Repair==null&&game.MacroFunctions.Length==6&&game.MacroFunctions.All(string.IsNullOrEmpty),"Macro starts empty and clears all repair state and old shortcut assignments");
            check(controller.Items.Where(x=>x.Stage>0&&!game.BelongsToProduct(x)).All(x=>!x.Fitted&&!x.Visual.enabled&&!x.Hitbox.enabled),"Macro hides and disables all 55 unused key pairs after a full-size repair");
            check(shop.Purchase(0)&&shop.Purchase(3)&&shop.Purchase(8),"Campaign restocks three macro assembly materials");
            balance=shop.Data.credits;
            check(shop.Purchase(4)&&shop.Refund(4)&&shop.Data.credits==balance,"Unused alternate macro switch kit can be returned at its original price");
            check(shop.Select(3),"Returning an alternate set allows reselection of the stocked linear set");
            yield return Assemble(game,check,"Campaign macro");
            check(shop.Data.looseParts[3]==55&&shop.Data.looseParts[8]==55,"Six-key job leaves 55 switches and 55 caps from opened full kits");
            check(!shop.Refund(3)&&!shop.Refund(8),"An opened partial kit cannot be refunded as an unopened full kit");
            check(!game.Testing.SetFunction(-1,"Kaydet")&&!game.Testing.SetFunction(6,"Kaydet")&&!game.Testing.SetFunction(0,"invalid"),"Macro binding input rejects out-of-range slots and unknown functions");
            game.Testing.Begin();for(int i=0;i<6;i++)game.Testing.SetFunction(i,"Kaydet");foreach(var key in game.TestKeys)game.Press(key);yield return null;
            check(!game.MacroReady&&!game.Testing.Passed&&game.Testing.Count==1,"Six duplicate shortcuts cannot pass the requested macro layout");
            for(int i=0;i<6;i++)game.Testing.SetFunction(i,WorkshopGameMode.DesiredFunctions[i]);foreach(var key in game.TestKeys)game.Press(key);yield return null;
            check(game.Testing.Passed&&game.Testing.Count==6,"Correct bindings and actual key presses pass all six macro keys");
            game.Testing.SetFunction(0,"Yinele");check(!game.Testing.Passed&&game.Testing.Count==5,"Changing a previously tested binding invalidates that key's test");game.Testing.SetFunction(0,"Geri al");game.Press(controller.Lookup["Keycap_01"]);yield return null;game.Testing.End();game.Tools.Tightened=15;
            game.SaveProgress();game.LoadProgress();check(game.IsMacro&&game.Testing.Passed&&game.MacroReady&&shop.Data.looseParts[3]==55,"Reload keeps compact geometry, bindings, per-key results and partial stock");
            balance=shop.Data.credits;yield return Ship(game,check,"Campaign macro");
            check(shop.Data.credits==balance+115&&game.Story.Has(WorkshopStory.MacroEvent)&&game.DeliveryMail.Count==3,"Linear macro for Deniz pays 115 and appends a third unique receipt");
            shop.OpenMail();shop.ReadMail(0);shop.ReadMail(1);shop.ReadMail(2);int paid=shop.Data.credits;shop.ReadMail(0);shop.Close();
            check(shop.UnreadMail==0&&shop.Data.credits==paid,"Reading every archived letter and rereading old letters never changes credits");
            game.SaveProgress();game.LoadProgress();check(game.DeliveryMail.Count==3&&shop.UnreadMail==0&&game.Story.Has(WorkshopStory.RepairEvent)&&game.Story.Has(WorkshopStory.MacroEvent),"Reload preserves all chapter events and read states across three product kinds");

            check(game.AcceptOrder("mina-keyboard"),"Campaign returns from macro to a second full keyboard");game.Menu.ClosePanels();
            check(!game.IsMacro&&!game.IsRepair&&game.KeyCount==61&&game.PartCount==124&&game.Repair==null&&game.MacroFunctions==null,"Full keyboard restores full geometry and clears the previous product's state");
            check(!shop.HasSupply(3)&&!shop.HasSupply(4),"55 remaining loose switches or caps cannot satisfy a 61-key job");
            check(shop.Purchase(0)&&shop.Purchase(3)&&shop.Purchase(8),"Full keyboard can restock alongside partial kits");
            yield return Assemble(game,check,"Second full keyboard");
            check(shop.Data.looseParts[3]==55&&shop.Data.looseParts[8]==55&&shop.Data.stock[3]==0&&shop.Data.stock[8]==0,"61-key assembly combines stock without losing the existing 55 leftover parts");
            check(game.ProductItems.Count(x=>x.Stage==4&&x.Fitted)==61&&game.TestKeys.Length==61,"Returning to a keyboard restores all 61 physical and test keys");
            game.Tools.Tightened=15;yield return TestAll(game,check,"Second full keyboard");
            string fullSave=game.SerializeProgress();

            // Old pre-story saves should remain playable and unlock current offers on delivery.
            var legacy=JsonUtility.FromJson<WorkshopGameMode.SaveData>(fullSave);legacy.version=10;legacy.story=null;legacy.activeOrder=null;legacy.receipt=null;legacy.mail=null;legacy.order=1;
            game.RestoreProgress(JsonUtility.ToJson(legacy));
            check(!game.Story.enabled&&!game.WaitingForOrder&&game.Completed&&game.Testing.Passed,"Version 10 keyboard migrates into playable free workshop without a fabricated customer order");
            balance=shop.Data.credits;check(game.FinishDelivery(),"Legacy free workshop can complete its existing delivery");
            check(game.Story.enabled&&game.Story.OrdersUnlocked&&game.Story.Has(WorkshopStory.LegacyEvent)&&!game.Story.FirstDelivered&&shop.Data.credits==balance+300,"Legacy shipment pays once and unlocks repair/macro offers without fabricating the first chapter");
            game.SaveProgress();game.LoadProgress();check(game.WaitingForOrder&&WorkshopStory.Offers(game.Story).Length==4,"Reload retains migrated free-to-story offers");

            // Invalid saves stay isolated and must not destroy an existing live workshop.
            check(game.AcceptOrder("mina-keyboard"),"Persistence boundary fixture accepts a fresh keyboard");game.Menu.ClosePanels();
            string preserved=game.SerializeProgress();
            foreach(int version in new[]{0,14}){var unsupported=JsonUtility.FromJson<WorkshopGameMode.SaveData>(preserved);unsupported.version=version;game.RestoreProgress(JsonUtility.ToJson(unsupported));check(game.SerializeProgress()==preserved,"Unsupported save version "+version+" leaves current work untouched");}
            File.WriteAllText(isolated,"{ invalid json");game.LoadProgress();check(game.SerializeProgress()==preserved,"Malformed JSON load leaves current live progress untouched");File.WriteAllText(isolated,preserved);
            var orphan=JsonUtility.FromJson<WorkshopGameMode.SaveData>(preserved);orphan.fitted=new[]{"missing-part","Keycap_61"};game.RestoreProgress(JsonUtility.ToJson(orphan));check(game.Installed==0,"Unknown parts and unsupported orphan caps are ignored on restore");game.RestoreProgress(preserved);
            game.RestoreProgress(fullSave);var nullPart=JsonUtility.FromJson<WorkshopGameMode.SaveData>(fullSave);nullPart.fitted=new[]{(string)null}.Concat(nullPart.fitted).ToArray();
            string nullJson=JsonUtility.ToJson(nullPart).Replace("\"fitted\":[\"\",","\"fitted\":[null,");bool threw=false;try{game.RestoreProgress(nullJson);}catch(Exception){threw=true;}
            bool intact=game.Completed&&game.Testing.Passed;game.RestoreProgress(preserved);
            check(!threw&&intact,"A null entry in saved fitted parts is ignored while all legitimate fitted parts and tests survive");
        }

        static IEnumerator Assemble(WorkshopGameMode game,Action<bool,string> check,string label){
            var board=game.Controller.Lookup["Case"].transform;
            foreach(int stage in Enumerable.Range(1,4)){
                bool placed=true;foreach(var item in game.ProductItems.Where(x=>x.Stage==stage&&!x.Fitted).OrderBy(x=>x.Id).ToArray())placed&=game.Controller.PaintAt(stage,board.TransformPoint(item.Slot));
                check(placed,label+": stage "+stage+" installs through normal stock and placement rules");yield return new WaitForSeconds(.8f);
            }
            check(game.Completed,label+": all required parts are fitted");
        }
        static IEnumerator TestAll(WorkshopGameMode game,Action<bool,string> check,string label){
            game.Menu.ClosePanels();game.Shop.Close();game.Testing.Begin();foreach(var key in game.TestKeys)game.Press(key);yield return null;
            check(game.Testing.Passed,label+": actual key presses complete the final test");int count=game.Testing.Count;for(int i=0;i<4;i++)game.Press(game.TestKeys[0]);yield return null;check(game.Testing.Count==count,label+": repeated press cannot inflate test coverage");game.Testing.End();
        }
        static IEnumerator Ship(WorkshopGameMode game,Action<bool,string> check,string label){
            game.Deliver();yield return null;check(game.Experience.PackStep==1,label+": delivery starts physical packing");
            for(int step=1;step<=3;step++){
                game.Experience.AdvancePacking();game.Experience.AdvancePacking();check(game.Experience.PackStep==step,label+": double-click cannot skip packing step "+step);
                yield return WaitUntil(()=>game.Experience.PackStep==step+1,3,check,label+": packing step "+step+" completes");
            }
            int money=game.Shop.Data.credits;game.Experience.AdvancePacking();game.Experience.AdvancePacking();
            yield return WaitUntil(()=>game.WaitingForOrder&&!game.Experience.Packing,4,check,label+": shipment curtain completes and returns to offers");
            check(game.Shop.Data.credits>money,label+": final packing sends and rewards the product");
        }
        static IEnumerator WaitUntil(Func<bool> done,float seconds,Action<bool,string> check,string label){
            float deadline=Time.realtimeSinceStartup+seconds;while(!done()&&Time.realtimeSinceStartup<deadline)yield return null;check(done(),label);
        }
    }
}
