using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CozyBoard {
    [Serializable] public sealed class WorkshopMouseState {
        public bool kitReserved,underbody;
        public int tunedMask,filmMask,testMask;
        public int[] clickFeel={1,1};
        public string[] functions=new string[2];
    }
    public sealed partial class WorkshopMouseProduct:MonoBehaviour {
        public WorkshopGameMode Game{get;private set;}
        public WorkshopMouseState State=new();
        public bool Busy{get;private set;}
        bool initialized;Vector3 restPosition;Quaternion restRotation;
        GameObject controls;UnityEngine.UI.Button leftTune,rightTune,flip,peel,takeScrew;
        public Vector3 SavedPosition=>State.underbody?restPosition:Game.Controller.Lookup["Case"].transform.position;
        public float SavedYaw=>State.underbody?restRotation.eulerAngles.y:Game.Controller.Lookup["Case"].transform.eulerAngles.y;
        public bool HousingClosed=>parts.Where(x=>x.Stage==4).All(x=>x.Fitted);
        public bool HardwareComplete=>parts.Where(x=>x.Stage<5).All(x=>x.Fitted)&&State.tunedMask==3;
        public int Stage{get{int stage=parts.Where(x=>!x.Fitted).Select(x=>x.Stage).DefaultIfEmpty(6).Min();return stage==4&&State.tunedMask!=3?3:stage;}}
        public int TestCount=>Enumerable.Range(0,8).Count(i=>(State.testMask&(1<<i))!=0);
        public bool MacrosReady=>State.functions!=null&&State.functions.Length==2&&State.functions[0]=="Geri al"&&State.functions[1]=="Kaydet";
        static readonly string[] AssemblyOrder={"Mouse_PCB","Mouse_Sensor","Mouse_Lens","Mouse_Cable","Mouse_LeftSwitch","Mouse_RightSwitch","Mouse_Wheel","Mouse_SideSwitch_0","Mouse_SideSwitch_1","Mouse_Shell","Mouse_LeftButton","Mouse_RightButton","Mouse_SideButton_0","Mouse_SideButton_1","Mouse_Skate_0","Mouse_Skate_1","Mouse_Skate_2","Mouse_Skate_3"};
        public int StockRank(WorkshopItem item)=>(item.Stage==5&&Prepared(item)?-100:0)+Array.IndexOf(AssemblyOrder,item.Id);
        WorkshopItem NextPart=>parts.Where(x=>!x.Fitted&&x.Stage==Stage&&Ready(x)).OrderBy(StockRank).FirstOrDefault();
        int Fastened=>Enumerable.Range(0,4).Count(i=>(Game.Tools.Tightened&(1<<i))!=0);
        public string NextInstruction=>NextPart?"Şimdi: "+NextPart.Label+" · Tepsiden al, yuvasına bırak.":"";
        public static readonly string[] Feels={"Yumuşak","Dengeli","Net"};
        public string Instruction=>!Game.Shop.HasSupply(Stage)?"Laptopun parça pazarından bir mouse kiti al.":Stage switch{
            1=>"Mouse devresini alt gövdeye yerleştir.",
            2=>NextInstruction,
            3=>NextPart?NextInstruction:"İstersen tık hissini ayarla; ardından üst gövdeyi tak.",
            4=>NextInstruction,
            5=>!State.underbody?"Şimdi altını çevir.":Game.Tools.Tightened!=15?$"Vidalar: {Fastened}/4 · Kaptan bir vida al, boş yuvaya tıkla.":Game.Controller.Dragged&&Game.Controller.Dragged.Stage==5?"Ayak elinde · İşaretli boş köşelerden birine bırak.":"Bir ayağı hazırla; işaretli boş köşelerden birine yerleştir.",
            _=>Game.Testing.Passed?"Mouse hazır. Özenle paketleyelim.":"Mouse’u test et; yan düğmelere geri al ve kaydet ata."};
        public void Ensure(WorkshopGameMode owner){Game=owner;if(initialized)return;initialized=true;BuildGeometry();BuildControls();}
        void BuildControls(){
            var card=WorkshopUI.Panel("Mouse assembly controls",Game.Menu.OrderPanel.transform.parent,new Vector2(.5f,0),new Vector2(0,250),new Vector2(780,76),WorkshopUI.Paper);controls=card.gameObject;WorkshopAtelierStyle.Paper(card,WorkshopUI.Paper,14).raycastTarget=false;
            leftTune=WorkshopUI.Button("Adjust left mouse click",card.transform,Game.Objective.font,"",Vector2.one*.5f,new Vector2(-180,0),new Vector2(320,48),()=>Tune(0));
            rightTune=WorkshopUI.Button("Adjust right mouse click",card.transform,Game.Objective.font,"",Vector2.one*.5f,new Vector2(180,0),new Vector2(320,48),()=>Tune(1));
            flip=WorkshopUI.Button("Turn mouse underside",card.transform,Game.Objective.font,"Altını çevir",Vector2.one*.5f,new Vector2(-160,0),new Vector2(280,48),ToggleUnderbody);
            peel=WorkshopUI.Button("Peel next mouse skate",card.transform,Game.Objective.font,"Ayağı hazırla",Vector2.one*.5f,Vector2.zero,new Vector2(360,48),PrepareNextFoot);
            takeScrew=WorkshopUI.Button("Take next mouse screw",card.transform,Game.Objective.font,"Kaptan vida al",Vector2.one*.5f,Vector2.zero,new Vector2(360,48),()=>Game.Tools.TakeScrew());controls.SetActive(false);
        }
        public void ResetState(){CloseUnderbody();StopAllCoroutines();Busy=false;State=new WorkshopMouseState();foreach(var pair in films){pair.Value.localPosition=Vector3.zero;pair.Value.localRotation=Quaternion.identity;}if(controls)controls.SetActive(false);}
        public void Restore(WorkshopMouseState state){State=state??new WorkshopMouseState();State.tunedMask&=3;State.filmMask&=15;State.testMask&=255;if(State.functions==null||State.functions.Length!=2)State.functions=new string[2];if(State.clickFeel==null||State.clickFeel.Length!=2)State.clickFeel=new int[2];for(int i=0;i<2;i++)State.clickFeel[i]=Mathf.Clamp(State.clickFeel[i],0,2);}
        public void RestoreClicks(string[] fitted){for(int side=0;side<2;side++){if(fitted.Contains(side==0?"Mouse_LeftSwitch":"Mouse_RightSwitch")&&(State.tunedMask&(1<<side))==0){State.clickFeel[side]=1;State.tunedMask|=1<<side;}}}
        public void RestorePose(){if(!State.underbody)return;var board=Game.Controller.Lookup["Case"].transform;restPosition=board.position;restRotation=board.rotation;board.position=restPosition+Vector3.up*1.55f;board.rotation=restRotation*Quaternion.Euler(180,0,0);}
        public void CloseUnderbody(){if(!Game||!State.underbody)return;var board=Game.Controller.Lookup["Case"].transform;board.SetPositionAndRotation(restPosition,restRotation);State.underbody=false;}
        public void ToggleUnderbody(){if(!Game.IsMouse||!HousingClosed||Game.ScreenChangeBlocked||Game.Menu.InputBlocked||Game.Testing.Active||Game.Painter.Editing)return;Game.Controller.CancelDrag();Game.Tools.Deselect();StartCoroutine(Turn());}
        IEnumerator Turn(){Busy=true;var board=Game.Controller.Lookup["Case"].transform;bool opening=!State.underbody;if(opening){restPosition=board.position;restRotation=board.rotation;}var from=board.position;var rotation=board.rotation;var target=opening?restPosition+Vector3.up*1.55f:restPosition;var targetRotation=opening?restRotation*Quaternion.Euler(180,0,0):restRotation;
            for(float t=0;t<.55f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.55f);board.SetPositionAndRotation(Vector3.Lerp(from,target,f)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*.4f),Quaternion.Slerp(rotation,targetRotation,f));yield return null;}board.SetPositionAndRotation(target,targetRotation);State.underbody=opening;Busy=false;Game.Refresh();}
        public bool Tune(int side){if(!Game.IsMouse||side<0||side>1||Game.ScreenChangeBlocked||Game.Menu.InputBlocked)return false;var sw=parts.First(x=>x.Id==(side==0?"Mouse_LeftSwitch":"Mouse_RightSwitch"));if(!sw.Fitted||parts.Any(x=>x.Stage==4&&x.Fitted))return false;StartCoroutine(Adjust(sw,side));return true;}
        IEnumerator Adjust(WorkshopItem item,int side){Busy=true;Game.Controller.CancelDrag();for(float t=0;t<.30f;t+=Time.unscaledDeltaTime){item.Visual.transform.localPosition=Vector3.up*(Mathf.Sin(t/.30f*Mathf.PI)*.045f);yield return null;}item.Visual.transform.localPosition=Vector3.zero;State.clickFeel[side]=(State.clickFeel[side]+1)%3;State.tunedMask|=1<<side;Invalidate();Game.Audio.MouseClick(State.clickFeel[side]);Busy=false;Game.PresentStock();Game.Refresh();}
        static int FootIndex(WorkshopItem item)=>int.Parse(item.Id.Substring(item.Id.Length-1));
        public bool Ready(WorkshopItem item)=>(item.Stage!=5||State.underbody&&Game.Tools.Tightened==15)&&(item.Stage!=4||State.tunedMask==3)&&(item.Id!="Mouse_Lens"||parts.First(x=>x.Id=="Mouse_Sensor").Fitted);
        bool Prepared(WorkshopItem foot)=>(State.filmMask&(1<<FootIndex(foot)))!=0;
        public bool CanFit(WorkshopItem item)=>Ready(item)&&(item.Stage!=5||State.underbody&&Game.Tools.Tightened==15&&(Prepared(item)||Game.Controller.Dragged&&Game.Controller.Dragged.Stage==5&&Prepared(Game.Controller.Dragged)));
        public void TransferFootPreparation(WorkshopItem from,WorkshopItem to){if(from==to||from.Stage!=5||to.Stage!=5||!Prepared(from))return;State.filmMask&=~(1<<FootIndex(from));State.filmMask|=1<<FootIndex(to);}
        public bool CanRestore(WorkshopItem item){
            bool pcbFitted=parts.First(x=>x.Stage==1).Fitted;
            if(item.Stage==1)return true;
            if(item.Stage<=3)return pcbFitted&&(item.Id!="Mouse_Lens"||parts.First(x=>x.Id=="Mouse_Sensor").Fitted);
            if(item.Stage==4)return pcbFitted&&parts.Where(x=>x.Stage==3).All(x=>x.Fitted)&&State.tunedMask==3;
            return HousingClosed&&(State.filmMask&(1<<FootIndex(item)))!=0&&Game.Tools.Tightened==15;
        }
        public bool Pick(WorkshopItem item){if(item.Fitted){if(item.Kind=="mouse-switch")Tune(item.Id=="Mouse_LeftSwitch"?0:1);else if(item.Kind=="mouse-button")Game.Press(item,true);return false;}if(item.Stage==5&&!Prepared(item)){Peel(item,true);return false;}return true;}
        void PrepareNextFoot(){if(Busy||Game.Tools.Busy||Game.Controller.Dragged)return;var foot=NextPart;if(!foot||foot.Stage!=5)return;if(!Prepared(foot))Peel(foot,true);else{Game.Menu.SelectTool(0);CarryFoot(foot);}}
        void CarryFoot(WorkshopItem foot){var point=UnityEngine.InputSystem.Mouse.current?.position.ReadValue()??new Vector2(Screen.width*.5f,Screen.height*.5f);Game.Controller.BeginClickCarry(foot,point);Game.Menu.Toast("Koruyucu kağıt çıktı · Ayağı işaretli boş bir köşeye bırak.");}
        public bool Peel(WorkshopItem foot,bool carry=false){if(!Game.IsMouse||!foot||foot.Stage!=5||foot.Fitted||Game.ScreenChangeBlocked||Game.Menu.InputBlocked||Prepared(foot))return false;if(!State.underbody||Game.Tools.Tightened!=15){Game.Menu.Toast(!State.underbody?"Önce mouse’un altını çevir.":$"Önce dört vidayı sabitle: {Fastened}/4.");return false;}Game.Menu.SelectTool(0);Game.Controller.CancelDrag();StartCoroutine(PeelFilm(foot,carry));return true;}
        IEnumerator PeelFilm(WorkshopItem foot,bool carry){Busy=true;var paper=films[foot];Game.Audio.Play(Game.Audio.Pickup,.25f);for(float t=0;t<.38f;t+=Time.unscaledDeltaTime){float f=t/.38f;paper.localPosition=new Vector3(f*.24f,-Mathf.Sin(f*Mathf.PI*.5f)*.50f,0);paper.localRotation=Quaternion.Euler(0,0,f*70);yield return null;}State.filmMask|=1<<FootIndex(foot);paper.gameObject.SetActive(false);paper.localPosition=Vector3.zero;paper.localRotation=Quaternion.identity;Busy=false;Game.PresentStock();Game.Refresh();if(carry)CarryFoot(foot);else Game.Menu.Toast("Ayak hazır · İşaretli boş bir köşeye yerleştir.");}
        public void Installed(WorkshopItem item){Invalidate();if(item.Kind=="mouse-switch"){int side=item.Id=="Mouse_LeftSwitch"?0:1;State.tunedMask|=1<<side;State.clickFeel[side]=1;}}
        public void Removed(WorkshopItem item){if(item.Stage==4)CloseUnderbody();Invalidate();if(item.Kind=="mouse-switch")State.tunedMask&=~(item.Id=="Mouse_LeftSwitch"?1:2);if(item.Stage==4)Game.Tools.Tightened=0;}
        public void Invalidate(){State.testMask=0;}
        public bool RecordTest(int index){if(!Game.IsMouse||!Game.Testing.Active||!Game.Completed||index<0||index>=8)return false;if((State.testMask&(1<<index))!=0)return true;if(index>=6&&(State.functions[index-6]!=(index==6?"Geri al":"Kaydet")))return false;State.testMask|=1<<index;Game.Testing.RefreshProduct();Game.Refresh();return true;}
        public WorkshopItem FindSlot(int stage,Vector3 world){var p=Game.Controller.Lookup["Case"].transform.InverseTransformPoint(world);var held=Game.Controller.Dragged;return parts.Where(x=>x.Stage==stage&&!x.Fitted&&(stage==5||!held||held.Stage!=stage||held==x)).Where(x=>Mathf.Abs(p.x-x.Slot.x)<Mathf.Max(stage==5?.28f:.19f,x.BoundsSize.x*.48f)&&Mathf.Abs(p.z-x.Slot.z)<(x.Id=="Mouse_Cable"?.24f:Mathf.Max(stage==5?.26f:.18f,x.BoundsSize.z*.43f))).OrderBy(x=>(new Vector2(p.x-x.Slot.x,p.z-x.Slot.z)).sqrMagnitude).FirstOrDefault();}
        public void PresentStock(){if(!initialized||!Game.IsMouse)return;var stock=Game.Stock;for(int i=0;i<stock.Length;i++){var item=stock[i];if(item==Game.Controller.Dragged)continue;item.transform.SetParent(Game.Controller.PartsRoot,true);item.transform.localScale=Vector3.one*Game.ProductScale;item.transform.rotation=item.Stage==5?Quaternion.Euler(180,0,0):Quaternion.identity;var center=item.BoundsCenter;item.transform.position=new Vector3(-4.55f,.16f+item.BoundsSize.y*Game.ProductScale*.5f,.55f)-item.transform.TransformVector(center);}}
        void Update(){if(!initialized)return;bool visible=Game.IsMouse&&!Game.Experience.MainVisible&&!Game.Experience.Packing&&!Game.Experience.Inspecting&&!Game.Painter.Editing&&!Game.Shop.IsOpen&&!Game.Menu.InputBlocked&&!Game.Testing.Active&&!Game.RepairBench.AwayFromProduct;
            if(tray)tray.gameObject.SetActive(visible&&Game.Stock.Length>0);foreach(var pair in films)pair.Value.gameObject.SetActive(Game.IsMouse&&pair.Key.Visual.enabled&&!pair.Key.Fitted&&(State.filmMask&(1<<FootIndex(pair.Key)))==0);
            bool tuning=Stage==4&&!parts.Any(x=>x.Stage==4&&x.Fitted),underside=HousingClosed;
            var cardRect=(RectTransform)controls.transform;cardRect.sizeDelta=new Vector2(tuning?780:400,76);cardRect.anchoredPosition=new Vector2(tuning?0:-450,250);
            controls.SetActive(visible&&(tuning||underside));leftTune.gameObject.SetActive(tuning);rightTune.gameObject.SetActive(tuning);
            flip.gameObject.SetActive(!tuning&&underside&&(Stage>=6||!State.underbody));((RectTransform)flip.transform).anchoredPosition=Vector2.zero;
            bool fastening=!tuning&&underside&&Stage==5&&State.underbody&&Game.Tools.Tightened!=15;
            takeScrew.gameObject.SetActive(fastening);peel.gameObject.SetActive(!tuning&&underside&&Stage==5&&State.underbody&&Game.Tools.Tightened==15);
            leftTune.interactable=rightTune.interactable=flip.interactable=peel.interactable=takeScrew.interactable=!Busy&&!Game.Tools.Busy;
            peel.interactable&=!Game.Controller.Dragged;
            takeScrew.interactable&=!Game.Tools.HoldingScrew&&Game.Tools.LooseScrewCount>0;
            takeScrew.GetComponentInChildren<TMP_Text>().text=Game.Tools.HoldingScrew?"Vida elinde · Boş yuvaya tıkla":$"Kaptan vida al · {Fastened}/4 sabitlendi";
            peel.GetComponentInChildren<TMP_Text>().text=Game.Controller.Dragged?"Ayak elinde · Boş köşeye bırak":$"Ayağı hazırla · {parts.Count(x=>x.Stage==5&&x.Fitted)+1}/4";
            leftTune.GetComponentInChildren<TMP_Text>().text="Sol tık · "+Feels[State.clickFeel[0]]+" (isteğe bağlı)";rightTune.GetComponentInChildren<TMP_Text>().text="Sağ tık · "+Feels[State.clickFeel[1]]+" (isteğe bağlı)";leftTune.GetComponentInChildren<TMP_Text>().fontSize=rightTune.GetComponentInChildren<TMP_Text>().fontSize=18;
            flip.GetComponentInChildren<TMP_Text>().text=State.underbody?"Üstüne çevir":"Altını çevir";
            UpdatePlacementGuides(visible);

        }
    }
    public sealed partial class WorkshopGameMode {
        public bool IsMouse=>ActiveOrder?.kind=="mouse";
        public WorkshopMouseProduct MouseProduct{get;private set;}
        public bool CanPaint(WorkshopItem item)=>item&&(item.Kind=="keycap"||IsMouse&&(item.Kind=="mouse-shell"||item.Kind=="mouse-button"));
        void EnsureMouse(){if(!IsMouse)return;MouseProduct=GetComponent<WorkshopMouseProduct>()??gameObject.AddComponent<WorkshopMouseProduct>();MouseProduct.Ensure(this);}
    }
}
