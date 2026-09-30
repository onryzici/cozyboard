using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CozyBoard {
    public static class WorkshopUI {
        public static readonly Color Ink=new Color(.21f,.25f,.22f),Paper=new Color(.94f,.88f,.73f),Sage=new Color(.38f,.52f,.43f);
        public static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        public static UnityEngine.UI.Image Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color){var r=Rect(name,parent,anchor,position,size);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.color=color;return i;}
        public static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string value,float size,Vector2 anchor,Vector2 position,Vector2 dimensions){var r=Rect(name,parent,anchor,position,dimensions);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=Ink;t.raycastTarget=false;return t;}
        public static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string label,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action){var bg=Panel(name,parent,anchor,position,size,Sage);var b=bg.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=bg;b.onClick.AddListener(action);var shadow=bg.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.13f,.18f,.14f,.25f);shadow.effectDistance=new Vector2(2,-3);var text=Text("Label",bg.transform,font,label,23,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*12);text.alignment=TextAlignmentOptions.Center;text.color=Paper;return b;}
        public static UnityEngine.UI.RawImage Art(string name,Transform parent,Texture texture,Vector2 anchor,Vector2 position,Vector2 size){var r=Rect(name,parent,anchor,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=texture;image.raycastTarget=false;return image;}
        public static void Icon(UnityEngine.UI.Button button,Texture atlas,int index,string tooltip,WorkshopExperience experience){
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
            var bg=button.GetComponent<UnityEngine.UI.Image>();if(bg)bg.enabled=false;
            var icon=button.transform.Find("Painted tool icon");var raw=icon?icon.GetComponent<UnityEngine.UI.RawImage>():Art("Painted tool icon",button.transform,atlas,Vector2.one*.5f,Vector2.zero,((RectTransform)button.transform).sizeDelta);raw.texture=atlas;raw.uvRect=new Rect(index%4*.25f,(3-index/4)*.25f,.25f,.25f);raw.color=Color.white;raw.raycastTarget=true;button.targetGraphic=raw;
            var tip=button.GetComponent<WorkshopTooltip>();if(!tip)tip=button.gameObject.AddComponent<WorkshopTooltip>();tip.Experience=experience;tip.Message=tooltip;
        }
    }
    public sealed class WorkshopTooltip : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler {
        public WorkshopExperience Experience;public string Message;
        public void OnPointerEnter(PointerEventData eventData){if(Experience)Experience.ShowTooltip(Message);}
        public void OnPointerExit(PointerEventData eventData){if(Experience)Experience.ShowTooltip(null);}
        void OnDisable(){if(Experience)Experience.ShowTooltip(null);}
    }
}
