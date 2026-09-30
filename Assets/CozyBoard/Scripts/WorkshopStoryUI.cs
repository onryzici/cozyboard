using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    public sealed class WorkshopStoryUI : MonoBehaviour {
        WorkshopGameMode game;
        GameObject panel;
        TMP_Text heading,body,footer;
        Button next;
        bool introduction;
        public bool Open=>panel&&panel.activeSelf;
        public void Initialize(WorkshopGameMode owner,RectTransform root,TMP_FontAsset font){
            if(panel)return;game=owner;
            var overlay=WorkshopUI.Panel("Nermin's notebook",root,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.12f,.18f,.14f,.65f));
            overlay.rectTransform.anchorMax=Vector2.one;overlay.rectTransform.offsetMax=Vector2.zero;panel=overlay.gameObject;
            var paper=WorkshopUI.Panel("Notebook paper",overlay.transform,Vector2.one*.5f,Vector2.zero,new Vector2(830,690),WorkshopUI.Paper);
            WorkshopAtelierStyle.Paper(paper,WorkshopUI.Paper,22);
            var eyebrow=WorkshopUI.Text("Story name",paper.transform,font,"BİR TUŞLUK MOLA",18,new Vector2(0,1),new Vector2(52,-34),new Vector2(720,32));eyebrow.characterSpacing=3;eyebrow.color=WorkshopUI.Sage;
            heading=WorkshopUI.Text("Notebook heading",paper.transform,font,"",36,new Vector2(0,1),new Vector2(52,-85),new Vector2(720,60));
            body=WorkshopUI.Text("Notebook letter",paper.transform,font,"",27,new Vector2(0,1),new Vector2(52,-164),new Vector2(720,360));body.textWrappingMode=TextWrappingModes.Normal;body.enableAutoSizing=true;body.fontSizeMin=22;body.fontSizeMax=27;body.lineSpacing=7;
            footer=WorkshopUI.Text("Notebook progress",paper.transform,font,"",19,new Vector2(0,0),new Vector2(52,100),new Vector2(720,54));footer.color=WorkshopUI.Sage;footer.textWrappingMode=TextWrappingModes.Normal;
            next=WorkshopUI.Button("Leave notebook",paper.transform,font,"",new Vector2(1,0),new Vector2(-48,32),new Vector2(290,56),CloseNotebook);
            next.targetGraphic=WorkshopAtelierStyle.Paper(next.GetComponent<Image>(),WorkshopUI.Sage,12);next.GetComponent<Shadow>().enabled=false;
            panel.SetActive(false);
        }
        public void OpenNotebook(bool opening=false){
            if(!panel||game.Experience.MainVisible||game.Experience.Packing||game.Tools&&game.Tools.Busy)return;
            if(game.Shop)game.Shop.Close();game.Menu.ClosePanels();game.Controller.CancelDrag();game.Painter.CloseEditor();if(game.Testing)game.Testing.End();game.Experience.EndInspection();
            introduction=opening;heading.text=opening?"Anahtar paspasın altında":"Nermin'in atölye defteri";
            body.text=game.Story.FirstDelivered?
                "Ece'nin klavyesi yeni masasına ulaştı. Kitapçının pencere kenarında boş bir masa varmış; Ece oraya bir şeyler yakıştırıyor.\n\nNermin'in defterine ilk notumuzu düşelim:\n\"Bugün bir klavye yaptım. Bir de komşu tanıdım.\"\n\nBirlikte hazırlayacağımız Mola Köşesi'nin hikâyesi burada başlayacak.":WorkshopStory.OpeningNote;
            if(!opening&&game.Story.Has(WorkshopStory.MacroEvent))body.text="Deniz'in altı tuşlu yardımcısı yazı masasına ulaştı. Geri al, yinele, kaydet… Küçük kolaylıkların büyük bir farkı olabiliyormuş.\n\nMola Köşesi için ilk öyküsüne başlamış. Birlikte hazırladığımız masanın artık anlatacak bir hikâyesi var.";else if(!opening&&game.Story.Has(WorkshopStory.RepairEvent))body.text="Ece'nin eski klavyesi yeniden çalışıyor. Yeni bir eşya yapmak kadar, eski bir eşyayı yaşatmak da güzelmiş.\n\nNermin'in notu:\n'Eskisi de kıymetli. Bazen bir tuşu değiştirirsin, bir hatırayı korursun.'\n\nSıradaki küçük fikir Deniz'den: yazı masası için altı tuşlu bir yardımcı.";else if(!opening&&game.Story.Has(WorkshopStory.LegacyEvent))body.text="Atölyenin eski siparişi tamamlandı. Nermin'in defterinde yeni işlere yer açalım.\n\nLaptopta şimdi kısa tamir, altı tuşlu makro pad ve klavye teklifleri var. Ece eskisini onarmak, Deniz küçük bir yardımcı yapmak istiyor.";
            footer.text=!game.Story.enabled?"Mevcut siparişini bitirince kısa tamir ve makro pad işleri açılacak.":game.Story.Has(WorkshopStory.MacroEvent)?"Altı küçük kolaylık · Yeni işler laptopta.":game.Story.Has(WorkshopStory.RepairEvent)?"Eskisi de kıymetli · Deniz'in makro pad'i laptopta.":game.Story.OrdersUnlocked?"Klavye, kısa tamir ve makro pad · Sıradaki işini seç.":"Bölüm 1 · İlk masanın sesi · İlk işimiz Ece'nin klavyesi.";
            next.GetComponentInChildren<TMP_Text>().text=opening?"Ece'nin notuna bakalım":"Atölyeye dön";
            panel.SetActive(true);panel.transform.SetAsLastSibling();
        }
        public void CloseNotebook(){
            if(!Open)return;
            if(introduction){bool previous=game.Story.openingRead;game.Story.openingRead=true;if(!game.TrySaveQuiet()){game.Story.openingRead=previous;return;}}
            panel.SetActive(false);if(introduction&&game.WaitingForOrder)game.Shop.OpenOrders();introduction=false;
        }
        public void Hide(){if(panel)panel.SetActive(false);introduction=false;}
    }
}
