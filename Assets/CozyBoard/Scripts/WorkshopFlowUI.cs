using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CozyBoard {
 public sealed class WorkshopPanelFade:MonoBehaviour {
  CanvasGroup group;float t;
  void Awake(){group=GetComponent<CanvasGroup>();if(!group)group=gameObject.AddComponent<CanvasGroup>();}
  void OnEnable(){t=0;if(group)group.alpha=0;}
  void Update(){t+=Time.unscaledDeltaTime;group.alpha=Mathf.SmoothStep(0,1,t/.22f);}
 }
 public sealed class WorkshopToolbarSelection:MonoBehaviour {
  public WorkshopGameMode Game;public int Mode;GameObject mark;
  public void Setup(WorkshopGameMode game,int mode){Game=game;Mode=mode;var line=WorkshopUI.Panel("Selected tool underline",transform,new Vector2(.5f,0),new Vector2(0,-9),new Vector2(70,5),new Color(1,.87f,.51f));line.raycastTarget=false;mark=line.gameObject;}
  void Update(){bool selected=Game.Menu.ToolMode==Mode&&(!Game.Tools||!Game.Tools.Active);if(Mode==2)selected=Game.Experience.Inspecting;mark.SetActive(selected);var graphic=GetComponent<Button>().targetGraphic;if(graphic)graphic.color=selected?Color.white:new Color(.87f,.87f,.87f);}
 }
 public sealed class WorkshopHomeMark:Graphic {
  void Quad(VertexHelper h,Vector2 a,Vector2 b,float width){var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int k=h.currentVertCount;h.AddVert(a+n,color,Vector2.zero);h.AddVert(b+n,color,Vector2.zero);h.AddVert(b-n,color,Vector2.zero);h.AddVert(a-n,color,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
  protected override void OnPopulateMesh(VertexHelper h){h.Clear();Quad(h,new Vector2(-24,0),new Vector2(0,22),5);Quad(h,new Vector2(0,22),new Vector2(24,0),5);Quad(h,new Vector2(-18,3),new Vector2(-18,-21),5);Quad(h,new Vector2(18,3),new Vector2(18,-21),5);Quad(h,new Vector2(-18,-21),new Vector2(18,-21),5);Quad(h,new Vector2(-5,-19),new Vector2(-5,-5),4);Quad(h,new Vector2(-5,-5),new Vector2(6,-5),4);Quad(h,new Vector2(6,-5),new Vector2(6,-19),4);}
 }
 public sealed class WorkshopSceneCurtain:MonoBehaviour {
  Image curtain;
  public void Run(Transform root,System.Action action){curtain=WorkshopUI.Panel("Soft workshop transition",root,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.12f,.18f,.15f,0));curtain.rectTransform.anchorMax=Vector2.one;curtain.rectTransform.offsetMax=Vector2.zero;StartCoroutine(Transition(action));}
  public void Cancel(){StopAllCoroutines();if(curtain)Destroy(curtain.gameObject);Destroy(this);}
  IEnumerator Transition(System.Action action){for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){curtain.color=new Color(.12f,.18f,.15f,Mathf.SmoothStep(0,1,t/.35f));yield return null;}curtain.color=new Color(.12f,.18f,.15f,1);action();curtain.transform.SetAsLastSibling();yield return new WaitForSecondsRealtime(.2f);for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){curtain.color=new Color(.12f,.18f,.15f,1-Mathf.SmoothStep(0,1,t/.45f));yield return null;}Destroy(curtain.gameObject);Destroy(this);}
 }
}
