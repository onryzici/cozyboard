using System;
namespace CozyBoard {
    // Customer requirements describe sound and feel. Colour is always the maker's choice.
    public static class WorkshopOrders {
        public sealed class Request {
            public readonly string Name,Message,Sound,Feel;
            public Request(string name,string message,string sound,string feel){Name=name;Message=message;Sound=sound;Feel=feel;}
        }
        static readonly Request[] requests={
            new("Ece","Ortak çalışma masamda çizim yapıyorum. Yanımdakileri yormayan, hafif bir klavye olsun.","Sessiz ses","Yumuşak, lineer basış"),
            new("Deniz","Akşamları hikâye yazıyorum. Tok bir ses ve parmağımda hissedebileceğim bir basma noktası arıyorum.","Tok ses","Belirgin basma noktası"),
            new("Mina","Her tuşta net bir tık duymak hoşuma gidiyor. Bastığımı parmaklarımda da hissetmek isterim.","Parlak tık sesi","Belirgin basma noktası")
        };
        static int Index(int order)=>(Math.Max(1,order)-1)%requests.Length;
        public static Request For(int order)=>requests[Index(order)];
        public static Request ForCustomer(string customer)=>requests[WorkshopStory.PortraitIndex(customer)];
        public static bool SoundMatchesCustomer(string customer,int switchId)=>SoundMatches(WorkshopStory.PortraitIndex(customer)+1,switchId);
        public static bool FeelMatchesCustomer(string customer,int switchId)=>FeelMatches(WorkshopStory.PortraitIndex(customer)+1,switchId);
        public static bool SoundMatches(int order,int switchId)=>Index(order) switch{0=>switchId==3,1=>switchId==3||switchId==4,_=>switchId==5};
        public static bool FeelMatches(int order,int switchId)=>Index(order)==0?switchId==3:switchId==4||switchId==5;
        public static string SwitchName(int id)=>id switch{3=>"Bulut · Lineer",4=>"Yaprak · Taktil",5=>"Çıtır · Clicky",_=>"Bilinmeyen switch"};
        [Serializable] public class Receipt {
            public int order,switchId,matches,reward;
            public bool pending;
            public string customerId,orderId,storyEvent,kind;
            public string CustomerId=>string.IsNullOrEmpty(customerId)?WorkshopStory.CustomerId(order):customerId;
            public string Subject=>kind=="repair"?"Eski klavyem yeniden çalışıyor!":kind=="macro"?"Altı küçük kolaylık geldi!":storyEvent==WorkshopStory.FirstEvent?"İlk masamızın klavyesi geldi!":"Klavyem geldi!";
            public string Letter=>storyEvent==WorkshopStory.FirstEvent?
                (matches==2?"Bugün yanımdaki Deniz, ben çalışmaya ne zaman başladım diye sordu. Tuşları duymamış!":Reaction)+"\n\nBu arada kitapçıda pencerenin yanındaki boş masayı gördün mü? Oraya bir şeyler yakışır gibi geliyor. Sonra anlatırım.":kind=="repair"?"O tuş yeniden çalışıyor! Eski klavyemi atmadığıma sevindim. İlk çizim masamdan küçük bir parça hâlâ benimle.\n\nNermin'e bir fotoğraf gönderdim; 'Eskisi de kıymetli' diye cevap yazmış.":kind=="macro"?Reaction+"\n\nAltı kısayolu denedim. Artık boş sayfadan kaçarken hiç değilse hangi tuşa basacağımı biliyorum! Mola Köşesi için küçük bir öykü yazmaya başladım.":Reaction;
            public string Reaction=>matches==2?"Tam hayal ettiğim gibi! Sesini de tuş hissini de çok sevdim. Ellerine sağlık.":matches==1?"Özenin belli oluyor! İsteklerimden biri tam yerinde, diğeri biraz farklı kalmış.":"Emeğin için teşekkürler. Ses ve tuş hissi istediğimden farklı olmuş; bir dahakine birlikte yakalarız.";
        }
        public static Receipt Evaluate(int order,int switchId){int matches=(SoundMatches(order,switchId)?1:0)+(FeelMatches(order,switchId)?1:0);return new Receipt{order=order,switchId=switchId,matches=matches,reward=240+matches*30,pending=true};}
        public static Receipt EvaluateCustomer(int order,int switchId,WorkshopStory.ActiveOrder active){int matches=active.kind=="repair"?2:(SoundMatchesCustomer(active.customerId,switchId)?1:0)+(FeelMatchesCustomer(active.customerId,switchId)?1:0);return new Receipt{order=order,switchId=switchId,matches=matches,reward=active.kind=="repair"?90:active.kind=="macro"?100+matches*15:240+matches*30,pending=true,customerId=active.customerId,orderId=active.instanceId,storyEvent=active.storyEvent,kind=active.kind};}
        public static string FitCustomer(string customer,int switchId)=>$"{(SoundMatchesCustomer(customer,switchId)?"Uygun":"Farklı")} ses · {(FeelMatchesCustomer(customer,switchId)?"Uygun":"Farklı")} tuş hissi";
        public static string Fit(int order,int switchId)=>$"{(SoundMatches(order,switchId)?"Uygun":"Farklı")} ses · {(FeelMatches(order,switchId)?"Uygun":"Farklı")} tuş hissi";
    }
}
