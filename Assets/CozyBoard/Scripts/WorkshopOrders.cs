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
        public static bool SoundMatches(int order,int switchId)=>Index(order) switch{0=>switchId==3,1=>switchId==3||switchId==4,_=>switchId==5};
        public static bool FeelMatches(int order,int switchId)=>Index(order)==0?switchId==3:switchId==4||switchId==5;
        public static string SwitchName(int id)=>id switch{3=>"Bulut · Lineer",4=>"Yaprak · Taktil",5=>"Çıtır · Clicky",_=>"Bilinmeyen switch"};
        [Serializable] public class Receipt {
            public int order,switchId,matches,reward;
            public bool pending;
            public string Reaction=>matches==2?"Tam hayal ettiğim gibi! Sesini de tuş hissini de çok sevdim. Ellerine sağlık.":matches==1?"Özenin belli oluyor! İsteklerimden biri tam yerinde, diğeri biraz farklı kalmış.":"Emeğin için teşekkürler. Ses ve tuş hissi istediğimden farklı olmuş; bir dahakine birlikte yakalarız.";
        }
        public static Receipt Evaluate(int order,int switchId){int matches=(SoundMatches(order,switchId)?1:0)+(FeelMatches(order,switchId)?1:0);return new Receipt{order=order,switchId=switchId,matches=matches,reward=240+matches*30,pending=true};}
        public static string Fit(int order,int switchId)=>$"{(SoundMatches(order,switchId)?"Uygun":"Farklı")} ses · {(FeelMatches(order,switchId)?"Uygun":"Farklı")} tuş hissi";
    }
}
