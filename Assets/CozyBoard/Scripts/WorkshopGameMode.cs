using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
namespace CozyBoard {
    public sealed partial class WorkshopGameMode : MonoBehaviour {
        public WorkshopController Controller;
        public WorkshopAudio Audio;
        public WorkshopMenu Menu;
        public WorkshopKeyPainter Painter;
        public WorkshopExperience Experience;
        public WorkshopShop Shop;
        public WorkshopTools Tools;
        public WorkshopTesting Testing;
        public TMP_Text Objective, Progress, StockLabel, CompletionLabel;
        public UnityEngine.UI.Image ProgressFill;
        public UnityEngine.UI.Button TestButton, NewOrderButton;
        public UnityEngine.UI.Slider MusicSlider, EffectsSlider;
        public MeshRenderer TargetMarker;
        [NonSerialized] public bool SessionActive,TypingMode;
        public int ActiveSupply=3, OrderNumber=1;
        [NonSerialized] public string VerificationSavePath;
        public WorkshopOrders.Receipt LastDelivery;
        public readonly List<WorkshopOrders.Receipt> DeliveryMail=new();
        readonly HashSet<string> pressing=new();
        readonly Dictionary<string,Coroutine> keyMotions=new();
        readonly HashSet<string> installing=new();
        readonly Stack<string> history=new();
        public int CurrentStage=>IsMouse?MouseProduct.Stage:Completed?5:!Controller.Lookup["PCB"].Fitted?1:!Controller.Lookup["Plate"].Fitted?2:ProductItems.Any(p=>p.Kind=="switch"&&!p.Fitted)?3:4;
        public bool Completed=>Installed==PartCount;
        public bool ScreenChangeBlocked=>(MouseProduct&&MouseProduct.Busy)||(Tools&&Tools.Busy)||(RepairBench&&(RepairBench.Moving||RepairBench.Unpacking))||(Experience&&Experience.Packing);
        public int Installed=>ProductItems.Count(p=>p.Stage>0&&p.Fitted);
        public WorkshopItem[] Stock=>!WaitingForOrder&&!(Tools&&Tools.KeyboardUnderbody)&&CurrentStage is >=1 and <=5?ProductItems.Where(p=>p.Stage==CurrentStage&&!p.Fitted&&(!IsMouse||MouseProduct.Ready(p))&&RepairPartVisible(p)&&(!NeedsSupply(p)||!Shop||Shop.HasSupply(CurrentStage))).OrderBy(p=>IsMouse?MouseProduct.StockRank(p):0).ThenBy(p=>p.Id).Take(IsMouse||CurrentStage<3?1:18).ToArray():Array.Empty<WorkshopItem>();
        public WorkshopItem SupplyItem(int stage)=>WaitingForOrder||Tools&&Tools.KeyboardUnderbody?null:ProductItems.OrderBy(p=>IsMouse?MouseProduct.StockRank(p):0).FirstOrDefault(p=>p.Stage==stage&&!p.Fitted&&(!IsMouse||MouseProduct.Ready(p))&&RepairPartReady(p)&&p!=Controller.Dragged&&(!NeedsSupply(p)||!Shop||Shop.HasSupply(stage)));
        public void BeginSession() {
            Testing=GetComponent<WorkshopTesting>()??gameObject.AddComponent<WorkshopTesting>();Testing.Game=this;
            if(MusicSlider){MusicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("CozyBoard.MusicVolume",.32f));MusicSlider.onValueChanged.AddListener(Audio.SetMusic);}
            if(EffectsSlider){EffectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("CozyBoard.EffectsVolume",.8f));EffectsSlider.onValueChanged.AddListener(Audio.SetEffects);}
            TestButton.onClick.AddListener(()=>{if(Testing.Active)Testing.End();else Testing.Begin();Refresh();});
            NewOrderButton.onClick.AddListener(RestartActiveWork);
            NewOrder();if(Menu)Menu.Bind(this);if(Painter)Painter.Bind(this);
            if(Application.isPlaying&&File.Exists(SavePath))LoadProgress();
            if(Application.isPlaying&&Experience)Experience.Initialize(this);
        }
        internal void StopAnimations(){StopAllCoroutines();pressing.Clear();keyMotions.Clear();installing.Clear();if(Audio)Audio.StopAssembly();foreach(var item in Controller.Items){item.Visual.transform.localPosition=Vector3.zero;item.Visual.transform.localRotation=Quaternion.identity;item.Visual.transform.localScale=Vector3.one;}}
        public void NewOrder() {
            if(MouseProduct)MouseProduct.ResetState();
            Repair=null;MacroFunctions=IsMacro?new string[6]:null;
            if(Testing)Testing.ResetOrder();
            if(Experience){Experience.EndInspection();Experience.CancelPacking();}
            if(Tools)Tools.ResetState();if(Shop)Shop.NextOrder();
            Controller.CancelDrag();StopAnimations();if(Menu)Menu.ToolMode=0;if(Painter)Painter.SetVisible(false);history.Clear();SessionActive=true;TypingMode=false;if(Painter)Painter.ResetPaint();
            ConfigureProduct();
            if(Menu&&Menu.ToastLabel)Menu.ToastLabel.gameObject.SetActive(false);
            foreach(var item in Controller.Items.OrderByDescending(p=>p.Stage)) {
                item.transform.SetParent(Controller.PartsRoot,false);item.transform.localRotation=Quaternion.identity;
                item.transform.localScale=Vector3.one*(item.Stage==0?1:1.22f);item.Fitted=false;
                if(item.Stage==0){item.transform.localPosition=item.InitialPosition;item.transform.localRotation=Quaternion.Euler(0,item.InitialYaw,0);if(item.Id=="Case")item.transform.localScale=Vector3.one*ProductScale;}
                else item.transform.localPosition=new Vector3(0,-20,0);
            }
            ActiveSupply=1;Controller.Yaw=0;Controller.Pitch=75;Controller.ViewWidth=21;Controller.ViewTarget=Vector3.zero;Controller.UpdateCamera();
            if(RepairBench)RepairBench.SyncProduct();if(Shop)Shop.ApplyVariants();PresentStock();Refresh();
        }
        public bool CanPick(WorkshopItem item) {
            if(ScreenChangeBlocked||Tools&&Tools.KeyboardUnderbody&&item.Stage>0)return false;
            if(IsMouse&&item.Id.StartsWith("Mouse_")&&!MouseProduct.Pick(item))return false;
            if(item.Kind=="keycap"&&item.Fitted){Press(item,true);return false;}
            if(item.Fitted)return false;
            if(IsRepair&&Repair!=null&&item.Id==Repair.switchId&&Repair.spareOpened&&!Repair.spareChecked){RepairBench.OpenPartInspection(true);return false;}
            if(item.Stage>0&&!CanInstall(item)){RejectStage(item.Stage);return false;}
            return item.Stage>0 || item.Id!="Case" || !Menu || Menu.ToolMode!=0;
        }
        public bool CanInstall(WorkshopItem item)=>!WaitingForOrder&&!ScreenChangeBlocked&&!(Tools&&Tools.KeyboardUnderbody)&&item&&BelongsToProduct(item)&&RepairPartReady(item)&&(!IsMouse||!ScreenChangeBlocked&&MouseProduct.CanFit(item))&&item.Stage>0&&!item.Fitted&&item.Stage==CurrentStage&&(!NeedsSupply(item)||!Shop||Shop.HasSupply(item.Stage));
        public void RejectStage(int requested){if(Menu)Menu.Toast(requested<CurrentStage?"Bu aşama tamamlandı.":StageInstruction());}
        public string StageInstruction()=>IsMouse?MouseProduct.Instruction:Tools&&Tools.KeyboardUnderbody?"Alt yüz · Kaptan bir vida al, boş yuvaya tıkla. Bitince üstüne çevir.":WaitingForOrder?"Yeni işini laptopun Siparişler sekmesinden seç (L).":IsRepair?RepairInstruction():Shop&&!Shop.HasSupply(CurrentStage)?"Malzeme bitti. Laptopu açıp yeni bir set sipariş et (L).":CurrentStage switch{1=>"Önce PCB'yi kasaya yerleştir.",2=>"Şimdi plakayı PCB'nin üzerine yerleştir.",3=>"Önce bütün switch'leri plakaya tak.",4=>"Switch'ler hazır. Şimdi tuşları yerleştir.",_=>"Klavye tamamlandı."};
        public bool Visible(WorkshopItem item)=>item.Id=="Case"? !WaitingForOrder:item.Stage==0||BelongsToProduct(item)&&(item.Fitted||item==Controller.Dragged||Stock.Contains(item)||(RepairBench&&RepairBench.ShowsRecoveredCap(item)));
        public void Picked(WorkshopItem item){if(item.Stage>0)ActiveSupply=item.Stage;Audio.Play(Audio.Pickup,.23f);UpdateTarget(item);}
        public void InstalledPart(WorkshopItem item){InstalledPart(item,item.transform.position+Vector3.up*.22f);}
        public void InstalledPart(WorkshopItem item,Vector3 from) {
            if(!SessionActive)return;if(Testing)Testing.Installed(item);if(Shop&&NeedsSupply(item))Shop.Consume(item.Stage);AfterPartInstalled(item);history.Push(item.Id);
            if(Application.isPlaying)StartCoroutine(InstallMotion(item,from));else Audio.Key();
            PresentStock();Refresh();if(Completed){TypingMode=true;Refresh();}
        }
        IEnumerator InstallMotion(WorkshopItem item,Vector3 from) {
            installing.Add(item.Id);
            var visual=item.Visual.transform;
            // Work in the socket's local space, so the ritual follows a moved/rotated board.
            var start=item.transform.InverseTransformPoint(from)*.35f+Vector3.up*.15f;
            bool isSwitch=item.Stage==3;
            float approach=.12f, resistance=isSwitch?.18f:.10f, snap=.045f;
            visual.localPosition=start;
            for(float t=0;t<approach;t+=Time.deltaTime){
                visual.localPosition=Vector3.Lerp(start,Vector3.up*.12f,Mathf.SmoothStep(0,1,t/approach));yield return null;
            }
            if(isSwitch)Audio.AssemblyContact();
            for(float t=0;t<resistance;t+=Time.deltaTime){
                float f=Mathf.Clamp01(t/resistance);
                // Pressure builds against the plate; the last few millimetres resist the hand.
                visual.localPosition=Vector3.up*Mathf.Lerp(.12f,.045f,1-Mathf.Pow(1-f,3));
                visual.localRotation=Quaternion.Euler(0,0,isSwitch?Mathf.Sin(f*Mathf.PI)*1.8f:0);
                yield return null;
            }
            for(float t=0;t<snap;t+=Time.deltaTime){
                visual.localPosition=Vector3.up*Mathf.Lerp(.045f,-.012f,t/snap);yield return null;
            }
            visual.localPosition=-Vector3.up*.012f;visual.localRotation=Quaternion.identity;
            Audio.AssemblySeat(item.Stage);
            for(float t=0;t<.18f;t+=Time.deltaTime){
                float wave=Mathf.Exp(-t*24)*Mathf.Cos(t*48);
                visual.localPosition=-Vector3.up*(.012f*wave);yield return null;
            }
            visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;
            installing.Remove(item.Id);
        }

        public void Dropped(){Audio.Play(Audio.Place,.18f);Refresh();}
        public void PresentStock() {
            if(IsMouse){MouseProduct.PresentStock();Refresh();return;}
            foreach(int stage in Enumerable.Range(1,4)) {
                var stock=Stock.Where(p=>p.Stage==stage).ToArray();
                for(int i=0;i<stock.Length;i++) {
                    var item=stock[i];if(item==Controller.Dragged)continue;
                    item.transform.SetParent(Controller.PartsRoot,false);
                    float scale=stage<3?.65f:ProductScale;item.transform.localScale=Vector3.one*scale;
                    float angle=i*2.399963f+stage*.7f;
                    float radius=Mathf.Sqrt((i+.5f)/18f);
                    item.transform.localRotation=stage<3?Quaternion.identity:Quaternion.Euler(Mathf.Sin(i*3.7f)*13,Mathf.Sin(i*7.1f)*170,Mathf.Cos(i*2.3f)*16);
                    Vector3 p=stage<3?new Vector3(stage==1?-2.4f:2.4f,.05f,3.82f):new Vector3((stage==3?-7.35f:7.35f)+Mathf.Cos(angle)*radius*.87f,.28f,(stage==3?.58f:-.22f)+Mathf.Sin(angle)*radius*1.28f);
                    float height=stage<3?.24f:-.015f+(1-radius)*.38f+(i%3)*.024f;
                    p.y=height-(item.BoundsCenter.y-item.BoundsSize.y*.5f)*scale;
                    item.transform.localPosition=p;
                }
            }
            if(RepairBench)RepairBench.ArrangeParts();
        }
        public void Refresh() {
            if(!SessionActive)return;
            if(Experience)Experience.RefreshSupplyBoxes();
            var stock=Stock;
            Controller.Lookup["Case"].gameObject.SetActive(!WaitingForOrder);
            foreach(var item in Controller.Items){bool visible=BelongsToProduct(item)&&(!WaitingForOrder||item.Id!="Case")&&(item.Stage==0||item.Fitted||item==Controller.Dragged||(RepairBench&&RepairBench.ShowsRecoveredCap(item))||(stock.Contains(item)&&(!Experience||Experience.SupplyArrived(item.Stage))));item.Visual.enabled=visible;item.Hitbox.enabled=visible&&!(Tools&&Tools.Active&&Tools.Selected==item.Id);}
            if(Objective)Objective.text=Tools&&Tools.KeyboardUnderbody?(Tools.Tightened==15?"Dört vida sabitlendi · Üstüne çevirip montaja devam et.":"Alt yüz · Kaptan bir vida al, boş yuvaya tıkla."):IsMouse?MouseProduct.Instruction:IsRepair?RepairInstruction():Completed?(Testing&&Testing.Passed?"Gönderilmeye hazır!":"Son dokunuş: tuş kontrolü"):StageInstruction();
            if(Progress)Progress.text=IsMouse?$"{Installed} / {PartCount} parça · Avuç mouse":WaitingForOrder?"Bir tuşluk mola · Acelemiz yok.":IsRepair?(RepairReady?"Switch değiştirildi · Üç komşu tuşu yeniden kontrol et.":"Kısa tamir · Arızayı bul, switch'i değiştir, yeniden dene."):$"{ProductItems.Count(p=>p.Kind=="switch"&&p.Fitted)} / {KeyCount} switch   ·   {ProductItems.Count(p=>p.Kind=="keycap"&&p.Fitted)} / {KeyCount} tuş";
            if(ProgressFill)ProgressFill.fillAmount=Installed/(float)PartCount;
            if(StockLabel)StockLabel.gameObject.SetActive(false);
            if(CompletionLabel){CompletionLabel.gameObject.SetActive(Completed);CompletionLabel.text=IsMouse?"Tıklama · Kaydırma · Hareket · Sürükleme":Testing&&Testing.Passed?$"{TestCount} tuş kontrol edildi · Özenle paketleyelim.":IsMacro?"Testi aç · Altı kısayolu ayarla ve dene.":IsRepair?"Testi aç · Üç komşu tuşu kontrol et.":"Testi aç · Her tuşun çalıştığından emin ol.";}
            if(TestButton)TestButton.interactable=Completed;
            if(Shop)Shop.UpdateDeskLaptop();if(Tools)Tools.RefreshProductVisibility();if(RepairBench)RepairBench.RefreshInstrument();
            if(Controller.StatusLabel)Controller.StatusLabel.gameObject.SetActive(false);
            if(Controller.HintLabel)Controller.HintLabel.text=StageInstruction()+"  ·  Basılı sürükle: seri yerleştir  ·  Esc: bırak";
            UpdateTarget(Controller.Dragged);if(Menu)Menu.RefreshOrder();
        }
        void UpdateTarget(WorkshopItem held) {
            if(!TargetMarker)return;
            var target=held?Controller.SnapCandidate(held):null;
            TargetMarker.enabled=target;
            if(!target)return;
            var board=Controller.Lookup["Case"].transform;var pos=board.TransformPoint(target.Slot);pos.y+=.012f;
            TargetMarker.transform.SetPositionAndRotation(pos,board.rotation);
            TargetMarker.transform.localScale=new Vector3(Mathf.Max(.36f,target.BoundsSize.x*1.22f),1,Mathf.Max(.36f,target.BoundsSize.z*1.22f));
        }
        void Update() {
            if(!SessionActive)return;UpdateTarget(Controller.Dragged);
            if(RepairBench&&(RepairBench.Moving||RepairBench.AwayFromProduct))return;
            if((Menu&&(Menu.InputBlocked||Menu.EscapeHandledThisFrame))||(Tools&&(Tools.Active||Tools.Busy)))return;
            if(Keyboard.current==null)return;
            foreach(var key in Keyboard.current.allKeys){if(!key.wasPressedThisFrame)continue;
                string name=key.keyCode switch{Key.Backspace=>"Back",Key.Escape=>"Esc",Key.CapsLock=>"Caps",Key.LeftShift or Key.RightShift=>"Shift",Key.LeftCtrl or Key.RightCtrl=>"Ctrl",Key.LeftAlt or Key.RightAlt=>"Alt",Key.LeftMeta or Key.RightMeta=>"Win",_=>key.displayName};
                var item=Controller.Items.FirstOrDefault(p=>p.Kind=="keycap"&&p.Fitted&&string.Equals(p.Label.Replace("Tuş ",""),name,StringComparison.OrdinalIgnoreCase));
                if(item==null&&key.keyCode==Key.Space)item=Controller.Items.FirstOrDefault(p=>p.Kind=="keycap"&&p.Fitted&&p.Label.Contains("Space"));
                if(item)Press(item,false,key);
            }
        }
        public void Press(WorkshopItem item,bool mousePress=false,UnityEngine.InputSystem.Controls.KeyControl physicalKey=null){
            if(!item||!item.Fitted||item.Stage!=4||IsMouse&&item.Kind!="mouse-button"||(Tools&&Tools.Busy)||Tools&&Tools.KeyboardUnderbody||installing.Contains(item.Id))return;
            if(IsMouse){if(Testing&&Testing.Active)MouseProduct.RecordTest(item.Id=="Mouse_LeftButton"?0:item.Id=="Mouse_RightButton"?1:item.Id=="Mouse_SideButton_0"?6:7);Audio.MouseClick((item.Id.StartsWith("Mouse_SideButton")?1:MouseProduct.State.clickFeel[item.Id=="Mouse_LeftButton"?0:1]));}else if(!Testing||Testing.Check(item))Audio.Key(.55f);else Audio.AssemblyContact();
            if(!Application.isPlaying)return;
            // Every tap gets a fresh impulse, including taps during the previous spring return.
            if(keyMotions.TryGetValue(item.Id,out var previous))StopCoroutine(previous);
            pressing.Add(item.Id);keyMotions[item.Id]=StartCoroutine(PressAnimation(item,mousePress,physicalKey));
        }
        bool MouseButtonHeld(WorkshopItem item){var mouse=Mouse.current;if(mouse==null)return false;if(!IsMouse)return mouse.leftButton.isPressed;return item.Id switch{"Mouse_RightButton"=>mouse.rightButton.isPressed,"Mouse_SideButton_0"=>mouse.backButton.isPressed,"Mouse_SideButton_1"=>mouse.forwardButton.isPressed,_=>mouse.leftButton.isPressed};}
        IEnumerator PressAnimation(WorkshopItem item,bool mousePress,UnityEngine.InputSystem.Controls.KeyControl physicalKey){
            var visual=item.Visual.transform;float travel=IsMouse?new[]{.050f,.033f,.020f}[(item.Id.StartsWith("Mouse_SideButton")?1:MouseProduct.State.clickFeel[item.Id=="Mouse_LeftButton"?0:1])]:Audio.SwitchVoice==0?.16f:Audio.SwitchVoice==1?.145f:.155f;
            var start=visual.localPosition;visual.localScale=Vector3.one;visual.localRotation=Quaternion.identity;
            for(float t=0;t<.055f;t+=Time.unscaledDeltaTime){float f=Mathf.Sin(Mathf.Clamp01(t/.055f)*Mathf.PI*.5f);visual.localPosition=Vector3.Lerp(start,-Vector3.up*travel,f);yield return null;}
            visual.localPosition=-Vector3.up*travel;
            float held=0;while(held<.055f||((mousePress&&MouseButtonHeld(item))||(physicalKey!=null&&physicalKey.isPressed))&&item.Fitted&&(!Tools||!Tools.Active)){held+=Time.unscaledDeltaTime;yield return null;}
            // A rigid keycap follows the switch stem down and returns to its top stop.
            for(float t=0;t<.12f;t+=Time.unscaledDeltaTime){float f=Mathf.Clamp01(t/.12f);visual.localPosition=-Vector3.up*travel*Mathf.Pow(1-f,3);yield return null;}
            visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;visual.localScale=Vector3.one;pressing.Remove(item.Id);keyMotions.Remove(item.Id);
        }
        public void Undo(){if(ScreenChangeBlocked||Tools&&Tools.KeyboardUnderbody||Menu&&Menu.InputBlocked||Testing&&Testing.Active)return;Controller.CancelDrag();StopAnimations();while(history.Count>0&&!Controller.Lookup[history.Peek()].Fitted)history.Pop();if(history.Count==0)return;var item=Controller.Lookup[history.Pop()];if(Testing)Testing.Removed(item);item.Fitted=false;item.transform.SetParent(Controller.PartsRoot,true);PresentStock();Refresh();}
        [Serializable] public class SaveData { public WorkshopMouseState mouse;public int version=13,order,tightened,mountedScrews,screwTrayVersion;public WorkshopStory.State story;public WorkshopStory.ActiveOrder activeOrder;public RepairState repair;public string[] macroFunctions;public string[] tested;public string looseSwitch;public bool faultAssigned,keyboardUnderbody;public WorkshopOrders.Receipt receipt;public WorkshopOrders.Receipt[] mail;public WorkshopShop.ShopData shop;public string[] fitted,paintIds,paintTextures,paintColors;public string[] tapeMasks;public bool[] legendVisibility;public Vector3 boardPosition;public float boardYaw; }
        public string SavePath=>!string.IsNullOrEmpty(VerificationSavePath)?VerificationSavePath:Environment.GetCommandLineArgs().Contains("--cozy-variety-smoke")?Path.Combine(Application.temporaryCachePath,"variety-smoke-save.json"):Environment.GetCommandLineArgs().Contains("--cozy-story-smoke")?Path.Combine(Application.temporaryCachePath,"story-smoke-save.json"):Path.Combine(Environment.GetCommandLineArgs().Contains("--cozy-shop-smoke")?Application.temporaryCachePath:Application.persistentDataPath,Environment.GetCommandLineArgs().Contains("--cozy-shop-smoke")?"shop-smoke-save.json":"workshop-save.json");
        public string SerializeProgress()=>JsonUtility.ToJson(new SaveData{keyboardUnderbody=Tools&&Tools.KeyboardUnderbody,mouse=IsMouse?MouseProduct.State:null,repair=Repair,macroFunctions=MacroFunctions,story=Story,activeOrder=ActiveOrder,tested=Testing?Testing.Tested:null,looseSwitch=Testing?Testing.LooseSwitch:null,faultAssigned=Testing&&Testing.FaultAssigned,order=OrderNumber,receipt=LastDelivery,mail=DeliveryMail.ToArray(),screwTrayVersion=1,mountedScrews=Tools?Tools.Mounted:0,tightened=Tools?Tools.Tightened:0,shop=Shop?Shop.Data:null,fitted=Controller.Items.Where(p=>p.Fitted).Select(p=>p.Id).ToArray(),paintIds=Painter?Painter.PaintIds:null,paintTextures=Painter?Painter.PaintTextures:null,tapeMasks=Painter?Painter.TapeMasks:null,legendVisibility=Painter?Painter.LegendVisibility:null,boardPosition=IsMouse?MouseProduct.SavedPosition:Tools?Tools.SavedKeyboardPosition:Controller.Lookup["Case"].transform.position,boardYaw=IsMouse?MouseProduct.SavedYaw:Tools?Tools.SavedKeyboardYaw:Controller.Lookup["Case"].transform.eulerAngles.y},true);
        public void RestoreProgress(string json){var save=JsonUtility.FromJson<SaveData>(json);if(save==null||(save.version<1||save.version>13)||save.fitted==null)return;Story=save.version>=11&&save.story!=null?save.story:new WorkshopStory.State();ActiveOrder=save.version>=11&&save.activeOrder!=null&&!string.IsNullOrEmpty(save.activeOrder.instanceId)?save.activeOrder:null;NewOrder();if(IsMouse){MouseProduct.Restore(save.mouse);MouseProduct.RestoreClicks(save.fitted);if(Tools)Tools.RestoreScrews(save.screwTrayVersion>=1?save.mountedScrews:save.fitted.Length>0?15:0,save.tightened&15);}Repair=save.version>=12&&save.repair!=null&&!string.IsNullOrEmpty(save.repair.switchId)?save.repair:null;MacroFunctions=save.version>=12?save.macroFunctions:MacroFunctions;LastDelivery=save.version>=7&&save.receipt!=null&&save.receipt.order>0?save.receipt:null;DeliveryMail.Clear();if(save.version>=9&&save.mail!=null)DeliveryMail.AddRange(save.mail.Where(x=>x!=null&&x.order>0));if(LastDelivery!=null){var stored=DeliveryMail.FirstOrDefault(x=>x.order==LastDelivery.order);if(stored!=null)LastDelivery=stored;else DeliveryMail.Add(LastDelivery);}if(Shop)Shop.Restore(save.shop);OrderNumber=Mathf.Max(1,save.order);Controller.Lookup["Case"].transform.SetPositionAndRotation(save.boardPosition,Quaternion.Euler(0,save.boardYaw,0));foreach(var id in save.fitted.Select(id=>Controller.Lookup.TryGetValue(id,out var value)?value:null).Where(item=>item&&item.Stage>0).OrderBy(item=>item.Stage))if(CanRestorePart(id))Controller.Attach(id);if(Painter&&save.version>=2)Painter.Restore(save.paintIds,save.paintTextures,save.paintColors,save.version>=4?save.legendVisibility:null,save.version>=10?save.tapeMasks:null);if(Tools)Tools.RestoreScrews(save.screwTrayVersion>=1?save.mountedScrews:save.fitted.Length>0?15:0,save.version>=6?save.tightened:(Completed?15:0));if(Testing){Testing.RebuildPads();Testing.Restore(save.version>=8?save.tested:null,save.version>=8?save.looseSwitch:null,save.version<8||save.faultAssigned);}if(RepairBench)RepairBench.SyncProduct(true);if(IsMouse)MouseProduct.RestorePose();else if(Tools)Tools.RestoreKeyboardUnderbody(save.keyboardUnderbody);PresentStock();Refresh();}
        public void SaveProgress(){if(Tools&&Tools.Busy||MouseProduct&&MouseProduct.Busy){Menu.Toast("Alet işlemi bitince kaydedebilirsin.");return;}if(Shop)Shop.EndLaptopMove(false);if(Experience){Experience.EndInspection();Experience.CancelPacking();}Controller.CancelDrag();if(TrySaveQuiet()&&Menu)Menu.Toast("Atölyen kaydedildi.");}
        public void LoadProgress(){try{RestoreProgress(File.ReadAllText(SavePath));}catch(Exception e){Debug.LogWarning("Save could not load: "+e.Message);}}
        public void Deliver(){if(WaitingForOrder||!Completed)return;if(Testing&&!Testing.Passed){Testing.Begin();return;}if(Tools&&Tools.Tightened!=15){Menu.ClosePanels();Tools.PrepareKeyboardFastening();Menu.Toast("Paketlemeden önce alt yüzdeki dört vidayı sabitle.");return;}if(Experience&&!Experience.DeliveryReady){Experience.BeginPacking();return;}FinishDelivery();}
        public bool FinishDelivery(){
            if(WaitingForOrder||!Completed||Testing&&!Testing.Passed||Tools&&Tools.Tightened!=15)return false;
            var receipt=CreateReceipt();if(!string.IsNullOrEmpty(receipt.orderId)&&DeliveryMail.Any(x=>x.orderId==receipt.orderId))return false;
            if(Experience&&Experience.Packing)Experience.CancelPacking();
            string before=SerializeProgress();LastDelivery=receipt;DeliveryMail.Add(receipt);if(Shop)Shop.Reward(receipt.reward);
            if(!Story.enabled){Story.enabled=true;Story.openingRead=true;Story.Record(WorkshopStory.LegacyEvent);}Story.Record(receipt.storyEvent);ActiveOrder=null;OrderNumber++;NewOrder();
            if(!TrySaveQuiet()){RestoreProgress(before);return false;}
            if(Menu)Menu.ClosePanels();if(Experience)Experience.Delivered();return true;
        }
    }
}
