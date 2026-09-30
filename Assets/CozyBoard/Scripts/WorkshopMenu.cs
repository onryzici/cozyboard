using UnityEngine;
using TMPro;
namespace CozyBoard {
    public sealed class WorkshopMenu:MonoBehaviour {
        public WorkshopGameMode Game;
        public GameObject OrderPanel,SettingsPanel;
        public TMP_Text OrderName,OrderBody,OrderAction,ToastLabel;
        public UnityEngine.UI.Button SaveButton,OrderButton,SettingsButton,ConfirmButton,PointerButton,MoveButton,RotateButton,PaintButton,UndoButton,CloseOrder,CloseSettings;
        public int ToolMode;
        float toastUntil;
        public bool InputBlocked=>(OrderPanel&&OrderPanel.activeSelf)||(SettingsPanel&&SettingsPanel.activeSelf)||(Game&&Game.Experience&&Game.Experience.Blocking);
        public void Bind(WorkshopGameMode game){Game=game;
            SaveButton.onClick.AddListener(Game.SaveProgress);OrderButton.onClick.AddListener(ShowOrder);SettingsButton.onClick.AddListener(()=>{Game.Controller.CancelDrag();SettingsPanel.SetActive(!SettingsPanel.activeSelf);OrderPanel.SetActive(false);});
            ConfirmButton.onClick.AddListener(()=>{if(Game.Completed)Game.Deliver();else ClosePanels();});
            PointerButton.onClick.AddListener(()=>SelectTool(0));MoveButton.onClick.AddListener(()=>SelectTool(1));RotateButton.onClick.AddListener(()=>SelectTool(2));PaintButton.onClick.AddListener(()=>SelectTool(3));UndoButton.onClick.AddListener(()=>{if(ToolMode==3&&Game.Painter)Game.Painter.Undo();else Game.Undo();});
            CloseOrder.onClick.AddListener(ClosePanels);CloseSettings.onClick.AddListener(ClosePanels);ToolMode=0;RefreshOrder();
        }
        public void SelectTool(int mode){if(Game.Tools)Game.Tools.Deselect();Game.Controller.CancelDrag();if(mode==3&&!System.Linq.Enumerable.Any(Game.Controller.Items,item=>item.Kind=="keycap"&&item.Fitted)){ToolMode=0;if(Game.Painter)Game.Painter.SetVisible(false);Toast("Boyamak için önce klavyeye en az bir tuş tak.");return;}ToolMode=mode;if(Game.Painter)Game.Painter.SetVisible(mode==3);if(ToastLabel)ToastLabel.rectTransform.anchoredPosition=mode==3?new Vector2(0,154):new Vector2(0,35);Toast(mode==0?"Yerleştir · Kutudan al, yuvaların üzerinde sürükle":mode==1?"Taşı · Kasayı veya aletleri sürükle":mode==2?"Çevir · Kasaya veya alete tıkla":"Boya · Klavyeden bir tuş seç, büyütülmüş yüzeyinde çiz");}
        public void ShowOrder(){if(Game.WaitingForOrder){Game.Shop.OpenOrders();return;}if(Game.Testing)Game.Testing.End();Game.Controller.CancelDrag();if(Game.Painter)Game.Painter.CloseEditor();SettingsPanel.SetActive(false);OrderPanel.SetActive(true);RefreshOrder();}
        public void ClosePanels(){OrderPanel.SetActive(false);SettingsPanel.SetActive(false);}
        public void RefreshOrder(){
            if(!Game||!OrderName)return;
            var portrait=OrderPanel.transform.Find("Photo paper/Drawn customer");if(portrait){var image=portrait.GetComponent<UnityEngine.UI.RawImage>();var atlas=Resources.Load<Texture2D>("Customers/Portraits");if(atlas){image.texture=atlas;image.uvRect=new Rect(WorkshopStory.PortraitIndex(Game.CustomerId)/3f,0,1f/3f,1);}}
            OrderBody.enableAutoSizing=true;OrderBody.fontSizeMin=19;OrderBody.fontSizeMax=24;
            var request=Game.CurrentRequest;int selected=Game.Shop?Game.Shop.Data.selected[1]:3;
            OrderName.text=$"SİPARİŞ {Game.OrderNumber:00}\n\n{request.Name} · {Game.ProductName}";
            OrderName.textWrappingMode=TextWrappingModes.Normal;OrderName.enableAutoSizing=true;OrderName.fontSizeMin=22;OrderName.fontSizeMax=28;
            string wishes=Game.IsRepair?"\n\nArızalı tuşu testte bul.\nKapağı sök, switch'i değiştir, kapağı tak.\nÜç komşu tuşu yeniden dene.":$"\n\nİstek: {request.Sound}\n{request.Feel}\nRenk ve desen tamamen sana ait.\n\nSeçim: {WorkshopOrders.SwitchName(selected)}\n{Game.CurrentFit(selected)}";
            string payment=Game.IsRepair?"Tamir ücreti: 90 Tık":Game.IsMacro?"100 Tık + en fazla 30 Tık teşekkür":"240 Tık + en fazla 60 Tık teşekkür";
            OrderBody.text=Game.CurrentOrderMessage+wishes+"\n\n"+(Game.Completed?(Game.Testing&&Game.Testing.Passed?"Hazır. Özenle paketleyelim.":"Teslimattan önce kontrol edelim."):$"{Game.Installed} / {Game.PartCount} parça hazır.")+"\n"+payment;
            OrderAction.text=Game.Completed?(Game.Testing&&!Game.Testing.Passed?"Tuşları kontrol et":"Paketlemeye geç"):"Atölyeye dön";
        }
        public void Toast(string text){if(!ToastLabel)return;ToastLabel.text=text;toastUntil=Time.unscaledTime+3;ToastLabel.gameObject.SetActive(true);}
        void Update(){if(ToastLabel&&ToastLabel.gameObject.activeSelf&&Time.unscaledTime>toastUntil)ToastLabel.gameObject.SetActive(false);}
    }
}
