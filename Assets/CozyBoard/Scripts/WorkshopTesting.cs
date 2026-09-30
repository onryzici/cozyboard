using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace CozyBoard {
 public sealed class WorkshopTesting:MonoBehaviour {
  public WorkshopGameMode Game;public bool Active{get;private set;}
  public string LooseSwitch;public bool FaultAssigned;
  readonly HashSet<string> tested=new(),failed=new();readonly Dictionary<string,UnityEngine.UI.Image> pads=new();
  GameObject panel;TMP_Text title,message;string lastMessage;
  public string[] Tested=>tested.ToArray();public int Count=>tested.Count;
  public bool Passed=>Game.Completed&&Count==61&&string.IsNullOrEmpty(LooseSwitch);
  public void Initialize(Transform root,TMP_FontAsset font){
   var card=WorkshopUI.Panel("Keyboard test card",root,new Vector2(.5f,0),new Vector2(0,28),new Vector2(1160,205),WorkshopUI.Paper);panel=card.gameObject;
   title=WorkshopUI.Text("Test progress",card.transform,font,"",29,new Vector2(0,1),new Vector2(24,-18),new Vector2(410,45));
   message=WorkshopUI.Text("Test instructions",card.transform,font,"",22,new Vector2(0,1),new Vector2(24,-69),new Vector2(420,95));
   WorkshopUI.Button("Close test",card.transform,font,"×",Vector2.one,new Vector2(-9,-8),new Vector2(38,38),End);
   foreach(var key in Game.Controller.Items.Where(x=>x.Stage==4)){
    var captured=key;float width=Mathf.Max(25,key.BoundsSize.x*94-3);var pos=new Vector2(486+(key.Slot.x+3)*96-width*.5f,-42-(1-key.Slot.z)*62);
    var button=WorkshopUI.Button("Test "+key.Id,card.transform,font,key.Label.Replace("Tuş ",""),new Vector2(0,1),pos,new Vector2(width,26),()=>Game.Press(captured));
    var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=12;label.enableAutoSizing=true;label.fontSizeMin=8;label.fontSizeMax=12;pads[key.Id]=button.GetComponent<UnityEngine.UI.Image>();
   }
   panel.SetActive(false);
  }
  public void Begin(){if(!Game.Completed||(Game.Tools&&Game.Tools.Busy))return;Game.Menu.ClosePanels();Game.Menu.SelectTool(0);Game.TypingMode=true;Active=true;lastMessage="Her tuşa bas. Klavyeyi veya sağdaki test tuşlarını kullanabilirsin.";Refresh();}
  public void End(){Active=false;if(panel)panel.SetActive(false);}
  public void ResetOrder(){End();tested.Clear();failed.Clear();LooseSwitch=null;FaultAssigned=false;lastMessage=null;}
  public void Installed(WorkshopItem item){
   if(item.Stage==3&&!FaultAssigned){var switches=Game.Controller.Items.Where(x=>x.Stage==3).OrderBy(x=>x.Id).ToArray();if(item==switches[(Game.OrderNumber*17)%switches.Length]){LooseSwitch=item.Id;FaultAssigned=true;}}
   if(item.Stage==4)tested.Remove(item.Id);
  }
  public void Removed(WorkshopItem item){
   foreach(var cap in Game.Controller.Items.Where(x=>x.Stage==4&&(x==item||Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(item.Slot.x,item.Slot.z))<.15f))){tested.Remove(cap.Id);failed.Remove(cap.Id);}
   if(item.Id==LooseSwitch)LooseSwitch=null;Refresh();
  }
  public bool Check(WorkshopItem cap){
   if(!Active||cap.Stage!=4||!cap.Fitted)return true;
   var sw=Game.Controller.Items.FirstOrDefault(x=>x.Stage==3&&Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(cap.Slot.x,cap.Slot.z))<.15f);
   bool ok=sw&&sw.Fitted&&sw.Id!=LooseSwitch;
   if(ok){failed.Remove(cap.Id);tested.Add(cap.Id);lastMessage=Passed?"Hepsi çalışıyor! Sipariş kartından paketlemeye geçebilirsin.":cap.Label+" çalışıyor. Kalan tuşları da deneyelim.";}
   else{failed.Add(cap.Id);lastMessage=cap.Label+" yanıt vermedi. Testi kapat; tuşu ve switch'i sökücüyle çıkarıp yeniden tak.";if(pads.TryGetValue(cap.Id,out var pad))pad.color=new Color(.78f,.43f,.24f);}
   if(Count==60&&!string.IsNullOrEmpty(LooseSwitch))lastMessage="Turuncu tuş yanıt vermedi. Testi kapat; kapağını ve switch’ini söküp yeniden tak.";Refresh();Game.Refresh();return ok;
  }
  public void Restore(string[] ids,string loose,bool assigned){tested.Clear();failed.Clear();if(ids!=null)foreach(var id in ids)if(Game.Controller.Lookup.TryGetValue(id,out var item)&&item.Stage==4&&item.Fitted)tested.Add(id);LooseSwitch=loose!=null&&Game.Controller.Lookup.TryGetValue(loose,out var sw)&&sw.Stage==3&&sw.Fitted?loose:null;FaultAssigned=assigned;Refresh();}
  void Refresh(){if(!panel)return;panel.SetActive(Active);title.text=Passed?"TEST TAMAM · 61 / 61":$"TUŞ KONTROLÜ · {Count} / 61";message.text=lastMessage;foreach(var pair in pads)pair.Value.color=tested.Contains(pair.Key)?WorkshopUI.Sage:failed.Contains(pair.Key)?new Color(.78f,.43f,.24f):new Color(.57f,.56f,.48f);}
 }
}
