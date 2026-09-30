using System;
using System.Linq;

namespace CozyBoard {
    public static class WorkshopStory {
        public const string FirstEvent="ece-first-delivery";
        public const string RepairEvent="ece-repair-delivery",MacroEvent="deniz-macro-delivery",LegacyEvent="legacy-workshop";
        public const string OpeningNote="Anahtar paspasın altında. Fincanı bıraktım; çayını sen tazele. Ece uğrayacak, bir klavye soracaktı. Her şeyi bir günde öğrenmeye çalışma. Önce sandalyeyi kendine göre çek. Gerisi gelir.\n\nP.S. Çekmecedeki kurabiyeler yedek parça değil. Gönül rahatlığıyla kullan.\n\n— Nermin";
        [Serializable] public sealed class State {
            public bool enabled,openingRead;
            public string[] events=Array.Empty<string>();
            public bool FirstDelivered=>events!=null&&events.Contains(FirstEvent);
            public bool OrdersUnlocked=>FirstDelivered||Has(LegacyEvent);
            public bool Has(string id)=>events!=null&&events.Contains(id);
            public bool Record(string id){if(string.IsNullOrEmpty(id)||events!=null&&events.Contains(id))return false;events=(events??Array.Empty<string>()).Append(id).ToArray();return true;}
        }
        [Serializable] public sealed class ActiveOrder {
            public string definitionId,instanceId,customerId,kind="keyboard",storyEvent;
        }
        public sealed class Definition {
            public readonly string Id,CustomerId,Title,Message,Event;
            public readonly string Kind;
            public Definition(string id,string customer,string title,string message,string storyEvent=null,string kind="keyboard"){Id=id;CustomerId=customer;Title=title;Message=message;Event=storyEvent;Kind=kind;}
            public ActiveOrder Accept()=>new(){definitionId=Id,instanceId=Guid.NewGuid().ToString("N"),customerId=CustomerId,kind=Kind,storyEvent=Event};
        }
        public static readonly Definition First=new("ece-first","ece","İlk masanın sesi","Nermin teyze artık tezgâhta senin olduğunu söyledi. Kitapçıdaki ortak masada çizim yapıyorum. Sessiz ve hafif basılan bir klavye istiyorum; yanımdakinin cümlesini bölmesin. Renkleri sana bırakıyorum.",FirstEvent);
        static readonly Definition[] sideOrders={
            new("ece-keyboard","ece","Çizim masasına küçük bir mola","Kitapçıda bir çizim masası daha hazırlıyoruz. Hafif basılan, yan masayı yormayan bir klavye iyi gelir. Bu kez de desenleri sana bırakıyorum."),
            new("deniz-keyboard","deniz","Boş sayfanın yanında","Yeni öyküme başladım. Tok bir ses ve parmağımda hissedebileceğim bir basma noktası arıyorum. Belki ilk cümle, ilk tuşla gelir."),
            new("mina-keyboard","mina","Mahalleden bir ses","Mahalle radyosunun kayıt notlarını yazıyorum. Her tuşta net bir tık ve belirgin bir basış isterim. Masamın en neşeli parçası olsun.")
        };
        public static readonly Definition FirstRepair=new("ece-repair","ece","Eskisi de kıymetli","Eski klavyemin sol üstteki harflerinden biri bazen çalışmıyor. Bunu atmak istemiyorum; ilk çizim masamda hep bu vardı. Üç komşu tuşu deneyip arızalı switch'i değiştirebilir misin?",RepairEvent,"repair");
        public static readonly Definition FirstMacro=new("deniz-macro","deniz","Altı küçük kolaylık","Metnimi düzenlerken kullanacağım altı tuşlu bir makro pad istiyorum. Geri al, yinele, kaydet, bul, önceki ve sonraki sayfa; tuşları bu sırayla ayarlayalım. Sesini ve tasarımını da seçelim.",MacroEvent,"macro");
        static readonly Definition repeatRepair=new("ece-repair-repeat","ece","Bir tuşluk tamir","Kitapçıdaki eski klavyelerden biri yanıt vermiyor. Üç komşu tuşu kontrol edip arızalı switch'i değiştirir misin? Sağlam parçalar yerinde kalsın.",null,"repair");
        static readonly Definition repeatMacro=new("deniz-macro-repeat","deniz","Yazı masasına altı tuş","Bir yazı masasına daha altı tuşlu bir pad hazırlayalım. İşlevler aynı sırada: geri al, yinele, kaydet, bul, önceki ve sonraki sayfa. Desen yine sana ait.",null,"macro");
        public static Definition[] Offers(State state)=>state!=null&&state.enabled&&!state.OrdersUnlocked?new[]{First}:new[]{state!=null&&state.Has(RepairEvent)?repeatRepair:FirstRepair,state!=null&&state.Has(MacroEvent)?repeatMacro:FirstMacro,sideOrders[2]};
        public static Definition Find(string id)=>new[]{First,FirstRepair,FirstMacro,repeatRepair,repeatMacro}.Concat(sideOrders).FirstOrDefault(x=>x.Id==id);
        public static string CustomerId(int legacyOrder)=>((Math.Max(1,legacyOrder)-1)%3) switch{0=>"ece",1=>"deniz",_=>"mina"};
        public static int PortraitIndex(string customer)=>customer switch{"deniz"=>1,"mina"=>2,_=>0};
    }

    public sealed partial class WorkshopGameMode {
        public WorkshopStory.State Story=new();
        public WorkshopStory.ActiveOrder ActiveOrder;
        [NonSerialized] public WorkshopStoryUI StoryUI;
        public bool WaitingForOrder=>Story!=null&&Story.enabled&&ActiveOrder==null;
        public string CustomerId=>ActiveOrder?.customerId??WorkshopStory.CustomerId(OrderNumber);
        public WorkshopOrders.Request CurrentRequest=>WorkshopOrders.ForCustomer(CustomerId);
        public string CurrentOrderMessage=>WorkshopStory.Find(ActiveOrder?.definitionId)?.Message??CurrentRequest.Message;
        public string CurrentFit(int switchId)=>WorkshopOrders.FitCustomer(CustomerId,switchId);
        public void StartStory(){Story=new WorkshopStory.State{enabled=true};ActiveOrder=null;OrderNumber=1;LastDelivery=null;DeliveryMail.Clear();if(Shop)Shop.ResetShop();NewOrder();}
        public bool AcceptOrder(string id){
            if(!WaitingForOrder||Installed>0||StoryUI&&StoryUI.Open||Tools&&Tools.Busy)return false;
            var offer=WorkshopStory.Offers(Story).FirstOrDefault(x=>x.Id==id);if(offer==null)return false;
            string before=SerializeProgress();ActiveOrder=offer.Accept();PrepareAcceptedProduct();PresentStock();Refresh();
            if(!TrySaveQuiet()){RestoreProgress(before);return false;}
            if(Shop)Shop.Close();if(Menu)Menu.ShowOrder();
            if(offer.Event==WorkshopStory.FirstEvent&&Experience)Experience.StartTutorial();return true;
        }
        // Does not close panels, move the board or cancel tools. Suitable for narrative/UI state.
        public bool TrySaveQuiet(){
            if(Tools&&Tools.Busy)return false;
            try{
                string path=SavePath;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string temporary=path+".tmp";System.IO.File.WriteAllText(temporary,SerializeProgress());
                if(System.IO.File.Exists(path))System.IO.File.Replace(temporary,path,null);else System.IO.File.Move(temporary,path);
                return true;
            }catch(Exception e){UnityEngine.Debug.LogWarning("Workshop save failed: "+e.Message);if(Menu)Menu.Toast("Kayıt yazılamadı. Biraz sonra yeniden deneyelim.");return false;}
        }
        public WorkshopOrders.Receipt CreateReceipt()=>ActiveOrder==null?WorkshopOrders.Evaluate(OrderNumber,Shop?Shop.Data.selected[1]:3):WorkshopOrders.EvaluateCustomer(OrderNumber,Shop?Shop.Data.selected[1]:3,ActiveOrder);
    }
}
