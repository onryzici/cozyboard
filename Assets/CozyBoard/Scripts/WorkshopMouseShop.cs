using TMPro;
using UnityEngine;
namespace CozyBoard {
    public sealed partial class WorkshopShop {
        GameObject mouseKitView;UnityEngine.UI.Button buyMouse,refundMouse;TMP_Text mouseKitStock;UnityEngine.UI.Image mouseIllustration;
        public const int MouseKitPrice=120;
        void BuildMouseKitUI(){
            var screen=panel.transform.Find("Laptop catalogue");var card=WorkshopUI.Panel("Product Mouse kit",screen,new Vector2(0,1),new Vector2(45,-260),new Vector2(1280,430),new Color(.88f,.85f,.73f));mouseKitView=card.gameObject;WorkshopAtelierStyle.Paper(card,new Color(.88f,.85f,.73f),18);
            mouseIllustration=WorkshopUI.Panel("Mouse kit silhouette",card.transform,new Vector2(0,.5f),new Vector2(45,0),new Vector2(180,268),WorkshopUI.Paper);WorkshopAtelierStyle.Paper(mouseIllustration,WorkshopUI.Paper,75).raycastTarget=false;mouseIllustration.raycastTarget=false;
            for(int i=0;i<2;i++){var b=WorkshopUI.Panel("Mouse button illustration",mouseIllustration.transform,new Vector2(.5f,1),new Vector2(i==0?-44:44,-16),new Vector2(78,94),new Color(.81f,.77f,.65f));WorkshopAtelierStyle.Paper(b,new Color(.81f,.77f,.65f),24).raycastTarget=false;b.raycastTarget=false;}
            var wheel=WorkshopUI.Panel("Mouse wheel illustration",mouseIllustration.transform,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(16,38),WorkshopUI.Sage);WorkshopAtelierStyle.Paper(wheel,WorkshopUI.Sage,8).raycastTarget=false;wheel.raycastTarget=false;
            WorkshopUI.Text("Mouse kit title",card.transform,font,"AVUÇ · MOUSE KİTİ",32,new Vector2(0,1),new Vector2(275,-32),new Vector2(950,45));
            WorkshopUI.Text("Mouse kit contents",card.transform,font,"Alt ve üst gövde · Devre · Optik sensör ve mercek\nAna ve yan düğme switch’leri · Tekerlek · USB kablosu\nDört kaydırıcı ayak · Koruyucu filmler",25,new Vector2(0,1),new Vector2(275,-102),new Vector2(925,115));
            string[] palettes={"Krem","Adaçayı","Lavanta"};for(int i=0;i<3;i++){int c=i;WorkshopUI.Button("Mouse kit palette "+i,card.transform,font,palettes[i],new Vector2(0,1),new Vector2(275+i*210,-232),new Vector2(195,44),()=>SelectMouseColor(c));}
            mouseKitStock=WorkshopUI.Text("Mouse kit stock",card.transform,font,"",22,new Vector2(0,1),new Vector2(275,-294),new Vector2(900,32));
            buyMouse=WorkshopUI.Button("Buy mouse kit",card.transform,font,"120 Tık · Kiti al",new Vector2(0,0),new Vector2(275,24),new Vector2(285,52),()=>PurchaseMouseKit());
            refundMouse=WorkshopUI.Button("Refund mouse kit",card.transform,font,"Kullanılmamış kiti iade et",new Vector2(0,0),new Vector2(580,24),new Vector2(410,52),()=>RefundMouseKit());mouseKitView.SetActive(false);
        }
        bool RefreshMouseKit(Transform root){bool mouse=category==3;mouseKitView.SetActive(mouse);for(int i=0;i<3;i++)root.Find("Product "+i).gameObject.SetActive(!mouse);if(!mouse)return false;mouseKitStock.text=Data.mouseKits+" kullanılmamış kit"+(Game.IsMouse&&Game.MouseProduct.State.kitReserved?" · Masadaki kit ayrıldı":"");buyMouse.interactable=Data.credits>=MouseKitPrice;refundMouse.interactable=Data.mouseKits>0;notice.text="Bir kit, bir mouse. Gövde ve düğmeleri fırçayla ayrı ayrı boyayabilirsin.";var colors=new[]{WorkshopUI.Paper,new Color(.59f,.69f,.51f),new Color(.68f,.59f,.76f)};mouseIllustration.color=colors[Data.mouseColor];mouseIllustration.GetComponentInChildren<WorkshopPaperGraphic>().color=colors[Data.mouseColor];return true;}
        public bool PurchaseMouseKit(){if(Game.ScreenChangeBlocked||Data.credits<MouseKitPrice)return false;string before=Game.SerializeProgress();Data.credits-=MouseKitPrice;Data.mouseKits++;if(!Game.TrySaveQuiet()){Game.RestoreProgress(before);Refresh();return false;}Game.PresentStock();Game.Refresh();Refresh();return true;}
        public bool RefundMouseKit(){if(Game.ScreenChangeBlocked||Data.mouseKits<1)return false;string before=Game.SerializeProgress();Data.mouseKits--;Data.credits+=MouseKitPrice;if(!Game.TrySaveQuiet()){Game.RestoreProgress(before);Refresh();return false;}Game.PresentStock();Game.Refresh();Refresh();return true;}
        public bool SelectMouseColor(int id){if(Game.ScreenChangeBlocked||id<0||id>2||Game.IsMouse&&Game.MouseProduct.State.kitReserved)return false;Data.mouseColor=id;ApplyVariants();Game.SaveProgress();Refresh();return true;}
    }
}
