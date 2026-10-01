using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopRepairBench {
        Material Paint(string name,Color color){var material=new Material(Shader.Find("CozyBoard/Painted")){name=name};material.SetColor("_BaseColor",color);owned.Add(material);return material;}
        Transform Shape(string name,Transform parent,Vector3 position,Vector3 size,Material material,float radius=.06f){
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var mesh=WorkshopPropMesh.RoundedBox(size,radius);owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            var shadow=new GameObject("Painted contact shadow");shadow.transform.SetParent(go.transform,false);
            shadow.AddComponent<MeshFilter>().sharedMesh=mesh;shadow.AddComponent<MeshRenderer>().sharedMaterial=game.Controller.ShadowMaterial;
            return go.transform;
        }
        TMP_Text Print(string name,Transform parent,Vector3 position,string text,float size,Vector2 area,TMP_FontAsset font,Color color){
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,0,0);
            var label=go.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=area;return label;
        }
        Transform Model(string name,Transform parent,Vector3 position,Mesh mesh,Material material,bool shadow=true){
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;owned.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(shadow){var contact=new GameObject("Painted contact shadow");contact.transform.SetParent(go.transform,false);contact.AddComponent<MeshFilter>().sharedMesh=mesh;contact.AddComponent<MeshRenderer>().sharedMaterial=game.Controller.ShadowMaterial;}
            return go.transform;
        }
        // Hollow profiles give the stoneware dishes real rims, walls and interiors.
        internal static Mesh Turn(params Vector2[] profile){
            const int sides=48;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int ring=0;ring<profile.Length;ring++)for(int i=0;i<=sides;i++){
                float angle=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(angle)*profile[ring].x,profile[ring].y,Mathf.Sin(angle)*profile[ring].x));uv.Add(new Vector2((float)i/sides,(float)ring/(profile.Length-1)));
            }
            for(int ring=0;ring<profile.Length-1;ring++)for(int i=0;i<sides;i++){int a=ring*(sides+1)+i,b=a+sides+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh{name="Atelier turned profile"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        Transform Round(string name,Transform parent,Vector3 position,float radius,float height,Material material)=>Model(name,parent,position,Turn(new Vector2(0,0),new Vector2(radius*.9f,0),new Vector2(radius,height*.2f),new Vector2(radius,height*.8f),new Vector2(radius*.9f,height),new Vector2(0,height)),material);
        static Mesh WireMesh(float radius,Vector3[] points){
            const int sides=8;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<points.Length;i++){
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;
                var across=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;var normal=Vector3.Cross(tangent,across).normalized;
                for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(points[i]+(across*Mathf.Cos(a)+normal*Mathf.Sin(a))*radius);uv.Add(new Vector2((float)j/sides,(float)i/(points.Length-1)));}
            }
            for(int i=0;i<points.Length-1;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Round atelier wire"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        Transform Tube(string name,Transform parent,Material material,float radius,params Vector3[] points)=>Model(name,parent,Vector3.zero,WireMesh(radius,points),material);
        void Ring(string name,Transform parent,Vector3 center,float radius,float wire,Material material){
            var points=new Vector3[49];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/48;points[i]=center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}Tube(name,parent,material,wire,points);
        }
        void Screw(Transform parent,Vector3 position,Material steel,Material dark){
            var screw=Round("Recessed steel screw",parent,position,.042f,.012f,steel);
            Shape("Screwdriver slot",screw,new Vector3(0,.014f,0),new Vector3(.054f,.004f,.012f),dark,.003f);
        }
        void BuildBench(TMP_FontAsset font){
            bench=new GameObject("Repair workbench").transform;bench.SetParent(game.Controller.transform,false);bench.position=Origin;
            var cream=Paint("Warm stoneware glaze",new Color(.86f,.80f,.65f));var sage=Paint("Sage enamel",new Color(.43f,.57f,.48f));
            var dark=Paint("Soft graphite rubber",new Color(.26f,.31f,.28f));var brass=Paint("Aged honey brass",new Color(.73f,.56f,.31f));
            var steel=Paint("Brushed warm steel",new Color(.69f,.72f,.66f));
            var source=GameObject.Find("FlatWorkspace2D");
            var surface=new GameObject("Matching painted repair surface");surface.transform.SetParent(bench,false);
            surface.transform.localPosition=new Vector3(0,.001f,0);surface.transform.localScale=new Vector3(23,1,24);
            surface.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
            var deskMaterial=new Material(source.GetComponent<MeshRenderer>().sharedMaterial){name="Repair desk / shared atelier finish"};
            deskMaterial.SetVector("_WorkspaceOrigin",new Vector4(Origin.x,Origin.z,0,0));owned.Add(deskMaterial);
            surface.AddComponent<MeshRenderer>().sharedMaterial=deskMaterial;
            surface.layer=9;benchHit=surface.AddComponent<BoxCollider>();((BoxCollider)benchHit).size=new Vector3(1,.025f,1);
            Shader.SetGlobalVector("_WorkshopSecondarySurface",new Vector4(Origin.x,Origin.z,11.5f,12));
            BuildPartsDishes(cream,sage);
            // Use exactly the original mat's dimensions, finish and loose tool arrangement.
            var originalRest=game.Controller.transform.Find("Linen tool rest");
            var rest=new GameObject("Repair linen tool rest");rest.transform.SetParent(bench,false);rest.transform.localPosition=originalRest.localPosition+Vector3.forward*.8f;
            rest.AddComponent<MeshFilter>().sharedMesh=originalRest.GetComponent<MeshFilter>().sharedMesh;
            rest.AddComponent<MeshRenderer>().sharedMaterial=originalRest.GetComponent<MeshRenderer>().sharedMaterial;
            BuildTestInstrument(font,sage,cream,dark,steel,brass);
            BuildRepairParts(font,cream,sage,steel,dark,brass);
        }
        internal static Mesh PartsDishMesh()=>Turn(new Vector2(0,0),new Vector2(.54f,0),new Vector2(.64f,.035f),new Vector2(.73f,.15f),new Vector2(.74f,.20f),new Vector2(.71f,.225f),new Vector2(.68f,.21f),new Vector2(.66f,.16f),new Vector2(.57f,.095f),new Vector2(0,.095f));
        internal static Mesh PartsDishRim(){var points=new Vector3[49];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/48;points[i]=new Vector3(Mathf.Cos(a)*.711f,.211f,Mathf.Sin(a)*.711f);}return WireMesh(.018f,points);}
        void BuildPartsDishes(Material glaze,Material sage){
            foreach(float z in new[]{1.05f,-.65f}){
                var dish=Model(z>0?"Recovered keycap dish":"Replacement switch dish",bench,new Vector3(-5.8f,.022f,z),PartsDishMesh(),glaze);
                Model("Hand glazed sage rim",dish,Vector3.zero,PartsDishRim(),sage,false);
                Ring("Glazed foot detail",dish,new Vector3(0,.015f,0),.53f,.018f,glaze);
            }
        }
        void BuildTestInstrument(TMP_FontAsset font,Material sage,Material cream,Material dark,Material steel,Material brass){
            var tester=Shape("Repair test instrument",bench,new Vector3(2.35f,.20f,4.12f),new Vector3(2.25f,.38f,1.5f),sage,.18f);
            tester.gameObject.layer=9;testerHit=tester.gameObject.AddComponent<BoxCollider>();((BoxCollider)testerHit).size=new Vector3(2.25f,.38f,1.5f);
            Shape("Inset enamel instrument face",tester,new Vector3(0,.197f,0),new Vector3(2.06f,.018f,1.28f),cream,.12f);
            Shape("Recessed LCD bezel",tester,new Vector3(-.16f,.216f,.25f),new Vector3(1.45f,.024f,.50f),dark,.055f);
            var lcd=Paint("Quiet olive LCD glass",new Color(.66f,.74f,.56f));
            Shape("LCD glass",tester,new Vector3(-.16f,.232f,.25f),new Vector3(1.29f,.008f,.36f),lcd,.035f);
            deviceText=Print("Tester status",tester,new Vector3(-.16f,.242f,.25f),"TESTE HAZIR",1.5f,new Vector2(1.24f,.40f),font,WorkshopUI.Ink);
            var knob=Round("Ribbed test dial",tester,new Vector3(.70f,.21f,-.23f),.22f,.10f,dark);
            instrumentDial=knob;
            Round("Dial brass cap",knob,new Vector3(0,.10f,0),.17f,.013f,brass);
            Shape("Dial pointer",knob,new Vector3(0,.116f,.085f),new Vector3(.018f,.008f,.10f),cream,.005f);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var ridge=Shape("Dial grip flute",knob,new Vector3(Mathf.Cos(a)*.214f,.052f,Mathf.Sin(a)*.214f),new Vector3(.032f,.075f,.018f),steel,.008f);ridge.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            ledOff=Paint("Instrument untested light",new Color(.40f,.45f,.36f));
            ledPass=Paint("Instrument signal light",new Color(.56f,.85f,.39f));
            ledFail=Paint("Instrument no signal light",new Color(.95f,.36f,.23f));
            ledPass.SetFloat("_Shading",.1f);ledFail.SetFloat("_Shading",.1f);
            for(int i=0;i<3;i++){
                Round("Indicator socket",tester,new Vector3(-.65f+i*.32f,.21f,-.18f),.095f,.018f,dark);
                instrumentLeds[i]=Round("Three key indicator "+i,tester,new Vector3(-.65f+i*.32f,.23f,-.18f),.073f,.016f,ledOff).GetComponent<MeshRenderer>();
                instrumentLabels[i]=Print("Measured key "+i,tester,new Vector3(-.65f+i*.32f,.231f,-.40f),"—",1.1f,new Vector2(.25f,.20f),font,WorkshopUI.Ink);
            }
            Print("Instrument dial label",tester,new Vector3(.70f,.23f,-.53f),"TEST",.85f,new Vector2(.45f,.18f),font,WorkshopUI.Ink);
            foreach(float x in new[]{-.91f,.91f})foreach(float z in new[]{-.52f,.52f})Screw(tester,new Vector3(x,.215f,z),steel,dark);
            Shape("USB instrument socket",tester,new Vector3(-.38f,-.025f,-.758f),new Vector3(.34f,.12f,.025f),dark,.012f);
            Shape("USB strain relief",bench,new Vector3(1.97f,.15f,3.23f),new Vector3(.25f,.13f,.30f),dark,.045f);
            instrumentSocket=new GameObject("Keyboard USB socket").transform;
            instrumentSocket.SetParent(game.Controller.Lookup["Case"].transform,false);instrumentSocket.localPosition=new Vector3(-2.5f,.235f,1.335f);
            Shape("USB socket metal rim",instrumentSocket,Vector3.zero,new Vector3(.32f,.10f,.09f),steel,.015f);
            Shape("USB socket opening",instrumentSocket,new Vector3(0,0,.050f),new Vector3(.24f,.058f,.008f),dark,.006f);
            var lead=Tube("Flexible braided USB lead",bench,dark,.027f,InstrumentLead(false,new Vector3(1.15f,.08f,1.78f),Quaternion.identity));
            instrumentLead=lead.GetComponent<MeshFilter>();instrumentLeadShadow=lead.Find("Painted contact shadow").GetComponent<MeshFilter>();
            instrumentPlug=new GameObject("Instrument USB plug").transform;instrumentPlug.SetParent(bench,false);instrumentPlug.localPosition=new Vector3(1.15f,.08f,1.78f);
            Shape("USB lead plug",instrumentPlug,Vector3.zero,new Vector3(.22f,.12f,.25f),dark,.03f);
            Shape("USB plug shell",instrumentPlug,new Vector3(0,0,-.16f),new Vector3(.17f,.075f,.11f),steel,.015f);
        }
        void BuildUI(Transform ui,TMP_FontAsset font){
            var nav=WorkshopUI.Panel("Workstation navigation",ui,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(476,58),WorkshopUI.Paper);navigation=nav.gameObject;
            WorkshopAtelierStyle.Paper(nav,WorkshopUI.Paper,12).raycastTarget=false;
            assemblyButton=WorkshopUI.Button("Visit assembly desk",nav.transform,font:font,label:"←  Montaj masası",anchor:new Vector2(0,.5f),position:new Vector2(6,0),size:new Vector2(226,46),action:()=>Visit(false));
            repairButton=WorkshopUI.Button("Visit repair desk",nav.transform,font,"Tamir masası  →",new Vector2(1,.5f),new Vector2(-6,0),new Vector2(226,46),()=>Visit(true));
            foreach(var button in new[]{assemblyButton,repairButton}){button.GetComponentInChildren<TMP_Text>().fontSize=19;button.GetComponent<UnityEngine.UI.Shadow>().enabled=false;button.targetGraphic=WorkshopAtelierStyle.Paper(button.GetComponent<UnityEngine.UI.Image>(),WorkshopUI.Sage,8);}
            var card=WorkshopUI.Panel("Repair service note",ui,new Vector2(1,1),new Vector2(-28,-145),new Vector2(310,424),WorkshopUI.Paper);jobCard=card.gameObject;WorkshopAtelierStyle.Paper(card,WorkshopUI.Paper,14);
            WorkshopUI.Text("Repair note heading",card.transform,font,"SERVİS NOTU",15,new Vector2(0,1),new Vector2(22,-18),new Vector2(266,24)).color=WorkshopUI.Sage;
            jobTitle=WorkshopUI.Text("Repair customer",card.transform,font,"",25,new Vector2(0,1),new Vector2(22,-52),new Vector2(266,70));
            jobBody=WorkshopUI.Text("Repair complaint",card.transform,font,"",19,new Vector2(0,1),new Vector2(22,-126),new Vector2(266,110));jobBody.enableAutoSizing=true;jobBody.fontSizeMin=16;jobBody.fontSizeMax=19;
            stepText=WorkshopUI.Text("Repair current step",card.transform,font,"",19,new Vector2(0,1),new Vector2(22,-248),new Vector2(266,98));stepText.enableAutoSizing=true;stepText.fontSizeMin=16;stepText.fontSizeMax=19;
            actionButton=WorkshopUI.Button("Repair next action",card.transform,font,"",new Vector2(.5f,0),new Vector2(0,20),new Vector2(266,48),NextRepairAction);actionText=actionButton.GetComponentInChildren<TMP_Text>();actionText.fontSize=19;
            BuildPartInspection(ui,font);
        }
    }
}
