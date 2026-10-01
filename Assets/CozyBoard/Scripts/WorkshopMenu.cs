using UnityEngine;
using TMPro;
namespace CozyBoard {
    [DefaultExecutionOrder(-50)]
    public sealed class WorkshopMenu:MonoBehaviour {
        public WorkshopGameMode Game;
        public GameObject OrderPanel,SettingsPanel;
        public TMP_Text OrderName,OrderBody,OrderAction,ToastLabel;
        public UnityEngine.UI.Button SaveButton,OrderButton,SettingsButton,ConfirmButton,PointerButton,MoveButton,RotateButton,PaintButton,UndoButton,CloseOrder,CloseSettings;
        public int ToolMode;
        float toastUntil;
        int escapeFrame=-1;
        public bool EscapeHandledThisFrame=>escapeFrame==Time.frameCount;
        public bool InputBlocked=>(OrderPanel&&OrderPanel.activeSelf)||(SettingsPanel&&SettingsPanel.activeSelf)||(Game&&Game.Experience&&Game.Experience.Blocking);
        public void Bind(WorkshopGameMode game){Game=game;
            SaveButton.onClick.AddListener(Game.SaveProgress);OrderButton.onClick.AddListener(ShowOrder);SettingsButton.onClick.AddListener(ShowSettings);
            ConfirmButton.onClick.AddListener(()=>{if(Game.IsRepair&&Game.RepairBench&&!Game.RepairBench.AtRepair){Game.RepairBench.Visit(true);return;}if(Game.Completed)Game.Deliver();else ClosePanels();});
            PointerButton.onClick.AddListener(()=>SelectTool(0));MoveButton.onClick.AddListener(()=>SelectTool(1));RotateButton.onClick.AddListener(()=>SelectTool(2));PaintButton.onClick.AddListener(()=>SelectTool(3));UndoButton.onClick.AddListener(()=>{if(ToolMode==3&&Game.Painter)Game.Painter.Undo();else Game.Undo();});
            CloseOrder.onClick.AddListener(ClosePanels);CloseSettings.onClick.AddListener(ClosePanels);ToolMode=0;RefreshOrder();
        }
        public void SelectTool(int mode){if(Game.ScreenChangeBlocked)return;if(mode==1&&Game.MouseProduct)Game.MouseProduct.CloseUnderbody();if(mode!=0&&Game.Tools)Game.Tools.CloseKeyboardUnderbody();if(Game.Shop)Game.Shop.EndLaptopMove(true);if(Game.Experience)Game.Experience.EndInspection();if(Game.Testing)Game.Testing.End();if(Game.RepairBench)Game.RepairBench.ClosePartInspection();if(Game.Tools)Game.Tools.Deselect();Game.Controller.CancelDrag();if(mode==3&&!System.Linq.Enumerable.Any(Game.Controller.Items,item=>Game.CanPaint(item)&&item.Fitted)){ToolMode=0;if(Game.Painter)Game.Painter.SetVisible(false);Toast(Game.IsMouse?"Boyamak için önce üst gövdeyi veya düğmelerden birini tak.":"Boyamak için önce klavyeye en az bir tuş tak.");return;}ToolMode=mode;if(Game.Painter)Game.Painter.SetVisible(mode==3);if(ToastLabel)ToastLabel.rectTransform.anchoredPosition=mode==3?new Vector2(0,154):new Vector2(0,35);Toast(mode==0?(Game.IsRepair?"Yerleştir · Tepsiden al, boş yuvaya bırak":"Yerleştir · Kutudan al, yuvaların üzerinde sürükle"):mode==1?"Taşı · Kasayı veya aletleri sürükle":mode==2?"Çevir · Kasaya veya alete tıkla":Game.IsMouse?"Boya · Gövdeden veya düğmelerden birini seç":"Boya · Klavyeden bir tuş seç, büyütülmüş yüzeyinde çiz");}
        bool CanOpenPanel=>!Game.ScreenChangeBlocked&&!(Game.StoryUI&&Game.StoryUI.Open);
        void PreparePanel(){if(Game.MouseProduct)Game.MouseProduct.CloseUnderbody();if(Game.Tools)Game.Tools.CloseKeyboardUnderbody();if(Game.Testing)Game.Testing.End();if(Game.Experience)Game.Experience.EndInspection();if(Game.Tools)Game.Tools.Deselect();Game.Controller.CancelDrag();if(Game.Painter)Game.Painter.CloseEditor();if(Game.Shop){Game.Shop.EndLaptopMove(true);Game.Shop.Close();}if(Game.RepairBench)Game.RepairBench.ClosePartInspection();}
        public void ShowSettings(){if(!CanOpenPanel)return;if(SettingsPanel.activeSelf){ClosePanels();return;}PreparePanel();OrderPanel.SetActive(false);SettingsPanel.SetActive(true);SettingsPanel.transform.SetAsLastSibling();}
        public void ShowOrder(){if(!CanOpenPanel)return;if(Game.WaitingForOrder){Game.Shop.OpenOrders();return;}PreparePanel();SettingsPanel.SetActive(false);OrderPanel.SetActive(true);OrderPanel.transform.SetAsLastSibling();RefreshOrder();}
        public void ClosePanels(){OrderPanel.SetActive(false);SettingsPanel.SetActive(false);if(Game&&Game.RepairBench)Game.RepairBench.ClosePartInspection();}
        public void RefreshOrder(){
            if(!Game||!OrderName)return;
            var portrait=OrderPanel.transform.Find("Photo paper/Drawn customer");if(portrait){var image=portrait.GetComponent<UnityEngine.UI.RawImage>();var atlas=Resources.Load<Texture2D>("Customers/Portraits");if(atlas){image.texture=atlas;image.uvRect=new Rect(WorkshopStory.PortraitIndex(Game.CustomerId)/3f,0,1f/3f,1);}}
            OrderBody.enableAutoSizing=true;OrderBody.fontSizeMin=19;OrderBody.fontSizeMax=24;
            var request=Game.CurrentRequest;int selected=Game.Shop?Game.Shop.Data.selected[1]:3;
            OrderName.text=$"SİPARİŞ {Game.OrderNumber:00}\n\n{request.Name} · {Game.ProductName}";
            OrderName.textWrappingMode=TextWrappingModes.Normal;OrderName.enableAutoSizing=true;OrderName.fontSizeMin=22;OrderName.fontSizeMax=28;
            string wishes=Game.IsMouse?"\n\nİstek: İki düğmede dengeli tıklama.\nTekerleği ve sensörü test et.\nGövdeyi boya, ayakları yapıştır.\nMouse kiti: 120 Tık.":Game.IsRepair?"\n\nArızalı tuşu testte bul.\nKapağı sök, switch'i değiştir, kapağı tak.\nÜç komşu tuşu yeniden dene.":$"\n\nİstek: {request.Sound}\n{request.Feel}\nRenk ve desen tamamen sana ait.\n\nSeçim: {WorkshopOrders.SwitchName(selected)}\n{Game.CurrentFit(selected)}";
            string payment=Game.IsRepair?"Tamir ücreti: 90 Tık":Game.IsMacro?"100 Tık + en fazla 30 Tık teşekkür":"240 Tık + en fazla 60 Tık teşekkür";
            OrderBody.text=Game.CurrentOrderMessage+wishes+"\n\n"+(Game.Completed?(Game.Testing&&Game.Testing.Passed?"Hazır. Özenle paketleyelim.":"Teslimattan önce kontrol edelim."):$"{Game.Installed} / {Game.PartCount} parça hazır.")+"\n"+payment;
            OrderAction.text=Game.IsRepair&&Game.RepairBench&&!Game.RepairBench.AtRepair?"Tamir masasına geç":Game.Completed?(Game.Testing&&!Game.Testing.Passed?"Tuşları kontrol et":"Paketlemeye geç"):"Atölyeye dön";
        }
        public void Toast(string text){if(!ToastLabel)return;ToastLabel.text=text;toastUntil=Time.unscaledTime+3;ToastLabel.gameObject.SetActive(true);}
        public void HandleEscape(){
            if(!Game||!Game.Experience||EscapeHandledThisFrame)return;escapeFrame=Time.frameCount;
            if(Game.Tools&&Game.Tools.Busy||Game.RepairBench&&(Game.RepairBench.Moving||Game.RepairBench.Unpacking))return;
            if(Game.Experience.CancelConfirmation())return;
            if(Game.StoryUI&&Game.StoryUI.Open){Game.StoryUI.CloseNotebook();return;}
            if(Game.Shop&&Game.Shop.IsOpen){Game.Shop.Close();return;}
            if(SettingsPanel.activeSelf||OrderPanel.activeSelf||Game.RepairBench&&Game.RepairBench.DetailOpen){ClosePanels();return;}
            if(Game.Painter&&Game.Painter.Editing){Game.Painter.CloseEditor();return;}
            if(Game.Experience.Inspecting){Game.Experience.EndInspection();return;}
            if(Game.Experience.Packing){Game.Experience.CancelPacking();return;}
            if(Game.Testing&&Game.Testing.Active){if(!Game.Testing.CloseFunctionPicker())Game.Testing.End();return;}
            if(Game.Tools&&Game.Tools.Active){Game.Tools.Deselect();return;}
            if(Game.Shop&&Game.Shop.IsMoving){Game.Shop.EndLaptopMove(true);return;}
            if(Game.Controller.Dragged){Game.Controller.CancelDrag();return;}
            if(ToolMode!=0){SelectTool(0);return;}
            if(!Game.Experience.MainVisible)Game.Experience.ShowMain();
        }
        void Update(){var keyboard=UnityEngine.InputSystem.Keyboard.current;if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)HandleEscape();if(ToastLabel&&ToastLabel.gameObject.activeSelf&&Time.unscaledTime>toastUntil)ToastLabel.gameObject.SetActive(false);}
    }
}
