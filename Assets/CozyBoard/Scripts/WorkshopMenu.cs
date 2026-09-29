using UnityEngine;
using TMPro;
namespace CozyBoard {
    public sealed class WorkshopMenu:MonoBehaviour {
        public WorkshopGameMode Game;
        public GameObject OrderPanel,SettingsPanel;
        public TMP_Text OrderName,OrderBody,OrderAction,ToastLabel;
        public UnityEngine.UI.Button SaveButton,OrderButton,SettingsButton,ConfirmButton,PointerButton,MoveButton,RotateButton,UndoButton,CloseOrder,CloseSettings;
        public int ToolMode;
        float toastUntil;
        public bool InputBlocked=>(OrderPanel&&OrderPanel.activeSelf)||(SettingsPanel&&SettingsPanel.activeSelf);
        public void Bind(WorkshopGameMode game){Game=game;
            SaveButton.onClick.AddListener(Game.SaveProgress);OrderButton.onClick.AddListener(ShowOrder);SettingsButton.onClick.AddListener(()=>{Game.Controller.CancelDrag();SettingsPanel.SetActive(!SettingsPanel.activeSelf);OrderPanel.SetActive(false);});
            ConfirmButton.onClick.AddListener(()=>{if(Game.Completed)Game.Deliver();else ClosePanels();});
            PointerButton.onClick.AddListener(()=>SelectTool(0));MoveButton.onClick.AddListener(()=>SelectTool(1));RotateButton.onClick.AddListener(()=>SelectTool(2));UndoButton.onClick.AddListener(Game.Undo);
            CloseOrder.onClick.AddListener(ClosePanels);CloseSettings.onClick.AddListener(ClosePanels);ToolMode=0;RefreshOrder();
        }
        public void SelectTool(int mode){Game.Controller.CancelDrag();ToolMode=mode;Toast(mode==0?"Yerleştir · Kutudan al, yuvaların üzerinde sürükle":mode==1?"Taşı · Kasayı veya aletleri sürükle":"Çevir · Kasaya veya alete tıkla");}
        public void ShowOrder(){Game.Controller.CancelDrag();SettingsPanel.SetActive(false);OrderPanel.SetActive(true);RefreshOrder();}
        public void ClosePanels(){OrderPanel.SetActive(false);SettingsPanel.SetActive(false);}
        public void RefreshOrder(){if(!Game||!OrderName)return;OrderName.text=$"SİPARİŞ {Game.OrderNumber:00}\n\nMüşteri: Ece";
            OrderBody.text="Merhaba!\n\nÇizim yaparken kullanacağım küçük, sakin sesli bir klavye istiyorum. Krem ve adaçayı tonlarını çok seviyorum.\n\n<size=25><b>Atölye notu</b></size>\n60% düzen · 61 tuş\nLineer switch · Mat krem kasa\n\n"+(Game.Completed?"<color=#477459>124 / 124 parça hazır.\nTeslim edebilirsin.</color>":$"{Game.Installed} / 124 parça yerleştirildi.\nİstediğin parçadan başlayabilirsin.");
            OrderAction.text=Game.Completed?"Siparişi teslim et":"Atölyeye dön";
        }
        public void Toast(string text){if(!ToastLabel)return;ToastLabel.text=text;toastUntil=Time.unscaledTime+3;ToastLabel.gameObject.SetActive(true);}
        void Update(){if(ToastLabel&&ToastLabel.gameObject.activeSelf&&Time.unscaledTime>toastUntil)ToastLabel.gameObject.SetActive(false);}
    }
}
