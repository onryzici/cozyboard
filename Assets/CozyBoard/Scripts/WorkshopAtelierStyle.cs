using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    public static class WorkshopAtelierStyle {
        static Texture2D icons;
        public static Texture2D Icons=>icons?icons:icons=Resources.Load<Texture2D>("UI/AtelierIcons");
        public static Rect IconUV(int index){
            // Cell bounds measured from the generated sheet; exclude neighbouring tiles.
            int[] left={27,250,475,699,916,1139},top={48,270,481,688,902};
            int[] right={238,466,686,912,1124,1353},bottom={257,474,683,891,1107};
            int col=index%6,row=index/6;
            return Rect.MinMaxRect((left[col]-2)/1374f,1-(bottom[row]+2)/1145f,(right[col]+2)/1374f,1-(top[row]-2)/1145f);
        }
        public static RawImage Art(string name,Transform parent,int index,Vector2 anchor,Vector2 position,Vector2 size){
            var raw=WorkshopUI.Art(name,parent,Icons,anchor,position,size);raw.uvRect=IconUV(index);return raw;
        }
        public static void Icon(Button button,int index,string hint,WorkshopExperience experience){
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))text.gameObject.SetActive(false);
            var old=button.GetComponent<RawImage>();if(old){old.enabled=false;old.raycastTarget=false;}
            var image=button.GetComponent<Image>();if(image)image.enabled=false;
            var shadow=button.GetComponent<Shadow>();if(shadow)shadow.enabled=false;
            foreach(Transform child in button.transform)if(child.name is "Painted tool icon" or "Painted atelier home")child.gameObject.SetActive(false);
            var existing=button.transform.Find("Atelier icon");
            var raw=existing?existing.GetComponent<RawImage>():Art("Atelier icon",button.transform,index,Vector2.one*.5f,Vector2.zero,((RectTransform)button.transform).sizeDelta);
            raw.texture=Icons;raw.uvRect=IconUV(index);raw.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;raw.raycastTarget=true;button.targetGraphic=raw;
            var tip=button.GetComponent<WorkshopTooltip>()??button.gameObject.AddComponent<WorkshopTooltip>();tip.Experience=experience;tip.Message=hint;
        }
        public static WorkshopPaperGraphic Paper(Image original,Color color,float radius=14){
            original.enabled=false;var child=original.transform.Find("Atelier paper");WorkshopPaperGraphic paper;
            if(child)paper=child.GetComponent<WorkshopPaperGraphic>();else{
                var r=WorkshopUI.Rect("Atelier paper",original.transform,Vector2.zero,Vector2.zero,Vector2.zero);r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();paper=r.gameObject.AddComponent<WorkshopPaperGraphic>();
            }
            paper.color=color;paper.Radius=radius;return paper;
        }
        public static float WheelSteps(float delta)=>Mathf.Abs(delta)>=10?delta/120f:delta;
    }
    [ExecuteAlways,RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorkshopPaperGraphic:MaskableGraphic {
        public float Radius=14;
        protected override void OnPopulateMesh(VertexHelper h){
            h.Clear();var r=rectTransform.rect;float radius=Mathf.Min(Radius,Mathf.Min(r.width,r.height)*.5f);
            h.AddVert(r.center,color,Vector2.one*.5f);
            for(int corner=0;corner<4;corner++)for(int i=0;i<=8;i++){
                float angle=(corner*90+i*90f/8)*Mathf.Deg2Rad;
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                var tint=color;float grain=1+Mathf.Sin(p.x*.37f+p.y*.19f)*.012f;tint.r*=grain;tint.g*=grain;tint.b*=grain;
                h.AddVert(p,tint,Vector2.zero);
            }
            for(int i=1;i<=36;i++)h.AddTriangle(0,i==36?1:i+1,i);
        }
    }
    public sealed class WorkshopStatusLayout:MonoBehaviour {
        public WorkshopGameMode Game;
        RectTransform rect,parent;Canvas canvas;Button test;
        void Awake(){rect=(RectTransform)transform;parent=(RectTransform)rect.parent;canvas=GetComponentInParent<Canvas>();}
        public void Arrange(){
            if(!rect)Awake();
            if(!Game||!canvas)return;
            var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            float edge=float.MaxValue;
            foreach(float x in new[]{-5.05f,5.05f}){
                var screen=Game.Controller.ViewCamera.WorldToScreenPoint(new Vector3(x,0,-3.53f));
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,uiCamera,out var p))edge=Mathf.Min(edge,p.y-parent.rect.yMin);
            }
            const float toolbarTop=112;
            float available=Mathf.Max(82,edge-toolbarTop-20);
            float height=Mathf.Min(92,available);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=new Vector2(0,toolbarTop+(Mathf.Max(available,height)-height)*.5f+10);rect.sizeDelta=new Vector2(740,height);
            bool ready=Game.Completed;
            foreach(var text in new[]{Game.Objective,Game.Progress,Game.CompletionLabel}){
                text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(.5f,1);
                text.rectTransform.sizeDelta=new Vector2(ready?450:690,text==Game.Objective?26:20);
                text.rectTransform.anchoredPosition=new Vector2(ready?-120:0,text==Game.Objective?-10:text==Game.Progress?-38:-62);
            }
            if(!test)test=transform.Find("Open keyboard test").GetComponent<Button>();test.gameObject.SetActive(ready);
        }
        void LateUpdate()=>Arrange();
    }
}
