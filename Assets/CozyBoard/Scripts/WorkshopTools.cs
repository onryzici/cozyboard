using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CozyBoard {
 public sealed partial class WorkshopTools:MonoBehaviour {
  public WorkshopGameMode Game;int tightened;public int Tightened{get=>tightened;set{tightened=value&15;Mounted|=tightened;}}public string Selected{get;private set;}public bool Active=>!string.IsNullOrEmpty(Selected);public bool Busy{get;private set;}
  Transform movingTool;Vector3 toolHome;Quaternion toolHomeRotation;Vector3 visualHomeScale;
  readonly List<Object> owned=new();readonly List<Transform> screws=new();GameObject bar;TMP_Text hint;
  public void Initialize(WorkshopGameMode game,Transform ui,TMP_FontAsset font){Game=game;LayoutTools();BuildScrews();UpdateProduct();BuildDetails();BuildHandAndDish();BuildKeyboardControls(ui,font);var p=WorkshopUI.Panel("Selected workshop tool",ui,new Vector2(.5f,1),new Vector2(0,-115),new Vector2(1000,70),WorkshopUI.Paper);WorkshopAtelierStyle.Paper(p,WorkshopUI.Paper,14);bar=p.gameObject;hint=WorkshopUI.Text("Tool instructions",p.transform,font,"",21,new Vector2(0,.5f),new Vector2(24,0),new Vector2(720,56));WorkshopUI.Button("Put tool down",p.transform,font,"Bırak · Esc",new Vector2(1,.5f),new Vector2(-18,0),new Vector2(220,48),Deselect);hint.alignment=TextAlignmentOptions.MidlineLeft;bar.SetActive(false);}
  public void UpdateProduct(){if(!Game)return;for(int i=0;i<screws.Count;i++){
   screws[i].localRotation=Quaternion.Euler(180,0,0);
   screws[i].localPosition=Game.IsMouse?WorkshopMouseProduct.ScrewSlot(i):KeyboardScrewSlot(i);
   if(i<keyboardSockets.Count){keyboardSockets[i].localPosition=KeyboardScrewSlot(i)+Vector3.up*.010f;keyboardSockets[i].localRotation=Quaternion.Euler(180,0,0);}
  }}
  public void RefreshProductVisibility(){for(int i=0;i<screws.Count;i++)if(screws[i])screws[i].gameObject.SetActive(!Game.WaitingForOrder&&(Mounted&(1<<i))!=0);foreach(var socket in keyboardSockets)socket.gameObject.SetActive(!Game.IsMouse&&!Game.WaitingForOrder);RefreshScrewDish();RefreshKeyboardControls();}
  void LayoutTools(){
   string[] ids={"Screwdriver","KeyPuller","Tweezers"};Vector3[] positions={new(7.0f,.065f,-2.9f),new(6.8f,.065f,-4.05f),new(8.55f,.065f,-3.85f)};float[] angles={90,55,-15};
   for(int i=0;i<ids.Length;i++){var item=Game.Controller.Lookup[ids[i]];item.InitialPosition=positions[i];item.InitialYaw=angles[i];item.transform.localPosition=positions[i];item.transform.localRotation=Quaternion.Euler(0,angles[i],0);}
   var fabric=new Material(Shader.Find("CozyBoard/WorkMat")){name="Printed precision tool mat"};fabric.SetColor("_BaseColor",new Color(.37f,.51f,.48f));fabric.SetVector("_Size",new Vector4(3.5f,2.7f,0,0));owned.Add(fabric);
   var rest=new GameObject("Linen tool rest");rest.transform.SetParent(Game.Controller.transform,false);rest.transform.localPosition=new Vector3(7.4f,-.01f,-3.7f);var mesh=WorkshopPropMesh.RoundedBox(new Vector3(3.5f,.025f,2.7f),.12f);owned.Add(mesh);rest.AddComponent<MeshFilter>().sharedMesh=mesh;rest.AddComponent<MeshRenderer>().sharedMaterial=fabric;
  }
  Material Mat(string name,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
  Transform Detail(string name,Transform parent,Vector3 position,Vector3 size,Material material){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;var mesh=WorkshopPropMesh.RoundedBox(size,Mathf.Min(size.x,size.z)*.4f);owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;}
  void BuildScrews(){var board=Game.Controller.Lookup["Case"].transform;var steel=Mat("Brushed screw steel",new Color(.62f,.65f,.60f));var slot=Mat("Screw recess",new Color(.22f,.28f,.25f));for(int i=0;i<4;i++){var screw=Detail("Case screw "+i,board,Vector3.zero,new Vector3(.13f,.025f,.13f),steel);Detail("Cross slot",screw,Vector3.up*.016f,new Vector3(.086f,.008f,.018f),slot);Detail("Cross slot",screw,Vector3.up*.016f,new Vector3(.018f,.008f,.086f),slot);screws.Add(screw);var socketMesh=WorkshopRepairBench.Turn(new Vector2(0,0),new Vector2(.095f,0),new Vector2(.095f,.020f),new Vector2(0,.020f));owned.Add(socketMesh);keyboardSockets.Add(MeshProp("Keyboard underside screw recess "+i,board,socketMesh,slot));}}
  void BuildDetails(){var steel=Mat("Polished tool metal",new Color(.64f,.68f,.66f));var grip=Mat("Soft sage tool grip",new Color(.30f,.43f,.36f));foreach(var id in new[]{"Screwdriver","KeyPuller","Tweezers"}){if(!Game.Controller.Lookup.TryGetValue(id,out var item))continue;var b=item.BoundsSize;var center=item.BoundsCenter;var parent=item.Visual.transform;if(id=="Screwdriver"){for(int i=0;i<4;i++)Detail("Handle grip band",parent,new Vector3(center.x,center.y,center.z-b.z*.36f+i*b.z*.075f),new Vector3(b.x*.92f,.018f,b.z*.018f),grip);Detail("Chrome ferrule",parent,new Vector3(center.x,center.y,center.z+b.z*.06f),new Vector3(.18f,.16f,.12f),steel);}else{Detail("Rounded rubber handle",parent,center+Vector3.up*b.y*.2f,new Vector3(b.x*.38f,Mathf.Max(.07f,b.y*.70f),Mathf.Max(.10f,b.z*.42f)),grip);for(int i=0;i<3;i++)Detail("Grip groove",parent,center+new Vector3((i-1)*b.x*.055f,b.y*.65f,0),new Vector3(b.x*.012f,.015f,b.z*.34f),steel);}}}
  public bool TrySelect(WorkshopItem item){if(Busy||Game.ScreenChangeBlocked||Game.WaitingForOrder||!item||item.Stage!=0||!(item.Id is "Screwdriver" or "KeyPuller" or "Tweezers"))return false;if(Game.Testing)Game.Testing.End();Game.Controller.CancelDrag();Game.Menu.SelectTool(0);Selected=item.Id;HoldTool(item);bar.SetActive(true);bar.transform.SetAsLastSibling();hint.text=item.Id=="Screwdriver"?"Tornavida · Kaptan bir vida al, boş yuvaya tıkla ve sabitle.":item.Id=="KeyPuller"?"Tuş sökücü · Çıkarmak istediğin tuşa tıkla.":"Switch sökücü · Önce üstündeki tuşu çıkar, sonra switch'e tıkla.";return true;}
  public void Deselect(){if(Busy)return;PutHeldToolDown();ReturnLooseScrew();Selected=null;if(bar)bar.SetActive(false);}
  public void HandleInput(){if(Busy||Game.Menu.EscapeHandledThisFrame)return;var m=Mouse.current;if(m==null||!m.leftButton.wasPressedThisFrame||(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))return;
   var ray=Game.Controller.ViewCamera.ScreenPointToRay(m.position.ReadValue());if(TryPickScrewDish(ray))return;if(Selected=="Screwdriver"){var plane=new Plane(screws[0].up,screws[0].position);if(plane.Raycast(ray,out float distance)){var hit=ray.GetPoint(distance);int index=Enumerable.Range(0,screws.Count).OrderBy(i=>Vector3.Distance(hit,screws[i].position)).First();if(Vector3.Distance(hit,screws[index].position)<.48f)UseScrew(index);else Game.Menu.Toast("Kaptan bir vida al, kasadaki boş yuvaya tıkla.");}return;}
   if(Game.IsMouse&&Selected=="Tweezers"&&Game.MouseProduct.State.underbody){var sensor=Game.Controller.Lookup[Game.Controller.Lookup["Mouse_Lens"].Fitted?"Mouse_Lens":"Mouse_Sensor"];if(sensor.Fitted&&sensor.Hitbox.Raycast(ray,out _,100)){TryRemove(sensor);return;}}
   if(Physics.Raycast(ray,out var contact,100,1<<8)){var item=contact.collider.GetComponent<WorkshopItem>();if(TrySelect(item))return;TryRemove(item);}
  }
  public bool UseScrew(int index){if(Busy||Game.ScreenChangeBlocked||Game.WaitingForOrder||index<0||index>3)return false;if(!Game.IsMouse&&!KeyboardUnderbody){Game.Menu.Toast("Vidalar alt yüzde · Önce klavyenin altını çevir.");return false;}if(Game.IsMouse&&(!Game.MouseProduct.HousingClosed||!Game.MouseProduct.State.underbody||Game.Controller.Lookup["Mouse_Skate_"+index].Fitted)){Game.Menu.Toast("Mouse’un altını çevir. Vidalar ayaklardan önce sabitlenir.");return false;}if(Game.Installed<2){Game.Menu.Toast("Önce PCB ve plakayı yerleştir.");return false;}if((Mounted&(1<<index))==0&&!HoldingScrew){Game.Menu.Toast("Önce vida kabından bir vida al.");return false;}if(Selected!="Screwdriver"&&!TrySelect(Game.Controller.Lookup["Screwdriver"]))return false;StartCoroutine(TurnScrew(index));return true;}
  Vector3 Contact(WorkshopItem tool){float sign=tool.Id=="Screwdriver"?1:-1;return tool.BoundsCenter+Vector3.forward*(sign*tool.BoundsSize.z*.5f);}
  void BeginTool(WorkshopItem tool){movingTool=tool.transform;toolHome=movingTool.position;toolHomeRotation=movingTool.rotation;visualHomeScale=tool.Visual.transform.localScale;}
  void PoseTool(WorkshopItem tool,Vector3 tip,Quaternion rotation){tool.transform.rotation=rotation;tool.transform.position=tip-tool.transform.TransformVector(Contact(tool));}
  IEnumerator MoveTool(WorkshopItem tool,Vector3 tip,Quaternion rotation,float duration,float arc){var from=tool.transform.position;var fromRotation=tool.transform.rotation;var to=tip-rotation*Vector3.Scale(Contact(tool),tool.transform.lossyScale);for(float t=0;t<duration;t+=Time.unscaledDeltaTime){float f=Mathf.Clamp01(t/duration),ease=Mathf.SmoothStep(0,1,f);tool.transform.SetPositionAndRotation(Vector3.Lerp(from,to,ease)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*arc),Quaternion.Slerp(fromRotation,rotation,ease));yield return null;}PoseTool(tool,tip,rotation);}
  IEnumerator ReturnTool(WorkshopItem tool){if(tool==heldTool)ReadyToolPose(tool,out toolHome,out toolHomeRotation);var from=tool.transform.position;var rotation=tool.transform.rotation;for(float t=0;t<.36f;t+=Time.unscaledDeltaTime){float f=t/.36f,ease=Mathf.SmoothStep(0,1,f);tool.transform.SetPositionAndRotation(Vector3.Lerp(from,toolHome,ease)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*.3f),Quaternion.Slerp(rotation,toolHomeRotation,ease));yield return null;}RestoreTool();}
  void RestoreTool(){if(movingTool){movingTool.SetPositionAndRotation(toolHome,toolHomeRotation);var item=movingTool.GetComponent<WorkshopItem>();if(item&&item.Visual)item.Visual.transform.localScale=visualHomeScale;}movingTool=null;}
  IEnumerator TurnScrew(int index){
   Busy=true;bool tighten=(Tightened&(1<<index))==0;var screw=screws[index];var from=screw.localRotation;var tool=Game.Controller.Lookup["Screwdriver"];BeginTool(tool);
   var axis=screw.up;var tip=screw.position+axis*.023f;var rotation=Quaternion.FromToRotation(Vector3.forward,-axis);
   if((Mounted&(1<<index))==0)yield return SeatScrew(index,tip,axis);
   yield return MoveTool(tool,tip+axis*.4f,rotation,.32f,.7f);yield return MoveTool(tool,tip,rotation,.14f,0);
   Game.Audio.AssemblyContact();
   // Three deliberate wrist turns; each slows as the screw meets resistance.
   float direction=tighten?1:-1;
   for(int turn=0;turn<3;turn++){
    Game.Audio.ScrewTurn();
    for(float t=0;t<.24f;t+=Time.unscaledDeltaTime){
     float f=Mathf.Clamp01(t/.24f);float angle=direction*(turn*150+150*Mathf.SmoothStep(0,1,f));
     screw.localRotation=from*Quaternion.Euler(0,angle,0);
     float wrist=direction*(110*Mathf.SmoothStep(0,1,f)-55);PoseTool(tool,tip,Quaternion.AngleAxis(wrist,axis)*rotation);yield return null;
    }
    if(turn<2)for(float t=0;t<.10f;t+=Time.unscaledDeltaTime){float f=t/.10f;PoseTool(tool,tip+axis*(Mathf.Sin(f*Mathf.PI)*.012f),Quaternion.AngleAxis(direction*Mathf.Lerp(55,-55,Mathf.SmoothStep(0,1,f)),axis)*rotation);yield return null;}
   }
   Tightened^=1<<index;Game.Audio.ScrewStop(tighten);
   // A short torque kick lives in the tool, not in a camera shake.
   for(float t=0;t<.14f;t+=Time.unscaledDeltaTime){
    float kick=Mathf.Sin(t*95)*Mathf.Exp(-t*28)*(tighten?3.5f:1.2f);
    PoseTool(tool,tip+axis*(Mathf.Abs(kick)*.002f),Quaternion.AngleAxis(direction*55+kick,axis)*rotation);yield return null;
   }
   screw.localRotation=from;

   yield return MoveTool(tool,tip+axis*.4f,tool.transform.rotation,.14f,0);yield return ReturnTool(tool);Busy=false;RefreshProductVisibility();Game.TrySaveQuiet();
   Game.Menu.Toast(Tightened==15?"Dört vida sabitlendi. Kasa hazır!":tighten?"Vida sabitlendi.":"Vida gevşetildi.");
  }
  public bool TryRemove(WorkshopItem item){if(Busy||!item||!item.Fitted||!Game.CanRemoveForRepair(item))return false;int stage=Game.IsMouse&&(item.Kind=="mouse-sensor"||item.Id=="Mouse_Lens")&&Selected=="Tweezers"?2:Selected=="KeyPuller"?4:Selected=="Tweezers"?3:0;if(item.Stage!=stage){Game.Menu.Toast(stage==4?"Bu alet tuş kapaklarını çıkarır.":"Bu alet switch'leri çıkarır.");return false;}if(stage==3&&Game.Controller.Items.Any(x=>x.Stage==4&&x.Fitted&&Vector2.Distance(new Vector2(x.Slot.x,x.Slot.z),new Vector2(item.Slot.x,item.Slot.z))<.15f)){Game.Menu.Toast("Önce üzerindeki tuşu tuş sökücüyle çıkar.");return false;}StartCoroutine(Remove(item));return true;}
  IEnumerator Remove(WorkshopItem item){
   Game.StopAnimations();Busy=true;Game.TypingMode=false;var tool=Game.Controller.Lookup[Selected];BeginTool(tool);var visual=item.Visual.transform;var start=visual.localPosition;
   float outward=Game.IsMouse&&Game.MouseProduct.State.underbody?-1:1;var axis=Game.Controller.Lookup["Case"].transform.up*outward;var tip=item.transform.TransformPoint(item.BoundsCenter+Vector3.up*(item.BoundsSize.y*.25f));var rotation=Quaternion.AngleAxis(180,axis)*Quaternion.FromToRotation(Vector3.back,-axis);
   yield return MoveTool(tool,tip+axis*.3f,rotation,.32f,.65f);yield return MoveTool(tool,tip,rotation,.14f,0);
   for(float t=0;t<.12f;t+=Time.unscaledDeltaTime){tool.Visual.transform.localScale=Vector3.Scale(visualHomeScale,new Vector3(Mathf.Lerp(1,.82f,t/.12f),1,1));yield return null;}
   Game.Audio.Play(Game.Audio.Pickup,.4f);
   for(float t=0;t<.42f;t+=Time.unscaledDeltaTime){float lift=.62f*Mathf.SmoothStep(0,1,t/.42f);visual.localPosition=start+Vector3.up*(lift*outward);float wiggle=Mathf.Sin(t*32)*2.5f*(1-t/.42f);visual.localRotation=Quaternion.Euler(0,0,wiggle);PoseTool(tool,tip+axis*(lift*item.transform.lossyScale.y),Quaternion.AngleAxis(wiggle,Game.Controller.Lookup["Case"].transform.forward)*rotation);yield return null;}
   visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;if(Game.Testing)Game.Testing.Removed(item);item.Fitted=false;item.transform.SetParent(Game.Controller.PartsRoot,true);Game.PresentStock();Game.Refresh();tool.Visual.transform.localScale=visualHomeScale;
   yield return ReturnTool(tool);Busy=false;Game.Menu.Toast(Game.IsRepair&&Game.Repair!=null&&item.Id==Game.Repair.switchId?"Arızalı switch alt kapta. Önce kontaklarını incele; sonra yeni parçanın paketini aç.":Game.IsMouse?"Optik parça tepside. Sensörü, ardından merceği yeniden tak ve tekrar test et.":Game.IsRepair?"Tuş kapağı tepside saklanıyor. Onarımdan sonra yerine tak.":"Parça kutuya döndü. Yeniden takabilirsin.");
  }
  public void ResetState(){StopAllCoroutines();RestoreTool();CloseKeyboardUnderbody(false);if(Game&&Game.Audio)Game.Audio.StopAssembly();foreach(var screw in screws)if(screw)screw.localRotation=Quaternion.Euler(180,0,0);Busy=false;Tightened=0;Mounted=0;Deselect();}
  void OnDisable(){StopAllCoroutines();RestoreTool();CloseKeyboardUnderbody(false);if(Game&&Game.Audio)Game.Audio.StopAssembly();foreach(var screw in screws)if(screw)screw.localRotation=Quaternion.Euler(180,0,0);Busy=false;Deselect();}
  void OnDestroy(){foreach(var value in owned)Destroy(value);}
 }
}
