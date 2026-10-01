using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CozyBoard {
 public sealed partial class WorkshopTesting:MonoBehaviour {
  public WorkshopGameMode Game;public bool Active{get;private set;}
  public string LooseSwitch;public bool FaultAssigned;
  readonly HashSet<string> tested=new(),failed=new();readonly Dictionary<string,Image> pads=new();
  readonly List<GameObject> generated=new();readonly List<Button> bindings=new();
  GameObject panel,functionPicker;RectTransform inset;TMP_Text title,message;TMP_FontAsset font;string lastMessage;int editingBinding;
  public string[] Tested=>tested.ToArray();public int Count=>Game.IsMouse?Game.MouseProduct.TestCount:tested.Count;
  public WorkshopItem LastMeasuredKey{get;private set;}public bool LastSignal{get;private set;}
  public bool HasSignal(string id)=>tested.Contains(id);
  public bool HasFailed(string id)=>failed.Contains(id);
  public void ClearMeasurements(){if(Game.IsMouse)Game.MouseProduct.Invalidate();tested.Clear();failed.Clear();LastMeasuredKey=null;lastMessage=null;Refresh();}
  public bool Passed=>Game.Completed&&Count==Game.TestCount&&string.IsNullOrEmpty(LooseSwitch)&&Game.RepairReady&&Game.MacroReady;
  public void Initialize(Transform root,TMP_FontAsset font){
   this.font=font;
   var card=WorkshopUI.Panel("Keyboard test card",root,new Vector2(.5f,0),new Vector2(0,40),new Vector2(1100,254),WorkshopUI.Paper);panel=card.gameObject;WorkshopAtelierStyle.Paper(card,WorkshopUI.Paper,18);
   title=WorkshopUI.Text("Test progress",card.transform,font,"",25,new Vector2(0,1),new Vector2(30,-29),new Vector2(310,38));
   message=WorkshopUI.Text("Test instructions",card.transform,font,"",20,new Vector2(0,1),new Vector2(30,-87),new Vector2(310,110));message.textWrappingMode=TextWrappingModes.Normal;message.enableAutoSizing=true;message.fontSizeMin=17;message.fontSizeMax=20;
   WorkshopUI.Text("Test section label",card.transform,font,"SON DOKUNUŞ",13,new Vector2(0,1),new Vector2(30,-10),new Vector2(310,20)).color=WorkshopUI.Sage;
   var close=WorkshopUI.Button("Close test",card.transform,font,"×",Vector2.one,new Vector2(-12,-12),new Vector2(34,34),End);WorkshopAtelierStyle.Icon(close,26,"Tuş kontrolünü kapat",Game.Experience);
   var keyboard=WorkshopUI.Panel("Test keyboard inset",card.transform,new Vector2(0,1),new Vector2(372,-28),new Vector2(674,199),new Color(.29f,.39f,.35f));WorkshopAtelierStyle.Paper(keyboard,new Color(.29f,.39f,.35f),12).raycastTarget=false;inset=keyboard.rectTransform;
   RebuildPads();panel.SetActive(false);
  }
  public void RebuildPads(){
   if(!panel)return;
   foreach(var go in generated){go.SetActive(false);if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}generated.Clear();pads.Clear();bindings.Clear();functionPicker=null;
   ((RectTransform)panel.transform).sizeDelta=new Vector2(1100,Game.IsMouse?430:Game.IsMacro?370:254);message.rectTransform.sizeDelta=new Vector2(310,Game.IsMouse?190:Game.IsMacro?210:110);
   if(Game.IsMouse){BuildMouseTest();Refresh();return;}
   var keys=Game.TestKeys;if(keys.Length==0)return;
   float minX=keys.Min(x=>x.Slot.x-x.BoundsSize.x*.5f),maxX=keys.Max(x=>x.Slot.x+x.BoundsSize.x*.5f);
   float maxZ=keys.Max(x=>x.Slot.z),minZ=keys.Min(x=>x.Slot.z),scale=632/(maxX-minX),height=Game.IsMacro||Game.IsRepair?48:28;
   foreach(var key in keys){
    var captured=key;float width=Mathf.Max(25,key.BoundsSize.x*scale-4);var pos=new Vector2(21+(key.Slot.x-key.BoundsSize.x*.5f-minX)*scale,-13-(maxZ-key.Slot.z)*(Game.IsMacro?120:Game.IsRepair?110:148)/Mathf.Max(.1f,maxZ-minZ));
    var button=WorkshopUI.Button("Test "+key.Id,inset,font,key.Label.Replace("Tuş ",""),new Vector2(0,1),pos,new Vector2(width,height),()=>Game.Press(captured));
    var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=Game.IsMacro?24:13;label.enableAutoSizing=true;label.fontSizeMin=9;label.fontSizeMax=Game.IsMacro?24:13;var image=button.GetComponent<Image>();button.targetGraphic=WorkshopAtelierStyle.Paper(image,new Color(.86f,.81f,.68f),5);button.GetComponent<Shadow>().effectDistance=new Vector2(0,-2);pads[key.Id]=image;generated.Add(button.gameObject);
   }
   if(Game.IsMacro)BuildBindings();Refresh();
  }
  void BuildBindings(){
   var root=WorkshopUI.Rect("Macro bindings",panel.transform,new Vector2(0,1),new Vector2(372,-238),new Vector2(674,104));generated.Add(root.gameObject);
   for(int i=0;i<6;i++){int slot=i;var button=WorkshopUI.Button("Macro function "+i,root,font,"",new Vector2(0,1),new Vector2(i%3*225,-(i/3)*51),new Vector2(214,43),()=>OpenFunctionPicker(slot));button.GetComponentInChildren<TMP_Text>().fontSize=18;bindings.Add(button);}
   BuildFunctionPicker();
  }
  void BuildFunctionPicker(){
   var picker=WorkshopUI.Panel("Macro function picker",panel.transform,new Vector2(1,1),new Vector2(-46,-35),new Vector2(690,184),WorkshopUI.Paper);WorkshopAtelierStyle.Paper(picker,WorkshopUI.Paper,14);functionPicker=picker.gameObject;generated.Add(functionPicker);
   WorkshopUI.Text("Choose function",picker.transform,font,Game.IsMouse?"BU DÜĞMEYE HANGİ KISAYOLU VERELİM?":"BU TUŞA HANGİ KISAYOLU VERELİM?",20,new Vector2(0,1),new Vector2(18,-14),new Vector2(625,28)).color=WorkshopUI.Sage;
   for(int i=0;i<6;i++){string value=WorkshopGameMode.FunctionNames[i];WorkshopUI.Button("Choose "+value,picker.transform,font,value,new Vector2(0,1),new Vector2(18+i%3*222,-55-i/3*56),new Vector2(210,46),()=>{if(Game.IsMouse)SetMouseFunction(editingBinding,value);else SetFunction(editingBinding,value);functionPicker.SetActive(false);});}
   var close=WorkshopUI.Button("Close function picker",picker.transform,font,"×",Vector2.one,new Vector2(-8,-8),new Vector2(32,32),()=>functionPicker.SetActive(false));WorkshopAtelierStyle.Icon(close,26,"Kısayol seçimini kapat",Game.Experience);functionPicker.SetActive(false);
  }
  void OpenFunctionPicker(int index){editingBinding=index;functionPicker.SetActive(true);functionPicker.transform.SetAsLastSibling();}
  public bool CloseFunctionPicker(){if(!functionPicker||!functionPicker.activeSelf)return false;functionPicker.SetActive(false);return true;}
  public bool SetFunction(int index,string value){
   if(!Game.IsMacro||index<0||index>=6||!WorkshopGameMode.FunctionNames.Contains(value))return false;
   if(Game.MacroFunctions==null||Game.MacroFunctions.Length!=6)Game.MacroFunctions=new string[6];Game.MacroFunctions[index]=value;
   string key=$"Keycap_{index+1:00}";tested.Remove(key);failed.Remove(key);lastMessage=$"{index+1}. tuş: {value}. Şimdi tuşa basıp deneyelim.";Refresh();Game.Refresh();return true;
  }
  public void Begin(){if(Active||Game.ScreenChangeBlocked||Game.Experience&&Game.Experience.MainVisible||Game.StoryUI&&Game.StoryUI.Open||Game.RepairBench&&Game.RepairBench.DetailOpen)return;if(Game.RepairBench&&Game.RepairBench.AwayFromProduct){Game.RepairBench.Visit(Game.IsRepair);return;}if(!Game.Completed){if(Game.IsRepair)Game.Menu.Toast("Test için switch’i ve tuş kapağını yerine tak.");return;}if(Game.Experience)Game.Experience.EndInspection();if(Game.MouseProduct)Game.MouseProduct.CloseUnderbody();if(Game.Tools)Game.Tools.CloseKeyboardUnderbody();if(Game.Shop)Game.Shop.Close();Game.Menu.ClosePanels();Game.Menu.SelectTool(0);Game.TypingMode=true;Active=true;lastMessage=Game.IsMouse?"Mouse’u hareket ettir. Küçük parçayı hedefe sürükle; iki düğmeyi ve tekerleğin iki yönünü de dene.":Game.IsMacro?"Altı tuşa sırayla geri al, yinele, kaydet, bul, önceki ve sonraki sayfa ata. Sonra tuşlara basarak dene.":Game.IsRepair?"Cihaz bağlı. Klavyedeki üç komşu tuşa ya da sağdaki tuşlara bas. Işıklar her tuşun sinyalini gösterir.":"Klavyendeki tuşlara bas veya sağdaki tuşlara dokun. Yeşil: hazır. Turuncu: switch’i kontrol et.";Refresh();}
  public void End(){Active=false;Game.TypingMode=false;if(panel)panel.SetActive(false);if(functionPicker)functionPicker.SetActive(false);if(Game.RepairBench)Game.RepairBench.RefreshInstrument();}
  public void ResetOrder(){End();tested.Clear();failed.Clear();LastMeasuredKey=null;LooseSwitch=null;FaultAssigned=false;lastMessage=null;}
  public void Installed(WorkshopItem item){if(item.Stage==3)FaultAssigned=true;if(item.Stage==4){tested.Remove(item.Id);failed.Remove(item.Id);if(LastMeasuredKey==item)LastMeasuredKey=null;}}
  public void Removed(WorkshopItem item){
   if(Game.IsMouse){Game.MouseProduct.Removed(item);Refresh();return;}
   foreach(var cap in Game.ProductItems.Where(x=>x.Stage==4&&(x==item||Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(item.Slot.x,item.Slot.z))<.15f))){tested.Remove(cap.Id);failed.Remove(cap.Id);if(LastMeasuredKey==cap)LastMeasuredKey=null;}
   if(item.Id==LooseSwitch)LooseSwitch=null;Refresh();
  }
  public bool Check(WorkshopItem cap){
   if(!Active||cap.Stage!=4||!cap.Fitted||!pads.ContainsKey(cap.Id))return true;
   var sw=Game.ProductItems.FirstOrDefault(x=>x.Stage==3&&Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(cap.Slot.x,cap.Slot.z))<.15f);
   bool hardware=sw&&sw.Fitted&&sw.Id!=LooseSwitch,ok=hardware;Game.DiagnoseRepair(cap,hardware);
   int macroIndex=Game.IsMacro?int.Parse(cap.Id.Substring(cap.Id.LastIndexOf('_')+1))-1:-1;
   if(Game.IsMacro)ok=hardware&&Game.MacroFunctions!=null&&Game.MacroFunctions.Length==6&&Game.MacroFunctions[macroIndex]==WorkshopGameMode.DesiredFunctions[macroIndex];
   LastMeasuredKey=cap;LastSignal=ok;
   if(ok){failed.Remove(cap.Id);tested.Add(cap.Id);lastMessage=Passed?"Hepsi çalışıyor! Sipariş kartından paketlemeye geçebilirsin.":Game.IsMacro?$"{macroIndex+1}. tuş → {Game.MacroFunctions[macroIndex]} · Denendi.":cap.Label+" çalışıyor. Kalan tuşları da deneyelim.";}
   else{failed.Add(cap.Id);lastMessage=Game.IsMouse?"Mouse’u hareket ettir. Küçük parçayı hedefe sürükle; iki düğmeyi ve tekerleğin iki yönünü de dene.":Game.IsMacro?$"{macroIndex+1}. tuşun işlevini '{WorkshopGameMode.DesiredFunctions[macroIndex]}' olarak ayarlayıp yeniden dene.":cap.Label+" yanıt vermedi. Testi kapat; tuşu ve switch'i sökücüyle çıkarıp yeniden tak.";}
   if(Count==Game.TestCount-1&&!string.IsNullOrEmpty(LooseSwitch))lastMessage="Turuncu tuş yanıt vermedi. Testi kapat; kapağını ve switch’ini söküp yeniden tak.";Refresh();Game.Refresh();return ok;
  }
  public void Restore(string[] ids,string loose,bool assigned){tested.Clear();failed.Clear();LastMeasuredKey=null;if(ids!=null)foreach(var id in ids)if(Game.Controller.Lookup.TryGetValue(id,out var item)&&item.Stage==4&&item.Fitted&&Game.TestKeys.Any(x=>x.Id==id))tested.Add(id);LooseSwitch=loose!=null&&Game.Controller.Lookup.TryGetValue(loose,out var sw)&&sw.Stage==3&&sw.Fitted?loose:null;FaultAssigned=assigned;Refresh();}
  void Refresh(){if(Game.RepairBench)Game.RepairBench.RefreshInstrument();if(!panel)return;panel.SetActive(Active);if(Game.IsMouse)RefreshMouseChecks();title.text=Passed?$"TEST TAMAM · {Game.TestCount} / {Game.TestCount}":$"{(Game.IsMouse?"MOUSE KONTROLÜ":Game.IsRepair?"CİHAZ ÖLÇÜMÜ":"TUŞ KONTROLÜ")} · {Count} / {Game.TestCount}";message.text=lastMessage;foreach(var pair in pads){var color=tested.Contains(pair.Key)?new Color(.42f,.61f,.46f):failed.Contains(pair.Key)?new Color(.81f,.43f,.27f):new Color(.86f,.81f,.68f);pair.Value.color=color;pair.Value.GetComponentInChildren<WorkshopPaperGraphic>().color=color;var text=pair.Value.GetComponentInChildren<TMP_Text>();text.color=tested.Contains(pair.Key)||failed.Contains(pair.Key)?WorkshopUI.Paper:WorkshopUI.Ink;}for(int i=0;i<bindings.Count;i++)bindings[i].GetComponentInChildren<TMP_Text>().text=$"{i+1}: "+(Game.MacroFunctions!=null&&i<Game.MacroFunctions.Length&&!string.IsNullOrEmpty(Game.MacroFunctions[i])?Game.MacroFunctions[i]:"Kısayol seç");}
 }
}
