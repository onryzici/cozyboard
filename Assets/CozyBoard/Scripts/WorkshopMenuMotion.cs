using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CozyBoard {
 public sealed class WorkshopMenuEntrance:MonoBehaviour {
  CanvasGroup group;RectTransform rect;Vector2 origin;float elapsed;
  void Awake(){rect=(RectTransform)transform;origin=rect.anchoredPosition;group=gameObject.AddComponent<CanvasGroup>();}
  void OnEnable(){elapsed=0;if(group)group.alpha=0;}
  void Update(){elapsed+=Time.unscaledDeltaTime;float f=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.65f));group.alpha=f;rect.anchoredPosition=origin+Vector2.left*(18*(1-f));}
 }
 public sealed class WorkshopMenuHover:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
  bool hover;float value;
  public void OnPointerEnter(PointerEventData e){hover=true;}public void OnPointerExit(PointerEventData e){hover=false;}
  public void OnSelect(BaseEventData e){hover=true;}public void OnDeselect(BaseEventData e){hover=false;}
  void Update(){var b=GetComponent<Button>();value=Mathf.MoveTowards(value,hover&&b.interactable?1:0,Time.unscaledDeltaTime*6);transform.localScale=Vector3.one*(1+value*.025f);}
  void OnDisable(){hover=false;value=0;transform.localScale=Vector3.one;}
 }
 public sealed class WorkshopMenuShade:Graphic {
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;float[] x={0,.28f,.48f,.68f,1};float[] a={.68f,.55f,.22f,0,.06f};for(int i=0;i<x.Length;i++){var c=new Color(.055f,.09f,.075f,a[i]);vh.AddVert(new Vector3(r.xMin+r.width*x[i],r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin+r.width*x[i],r.yMax),c,Vector2.one);if(i>0){int k=i*2;vh.AddTriangle(k-2,k-1,k);vh.AddTriangle(k,k-1,k+1);}}}
 }
}
