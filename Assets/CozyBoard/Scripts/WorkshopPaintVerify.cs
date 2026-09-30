using System;
using System.Linq;
using UnityEngine;
namespace CozyBoard {
 public static class WorkshopPaintVerify {
  public static void Run(WorkshopGameMode game,Action<bool,string> check){
   game.Tools.ResetState();game.Menu.ClosePanels();var p=game.Painter;p.ResetPaint();
   foreach(int selected in game.Shop.Data.selected)game.Shop.Data.stock[selected]=Mathf.Max(1,game.Shop.Data.stock[selected]);
   foreach(var item in game.Controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage)){game.Shop.Consume(item.Stage);game.Controller.Attach(item);}
   var cap=game.Controller.Lookup["Keycap_00"];var bounds=cap.Visual.GetComponent<MeshFilter>().sharedMesh.bounds;
   var top=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);var uv=p.SurfaceUV(cap,top,Vector3.up);var clean=p.Sample(cap,uv);
   p.PaintAtSurface(cap,top,Vector3.up,Color.red,.09f);var first=p.Sample(cap,uv);
   check(Vector4.Distance(clean,first)>.05f&&first.g>.08f,"One brush contact deposits a translucent coat");
   for(int i=0;i<5;i++)p.PaintAtSurface(cap,top,Vector3.up,Color.red,.09f);var layered=p.Sample(cap,uv);
   check(layered.g<first.g-.05f,"Repeated passes build pigment coverage gradually");
   var wet=WorkshopPaintMedium.Deposit(Color.red,Color.blue,1,.12f,1);var dry=WorkshopPaintMedium.Deposit(Color.red,Color.blue,1,.12f,0);
   check(wet.r>dry.r&&wet.b<dry.b,"A wet undercoat blends into a new colour more than a dry undercoat");
   check(WorkshopPaintMedium.Wetness(10,10)==1&&WorkshopPaintMedium.Wetness(10,13.5f)==.5f&&WorkshopPaintMedium.Wetness(10,17)==0,"Paint dries progressively over seven seconds");
   Color one=WorkshopPaintMedium.Deposit(Color.white,Color.blue,.7f,.2f,0),many=Color.white;for(int i=0;i<12;i++)many=WorkshopPaintMedium.Deposit(many,Color.blue,.7f,.2f/12,0);
   check(Vector4.Distance(one,many)<.0001f,"Equal contact time yields equal coverage independent of frame subdivision");
   p.ResetPaint();uv=p.SurfaceUV(cap,top,Vector3.up);clean=p.Sample(cap,uv);
   var tape=new WorkshopTapeStrip{Start=top-Vector3.forward*bounds.size.z,End=top+Vector3.forward*bounds.size.z,Normal=Vector3.up,Width=bounds.size.x*.22f};
   check(p.AddTape(cap,tape)&&p.TapeCount(cap)==1,"A tape strip can be laid across the key");
   p.PaintAtSurface(cap,top,Vector3.up,Color.blue,bounds.size.x*.5f);
   check(Vector4.Distance(clean,p.Sample(cap,uv))<.001f,"Tape completely protects the surface beneath a brush stroke");
   var side=new Vector3(top.x,bounds.center.y,bounds.min.z+.002f);var sideUV=p.SurfaceUV(cap,side,Vector3.back);var sideBefore=p.Sample(cap,sideUV);
   p.PaintAtSurface(cap,side,Vector3.back,Color.blue,.09f);
   check(Vector4.Distance(sideBefore,p.Sample(cap,sideUV))<.001f,"Tape protection continues from the top onto the key side");
   var outside=top+Vector3.right*bounds.size.x*.28f;var outsideUV=p.SurfaceUV(cap,outside,Vector3.up);var beforeFill=p.Sample(cap,outsideUV);p.Fill(cap,Color.blue);
   check(Vector4.Distance(clean,p.Sample(cap,uv))<.001f&&p.Sample(cap,outsideUV).b>.9f&&p.Sample(cap,outsideUV).r<.05f,"Fill respects tape while colouring exposed surface");
   p.PeelTape(cap);check(p.TapeCount(cap)==0&&Vector4.Distance(clean,p.Sample(cap,uv))<.001f,"Peeling reveals a clean unpainted stripe");
   p.Undo();check(p.TapeCount(cap)==1,"Undo restores peeled tape");p.Redo();check(p.TapeCount(cap)==0,"Redo peels the tape again");p.Undo();
   var saved=game.SerializeProgress();game.RestoreProgress(saved);
   check(cap.Fitted&&p.TapeCount(cap)==1&&Vector4.Distance(clean,p.Sample(cap,uv))<.01f,"Saving and loading preserve tape and protected artwork");
   var old=JsonUtility.FromJson<WorkshopGameMode.SaveData>(saved);old.version=9;old.tapeMasks=null;game.RestoreProgress(JsonUtility.ToJson(old));
   check(p.TapeCount(cap)==0&&p.Sample(cap,outsideUV).b>.9f,"Version 9 artwork loads without inventing masking tape");
   p.ResetPaint();var sideClean=p.Sample(cap,sideUV);for(int i=0;i<8;i++)p.PaintAtSurface(cap,side,Vector3.back,Color.red,.09f);
   var opposite=new Vector3(top.x,bounds.center.y,bounds.max.z-.002f);var oppositeUV=p.SurfaceUV(cap,opposite,Vector3.forward);
   check(p.Sample(cap,sideUV).g<sideClean.g*.35f&&Vector4.Distance(sideClean,p.Sample(cap,oppositeUV))<.01f,"Layered side strokes do not bleed through to the opposite wall (base="+sideClean+", side="+p.Sample(cap,sideUV)+", opposite="+p.Sample(cap,oppositeUV)+")");
  }
 }
}
