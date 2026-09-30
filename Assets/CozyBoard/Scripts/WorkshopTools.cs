using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CozyBoard {
 public sealed class WorkshopTools:MonoBehaviour {
  public WorkshopGameMode Game;public int Tightened;public string Selected{get;private set;}public bool Active=>!string.IsNullOrEmpty(Selected);public bool Busy{get;private set;}
  Transform movingTool;Vector3 toolHome;Quaternion toolHomeRotation;Vector3 visualHomeScale;
  readonly List<Object> owned=new();readonly List<Transform> screws=new();GameObject bar;TMP_Text hint;
  public void Initialize(WorkshopGameMode game,Transform ui,TMP_FontAsset font){Game=game;BuildScrews();BuildDetails();var p=WorkshopUI.Panel("Selected workshop tool",ui,new Vector2(.5f,0),new Vector2(0,35),new Vector2(1000,105),WorkshopUI.Paper);bar=p.gameObject;hint=WorkshopUI.Text("Tool instructions",p.transform,font,"",26,new Vector2(0,.5f),new Vector2(24,0),new Vector2(720,80));WorkshopUI.Button("Put tool down",p.transform,font,"Bırak · Esc",new Vector2(1,.5f),new Vector2(-18,0),new Vector2(220,58),Deselect);bar.SetActive(false);}
  Material Mat(string name,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
  Transform Detail(string name,Transform parent,Vector3 position,Vector3 size,Material material){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;var mesh=WorkshopPropMesh.RoundedBox(size,Mathf.Min(size.x,size.z)*.4f);owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;}
  void BuildScrews(){var board=Game.Controller.Lookup["Case"].transform;var steel=Mat("Brushed screw steel",new Color(.62f,.65f,.60f));var slot=Mat("Screw recess",new Color(.22f,.28f,.25f));for(int i=0;i<4;i++){var screw=Detail("Case screw "+i,board,new Vector3(i%2==0?-3.13f:3.13f,.308f,i<2?-1.17f:1.17f),new Vector3(.13f,.025f,.13f),steel);Detail("Cross slot",screw,Vector3.up*.016f,new Vector3(.086f,.008f,.018f),slot);Detail("Cross slot",screw,Vector3.up*.016f,new Vector3(.018f,.008f,.086f),slot);screws.Add(screw);}}
  void BuildDetails(){var steel=Mat("Polished tool metal",new Color(.64f,.68f,.66f));var grip=Mat("Soft sage tool grip",new Color(.30f,.43f,.36f));foreach(var id in new[]{"Screwdriver","KeyPuller","Tweezers"}){if(!Game.Controller.Lookup.TryGetValue(id,out var item))continue;var b=item.BoundsSize;var center=item.BoundsCenter;var parent=item.Visual.transform;if(id=="Screwdriver"){for(int i=0;i<4;i++)Detail("Handle grip band",parent,new Vector3(center.x,center.y,center.z-b.z*.36f+i*b.z*.075f),new Vector3(b.x*.92f,.018f,b.z*.018f),grip);Detail("Chrome ferrule",parent,new Vector3(center.x,center.y,center.z+b.z*.06f),new Vector3(.18f,.16f,.12f),steel);}else{Detail("Rounded rubber handle",parent,center+Vector3.up*b.y*.2f,new Vector3(b.x*.38f,Mathf.Max(.07f,b.y*.70f),Mathf.Max(.10f,b.z*.42f)),grip);for(int i=0;i<3;i++)Detail("Grip groove",parent,center+new Vector3((i-1)*b.x*.055f,b.y*.65f,0),new Vector3(b.x*.012f,.015f,b.z*.34f),steel);}}}
  public bool TrySelect(WorkshopItem item){if(Busy||!item||item.Stage!=0||!(item.Id is "Screwdriver" or "KeyPuller" or "Tweezers"))return false;Game.Controller.CancelDrag();Game.Menu.SelectTool(0);Selected=item.Id;bar.SetActive(true);bar.transform.SetAsLastSibling();hint.text=item.Id=="Screwdriver"?"Tornavida · Kasadaki dört köşe vidasına tıkla. Tekrar tıklayarak gevşet.":item.Id=="KeyPuller"?"Tuş sökücü · Çıkarmak istediğin tuşa tıkla.":"Switch sökücü · Önce üstündeki tuşu çıkar, sonra switch'e tıkla.";return true;}
  public void Deselect(){if(Busy)return;Selected=null;if(bar)bar.SetActive(false);}
  public void HandleInput(){if(Busy)return;var k=Keyboard.current;if(k!=null&&k.escapeKey.wasPressedThisFrame){Deselect();return;}var m=Mouse.current;if(m==null||!m.leftButton.wasPressedThisFrame||(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))return;
   var ray=Game.Controller.ViewCamera.ScreenPointToRay(m.position.ReadValue());if(Selected=="Screwdriver"){var board=Game.Controller.Lookup["Case"].transform;var plane=new Plane(board.up,board.TransformPoint(new Vector3(0,.31f,0)));if(plane.Raycast(ray,out float distance)){var hit=ray.GetPoint(distance);int index=Enumerable.Range(0,screws.Count).OrderBy(i=>Vector3.Distance(hit,screws[i].position)).First();if(Vector3.Distance(hit,screws[index].position)<.48f)UseScrew(index);else Game.Menu.Toast("Kasanın dört köşesindeki metal vidaları seç.");}return;}
   if(Physics.Raycast(ray,out var contact,100,1<<8)){var item=contact.collider.GetComponent<WorkshopItem>();if(TrySelect(item))return;TryRemove(item);}
  }
  public bool UseScrew(int index){if(Busy||index<0||index>3)return false;if(Game.Installed<2){Game.Menu.Toast("Önce PCB ve plakayı yerleştir.");return false;}StartCoroutine(TurnScrew(index));return true;}
  Vector3 Contact(WorkshopItem tool){float sign=tool.Id=="Screwdriver"?1:-1;return tool.BoundsCenter+Vector3.forward*(sign*tool.BoundsSize.z*.5f);}
  void BeginTool(WorkshopItem tool){movingTool=tool.transform;toolHome=movingTool.position;toolHomeRotation=movingTool.rotation;visualHomeScale=tool.Visual.transform.localScale;}
  void PoseTool(WorkshopItem tool,Vector3 tip,Quaternion rotation){tool.transform.rotation=rotation;tool.transform.position=tip-tool.transform.TransformVector(Contact(tool));}
  IEnumerator MoveTool(WorkshopItem tool,Vector3 tip,Quaternion rotation,float duration,float arc){var from=tool.transform.position;var fromRotation=tool.transform.rotation;var to=tip-rotation*Vector3.Scale(Contact(tool),tool.transform.lossyScale);for(float t=0;t<duration;t+=Time.unscaledDeltaTime){float f=Mathf.Clamp01(t/duration),ease=Mathf.SmoothStep(0,1,f);tool.transform.SetPositionAndRotation(Vector3.Lerp(from,to,ease)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*arc),Quaternion.Slerp(fromRotation,rotation,ease));yield return null;}PoseTool(tool,tip,rotation);}
  IEnumerator ReturnTool(WorkshopItem tool){var from=tool.transform.position;var rotation=tool.transform.rotation;for(float t=0;t<.36f;t+=Time.unscaledDeltaTime){float f=t/.36f,ease=Mathf.SmoothStep(0,1,f);tool.transform.SetPositionAndRotation(Vector3.Lerp(from,toolHome,ease)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*.3f),Quaternion.Slerp(rotation,toolHomeRotation,ease));yield return null;}RestoreTool();}
  void RestoreTool(){if(movingTool){movingTool.SetPositionAndRotation(toolHome,toolHomeRotation);var item=movingTool.GetComponent<WorkshopItem>();if(item&&item.Visual)item.Visual.transform.localScale=visualHomeScale;}movingTool=null;}
  IEnumerator TurnScrew(int index){
   Busy=true;bool tighten=(Tightened&(1<<index))==0;var screw=screws[index];var from=screw.localRotation;var tool=Game.Controller.Lookup["Screwdriver"];BeginTool(tool);
   var axis=screw.up;var tip=screw.position+axis*.023f;var rotation=Quaternion.FromToRotation(Vector3.forward,-axis);
   yield return MoveTool(tool,tip+axis*.4f,rotation,.32f,.7f);yield return MoveTool(tool,tip,rotation,.14f,0);
   Game.Audio.Play(Game.Audio.Pickup,.2f);
   for(float t=0;t<.72f;t+=Time.unscaledDeltaTime){float f=Mathf.Clamp01(t/.72f);float angle=(tighten?1:-1)*540*Mathf.SmoothStep(0,1,f);screw.localRotation=from*Quaternion.Euler(0,angle,0);PoseTool(tool,tip,Quaternion.AngleAxis(angle,axis)*rotation);yield return null;}
   screw.localRotation=from;Tightened^=1<<index;Game.Audio.Play(Game.Audio.Snap,.3f);
   yield return MoveTool(tool,tip+axis*.4f,tool.transform.rotation,.14f,0);yield return ReturnTool(tool);Busy=false;
   Game.Menu.Toast(Tightened==15?"Dört vida sabitlendi. Kasa hazır!":tighten?"Vida sabitlendi.":"Vida gevşetildi.");
  }
  public bool TryRemove(WorkshopItem item){if(Busy||!item||!item.Fitted)return false;int stage=Selected=="KeyPuller"?4:Selected=="Tweezers"?3:0;if(item.Stage!=stage){Game.Menu.Toast(stage==4?"Bu alet tuş kapaklarını çıkarır.":"Bu alet switch'leri çıkarır.");return false;}if(stage==3&&Game.Controller.Items.Any(x=>x.Stage==4&&x.Fitted&&Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(item.Slot.x,item.Slot.z))<.15f)){Game.Menu.Toast("Önce üzerindeki tuşu tuş sökücüyle çıkar.");return false;}StartCoroutine(Remove(item));return true;}
  IEnumerator Remove(WorkshopItem item){
   Game.StopAnimations();Busy=true;Game.TypingMode=false;var tool=Game.Controller.Lookup[Selected];BeginTool(tool);var visual=item.Visual.transform;var start=visual.localPosition;
   var axis=Game.Controller.Lookup["Case"].transform.up;var tip=item.transform.TransformPoint(item.BoundsCenter+Vector3.up*(item.BoundsSize.y*.25f));var rotation=Quaternion.FromToRotation(Vector3.back,-axis);
   yield return MoveTool(tool,tip+axis*.3f,rotation,.32f,.65f);yield return MoveTool(tool,tip,rotation,.14f,0);
   for(float t=0;t<.12f;t+=Time.unscaledDeltaTime){tool.Visual.transform.localScale=Vector3.Scale(visualHomeScale,new Vector3(Mathf.Lerp(1,.82f,t/.12f),1,1));yield return null;}
   Game.Audio.Play(Game.Audio.Pickup,.4f);
   for(float t=0;t<.42f;t+=Time.unscaledDeltaTime){float lift=.62f*Mathf.SmoothStep(0,1,t/.42f);visual.localPosition=start+Vector3.up*lift;float wiggle=Mathf.Sin(t*32)*2.5f*(1-t/.42f);visual.localRotation=Quaternion.Euler(0,0,wiggle);PoseTool(tool,tip+axis*(lift*item.transform.lossyScale.y),Quaternion.AngleAxis(wiggle,Game.Controller.Lookup["Case"].transform.forward)*rotation);yield return null;}
   visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;item.Fitted=false;item.transform.SetParent(Game.Controller.PartsRoot,true);Game.PresentStock();Game.Refresh();tool.Visual.transform.localScale=visualHomeScale;
   yield return ReturnTool(tool);Busy=false;Game.Menu.Toast("Parça kutuya döndü. Yeniden takabilirsin.");
  }
  public void ResetState(){StopAllCoroutines();RestoreTool();Busy=false;Tightened=0;Deselect();}
  void OnDisable(){StopAllCoroutines();RestoreTool();Busy=false;}
  void OnDestroy(){foreach(var value in owned)Destroy(value);}
 }
}
