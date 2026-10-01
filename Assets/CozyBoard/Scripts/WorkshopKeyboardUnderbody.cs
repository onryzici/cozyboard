using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
 public sealed partial class WorkshopTools {
  public bool KeyboardUnderbody{get;private set;}
  bool keyboardPoseCaptured,keyboardTurning;
  Vector3 keyboardRestPosition;Quaternion keyboardRestRotation;
  GameObject keyboardControls;Button keyboardFlip;
  readonly System.Collections.Generic.List<Transform> keyboardSockets=new();
  public Vector3 SavedKeyboardPosition=>keyboardPoseCaptured?keyboardRestPosition:Game.Controller.Lookup["Case"].transform.position;
  public float SavedKeyboardYaw=>keyboardPoseCaptured?keyboardRestRotation.eulerAngles.y:Game.Controller.Lookup["Case"].transform.eulerAngles.y;
  Vector3 KeyboardScrewSlot(int i){var board=Game.Controller.Lookup["Case"];var lower=board.transform.Find("LowerShell");var bounds=lower.GetComponent<MeshFilter>().sharedMesh.bounds;var low=lower.localPosition+Vector3.Scale(bounds.min,lower.localScale);var high=lower.localPosition+Vector3.Scale(bounds.max,lower.localScale);return new Vector3(i%2==0?low.x+.15f:high.x-.15f,low.y+.007f,i<2?low.z+.15f:high.z-.15f);}
  void BuildKeyboardControls(Transform ui,TMP_FontAsset font){
   var panel=WorkshopUI.Panel("Keyboard underside controls",ui,new Vector2(.5f,0),new Vector2(-450,250),new Vector2(400,76),WorkshopUI.Paper);WorkshopAtelierStyle.Paper(panel,WorkshopUI.Paper,14).raycastTarget=false;keyboardControls=panel.gameObject;
   keyboardFlip=WorkshopUI.Button("Turn keyboard underside",panel.transform,font,"Altını çevir · Vidalar",Vector2.one*.5f,Vector2.zero,new Vector2(360,48),()=>ToggleKeyboardUnderbody());keyboardControls.SetActive(false);
  }
  void RefreshKeyboardControls(){
   if(!keyboardControls||!Game.Experience||!Game.RepairBench)return;
   bool visible=!Game.IsMouse&&!Game.WaitingForOrder&&Game.Installed>=2&&!Game.Experience.MainVisible&&!Game.Experience.Packing&&!Game.Experience.Inspecting&&!Game.Testing.Active&&!Game.Painter.Editing&&!Game.Shop.IsOpen&&!Game.Menu.InputBlocked&&!Game.RepairBench.AwayFromProduct&&!Game.RepairBench.DetailOpen;
   keyboardControls.SetActive(visible);keyboardFlip.interactable=!Busy&&!Game.ScreenChangeBlocked;keyboardFlip.GetComponentInChildren<TMP_Text>().text=KeyboardUnderbody?"Üstüne çevir":"Altını çevir · Vidalar";
  }
  public void ToggleKeyboardUnderbody(bool selectDriver=false){
   if(!Game||Game.IsMouse||Game.WaitingForOrder||Game.Installed<2||Game.ScreenChangeBlocked||Game.Menu.InputBlocked||Game.Painter.Editing||Game.Testing.Active||Game.Experience.Inspecting||Game.RepairBench.AwayFromProduct)return;
   Game.Controller.CancelDrag();Game.StopAnimations();Game.Menu.SelectTool(0);StartCoroutine(TurnKeyboard(selectDriver));
  }
  public void PrepareKeyboardFastening(){if(Game.IsMouse||KeyboardUnderbody)TrySelect(Game.Controller.Lookup["Screwdriver"]);else ToggleKeyboardUnderbody(true);}
  float KeyboardLift=>1.35f;
  IEnumerator TurnKeyboard(bool selectDriver){
   Busy=keyboardTurning=true;bool opening=!KeyboardUnderbody;var board=Game.Controller.Lookup["Case"].transform;
   if(opening){keyboardRestPosition=board.position;keyboardRestRotation=board.rotation;keyboardPoseCaptured=true;}
   var from=board.position;var rotation=board.rotation;var target=opening?keyboardRestPosition+Vector3.up*KeyboardLift:keyboardRestPosition;var angle=opening?keyboardRestRotation*Quaternion.Euler(180,0,0):keyboardRestRotation;
   for(float t=0;t<.55f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.55f);board.SetPositionAndRotation(Vector3.Lerp(from,target,f)+Vector3.up*(Mathf.Sin(f*Mathf.PI)*.4f),Quaternion.Slerp(rotation,angle,f));yield return null;}
   board.SetPositionAndRotation(target,angle);KeyboardUnderbody=opening;keyboardPoseCaptured=opening;Busy=keyboardTurning=false;Game.PresentStock();Game.Refresh();if(selectDriver&&opening)TrySelect(Game.Controller.Lookup["Screwdriver"]);
  }
  public void CloseKeyboardUnderbody(bool refresh=true){
   if(!keyboardPoseCaptured)return;if(keyboardTurning){StopAllCoroutines();keyboardTurning=false;Busy=false;}
   Game.Controller.Lookup["Case"].transform.SetPositionAndRotation(keyboardRestPosition,keyboardRestRotation);KeyboardUnderbody=keyboardPoseCaptured=false;if(refresh)Game.PresentStock();
  }
  public void RestoreKeyboardUnderbody(bool underside){
   if(!underside||Game.IsMouse||Game.WaitingForOrder||Game.Installed<2)return;
   var board=Game.Controller.Lookup["Case"].transform;keyboardRestPosition=board.position;keyboardRestRotation=board.rotation;keyboardPoseCaptured=KeyboardUnderbody=true;board.SetPositionAndRotation(keyboardRestPosition+Vector3.up*KeyboardLift,keyboardRestRotation*Quaternion.Euler(180,0,0));
  }
 }
}
