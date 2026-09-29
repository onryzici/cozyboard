using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
namespace CozyBoard {
    public sealed class WorkshopGameMode : MonoBehaviour {
        public WorkshopController Controller;
        public WorkshopAudio Audio;
        public WorkshopMenu Menu;
        public WorkshopKeyPainter Painter;
        public TMP_Text Objective, Progress, StockLabel, CompletionLabel;
        public UnityEngine.UI.Image ProgressFill;
        public UnityEngine.UI.Button TestButton, NewOrderButton;
        public UnityEngine.UI.Slider MusicSlider, EffectsSlider;
        public MeshRenderer TargetMarker;
        [NonSerialized] public bool SessionActive,TypingMode;
        public int ActiveSupply=3, OrderNumber=1;
        readonly HashSet<string> pressing=new();
        readonly Stack<string> history=new();
        public int CurrentStage=>Completed?5:!Controller.Lookup["PCB"].Fitted?1:!Controller.Lookup["Plate"].Fitted?2:Controller.Items.Any(p=>p.Kind=="switch"&&!p.Fitted)?3:4;
        public bool Completed=>Installed==124;
        public int Installed=>Controller.Items.Count(p=>p.Stage>0&&p.Fitted);
        public WorkshopItem[] Stock=>CurrentStage is >=1 and <=4?Controller.Items.Where(p=>p.Stage==CurrentStage&&!p.Fitted).OrderBy(p=>p.Id).Take(CurrentStage<3?1:6).ToArray():Array.Empty<WorkshopItem>();
        public WorkshopItem SupplyItem(int stage)=>Controller.Items.FirstOrDefault(p=>p.Stage==stage&&!p.Fitted&&p!=Controller.Dragged);
        public void BeginSession() {
            if(MusicSlider){MusicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("CozyBoard.MusicVolume",.32f));MusicSlider.onValueChanged.AddListener(Audio.SetMusic);}
            if(EffectsSlider){EffectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("CozyBoard.EffectsVolume",.8f));EffectsSlider.onValueChanged.AddListener(Audio.SetEffects);}
            TestButton.onClick.AddListener(()=>{TypingMode=!TypingMode;Refresh();});
            NewOrderButton.onClick.AddListener(NewOrder);
            NewOrder();if(Menu)Menu.Bind(this);if(Painter)Painter.Bind(this);
            if(Application.isPlaying&&File.Exists(SavePath))LoadProgress();
        }
        void StopAnimations(){StopAllCoroutines();pressing.Clear();foreach(var item in Controller.Items){item.Visual.transform.localPosition=Vector3.zero;item.Visual.transform.localRotation=Quaternion.identity;}}
        public void NewOrder() {
            Controller.CancelDrag();StopAnimations();history.Clear();SessionActive=true;TypingMode=false;if(Painter)Painter.ResetPaint();
            if(Menu&&Menu.ToastLabel)Menu.ToastLabel.gameObject.SetActive(false);
            foreach(var item in Controller.Items.OrderByDescending(p=>p.Stage)) {
                item.transform.SetParent(Controller.PartsRoot,false);item.transform.localRotation=Quaternion.identity;
                item.transform.localScale=Vector3.one*(item.Stage==0?1:1.22f);item.Fitted=false;
                if(item.Stage==0){item.transform.localPosition=item.InitialPosition;item.transform.localRotation=Quaternion.Euler(0,item.InitialYaw,0);if(item.Id=="Case")item.transform.localScale=Vector3.one*1.22f;}
                else item.transform.localPosition=new Vector3(0,-20,0);
            }
            ActiveSupply=1;Controller.Yaw=0;Controller.Pitch=81;Controller.ViewWidth=21;Controller.ViewTarget=Vector3.zero;Controller.UpdateCamera();
            PresentStock();Refresh();
        }
        public bool CanPick(WorkshopItem item) {
            if(item.Kind=="keycap"&&item.Fitted){Press(item);return false;}
            if(item.Fitted)return false;
            if(item.Stage>0&&!CanInstall(item)){RejectStage(item.Stage);return false;}
            return item.Stage>0 || item.Id!="Case" || !Menu || Menu.ToolMode!=0;
        }
        public bool CanInstall(WorkshopItem item)=>item&&item.Stage>0&&!item.Fitted&&item.Stage==CurrentStage;
        public void RejectStage(int requested){if(Menu)Menu.Toast(requested<CurrentStage?"Bu aşama tamamlandı.":StageInstruction());}
        public string StageInstruction()=>CurrentStage switch{1=>"Önce PCB'yi kasaya yerleştir.",2=>"Şimdi plakayı PCB'nin üzerine yerleştir.",3=>"Önce bütün switch'leri plakaya tak.",4=>"Switch'ler hazır. Şimdi tuşları yerleştir.",_=>"Klavye tamamlandı."};
        public bool Visible(WorkshopItem item)=>item.Stage==0||item.Fitted||item==Controller.Dragged||Stock.Contains(item);
        public void Picked(WorkshopItem item){if(item.Stage>0)ActiveSupply=item.Stage;Audio.Play(Audio.Pickup,.23f);UpdateTarget(item);}
        public void InstalledPart(WorkshopItem item){InstalledPart(item,item.transform.position+Vector3.up*.22f);}
        public void InstalledPart(WorkshopItem item,Vector3 from) {
            if(!SessionActive)return;history.Push(item.Id);
            if(Application.isPlaying)StartCoroutine(InstallMotion(item,from));else Audio.Key();
            PresentStock();Refresh();if(Completed){TypingMode=true;Refresh();}
        }
        IEnumerator InstallMotion(WorkshopItem item,Vector3 from) {
            var visual=item.Visual.transform;var end=item.transform.position;var aligned=end+Vector3.up*.17f;
            var start=Vector3.Lerp(from,aligned,.65f);visual.position=start;
            for(float e=0;e<.13f;e+=Time.deltaTime){visual.position=Vector3.Lerp(start,aligned,Mathf.SmoothStep(0,1,e/.13f));yield return null;}
            for(float e=0;e<.11f;e+=Time.deltaTime){float t=e/.11f;visual.position=Vector3.Lerp(aligned,end+Vector3.up*.035f,t*t);yield return null;}
            visual.position=end+Vector3.up*.035f;yield return new WaitForSeconds(.045f);
            for(float e=0;e<.055f;e+=Time.deltaTime){visual.position=Vector3.Lerp(end+Vector3.up*.035f,end,e/.055f);yield return null;}
            Audio.Key(item.Stage==4?.48f:.36f);
            for(float e=0;e<.22f;e+=Time.deltaTime){float t=e/.22f;visual.position=end+Vector3.up*(.035f*Mathf.Exp(-3.2f*t)*Mathf.Sin(t*Mathf.PI*3));yield return null;}
            visual.localPosition=Vector3.zero;
        }
        public void Dropped(){Audio.Play(Audio.Place,.18f);Refresh();}
        public void PresentStock() {
            foreach(int stage in Enumerable.Range(1,4)) {
                var stock=Stock.Where(p=>p.Stage==stage).ToArray();
                for(int i=0;i<stock.Length;i++) {
                    var item=stock[i];if(item==Controller.Dragged)continue;
                    item.transform.SetParent(Controller.PartsRoot,false);
                    float scale=stage<3?.65f:1.22f;item.transform.localScale=Vector3.one*scale;
                    item.transform.localRotation=Quaternion.Euler(0,stage<3?0:(i%3-1)*8,0);
                    Vector3 p=stage<3?new Vector3(stage==1?-2.4f:2.4f,.05f,3.82f):new Vector3((stage==3?-7.35f:7.35f)+(i%2-.5f)*.85f,.28f,.65f-(i/2)*.88f);
                    p.y=(stage<3?.24f:.22f)-(item.BoundsCenter.y-item.BoundsSize.y*.5f)*scale;
                    item.transform.localPosition=p;
                }
            }
        }
        public void Refresh() {
            if(!SessionActive)return;
            var stock=Stock;
            foreach(var item in Controller.Items){bool visible=item.Stage==0||item.Fitted||item==Controller.Dragged||stock.Contains(item);item.Visual.enabled=visible;item.Hitbox.enabled=visible;}
            if(Objective)Objective.text=Completed?"Klavyen hazır!":StageInstruction();
            if(Progress)Progress.text=$"{Controller.Items.Count(p=>p.Kind=="switch"&&p.Fitted)} / 61 switch   ·   {Controller.Items.Count(p=>p.Kind=="keycap"&&p.Fitted)} / 61 tuş";
            if(ProgressFill)ProgressFill.fillAmount=Installed/124f;
            if(StockLabel)StockLabel.gameObject.SetActive(false);
            if(CompletionLabel){CompletionLabel.gameObject.SetActive(Completed);CompletionLabel.text="Tuşları dene · Sipariş kartından teslim et.";}
            if(TestButton)TestButton.interactable=Installed>0;
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
            if(Menu&&Menu.InputBlocked)return;
            if(Keyboard.current==null)return;
            foreach(var key in Keyboard.current.allKeys){if(!key.wasPressedThisFrame)continue;
                string name=key.keyCode switch{Key.Backspace=>"Back",Key.Escape=>"Esc",Key.CapsLock=>"Caps",Key.LeftShift or Key.RightShift=>"Shift",Key.LeftCtrl or Key.RightCtrl=>"Ctrl",Key.LeftAlt or Key.RightAlt=>"Alt",Key.LeftMeta or Key.RightMeta=>"Win",_=>key.displayName};
                var item=Controller.Items.FirstOrDefault(p=>p.Kind=="keycap"&&p.Fitted&&string.Equals(p.Label.Replace("Tuş ",""),name,StringComparison.OrdinalIgnoreCase));
                if(item==null&&key.keyCode==Key.Space)item=Controller.Items.FirstOrDefault(p=>p.Kind=="keycap"&&p.Fitted&&p.Label.Contains("Space"));
                if(item)Press(item);
            }
        }
        public void Press(WorkshopItem item){Audio.Key(.55f);if(Application.isPlaying&&pressing.Add(item.Id))StartCoroutine(PressAnimation(item));}
        IEnumerator PressAnimation(WorkshopItem item){var visual=item.Visual.transform;visual.localPosition=-Vector3.up*.026f;yield return new WaitForSeconds(.07f);visual.localPosition=Vector3.zero;pressing.Remove(item.Id);}
        public void Undo(){Controller.CancelDrag();StopAnimations();if(history.Count==0)return;var item=Controller.Lookup[history.Pop()];item.Fitted=false;item.transform.SetParent(Controller.PartsRoot,true);PresentStock();Refresh();}
        [Serializable] public class SaveData { public int version=4,order;public string[] fitted,paintIds,paintTextures,paintColors;public bool[] legendVisibility;public Vector3 boardPosition;public float boardYaw; }
        public string SavePath=>Path.Combine(Application.persistentDataPath,"workshop-save.json");
        public string SerializeProgress()=>JsonUtility.ToJson(new SaveData{order=OrderNumber,fitted=Controller.Items.Where(p=>p.Fitted).Select(p=>p.Id).ToArray(),paintIds=Painter?Painter.PaintIds:null,paintTextures=Painter?Painter.PaintTextures:null,legendVisibility=Painter?Painter.LegendVisibility:null,boardPosition=Controller.Lookup["Case"].transform.position,boardYaw=Controller.Lookup["Case"].transform.eulerAngles.y},true);
        public void RestoreProgress(string json){var save=JsonUtility.FromJson<SaveData>(json);if(save==null||(save.version<1||save.version>4)||save.fitted==null)return;NewOrder();OrderNumber=Mathf.Max(1,save.order);Controller.Lookup["Case"].transform.SetPositionAndRotation(save.boardPosition,Quaternion.Euler(0,save.boardYaw,0));foreach(var id in save.fitted.Select(id=>Controller.Lookup.TryGetValue(id,out var value)?value:null).Where(item=>item&&item.Stage>0).OrderBy(item=>item.Stage))if(CanInstall(id))Controller.Attach(id);if(Painter&&save.version>=2)Painter.Restore(save.paintIds,save.paintTextures,save.paintColors,save.version>=4?save.legendVisibility:null);PresentStock();Refresh();}
        public void SaveProgress(){Controller.CancelDrag();File.WriteAllText(SavePath,SerializeProgress());if(Menu)Menu.Toast("Atölyen kaydedildi.");}
        public void LoadProgress(){try{RestoreProgress(File.ReadAllText(SavePath));}catch(Exception e){Debug.LogWarning("Save could not load: "+e.Message);}}
        public void Deliver(){if(!Completed)return;OrderNumber++;NewOrder();SaveProgress();if(Menu){Menu.ShowOrder();Menu.Toast("Sipariş teslim edildi. Yeni kart hazır.");}}
    }
}
