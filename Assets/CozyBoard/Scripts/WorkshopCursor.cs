using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CozyBoard {
    public sealed class WorkshopCursor : MonoBehaviour {
        public Texture2D Pointer; // Retained for compatibility with the authored scene.
        Texture2D arrow,openHand,closedHand;
        WorkshopGameMode game;
        int current=-1;
        public string CurrentStyle=>current==2?"Grab":current==1?"Hand":"Arrow";
        static readonly Vector2[] ArrowShape={new(5,3),new(5,23),new(10,18),new(14,26),new(18,24),new(14,17),new(22,16)};
        public void Apply(){
            if(!arrow){arrow=Draw(0);openHand=Draw(1);closedHand=Draw(2);}
            Set(0,true);
        }
        void Start(){game=FindFirstObjectByType<WorkshopGameMode>();Apply();}
        void Set(int style,bool force=false){if(!force&&current==style)return;current=style;Cursor.SetCursor(style==2?closedHand:style==1?openHand:arrow,style==0?new Vector2(5,3):new Vector2(16,18),CursorMode.ForceSoftware);}
        void LateUpdate(){
            if(!arrow||!game||!game.Menu||Mouse.current==null)return;
            if(game.Menu.InputBlocked||game.Painter.Editing||game.Menu.ToolMode!=0||(game.Tools&&(game.Tools.Active||game.Tools.Busy))){Set(0);return;}
            if(game.Controller.Dragged&&game.Controller.Dragged.Stage>0){Set(2);return;}
            if(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()){Set(0);return;}
            var point=Mouse.current.position.ReadValue();var controller=game.Controller;
            if(Physics.Raycast(controller.ViewCamera.ScreenPointToRay(point),out var hit,100,1<<8)){
                var supply=hit.collider.GetComponent<WorkshopSupply>();var item=supply?game.SupplyItem(supply.Stage):hit.collider.GetComponent<WorkshopItem>();
                if(item&&item.Stage>0&&!item.Fitted){Set(1);return;}
            }
            if(game.ActiveSupply>=3&&game.SupplyItem(game.ActiveSupply)&&controller.FindSocket(game.ActiveSupply,controller.MousePlane(point,controller.Lookup["Case"].transform.position.y+.5f))){Set(1);return;}
            Set(0);
        }
        static float Capsule(Vector2 p,Vector2 a,Vector2 b,float radius){var v=b-a;return Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude))-radius;}
        static float Box(Vector2 p,Vector2 center,Vector2 half,float round){var q=new Vector2(Mathf.Abs(p.x-center.x),Mathf.Abs(p.y-center.y))-half+Vector2.one*round;return new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-round;}
        static float Shape(Vector2 p,int style){
            if(style==0){float distance=100;bool inside=false;for(int i=0,j=ArrowShape.Length-1;i<ArrowShape.Length;j=i++){var a=ArrowShape[i];var b=ArrowShape[j];distance=Mathf.Min(distance,Capsule(p,a,b,0));if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside?-distance:distance;}
            float d=Box(p,new Vector2(17,21),new Vector2(7.2f,6),3);
            d=Mathf.Min(d,Box(p,new Vector2(17,26),new Vector2(5,3),1.4f));
            if(style==1){d=Mathf.Min(d,Capsule(p,new Vector2(11,6),new Vector2(11,20),1.8f));d=Mathf.Min(d,Capsule(p,new Vector2(15.5f,4.5f),new Vector2(15.5f,19),1.8f));d=Mathf.Min(d,Capsule(p,new Vector2(20,6.5f),new Vector2(20,20),1.7f));d=Mathf.Min(d,Capsule(p,new Vector2(24,10),new Vector2(24,21),1.6f));d=Mathf.Min(d,Capsule(p,new Vector2(5.5f,17),new Vector2(11,23),2));}
            else{d=Mathf.Min(d,Box(p,new Vector2(16.5f,15.5f),new Vector2(8,5),2.8f));d=Mathf.Min(d,Capsule(p,new Vector2(7,18),new Vector2(13,22),2.4f));}
            return d;
        }
        static Texture2D Draw(int style){
            const int size=32,samples=4;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=style==0?"Soft white arrow":style==1?"Soft white open hand":"Soft white holding hand",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[size*size];
            var paper=new Color(1,.995f,.975f,1);var edge=new Color(.92f,.91f,.88f,1);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){Color sum=Color.clear;float alpha=0;
                for(int sy=0;sy<samples;sy++)for(int sx=0;sx<samples;sx++){var p=new Vector2(x+(sx+.5f)/samples,y+(sy+.5f)/samples);float d=Shape(p,style);float a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.85f,d));var c=d<-.3f?paper:edge;
                    if(style>0){float line=100;for(int k=0;k<3;k++)line=Mathf.Min(line,Capsule(p,new Vector2(12.5f+k*4,style==1?15:13),new Vector2(12.5f+k*4,19),.12f));if(line<.22f)c=Color.Lerp(paper,edge,.28f);}
                    sum+=c*a;alpha+=a;
                }
                if(alpha>0){sum/=alpha;sum.a=alpha/(samples*samples);}else sum=new Color(paper.r,paper.g,paper.b,0);pixels[(size-1-y)*size+x]=sum;
            }
            texture.SetPixels(pixels);texture.Apply();return texture;
        }
        void OnApplicationFocus(bool focused){if(focused)Apply();}
        void OnDestroy(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);if(arrow)Destroy(arrow);if(openHand)Destroy(openHand);if(closedHand)Destroy(closedHand);}
    }
}
