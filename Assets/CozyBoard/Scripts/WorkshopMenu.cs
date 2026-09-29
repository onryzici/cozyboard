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
        public bool InputBlocked=>(OrderPanel&&OrderPanel.activeSelf)||(SettingsPanel&&SettingsPanel.activeSelf);
        public void Bind(WorkshopGameMode game){Game=game;
            SaveButton.onClick.AddListener(Game.SaveProgress);OrderButton.onClick.AddListener(ShowOrder);SettingsButton.onClick.AddListener(()=>{Game.Controller.CancelDrag();SettingsPanel.SetActive(!SettingsPanel.activeSelf);OrderPanel.SetActive(false);});
            ConfirmButton.onClick.AddListener(()=>{if(Game.Completed)Game.Deliver();else ClosePanels();});
            PointerButton.onClick.AddListener(()=>SelectTool(0));MoveButton.onClick.AddListener(()=>SelectTool(1));RotateButton.onClick.AddListener(()=>SelectTool(2));PaintButton.onClick.AddListener(()=>SelectTool(3));UndoButton.onClick.AddListener(()=>{if(ToolMode==3&&Game.Painter)Game.Painter.Undo();else Game.Undo();});
            CloseOrder.onClick.AddListener(ClosePanels);CloseSettings.onClick.AddListener(ClosePanels);ToolMode=0;RefreshOrder();
        }
        public void SelectTool(int mode){Game.Controller.CancelDrag();if(mode==3&&!System.Linq.Enumerable.Any(Game.Controller.Items,item=>item.Kind=="keycap"&&item.Fitted)){ToolMode=0;if(Game.Painter)Game.Painter.SetVisible(false);Toast("Boyamak için önce klavyeye en az bir tuş tak.");return;}ToolMode=mode;if(Game.Painter)Game.Painter.SetVisible(mode==3);if(ToastLabel)ToastLabel.rectTransform.anchoredPosition=mode==3?new Vector2(0,154):new Vector2(0,35);Toast(mode==0?"Yerleştir · Kutudan al, yuvaların üzerinde sürükle":mode==1?"Taşı · Kasayı veya aletleri sürükle":mode==2?"Çevir · Kasaya veya alete tıkla":"Boya · Klavyeden bir tuş seç, büyütülmüş yüzeyinde çiz");}
        public void ShowOrder(){Game.Controller.CancelDrag();SettingsPanel.SetActive(false);OrderPanel.SetActive(true);RefreshOrder();}
        public void ClosePanels(){OrderPanel.SetActive(false);SettingsPanel.SetActive(false);}
        public void RefreshOrder(){if(!Game||!OrderName)return;OrderName.text=$"SİPARİŞ {Game.OrderNumber:00}\n\nMüşteri: Ece";
            OrderBody.text="Merhaba!\n\nÇizim yaparken kullanacağım küçük, sakin sesli bir klavye istiyorum. Krem ve adaçayı tonlarını çok seviyorum.\n\n<size=25><b>Atölye notu</b></size>\n60% düzen · 61 tuş\nLineer switch · Mat krem kasa\n\n"+(Game.Completed?"<color=#477459>124 / 124 parça hazır.\nTeslim edebilirsin.</color>":$"{Game.Installed} / 124 parça yerleştirildi.\n{Game.StageInstruction()}");
            OrderAction.text=Game.Completed?"Siparişi teslim et":"Atölyeye dön";
        }
        public void Toast(string text){if(!ToastLabel)return;ToastLabel.text=text;toastUntil=Time.unscaledTime+3;ToastLabel.gameObject.SetActive(true);}
        void Update(){if(ToastLabel&&ToastLabel.gameObject.activeSelf&&Time.unscaledTime>toastUntil)ToastLabel.gameObject.SetActive(false);}
    }
}
