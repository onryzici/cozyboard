using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CozyBoard {
 public sealed partial class WorkshopTools {
  WorkshopItem heldTool;Vector3 deskToolPosition;Quaternion deskToolRotation;bool deskHitboxEnabled;
  Transform gripHand,pinchHand,screwDish,looseScrew,driverSleeve,pullerSleeve;Collider dishHit;Material glove,cuff,looseSteel,looseRecess;
  readonly List<Transform> dishScrews=new();
  public int Mounted{get;private set;}
  public bool HoldingScrew{get;private set;}
  public bool HandVisible=>gripHand&&gripHand.gameObject.activeInHierarchy;
  public int LooseScrewCount=>4-CountBits(Mounted)-(HoldingScrew?1:0);
  static int CountBits(int v){int n=0;for(int i=0;i<4;i++)if((v&(1<<i))!=0)n++;return n;}
  Transform MeshProp(string name,Transform parent,Mesh mesh,Material mat){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go.transform;}
  void BuildHandAndDish(){
   if(screwDish)return;
   glove=Mat("Warm skin workshop hands",new Color(.92f,.70f,.55f));cuff=Mat("Sage workshop sleeve",new Color(.39f,.51f,.43f));
   gripHand=MeshProp("Soft hand gripping selected tool",Game.Controller.transform,Resources.Load<Mesh>("WorkshopHands/GRIP"),glove);gripHand.gameObject.SetActive(false);
   pullerSleeve=Detail("Puller wrist sleeve",gripHand,new Vector3(.87f,.018f,-.075f),new Vector3(.14f,.32f,.43f),cuff);
   driverSleeve=Detail("Driver wrist sleeve",gripHand,new Vector3(.03f,-.02f,-.84f),new Vector3(.43f,.32f,.14f),cuff);
   pinchHand=MeshProp("Soft hand placing a screw",Game.Controller.transform,Resources.Load<Mesh>("WorkshopHands/PINCH"),glove);pinchHand.localScale=new Vector3(-.62f,.62f,.62f);pinchHand.gameObject.SetActive(false);
   Detail("Rolled sage sleeve",pinchHand,new Vector3(.87f,.018f,-.075f),new Vector3(.14f,.32f,.43f),cuff);
   var dishMesh=WorkshopRepairBench.PartsDishMesh();var rimMesh=WorkshopRepairBench.PartsDishRim();owned.Add(dishMesh);owned.Add(rimMesh);
   screwDish=MeshProp("Stoneware screw bowl",Game.Controller.transform,dishMesh,Mat("Warm stoneware glaze / screw bowl",new Color(.86f,.80f,.65f)));
   MeshProp("Hand glazed sage rim",screwDish,rimMesh,Mat("Sage enamel / screw bowl rim",new Color(.43f,.57f,.48f)));
   screwDish.gameObject.layer=8;var hit=screwDish.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.12f,0);hit.size=new Vector3(1.48f,.25f,1.48f);dishHit=hit;
   looseSteel=Mat("Loose screw brushed steel",new Color(.62f,.65f,.60f));looseRecess=Mat("Loose screw cross recess",new Color(.22f,.28f,.25f));
   for(int i=0;i<4;i++){var s=LooseScrew("Loose bowl screw "+i,screwDish);s.localPosition=new Vector3(i%2==0?-.25f:.23f,.19f,i<2?-.15f:.15f);s.localRotation=Quaternion.Euler(0,i*71,75);dishScrews.Add(s);}
   looseScrew=LooseScrew("Screw held between glove fingers",Game.Controller.transform);looseScrew.gameObject.SetActive(false);
   RefreshScrewDish();
  }
  Transform LooseScrew(string name,Transform parent){var s=Detail(name,parent,Vector3.zero,new Vector3(.17f,.030f,.17f),looseSteel);Detail("Screw shaft",s,new Vector3(0,-.08f,0),new Vector3(.055f,.15f,.055f),looseSteel);for(int i=0;i<4;i++)Detail("Thread collar",s,new Vector3(0,-.04f-i*.029f,0),new Vector3(.071f,.011f,.071f),looseSteel);Detail("Cross recess",s,Vector3.up*.017f,new Vector3(.090f,.007f,.019f),looseRecess);Detail("Cross recess",s,Vector3.up*.017f,new Vector3(.019f,.007f,.090f),looseRecess);return s;}
  void HoldTool(WorkshopItem tool){
   BuildHandAndDish();heldTool=tool;deskToolPosition=tool.transform.position;deskToolRotation=tool.transform.rotation;deskHitboxEnabled=tool.Hitbox.enabled;tool.Hitbox.enabled=false;
   bool driver=tool.Id=="Screwdriver";gripHand.GetComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>(driver?"WorkshopHands/DRIVER-GRIP":"WorkshopHands/GRIP");driverSleeve.gameObject.SetActive(driver);pullerSleeve.gameObject.SetActive(!driver);
   gripHand.SetParent(tool.transform,false);gripHand.localPosition=tool.BoundsCenter+Vector3.forward*(tool.BoundsSize.z*(driver?-.33f:.22f));gripHand.localRotation=driver?Quaternion.identity:Quaternion.Euler(0,180,0);gripHand.localScale=Vector3.one*(driver?1:.85f);gripHand.gameObject.SetActive(true);
   ReadyToolPose(tool,out var p,out var r);tool.transform.SetPositionAndRotation(p,r);
  }
  void PutHeldToolDown(){if(gripHand){gripHand.gameObject.SetActive(false);gripHand.SetParent(Game.Controller.transform,false);}if(heldTool){heldTool.transform.SetPositionAndRotation(deskToolPosition,deskToolRotation);heldTool.Hitbox.enabled=deskHitboxEnabled;heldTool=null;}}
  void ReadyToolPose(WorkshopItem tool,out Vector3 p,out Quaternion r){
   var mouse=Mouse.current;var point=mouse!=null?mouse.position.ReadValue():new Vector2(Screen.width*.65f,Screen.height*.52f);
   if(point.x<=0||point.y<=0||point.x>Screen.width||point.y>Screen.height||EventSystem.current&&EventSystem.current.IsPointerOverGameObject())point=new Vector2(Screen.width*.72f,Screen.height*.50f);
   float height=Game.Controller.Lookup["Case"].transform.position.y+.9f;
   r=Quaternion.Euler(32,tool.Id=="Screwdriver"?-35:145,0);var tip=Game.Controller.MousePlane(point,height)+new Vector3(.22f,0,-.08f);p=tip-r*Vector3.Scale(Contact(tool),tool.transform.lossyScale);
  }
  void LateUpdate(){
   if(!Game)return;RefreshScrewDish();RefreshKeyboardControls();
   if(heldTool&&!Busy){heldTool.Hitbox.enabled=false;ReadyToolPose(heldTool,out var p,out var r);float ease=1-Mathf.Exp(-Time.unscaledDeltaTime*24);heldTool.transform.SetPositionAndRotation(Vector3.Lerp(heldTool.transform.position,p,ease),Quaternion.Slerp(heldTool.transform.rotation,r,ease));}
   if(HoldingScrew&&!Busy&&heldTool){pinchHand.position=heldTool.transform.TransformPoint(Contact(heldTool))+new Vector3(-.65f,.10f,-.20f);pinchHand.rotation=Quaternion.Euler(0,20,0);looseScrew.SetPositionAndRotation(pinchHand.TransformPoint(new Vector3(0,.052f,.055f)),Quaternion.identity);}
  }
  void RefreshScrewDish(){
   if(!screwDish)return;bool visible=Game&&!Game.WaitingForOrder;
   screwDish.gameObject.SetActive(visible);screwDish.localPosition=(Game.IsRepair?WorkshopRepairBench.Origin:Vector3.zero)+new Vector3(5.0f,0,-3.8f);
   for(int i=0;i<dishScrews.Count;i++)dishScrews[i].gameObject.SetActive(i<LooseScrewCount);
  }
  public bool TryPickScrewDish(Ray ray){if(!dishHit||!dishHit.gameObject.activeInHierarchy||!dishHit.Raycast(ray,out _,100))return false;TakeScrew();return true;}
  public bool TakeScrew(){
   if(Busy||!Game||Game.ScreenChangeBlocked||Game.WaitingForOrder||HoldingScrew||LooseScrewCount<=0)return false;
   if(Game.Installed<2){Game.Menu.Toast("Önce iç parçaları yerleştir; vidalar kapta bekliyor.");return false;}
   if(Game.IsMouse? !Game.MouseProduct.State.underbody||!Game.MouseProduct.HousingClosed:!KeyboardUnderbody){Game.Menu.Toast("Vidalar alt yüzeye takılır · Önce ürünün altını çevir.");return false;}
   if(Selected!="Screwdriver"&&!TrySelect(Game.Controller.Lookup["Screwdriver"]))return false;
   HoldingScrew=true;pinchHand.gameObject.SetActive(true);looseScrew.gameObject.SetActive(true);RefreshScrewDish();Game.Audio.Play(Game.Audio.Pickup,.35f);Game.Menu.Toast("Vida elinde · Boş yuvaya tıklayarak yerleştir ve sabitle.");return true;
  }
  void ReturnLooseScrew(){HoldingScrew=false;if(pinchHand)pinchHand.gameObject.SetActive(false);if(looseScrew)looseScrew.gameObject.SetActive(false);RefreshScrewDish();}
  IEnumerator SeatScrew(int index,Vector3 tip,Vector3 axis){
   Vector3 start=pinchHand.position;Quaternion rotation=Quaternion.FromToRotation(Vector3.up,axis);
   for(float t=0;t<.30f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.30f);var pos=Vector3.Lerp(start,tip+axis*.006f,f)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*.25f);pinchHand.SetPositionAndRotation(pos,rotation);looseScrew.SetPositionAndRotation(pos,rotation);yield return null;}
   Mounted|=1<<index;ReturnLooseScrew();RefreshProductVisibility();Game.Audio.AssemblyContact();
  }
  public void RestoreScrews(int mounted,int fastened){Mounted=mounted&15;Tightened=fastened;ReturnLooseScrew();RefreshProductVisibility();}
 }
}
