using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
namespace CozyBoard {
 public sealed partial class WorkshopKeyPainter {
  bool tapeMode,tapeDragging,peeling;
  WorkshopTapeStrip tapePreview;
  UnityEngine.UI.Button tapeButton,peelButton;
  TMP_Text tapeHint;
  public bool TapeMode=>tapeMode;
  static bool Masked(CanvasState canvas,Vector3 point){foreach(var strip in canvas.Tape)if(strip.Covers(point))return true;return false;}
  void BuildTapeUI(){
   if(tapeButton)return;
   var font=EditorTitle.font;
   tapeButton=WorkshopUI.Button("Masking tape",PalettePanel.transform,font,"Bant çek",new Vector2(.5f,1),new Vector2(-74,-525),new Vector2(136,38),()=>SetTapeMode(!tapeMode));
   peelButton=WorkshopUI.Button("Peel masking tape",PalettePanel.transform,font,"Bandı sök",new Vector2(.5f,1),new Vector2(74,-525),new Vector2(136,38),PeelTape);
   tapeHint=WorkshopUI.Text("Tape instructions",PalettePanel.transform,font,"",16,new Vector2(.5f,0),new Vector2(0,18),new Vector2(284,48));tapeHint.alignment=TextAlignmentOptions.Center;tapeHint.color=WorkshopUI.Paper;
   var previous=PalettePanel.transform.Find("PaintHint");if(previous)previous.gameObject.SetActive(false);
  }
  public void SetTapeMode(bool value){EndStroke();tapeDragging=false;tapeMode=value;SyncTape();RefreshTapeUI();}
  void RefreshTapeUI(){if(!tapeButton)return;tapeButton.GetComponentInChildren<TMP_Text>().text=tapeMode?"Fırçaya dön":"Bant çek";tapeButton.GetComponent<UnityEngine.UI.Image>().color=tapeMode?new Color(.72f,.48f,.30f):WorkshopUI.Sage;peelButton.interactable=editing&&Canvas(editing).Tape.Count>0;tapeHint.text=tapeMode?"Tuşun üstünde sürükle.\nTekerlek: bant genişliği.":"Islak renkler hafifçe karışır.\nBantla boya, sök ve çizgiyi gör.";}
  bool HandleTape(Mouse mouse,bool inside,Vector2 viewport){
   if(!tapeMode)return false;
   if(mouse.rightButton.isPressed||mouse.middleButton.isPressed){tapeDragging=false;SyncTape();return false;}
   bool hit=inside&&studio.Hit(viewport,out _);
   if(hit){studio.Hit(viewport,out var contact);var canvas=Canvas(editing);float width=radius*canvas.Surface.Bounds.size.z*3;
    studio.Cursor(contact,width*.5f,new Color(.92f,.83f,.55f));Cursor.visible=false;
    if(mouse.leftButton.wasPressedThisFrame){tapeDragging=true;tapePreview=new WorkshopTapeStrip{Start=studio.Local(contact.point),End=studio.Local(contact.point),Normal=contact.normal,Width=width};}
    if(tapeDragging&&mouse.leftButton.isPressed){tapePreview.End=studio.Local(contact.point);SyncTape(true);}
   }
   if(mouse.leftButton.wasReleasedThisFrame&&tapeDragging){tapeDragging=false;if(hit)AddTape(editing,tapePreview);SyncTape();RefreshTapeUI();}
   return true;
  }
  public bool AddTape(WorkshopItem item,WorkshopTapeStrip strip){
   if(!item||!item.Fitted||item.Kind!="keycap")return false;var canvas=Canvas(item);
   if(canvas.Tape.Count>=8){Game.Menu.Toast("Önce bantları sök; aynı tuşa en fazla 8 şerit çekebilirsin.");return false;}
   if(strip.Normal.sqrMagnitude<.1f||strip.Width<=0||Vector3.ProjectOnPlane(strip.End-strip.Start,strip.Normal).magnitude<.02f)return false;
   strip.Normal.Normalize();BeginStroke();Touch(canvas);canvas.Tape.Add(strip);EndStroke();if(editing==item){SyncTape();RefreshTapeUI();}return true;
  }
  public int TapeCount(WorkshopItem item)=>Canvas(item).Tape.Count;
  public void PeelTape(){if(!editing||peeling)return;StartCoroutine(PeelReveal(editing));}
  System.Collections.IEnumerator PeelReveal(WorkshopItem item){
   peeling=true;EndStroke();var strips=Canvas(item).Tape.ToArray();
   for(float t=0;t<.45f&&editing==item;t+=Time.unscaledDeltaTime){var view=new WorkshopTapeStrip[strips.Length];float f=Mathf.SmoothStep(0,1,t/.45f);for(int i=0;i<view.Length;i++){view[i]=strips[i];view[i].End=Vector3.Lerp(strips[i].End,strips[i].Start,f);}studio?.SetTape(view);yield return null;}
   if(editing==item)PeelTape(item);peeling=false;
  }
  public void PeelTape(WorkshopItem item){var canvas=Canvas(item);if(canvas.Tape.Count==0)return;EndStroke();BeginStroke();Touch(canvas);canvas.Tape.Clear();EndStroke();if(editing==item){SyncTape();RefreshTapeUI();}Game.Audio.Play(Game.Audio.Pickup,.18f);}
  void SyncTape(bool preview=false){if(studio==null||!editing)return;var strips=new System.Collections.Generic.List<WorkshopTapeStrip>(Canvas(editing).Tape);if(preview&&strips.Count<8)strips.Add(tapePreview);studio.SetTape(strips.ToArray());}
 }
}
