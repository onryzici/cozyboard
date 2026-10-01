using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopRepairBench {
        public bool DetailOpen=>partInspection&&partInspection.activeSelf;
        public bool Unpacking{get;private set;}
        Transform oldSwitch,oldSwitchModel,sparePacket,packetFlap,packetSeal,repairPreviewModel;
        MeshRenderer oldSwitchRenderer,repairPreviewRenderer;
        Collider oldSwitchHit,replacementPacketHit;
        GameObject oldPartTag,partInspection;
        TMP_Text packetText,inspectionTitle,inspectionBody,inspectionAction;
        UnityEngine.UI.Button inspectionDone;
        Camera repairPreviewCamera;
        RenderTexture repairPreviewTarget;
        WorkshopGameMode.RepairState shownRepair;
        bool inspectingNew,previewUnderside;
        Material[] wornMaterials;
        Transform[] oldPins,previewPins,freshPins;
        Transform freshPinRoot;
        Material newPinMetal,oldPinMetal;
        Quaternion previewRotation;

        void BuildRepairParts(TMP_FontAsset font,Material paper,Material sage,Material steel,Material dark,Material brass){
            oldSwitch=new GameObject("Removed faulty switch").transform;oldSwitch.SetParent(bench,false);oldSwitch.gameObject.layer=9;
            oldSwitchModel=new GameObject("Old switch visual").transform;oldSwitchModel.SetParent(oldSwitch,false);
            oldSwitchModel.gameObject.AddComponent<MeshFilter>();oldSwitchRenderer=oldSwitchModel.gameObject.AddComponent<MeshRenderer>();
            oldSwitchHit=oldSwitch.gameObject.AddComponent<BoxCollider>();oldSwitch.gameObject.SetActive(false);
            newPinMetal=Paint("New copper switch pins",new Color(.78f,.58f,.31f));oldPinMetal=Paint("Used copper switch pins",new Color(.53f,.38f,.23f));
            oldPins=CreateSwitchPins(oldSwitch,oldPinMetal,9);
            oldPartTag=Print("Old switch tag",bench,new Vector3(-5.8f,.03f,-1.51f),"ESKİ · ARIZALI",1.25f,new Vector2(1.65f,.25f),font,WorkshopUI.Ink).gameObject;oldPartTag.SetActive(false);
            sparePacket=Shape("Replacement switch packet",bench,new Vector3(-5.8f,.043f,-2.45f),new Vector3(1.72f,.07f,1.05f),paper,.065f);
            sparePacket.gameObject.layer=9;replacementPacketHit=sparePacket.gameObject.AddComponent<BoxCollider>();((BoxCollider)replacementPacketHit).size=new Vector3(1.72f,.12f,1.05f);
            packetFlap=new GameObject("Packet fold hinge").transform;packetFlap.SetParent(sparePacket,false);packetFlap.localPosition=new Vector3(0,.05f,.41f);
            Shape("Folded paper flap",packetFlap,new Vector3(0,0,-.18f),new Vector3(1.59f,.018f,.36f),paper,.035f);
            packetSeal=Shape("Breakable packet seal",sparePacket,new Vector3(0,.075f,.14f),new Vector3(.42f,.012f,.20f),sage,.02f);
            Print("Packet seal marking",packetSeal,new Vector3(0,.009f,0),"YENİ",.85f,new Vector2(.36f,.14f),font,WorkshopUI.Paper);
            packetText=Print("Replacement packet label",sparePacket,new Vector3(0,.044f,-.36f),"YEDEK SWITCH",1.15f,new Vector2(1.50f,.20f),font,WorkshopUI.Ink);
            sparePacket.gameObject.SetActive(false);
        }
        void UpdateRepairParts(){
            if(!oldSwitch)return;
            bool repair=game.IsRepair&&game.Repair!=null;
            if(shownRepair!=game.Repair){
                shownRepair=game.Repair;ClosePartInspection();
                if(repair)CopyOldSwitch(game.Controller.Lookup[game.Repair.switchId]);
            }
            bool removed=repair&&game.Repair.diagnosed&&(!game.Controller.Lookup[game.Repair.switchId].Fitted||game.Repair.replaced);
            oldSwitch.gameObject.SetActive(removed);oldPartTag.SetActive(removed);
            if(freshPinRoot){var part=repair?game.Controller.Lookup[game.Repair.switchId]:null;freshPinRoot.gameObject.SetActive(part&&(game.Repair.spareOpened||game.Repair.replaced)&&(part.Fitted||part==game.Controller.Dragged||game.Stock.Contains(part)));}
            bool packet=repair&&(game.Repair.spareOpened||!game.Repair.replaced&&game.Shop.HasSupply(3));
            sparePacket.gameObject.SetActive(packet);
            if(!Unpacking){packetFlap.localRotation=Quaternion.Euler(game.Repair!=null&&game.Repair.spareOpened?-115:0,0,0);packetSeal.gameObject.SetActive(game.Repair==null||!game.Repair.spareOpened);}
            packetText.text=repair&&game.Repair.spareOpened?"YENİ · PAKET AÇILDI":"YEDEK SWITCH";
        }
        void CopyOldSwitch(WorkshopItem source){
            oldSwitchModel.GetComponent<MeshFilter>().sharedMesh=source.Visual.GetComponent<MeshFilter>().sharedMesh;
            oldSwitchModel.localPosition=-source.BoundsCenter;
            if(wornMaterials!=null)foreach(var material in wornMaterials){owned.Remove(material);if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            wornMaterials=source.Visual.sharedMaterials.Select(m=>new Material(m){name="Recovered worn switch finish"}).ToArray();
            foreach(var material in wornMaterials){if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",Color.Lerp(material.GetColor("_BaseColor"),new Color(.49f,.39f,.26f),.45f));owned.Add(material);}
            oldSwitchRenderer.sharedMaterials=wornMaterials;
            PositionSwitchPins(oldPins,source.BoundsSize);
            if(!freshPinRoot){freshPinRoot=new GameObject("New replacement electrical pins").transform;freshPins=CreateSwitchPins(freshPinRoot,newPinMetal,source.gameObject.layer);}
            freshPinRoot.SetParent(source.transform,false);freshPinRoot.localPosition=source.BoundsCenter;PositionSwitchPins(freshPins,source.BoundsSize);
            oldSwitch.localScale=Vector3.one*game.ProductScale;oldSwitch.localRotation=Quaternion.Euler(168,0,12);
            oldSwitch.localPosition=new Vector3(-5.8f,.14f+source.BoundsSize.y*.5f*game.ProductScale,-.65f);
            var collider=(BoxCollider)oldSwitchHit;collider.center=new Vector3(0,-.04f,0);collider.size=source.BoundsSize+new Vector3(.055f,.135f,.055f);
        }
        Transform[] CreateSwitchPins(Transform parent,Material metal,int layer){
            var pins=new Transform[2];for(int i=0;i<2;i++){pins[i]=Shape("Electrical contact pin "+i,parent,Vector3.zero,new Vector3(.021f,.08f,.029f),metal,.004f);pins[i].gameObject.layer=layer;}return pins;
        }
        static void PositionSwitchPins(Transform[] pins,Vector3 body){
            pins[0].localPosition=new Vector3(-body.x*.24f,-body.y*.5f-.038f,body.z*.22f);
            pins[1].localPosition=new Vector3(body.x*.24f,-body.y*.5f-.038f,-body.z*.12f);
        }
        public bool OpenReplacementPacket(){
            if(!initialized||Moving||Unpacking||game.Tools.Busy||!game.IsRepair||game.Repair==null)return false;
            if(!game.Repair.oldSwitchInspected){game.Menu.Toast("Önce alt kaptaki eski switch’i çevirip incele.");return false;}
            if(game.Repair.replaced||game.Repair.spareOpened){game.Menu.Toast("Yeni switch’i açık paketinden al; eskisi alt kapta kalıyor.");return false;}
            if(!game.Shop.HasSupply(3)){game.Shop.Open();return false;}
            game.Tools.Deselect();game.Controller.CancelDrag();ClosePartInspection();
            if(Application.isPlaying)StartCoroutine(UnpackReplacement());else FinishUnpacking();return true;
        }
        IEnumerator UnpackReplacement(){
            Unpacking=true;game.Audio.Play(game.Audio.Pickup,.3f);
            for(float t=0;t<.42f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.42f);packetFlap.localRotation=Quaternion.Euler(-115*f,0,0);packetSeal.localPosition=new Vector3(.64f*f,.075f,.14f);yield return null;}
            Unpacking=false;FinishUnpacking();
        }
        void FinishUnpacking(){
            game.Repair.spareOpened=true;packetSeal.localPosition=new Vector3(0,.075f,.14f);
            game.PresentStock();game.Refresh();game.SaveProgress();game.Audio.Play(game.Audio.Place,.25f);
            game.Menu.Toast("Yeni switch paketten çıktı. Takmadan önce altındaki pinleri kontrol et.");
        }
        void BuildPartInspection(Transform ui,TMP_FontAsset font){
            var card=WorkshopUI.Panel("Repair part inspection",ui,Vector2.one*.5f,new Vector2(0,70),new Vector2(690,412),WorkshopUI.Paper);partInspection=card.gameObject;WorkshopAtelierStyle.Paper(card,WorkshopUI.Paper,14);
            inspectionTitle=WorkshopUI.Text("Inspection title",card.transform,font,"ESKİ SWITCH",25,new Vector2(0,1),new Vector2(24,-20),new Vector2(602,36));
            var close=WorkshopUI.Button("Close part inspection",card.transform,font,"×",Vector2.one,new Vector2(-12,-12),new Vector2(34,34),ClosePartInspection);WorkshopAtelierStyle.Icon(close,26,"Parça incelemesini kapat",game.Experience);
            var stage=new GameObject("Repair part close-up studio").transform;stage.SetParent(bench,false);stage.localPosition=Vector3.up*3000;
            repairPreviewModel=new GameObject("Inspected switch model").transform;repairPreviewModel.SetParent(stage,false);repairPreviewModel.gameObject.layer=30;
            repairPreviewModel.gameObject.AddComponent<MeshFilter>();repairPreviewRenderer=repairPreviewModel.gameObject.AddComponent<MeshRenderer>();
            previewPins=CreateSwitchPins(repairPreviewModel,newPinMetal,30);
            var cameraObject=new GameObject("Repair part studio camera");cameraObject.transform.SetParent(stage,false);repairPreviewCamera=cameraObject.AddComponent<Camera>();repairPreviewCamera.enabled=false;repairPreviewCamera.orthographic=true;repairPreviewCamera.orthographicSize=.43f;repairPreviewCamera.cullingMask=1<<30;repairPreviewCamera.clearFlags=CameraClearFlags.SolidColor;repairPreviewCamera.backgroundColor=new Color(.76f,.79f,.68f);repairPreviewCamera.nearClipPlane=.01f;repairPreviewCamera.farClipPlane=10;
            repairPreviewCamera.transform.position=stage.position+new Vector3(1.1f,1.5f,-2.3f);repairPreviewCamera.transform.LookAt(stage.position);
            repairPreviewTarget=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32){name="Repair switch inspection",antiAliasing=4};repairPreviewTarget.Create();owned.Add(repairPreviewTarget);repairPreviewCamera.targetTexture=repairPreviewTarget;
            WorkshopUI.Art("Live inspected switch",card.transform,repairPreviewTarget,new Vector2(0,1),new Vector2(22,-74),new Vector2(258,258));
            inspectionBody=WorkshopUI.Text("Inspection findings",card.transform,font,"",21,new Vector2(0,1),new Vector2(304,-82),new Vector2(356,232));inspectionBody.textWrappingMode=TextWrappingModes.Normal;inspectionBody.enableAutoSizing=true;inspectionBody.fontSizeMin=17;inspectionBody.fontSizeMax=21;
            WorkshopUI.Button("Turn inspected switch",card.transform,font,"Altını çevir",new Vector2(0,0),new Vector2(22,22),new Vector2(258,48),TurnInspectedSwitch);
            inspectionDone=WorkshopUI.Button("Finish part inspection",card.transform,font,"İncelemeyi bitir",new Vector2(1,0),new Vector2(-22,22),new Vector2(356,48),()=>{ClosePartInspection();game.Menu.Toast(game.Repair.spareOpened?"Yeni switch’in pinleri hazır. Açık paketinden alıp yuvaya tak.":"Eski switch alt kapta kalıyor. Yeni parçanın paketini aç.");});inspectionAction=inspectionDone.GetComponentInChildren<TMP_Text>();inspectionAction.fontSize=20;
            partInspection.SetActive(false);
        }
        public bool OpenPartInspection(bool replacement=false){
            if(!initialized||!game.IsRepair||game.Repair==null||Moving||Unpacking||game.Tools.Busy||!AtRepair)return false;
            UpdateRepairParts();
            if(replacement?(!game.Repair.spareOpened||game.Repair.replaced):!oldSwitch.gameObject.activeSelf)return false;
            game.Testing.End();game.Tools.Deselect();game.Controller.CancelDrag();game.Menu.ClosePanels();game.Shop.Close();
            inspectingNew=replacement;previewUnderside=false;
            var source=game.Controller.Lookup[game.Repair.switchId];var filter=source.Visual.GetComponent<MeshFilter>();
            repairPreviewRenderer.sharedMaterials=replacement?source.Visual.sharedMaterials:oldSwitchRenderer.sharedMaterials;
            var block=new MaterialPropertyBlock();for(int i=0;i<repairPreviewRenderer.sharedMaterials.Length;i++){block.Clear();if(replacement)source.Visual.GetPropertyBlock(block,i);repairPreviewRenderer.SetPropertyBlock(block,i);}
            // Centre imported geometry at the pivot so turning it reveals the actual underside.
            var centred=Instantiate(filter.sharedMesh);centred.name="Centred switch inspection mesh";var vertices=centred.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]-=filter.sharedMesh.bounds.center;centred.vertices=vertices;centred.RecalculateBounds();
            var previous=repairPreviewModel.GetComponent<MeshFilter>().sharedMesh;
            if(previous&&previous.name=="Centred switch inspection mesh"){owned.Remove(previous);if(Application.isPlaying)Destroy(previous);else DestroyImmediate(previous);}owned.Add(centred);repairPreviewModel.GetComponent<MeshFilter>().sharedMesh=centred;
            PositionSwitchPins(previewPins,centred.bounds.size);foreach(var pin in previewPins)pin.GetComponent<MeshRenderer>().sharedMaterial=replacement?newPinMetal:oldPinMetal;
            repairPreviewModel.localScale=Vector3.one*1.25f;repairPreviewModel.localRotation=Quaternion.identity;previewRotation=Quaternion.identity;
            inspectionTitle.text=replacement?"YENİ SWITCH · PİN KONTROLÜ":"ESKİ SWITCH · İNCELEME";
            partInspection.SetActive(true);partInspection.transform.SetAsLastSibling();RefreshInspectionFinding();repairPreviewCamera.Render();return true;
        }
        public void TurnInspectedSwitch(){
            if(!DetailOpen)return;
            previewUnderside=!previewUnderside;previewRotation=Quaternion.Euler(previewUnderside?180:0,0,0);
            if(previewUnderside){if(inspectingNew)game.Repair.spareChecked=true;else game.Repair.oldSwitchInspected=true;game.SaveProgress();}
            if(!Application.isPlaying)repairPreviewModel.localRotation=previewRotation;
            RefreshInspectionFinding();repairPreviewCamera.Render();game.Refresh();lastStatus=null;
        }
        void RefreshInspectionFinding(){
            bool checkedPart=inspectingNew?game.Repair.spareChecked:game.Repair.oldSwitchInspected;
            inspectionBody.text=!previewUnderside?"Switch’i çevirip altındaki iki metal pini incele.":inspectingNew?"Pinler düz ve hizalı.\n\nBu parça yeni paketten çıktı. Pinleri soketteki deliklere hizalayıp switch’i düz bastırarak takacağız.":"Bu, cihazın yanıt alamadığı eski switch.\n\nBağlantı uçlarını inceledik. Eski parça alt kapta kalacak; yerine yeni paketteki switch’i takacağız.";
            inspectionDone.interactable=checkedPart;inspectionAction.text=checkedPart?"İncelemeyi bitir":"Önce altını incele";
        }
        public void ClosePartInspection(){if(partInspection)partInspection.SetActive(false);}
        void UpdateRepairPartMotion(){
            if(!DetailOpen)return;
            repairPreviewModel.localRotation=Quaternion.RotateTowards(repairPreviewModel.localRotation,previewRotation,380*Time.unscaledDeltaTime);repairPreviewCamera.Render();
        }
    }
}
