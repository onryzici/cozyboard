using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopShop {
        Transform lidHinge;BoxCollider laptopCollider;TMP_Text deskScreenTitle,deskScreenNote;
        bool deskWasIdle;float lidAngle;
        readonly List<Transform> lidSurfaces=new();
        void BuildOpenLaptop(){
            lidHinge=new GameObject("Laptop display hinge").transform;lidHinge.SetParent(laptop.transform,false);lidHinge.localPosition=new Vector3(0,.205f,1.11f);
            foreach(string name in new[]{"Continuous lid seam","Rolled lid edge","Closed sculpted lid","Embossed keycap maker mark","Logo inset key","Logo dot"}){
                var surface=laptop.transform.Find(name);if(surface){surface.SetParent(lidHinge,true);lidSurfaces.Add(surface);}
            }
            var bezel=Mat("Laptop inner bezel",new Color(.23f,.30f,.28f));var display=Mat("Laptop warm display",new Color(.86f,.85f,.71f));var key=Mat("Laptop keyboard sage",new Color(.40f,.49f,.43f));
            Transform Face(string name,Vector3 p,Vector3 size,Material mat,float radius=.06f){var f=Shell(name,p,size,mat,radius);f.SetParent(lidHinge,true);return f;}
            Face("Laptop screen bezel",new Vector3(0,.183f,0),new Vector3(3.18f,.022f,2.10f),bezel,.13f);
            Face("Laptop screen surface",new Vector3(0,.161f,.015f),new Vector3(2.93f,.014f,1.77f),display,.06f);
            // These details belong to the laptop, beneath the closed lid when a job is active.
            Shell("Laptop keyboard inset",new Vector3(0,.201f,.30f),new Vector3(2.82f,.016f,1.14f),bezel,.10f);
            for(int row=0;row<4;row++)for(int col=0;col<12;col++)Shell("Laptop keyboard key",new Vector3(-1.27f+col*.23f,.221f,.70f-row*.23f),new Vector3(.18f,.025f,.16f),key,.025f);
            Shell("Laptop space key",new Vector3(0,.22f,-.19f),new Vector3(1.05f,.025f,.15f),key,.025f);
            Shell("Laptop touchpad",new Vector3(0,.205f,-.69f),new Vector3(1.02f,.014f,.57f),key,.045f);
            TMP_Text ScreenText(string name,string value,Vector3 p,float size,Vector2 bounds){var go=new GameObject(name);go.transform.SetParent(lidHinge,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(-90,0,0);var text=go.AddComponent<TextMeshPro>();text.font=font;text.text=value;text.fontSize=size;text.color=new Color(.26f,.37f,.31f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=bounds;return text;}
            deskScreenTitle=ScreenText("Laptop workshop display title","COZY ATÖLYE",new Vector3(0,-.063f,-1.69f),2.15f,new Vector2(2.7f,.35f));
            deskScreenNote=ScreenText("Laptop workshop display note","Yeni işini seç\nSiparişler · Parça pazarı",new Vector3(0,-.064f,-1.08f),1.60f,new Vector2(2.7f,.80f));
            laptopCollider=(BoxCollider)laptopHit;UpdateDeskLaptop(true);
        }
        public void UpdateDeskLaptop(bool immediate=false){
            if(!lidHinge||!Game)return;bool idle=Game.WaitingForOrder;
            if(idle!=deskWasIdle||immediate){
                EndLaptopMove(true);deskWasIdle=idle;
                // Idle pose is presentation only; the player's chosen laptop position stays in the save.
                if(idle)laptop.transform.localPosition=new Vector3(0,.025f,.15f);else RestoreLaptopPosition();
            }
            float target=idle?110:0;lidAngle=immediate||!Application.isPlaying?target:Mathf.MoveTowards(lidAngle,target,Time.unscaledDeltaTime*150);lidHinge.localRotation=Quaternion.Euler(lidAngle,0,0);
            if(laptopCollider){laptopCollider.center=idle?new Vector3(0,1.1f,.8f):new Vector3(0,.18f,0);laptopCollider.size=idle?new Vector3(3.42f,2.6f,3.8f):new Vector3(3.42f,.36f,2.32f);}
        }
    }
}
