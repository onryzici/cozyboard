using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CozyBoard {
    public sealed partial class WorkshopTesting {
        static readonly string[] mouseChecks={"Sol tık","Sağ tık","Yukarı kaydır","Aşağı kaydır","Hareket","Sürükle ve bırak","Yan 1","Yan 2"};
        TMP_Text[] mouseCheckLabels;UnityEngine.UI.Button[] mouseBindings;
        void BuildMouseTest(){
            var area=WorkshopUI.Panel("Mouse diagnostic pad",inset,new Vector2(.5f,.5f),Vector2.zero,new Vector2(648,174),new Color(.38f,.50f,.43f));generated.Add(area.gameObject);WorkshopAtelierStyle.Paper(area,new Color(.38f,.50f,.43f),12);
            var note=WorkshopUI.Text("Mouse diagnostic instructions",area.transform,font,"Sol / sağ tıkla · Tekerleği iki yöne çevir · Parçayı hedefe taşı",17,new Vector2(.5f,1),new Vector2(0,-12),new Vector2(600,28));note.alignment=TextAlignmentOptions.Center;note.color=WorkshopUI.Paper;
            var start=WorkshopUI.Panel("Mouse test movable piece",area.transform,new Vector2(.5f,.5f),new Vector2(-185,-10),new Vector2(58,58),WorkshopUI.Paper);WorkshopAtelierStyle.Paper(start,WorkshopUI.Paper,10).raycastTarget=false;start.raycastTarget=false;
            var target=WorkshopUI.Panel("Mouse test drop target",area.transform,new Vector2(.5f,.5f),new Vector2(185,-10),new Vector2(76,76),new Color(.24f,.37f,.29f));WorkshopAtelierStyle.Paper(target,new Color(.24f,.37f,.29f),12).raycastTarget=false;target.raycastTarget=false;
            var mark=WorkshopUI.Text("Mouse target mark",target.transform,font,"BURAYA",13,Vector2.one*.5f,Vector2.zero,new Vector2(65,26));mark.color=WorkshopUI.Paper;mark.alignment=TextAlignmentOptions.Center;
            var cursor=WorkshopUI.Panel("Mouse test cursor",area.transform,Vector2.one*.5f,Vector2.zero,new Vector2(12,12),WorkshopUI.Paper);cursor.raycastTarget=false;cursor.gameObject.SetActive(false);
            var diagnostics=area.gameObject.AddComponent<WorkshopMouseDiagnostics>();diagnostics.Game=Game;diagnostics.Piece=start.rectTransform;diagnostics.Target=target.rectTransform;diagnostics.Cursor=cursor.rectTransform;
            var status=WorkshopUI.Rect("Mouse test checklist",panel.transform,new Vector2(0,1),new Vector2(374,-238),new Vector2(674,106));generated.Add(status.gameObject);mouseCheckLabels=new TMP_Text[8];
            for(int i=0;i<8;i++)mouseCheckLabels[i]=WorkshopUI.Text("Mouse check "+i,status,font,"",16,new Vector2(0,1),new Vector2(i%3*223,-(i/3)*34),new Vector2(218,28));
            mouseBindings=new UnityEngine.UI.Button[2];
            for(int side=0;side<2;side++){
                int index=side;var button=WorkshopUI.Button("Assign mouse macro "+side,panel.transform,font,"",new Vector2(0,1),new Vector2(374+side*337,-363),new Vector2(212,44),()=>OpenFunctionPicker(index));generated.Add(button.gameObject);button.GetComponentInChildren<TMP_Text>().fontSize=16;mouseBindings[side]=button;
                var test=WorkshopUI.Button("Test mouse macro "+side,panel.transform,font,"Dene",new Vector2(0,1),new Vector2(596+side*337,-363),new Vector2(105,44),()=>Game.Press(Game.Controller.Lookup["Mouse_SideButton_"+index],true));generated.Add(test.gameObject);
            }
            BuildFunctionPicker();
        }
        public bool SetMouseFunction(int side,string function){if(!Game.IsMouse||side<0||side>1||!WorkshopGameMode.FunctionNames.Contains(function))return false;Game.MouseProduct.State.functions[side]=function;Game.MouseProduct.State.testMask&=~(1<<(side+6));Refresh();Game.Refresh();return true;}
        public void RefreshProduct()=>Refresh();
        void RefreshMouseChecks(){if(mouseCheckLabels==null)return;for(int i=0;i<8;i++)mouseCheckLabels[i].text=((Game.MouseProduct.State.testMask&(1<<i))!=0?"Bitti · ":"Dene · ")+mouseChecks[i];if(mouseBindings!=null)for(int i=0;i<mouseBindings.Length;i++)mouseBindings[i].GetComponentInChildren<TMP_Text>().text=$"Yan {i+1}: "+(string.IsNullOrEmpty(Game.MouseProduct.State.functions[i])?"Kısayol seç":Game.MouseProduct.State.functions[i]);}
    }
    public sealed class WorkshopMouseDiagnostics:MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler {
        public WorkshopGameMode Game;public RectTransform Piece,Target,Cursor;
        RectTransform area;Vector2 home,lastPoint;bool hasPoint,carrying;float travel;
        void Awake(){area=(RectTransform)transform;}
        void Start(){home=Piece.anchoredPosition;}
        void OnDisable(){if(Piece)Piece.anchoredPosition=home;carrying=false;hasPoint=false;travel=0;if(Cursor)Cursor.gameObject.SetActive(false);}
        bool Active=>Game&&Game.IsMouse&&Game.Testing.Active&&Game.Completed;
        public void OnPointerDown(PointerEventData e){if(!Active)return;if(e.button==PointerEventData.InputButton.Left)Game.Press(Game.Controller.Lookup["Mouse_LeftButton"],true);else if(e.button==PointerEventData.InputButton.Right)Game.Press(Game.Controller.Lookup["Mouse_RightButton"],true);carrying=e.button==PointerEventData.InputButton.Left&&RectTransformUtility.RectangleContainsScreenPoint(Piece,e.position,e.pressEventCamera);}
        public void OnBeginDrag(PointerEventData e){}
        public void OnDrag(PointerEventData e){if(!Active||!carrying)return;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out var p))Piece.anchoredPosition=p;}
        public void OnEndDrag(PointerEventData e){if(!carrying)return;if(Active&&RectTransformUtility.RectangleContainsScreenPoint(Target,e.position,e.pressEventCamera))Game.MouseProduct.RecordTest(5);Piece.anchoredPosition=home;carrying=false;}
        public void OnScroll(PointerEventData e){if(Active&&Mathf.Abs(e.scrollDelta.y)>.01f)Game.MouseProduct.RecordTest(e.scrollDelta.y>0?2:3);}
        void Update(){var mouse=Mouse.current;if(!Active||mouse==null){if(Cursor)Cursor.gameObject.SetActive(false);return;}var p=mouse.position.ReadValue();bool inside=RectTransformUtility.RectangleContainsScreenPoint(area,p);Cursor.gameObject.SetActive(inside);if(!inside){hasPoint=false;return;}if(mouse.backButton.wasPressedThisFrame)Game.Press(Game.Controller.Lookup["Mouse_SideButton_0"],true);if(mouse.forwardButton.wasPressedThisFrame)Game.Press(Game.Controller.Lookup["Mouse_SideButton_1"],true);float wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.01f)Game.MouseProduct.RecordTest(wheel>0?2:3);if(RectTransformUtility.ScreenPointToLocalPointInRectangle(area,p,null,out var local))Cursor.anchoredPosition=local;if(hasPoint){travel+=Vector2.Distance(p,lastPoint);if(travel>90)Game.MouseProduct.RecordTest(4);}lastPoint=p;hasPoint=true;}
    }
}
