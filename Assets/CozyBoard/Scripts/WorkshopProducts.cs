using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopGameMode {
        [Serializable] public sealed class RepairState {
            public string switchId,capId;
            public string[] testKeys;
            public bool diagnosed,replaced,oldSwitchInspected,spareOpened,spareChecked;
        }
        public RepairState Repair;
        public string[] MacroFunctions;
        WorkshopProductGeometry productGeometry;
        public WorkshopRepairBench RepairBench { get; internal set; }
        public bool IsMacro=>ActiveOrder?.kind=="macro";
        public bool IsRepair=>ActiveOrder?.kind=="repair";
        public int KeyCount=>IsMouse?4:IsMacro?6:61;
        public int PartCount=>IsMouse?18:2+KeyCount*2;
        public float ProductScale=>IsMouse?1.45f:IsMacro?1.8f:1.22f;
        public string ProductName=>IsMouse?"Kablolu mouse":IsMacro?"6 tuşlu makro pad":IsRepair?"Klavye tamiri":"61 tuşlu klavye";
        public IEnumerable<WorkshopItem> ProductItems=>Controller.Items.Where(BelongsToProduct);
        static readonly HashSet<string> macroIds=new(Enumerable.Range(1,6).SelectMany(i=>new[]{$"Keycap_{i:00}",$"Switch_{i:00}"}));
        public bool BelongsToProduct(WorkshopItem item)=>item&&(IsMouse?item.Stage==0||item.Id.StartsWith("Mouse_"):!item.Id.StartsWith("Mouse_")&&(!IsMacro||item.Stage<3||macroIds.Contains(item.Id)));
        public WorkshopItem[] TestKeys=>IsMouse?ProductItems.Where(x=>x.Kind=="mouse-button").ToArray():IsRepair&&Repair?.testKeys!=null?Repair.testKeys.Select(id=>Controller.Lookup.TryGetValue(id,out var item)?item:null).Where(x=>x).ToArray():ProductItems.Where(x=>x.Stage==4).ToArray();
        public int TestCount=>IsMouse?8:IsRepair?3:KeyCount;
        public bool RepairReady=>!IsRepair||Repair!=null&&Repair.diagnosed&&Repair.replaced;
        public static readonly string[] FunctionNames={"Geri al","Yinele","Kaydet","Bul","Önceki sayfa","Sonraki sayfa"};
        public static readonly string[] DesiredFunctions={"Geri al","Yinele","Kaydet","Bul","Önceki sayfa","Sonraki sayfa"};
        public bool MacroReady=>IsMouse?MouseProduct.MacrosReady:!IsMacro||MacroFunctions!=null&&MacroFunctions.Length==6&&MacroFunctions.SequenceEqual(DesiredFunctions);
        public void ConfigureProduct(){
            EnsureMouse();
            productGeometry=GetComponent<WorkshopProductGeometry>()??gameObject.AddComponent<WorkshopProductGeometry>();productGeometry.Apply(this);
            if(Tools)Tools.UpdateProduct();if(Testing)Testing.RebuildPads();
        }
        public void PrepareAcceptedProduct(){
            Repair=null;MacroFunctions=IsMacro?new string[6]:null;ConfigureProduct();
            Controller.Lookup["Case"].transform.localScale=Vector3.one*ProductScale;
            if(!IsRepair)return;
            foreach(var part in ProductItems.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))Controller.Attach(part);
            var cap=Controller.Lookup[$"Keycap_{16+OrderNumber%4:00}"];
            var nearby=ProductItems.Where(x=>x.Stage==4).OrderBy(x=>Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(cap.Slot.x,cap.Slot.z))).Take(3).Select(x=>x.Id).ToArray();
            Repair=new RepairState{capId=cap.Id,switchId=cap.Id.Replace("Keycap_","Switch_"),testKeys=nearby};
            Tools.Tightened=15;Shop.Data.used=new[]{true,false,true};Testing.Restore(null,Repair.switchId,true);Testing.RebuildPads();
            if(RepairBench)RepairBench.SyncProduct();PresentStock();Refresh();
        }
        public string RepairInstruction(){
            if(Repair==null||!Repair.diagnosed)return "Test cihazıyla arızalı tuşu bul.";
            if(Testing&&Testing.Passed)return "Tamir tamamlandı. Özenle paketleyelim.";
            if(Repair.replaced)return Completed?"Üç komşu tuşu yeniden test et.":!Controller.Lookup[Repair.switchId].Fitted?"Yeni switch’i yeniden boş yuvaya yerleştir.":"Sakladığın tuş kapağını tepsiden alıp yerine tak.";
            if(Controller.Lookup[Repair.capId].Fitted)return "Tuş sökücüyle arızalı tuşun kapağını çıkar.";
            if(Controller.Lookup[Repair.switchId].Fitted)return "Switch sökücüyle arızalı switch'i çıkar.";
            if(!Repair.oldSwitchInspected)return "Çıkardığın arızalı switch’i alt kaptan alıp incele.";
            if(Shop&&Shop.HasSupply(3)&&!Repair.spareOpened)return "Yeni switch’in mühürlü paketini aç.";
            if(Repair.spareOpened&&!Repair.spareChecked)return "Yeni switch’in altını çevirip pinlerini kontrol et.";
            return Shop&&!Shop.HasSupply(3)?"Malzemelerden tek bir yedek switch al.":"Tepsideki yedek switch'i boş yuvaya yerleştir.";
        }
        // Loading an assembled neighbour must not depend on the global assembly stage.
        bool CanRestorePart(WorkshopItem item){
            if(!BelongsToProduct(item))return false;
            if(IsMouse)return MouseProduct.CanRestore(item);
            return item.Stage switch{
                1=>true,
                2=>Controller.Lookup["PCB"].Fitted,
                3=>Controller.Lookup["Plate"].Fitted,
                4=>Controller.Lookup.TryGetValue(item.Id.Replace("Keycap_","Switch_"),out var sw)&&sw.Fitted,
                _=>false};
        }
        public void RestartActiveWork(){
            if(ScreenChangeBlocked)return;
            bool mouseReserved=IsMouse&&MouseProduct.State.kitReserved;var reserved=Shop?Shop.Data.used.ToArray():null;bool replaced=IsRepair&&Repair!=null&&Repair.replaced;int spare=Shop?Shop.Data.selected[1]:3;
            NewOrder();
            if(IsMouse){MouseProduct.State.kitReserved=mouseReserved;PresentStock();Refresh();}
            else if(IsRepair){if(replaced&&Shop)Shop.Data.looseParts[spare]++;PrepareAcceptedProduct();}
            else if(Shop&&reserved!=null){Shop.Data.used=reserved;PresentStock();Refresh();}
        }
        public bool RepairPartVisible(WorkshopItem item)=>!IsRepair||Repair==null||item.Id!=Repair.switchId||Repair.replaced||Repair.spareOpened;
        public bool RepairPartReady(WorkshopItem item)=>!IsRepair||Repair==null||item.Id!=Repair.switchId||Repair.replaced||Repair.spareOpened&&Repair.spareChecked;
        public bool NeedsSupply(WorkshopItem item)=>!IsRepair||item.Stage==3&&item.Id==Repair?.switchId&&!Repair.replaced;
        public void AfterPartInstalled(WorkshopItem item){if(IsMouse){MouseProduct.Installed(item);return;}if(IsRepair&&Repair!=null&&item.Id==Repair.switchId){Repair.replaced=true;Testing.ClearMeasurements();}}
        public void DiagnoseRepair(WorkshopItem cap,bool working){if(IsRepair&&Repair!=null&&!working&&cap.Id==Repair.capId)Repair.diagnosed=true;}
        public bool CanRemoveForRepair(WorkshopItem item){if(!IsMouse&&Tools&&Tools.KeyboardUnderbody){Menu.Toast("Parçaları sökmek için önce klavyenin üstüne çevir.");return false;}if(IsMouse){if(MouseProduct.State.underbody&&!ScreenChangeBlocked){if(item.Id=="Mouse_Lens")return true;if(item.Kind=="mouse-sensor"){if(!Controller.Lookup["Mouse_Lens"].Fitted)return true;Menu.Toast("Sensöre ulaşmak için önce merceği çıkar.");return false;}}Menu.Toast("Sensörü sökmek için mouse’un altını çevirip switch sökücüyü seç.");return false;}if(IsRepair&&Repair!=null&&item.Id!=Repair.capId&&item.Id!=Repair.switchId){Menu.Toast("Sağlam parçaları yerinde bırakalım. Önce testte arızalı tuşu bul.");return false;}if(IsRepair&&Repair!=null&&!Repair.diagnosed&&(item.Id==Repair.capId||item.Id==Repair.switchId)){Menu.Toast("Önce tuş kontrolünü açıp arızalı tuşu bulalım.");return false;}return true;}
    }

}
