using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopGameMode {
        [Serializable] public sealed class RepairState {
            public string switchId,capId;
            public string[] testKeys;
            public bool diagnosed,replaced;
        }
        public RepairState Repair;
        public string[] MacroFunctions;
        WorkshopProductGeometry productGeometry;
        public bool IsMacro=>ActiveOrder?.kind=="macro";
        public bool IsRepair=>ActiveOrder?.kind=="repair";
        public int KeyCount=>IsMacro?6:61;
        public int PartCount=>2+KeyCount*2;
        public float ProductScale=>IsMacro?1.8f:1.22f;
        public string ProductName=>IsMacro?"6 tuşlu makro pad":IsRepair?"Klavye tamiri":"61 tuşlu klavye";
        public IEnumerable<WorkshopItem> ProductItems=>Controller.Items.Where(BelongsToProduct);
        static readonly HashSet<string> macroIds=new(Enumerable.Range(1,6).SelectMany(i=>new[]{$"Keycap_{i:00}",$"Switch_{i:00}"}));
        public bool BelongsToProduct(WorkshopItem item)=>item&&(!IsMacro||item.Stage<3||macroIds.Contains(item.Id));
        public WorkshopItem[] TestKeys=>IsRepair&&Repair?.testKeys!=null?Repair.testKeys.Select(id=>Controller.Lookup.TryGetValue(id,out var item)?item:null).Where(x=>x).ToArray():ProductItems.Where(x=>x.Stage==4).ToArray();
        public int TestCount=>IsRepair?3:KeyCount;
        public bool RepairReady=>!IsRepair||Repair!=null&&Repair.diagnosed&&Repair.replaced;
        public static readonly string[] FunctionNames={"Geri al","Yinele","Kaydet","Bul","Önceki sayfa","Sonraki sayfa"};
        public static readonly string[] DesiredFunctions={"Geri al","Yinele","Kaydet","Bul","Önceki sayfa","Sonraki sayfa"};
        public bool MacroReady=>!IsMacro||MacroFunctions!=null&&MacroFunctions.Length==6&&MacroFunctions.SequenceEqual(DesiredFunctions);
        public void ConfigureProduct(){
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
            PresentStock();Refresh();
        }
        public void RestartActiveWork(){NewOrder();if(IsRepair)PrepareAcceptedProduct();}
        public bool NeedsSupply(WorkshopItem item)=>!IsRepair||item.Stage==3&&item.Id==Repair?.switchId&&!Repair.replaced;
        public void AfterPartInstalled(WorkshopItem item){if(IsRepair&&Repair!=null&&item.Id==Repair.switchId)Repair.replaced=true;}
        public void DiagnoseRepair(WorkshopItem cap,bool working){if(IsRepair&&Repair!=null&&!working&&cap.Id==Repair.capId)Repair.diagnosed=true;}
        public bool CanRemoveForRepair(WorkshopItem item){if(IsRepair&&Repair!=null&&!Repair.diagnosed&&(item.Id==Repair.capId||item.Id==Repair.switchId)){Menu.Toast("Önce tuş kontrolünü açıp arızalı tuşu bulalım.");return false;}return true;}
    }

}
