using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    public sealed partial class WorkshopShop {
        GameObject ordersView;
        readonly TMP_Text[] offerTitles=new TMP_Text[3],offerBodies=new TMP_Text[3],offerPayments=new TMP_Text[3];
        readonly RawImage[] offerPortraits=new RawImage[3];
        readonly Button[] offerButtons=new Button[3];
        readonly GameObject[] offerCards=new GameObject[3];
        string[] offerIds=new string[3];
        TMP_Text jobsSubtitle;
        void BuildOrdersUI(){
            var screen=panel.transform.Find("Laptop catalogue");
            WorkshopUI.Button("Orders tab",screen,font,"Siparişler",new Vector2(0,1),new Vector2(533,-112),new Vector2(210,48),SelectOrders);
            WorkshopUI.Button("Notebook tab",screen,font,"Atölye defteri",new Vector2(0,1),new Vector2(757,-112),new Vector2(240,48),()=>Game.StoryUI.OpenNotebook());
            var view=WorkshopUI.Rect("Available orders",screen,new Vector2(0,1),new Vector2(45,-184),new Vector2(1300,650));ordersView=view.gameObject;
            jobsSubtitle=WorkshopUI.Text("Order selection hint",view,font,"",23,new Vector2(0,1),Vector2.zero,new Vector2(1290,56));
            for(int i=0;i<3;i++){
                int slot=i;
                var card=WorkshopUI.Panel("Offer "+i,view,new Vector2(0,1),new Vector2(i*435,-76),new Vector2(410,494),new Color(.88f,.85f,.73f));offerCards[i]=card.gameObject;
                WorkshopAtelierStyle.Paper(card,new Color(.88f,.85f,.73f),18);
                offerPortraits[i]=WorkshopUI.Art("Offer customer",card.transform,Resources.Load<Texture2D>("Customers/Portraits"),new Vector2(0,1),new Vector2(22,-22),new Vector2(80,94));
                offerTitles[i]=WorkshopUI.Text("Offer title",card.transform,font,"",27,new Vector2(0,1),new Vector2(120,-25),new Vector2(270,100));offerTitles[i].textWrappingMode=TextWrappingModes.Normal;offerTitles[i].enableAutoSizing=true;offerTitles[i].fontSizeMin=22;offerTitles[i].fontSizeMax=27;
                offerBodies[i]=WorkshopUI.Text("Offer brief",card.transform,font,"",23,new Vector2(0,1),new Vector2(24,-137),new Vector2(362,207));offerBodies[i].textWrappingMode=TextWrappingModes.Normal;offerBodies[i].enableAutoSizing=true;offerBodies[i].fontSizeMin=19;offerBodies[i].fontSizeMax=23;
                var payment=WorkshopUI.Text("Offer payment",card.transform,font,"61 tuş · Boyama serbest\nTeslimat: 240–300 Tık\nSetler: 145–195 Tık (stok kullanılabilir)",17,new Vector2(0,1),new Vector2(24,-351),new Vector2(362,72));payment.enableAutoSizing=true;payment.fontSizeMin=15;payment.fontSizeMax=17;offerPayments[i]=payment;
                offerButtons[i]=WorkshopUI.Button("Accept offer",card.transform,font,"Bu işi al",new Vector2(.5f,0),new Vector2(0,20),new Vector2(362,52),()=>{if(Game.AcceptOrder(offerIds[slot]))RefreshOffers();});
                offerButtons[i].targetGraphic=WorkshopAtelierStyle.Paper(offerButtons[i].GetComponent<Image>(),WorkshopUI.Sage,12);offerButtons[i].GetComponent<Shadow>().enabled=false;
            }
            WorkshopUI.Button("Show active order",view,font,"Masadaki siparişi aç",new Vector2(0,0),new Vector2(0,0),new Vector2(290,52),()=>{Close();Game.Menu.ShowOrder();});
            WorkshopUI.Button("Leave orders",view,font,"Atölyeye dön",new Vector2(1,0),new Vector2(0,0),new Vector2(220,52),Close);
            ordersView.SetActive(false);
        }
        public void OpenOrders(){if(!IsOpen)Open();if(IsOpen)SelectOrders();}
        void SelectOrders(){SelectMailbox(true,false);mailView.SetActive(false);ordersView.SetActive(true);RefreshOffers();}
        public void RefreshOffers(){
            if(!ordersView)return;
            bool canAccept=Game.WaitingForOrder;
            var offers=WorkshopStory.Offers(Game.Story);
            jobsSubtitle.text=canAccept?(Game.Story.OrdersUnlocked?"Sıradaki işini seç. Aynı anda bir iş; acelemiz yok.":"Nermin'in bıraktığı ilk not · Önce Ece'nin masasıyla başlayalım."):"Masanda bir sipariş var. Önce onu tamamlayalım; teklifler burada kalır.";
            for(int i=0;i<3;i++){
                offerCards[i].SetActive(i<offers.Length);if(i>=offers.Length)continue;
                ((RectTransform)offerCards[i].transform).anchoredPosition=new Vector2(offers.Length==1?435:i*435,-76);
                var offer=offers[i];offerIds[i]=offer.Id;var request=WorkshopOrders.ForCustomer(offer.CustomerId);
                offerTitles[i].text=request.Name+"\n"+offer.Title;
                offerBodies[i].text=offer.Message+(offer.Kind=="repair"?"\n\nÜrün hazır gelir · 3 tuşluk kontrol":"\n\n"+request.Sound+"\n"+request.Feel);offerPayments[i].text=offer.Kind=="repair"?"Kısa tamir · Bir yedek switch\nÜcret: 90 Tık\nYedek switch: 8–12 Tık":offer.Kind=="macro"?"6 tuş · Kısayol seçimi ve boyama\nTeslimat: 100–130 Tık\nSetlerden yalnızca 6 parça kullanılır":"61 tuş · Boyama serbest\nTeslimat: 240–300 Tık\nSetler: 145–195 Tık (stok kullanılabilir)";
                offerPortraits[i].uvRect=new Rect(WorkshopStory.PortraitIndex(offer.CustomerId)/3f,0,1f/3f,1);
                offerButtons[i].interactable=canAccept;offerButtons[i].GetComponentInChildren<TMP_Text>().text=canAccept?"Bu işi al":"Önce masadaki işi bitir";
            }
            ordersView.transform.Find("Show active order").gameObject.SetActive(!canAccept);
        }
    }
}
