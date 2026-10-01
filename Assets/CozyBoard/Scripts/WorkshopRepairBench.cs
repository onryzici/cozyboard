using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CozyBoard {
    // Stations share the same scene and product state; only the camera travels between them.
    [DefaultExecutionOrder(60)]
    public sealed partial class WorkshopRepairBench : MonoBehaviour {
        public static readonly Vector3 Origin=new(23,0,0);
        public bool AtRepair{get;private set;}
        public bool Moving{get;private set;}
        public Vector3 ViewOrigin=>AtRepair?Origin:Vector3.zero;
        public Vector3 ProductOrigin=>game.IsRepair?Origin:Vector3.zero;
        public bool AwayFromProduct=>AtRepair!=game.IsRepair;
        WorkshopGameMode game;
        Transform bench;
        GameObject navigation,jobCard;
        TMP_Text jobTitle,jobBody,stepText,actionText,deviceText;
        UnityEngine.UI.Button assemblyButton,repairButton,actionButton;
        readonly Dictionary<WorkshopItem,(Vector3 position,Quaternion rotation)> toolHomes=new();
        readonly List<Object> owned=new();
        Collider testerHit,benchHit;
        bool initialized;
        string lastStatus;

        public void Initialize(WorkshopGameMode owner,Transform ui,TMP_FontAsset font){
            if(initialized)return;initialized=true;game=owner;
            foreach(string id in new[]{"Screwdriver","KeyPuller","Tweezers"}){
                var item=game.Controller.Lookup[id];toolHomes[item]=(item.transform.position,item.transform.rotation);
            }
            BuildBench(font);BuildUI(ui,font);SyncProduct(true);
        }
        public void SyncProduct(bool restoreView=false){
            if(!initialized)return;
            ClosePartInspection();if(restoreView){StopAllCoroutines();Unpacking=false;}
            var board=game.Controller.Lookup["Case"].transform;
            // Legacy saves used the assembly desk for repairs; migrate the pose once.
            if(game.IsRepair&&board.position.x<12)board.position+=Origin;
            if(!game.IsRepair&&board.position.x>12)board.position-=Origin;
            foreach(var pair in toolHomes){
                pair.Key.transform.SetPositionAndRotation(game.IsRepair?Origin+pair.Value.position+Vector3.forward*.8f:pair.Value.position,
                    pair.Value.rotation);
            }
            if(restoreView||!game.IsRepair){StopAllCoroutines();Moving=false;AtRepair=game.IsRepair;SetCamera(ViewOrigin,21,75);}
            game.PresentStock();lastStatus=null;RefreshInstrument();
        }
        public bool Visit(bool repair,bool animate=true){
            if(!initialized||game.ScreenChangeBlocked||game.Painter.Editing)return false;
            game.Controller.CancelDrag();if(game.MouseProduct)game.MouseProduct.CloseUnderbody();if(game.Tools)game.Tools.CloseKeyboardUnderbody();game.Testing.End();game.Tools.Deselect();game.Shop.EndLaptopMove(true);game.Shop.Close();game.Menu.ClosePanels();game.Menu.SelectTool(0);game.Experience.EndInspection();
            AtRepair=repair;
            if(animate&&Application.isPlaying)StartCoroutine(Travel(ViewOrigin));else SetCamera(ViewOrigin,21,75);
            lastStatus=null;return true;
        }
        IEnumerator Travel(Vector3 target){
            Moving=true;var controller=game.Controller;var from=controller.ViewTarget;float width=controller.ViewWidth,pitch=controller.Pitch;
            for(float t=0;t<.9f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.9f);SetCamera(Vector3.Lerp(from,target,f),Mathf.Lerp(width,21,f)+Mathf.Sin(f*Mathf.PI)*2,Mathf.Lerp(pitch,75,f));yield return null;}
            SetCamera(target,21,75);Moving=false;
        }
        void SetCamera(Vector3 target,float width,float pitch){game.Controller.ViewTarget=target;game.Controller.ViewWidth=width;game.Controller.Pitch=pitch;game.Controller.Yaw=0;game.Controller.UpdateCamera();}
        public bool ShowsRecoveredCap(WorkshopItem item)=>game.IsRepair&&game.Repair!=null&&item.Id==game.Repair.capId&&!item.Fitted;
        public void ArrangeParts(){
            if(!initialized)return;UpdateRepairParts();
            if(!game.IsRepair||game.Repair==null)return;
            foreach(string id in new[]{game.Repair.capId,game.Repair.switchId}){
                var part=game.Controller.Lookup[id];if(part.Fitted||part==game.Controller.Dragged)continue;
                part.transform.SetParent(game.Controller.PartsRoot,true);part.transform.localScale=Vector3.one*game.ProductScale;part.transform.rotation=Quaternion.identity;
                part.transform.position=Origin+new Vector3(-5.8f,.12f+(part.Stage==3?.08f*game.ProductScale:0)-(part.BoundsCenter.y-part.BoundsSize.y*.5f)*game.ProductScale,part.Stage==4?1.05f:-2.45f);
            }
        }
        public void NextRepairAction(){
            if(!game.IsRepair){game.Shop.OpenOrders();return;}
            if(!AtRepair){Visit(true);return;}
            if(game.Testing.Passed){game.Deliver();return;}
            if(game.Repair==null)return;
            if(!game.Repair.diagnosed||game.Repair.replaced&&game.Completed){game.Testing.Begin();return;}
            var cap=game.Controller.Lookup[game.Repair.capId];var sw=game.Controller.Lookup[game.Repair.switchId];
            if(game.Repair.replaced){game.Tools.Deselect();game.Menu.SelectTool(0);game.Menu.Toast(game.RepairInstruction());return;}
            if(cap.Fitted)game.Tools.TrySelect(game.Controller.Lookup["KeyPuller"]);
            else if(sw.Fitted)game.Tools.TrySelect(game.Controller.Lookup["Tweezers"]);
            else if(!game.Repair.oldSwitchInspected)OpenPartInspection();
            else if(!game.Shop.HasSupply(3))game.Shop.Open();
            else if(!game.Repair.spareOpened)OpenReplacementPacket();
            else if(!game.Repair.spareChecked)OpenPartInspection(true);
            else {game.Tools.Deselect();game.Menu.SelectTool(0);game.Menu.Toast(game.Repair.replaced?"Sakladığın tuş kapağını tepsiden alıp yerine tak.":"Yeni switch’i açılmış paketinden alıp boş yuvaya yerleştir.");}
        }
        string StatusKey()=>!game.IsRepair?"empty":game.Testing.Passed?"ready":game.Repair==null||!game.Repair.diagnosed?"diagnose":game.Repair.replaced?(game.Completed?"test":game.Controller.Lookup[game.Repair.switchId].Fitted?"cap":"refit-switch"):game.Controller.Lookup[game.Repair.capId].Fitted?"remove-cap":game.Controller.Lookup[game.Repair.switchId].Fitted?"remove-switch":!game.Repair.oldSwitchInspected?"inspect-old":!game.Shop.HasSupply(3)?"supply":!game.Repair.spareOpened?"open-spare":!game.Repair.spareChecked?"check-spare":"replace";
        void RefreshCard(){
            string state=StatusKey();if(state==lastStatus)return;lastStatus=state;
            jobTitle.text=game.IsRepair?game.CurrentRequest.Name+"'nin klavyesi":"Her eşyanın bir ömrü daha var.";
            jobBody.text=game.IsRepair?game.CurrentOrderMessage:"Tamir siparişlerini laptopta bulabilirsin. Ürün geldiğinde burada birlikte inceleyelim.";
            stepText.text=state switch{
                "diagnose"=>"01 / TEŞHİS\nÜç komşu tuşu dene. Hangisi yanıt vermiyor?",
                "remove-cap"=>"02 / SÖKÜM\nArızalı tuşun kapağını sökücüyle çıkar.",
                "remove-switch"=>"02 / SÖKÜM\nŞimdi switch sökücüyle arızalı parçayı al.",
                "inspect-old"=>"03 / PARÇA İNCELEME\nEski switch’i çevirip bağlantı pinlerine bak.",
                "check-spare"=>"04 / PİN KONTROLÜ\nYeni switch’i çevir. İki pin de düz ve hizalı olmalı.",
                "open-spare"=>"04 / YENİ PARÇA\nMühürlü paketi aç. Eski switch alt kapta kalacak.",
                "supply"=>"04 / YENİ PARÇA\nTek bir yedek switch sipariş et.",
                "replace"=>"05 / MONTAJ\nPaketindeki yeni switch’i boş yuvaya tak.",
                "refit-switch"=>"05 / MONTAJ\nYeni switch’i yeniden boş yuvaya yerleştir.",
                "cap"=>"05 / MONTAJ\nSakladığın tuş kapağını yerine yerleştir.",
                "test"=>"06 / SON KONTROL\nÜç tuşu yeniden dene. Hepsi çalışmalı.",
                "ready"=>"TAMİR TAMAMLANDI\nEski klavye yeniden hazır. Özenle paketleyelim.",
                _=>"TAMİR MASASI\nİncele, onar, yeniden kullan."};
            actionText.text=state switch{"empty"=>"Tamir işi bul","diagnose" or "test"=>"Test cihazını aç","remove-cap"=>"Tuş sökücüyü al","remove-switch"=>"Switch sökücüyü al","inspect-old"=>"Eski switch’i incele","open-spare"=>"Yeni paketi aç","check-spare"=>"Yeni pinleri incele","supply"=>"Yedek parça al","ready"=>"Paketlemeye geç",_=>"Parçayı yerleştir"};
        }
        void Update(){
            if(!initialized)return;
            UpdateInstrumentMotion();UpdateRepairPartMotion();
            bool visible=!DetailOpen&&!Unpacking&&!game.Experience.MainVisible&&!game.Experience.Packing&&!game.Experience.Inspecting&&!game.Painter.Editing&&!(game.StoryUI&&game.StoryUI.Open)&&!game.Shop.IsOpen&&!game.Menu.OrderPanel.activeSelf&&!game.Menu.SettingsPanel.activeSelf;
            navigation.SetActive(visible);jobCard.SetActive(visible&&AtRepair&&!Moving&&!game.Testing.Active&&!game.Tools.Active);
            assemblyButton.interactable=!Moving&&AtRepair;repairButton.interactable=!Moving&&!AtRepair;
            repairButton.GetComponentInChildren<TMP_Text>().text=game.IsRepair?"Tamir masası · 1 iş  →":"Tamir masası  →";
            actionButton.interactable=!Moving&&!game.Tools.Busy;
            game.Menu.SaveButton.interactable=game.Menu.OrderButton.interactable=game.Menu.SettingsButton.interactable=!Moving&&!Unpacking&&!DetailOpen;RefreshCard();
            if(!visible||Moving||game.Controller.Dragged)return;
            var mouse=Mouse.current;if(mouse==null||!mouse.leftButton.wasPressedThisFrame||EventSystem.current&&EventSystem.current.IsPointerOverGameObject())return;
            if(Physics.Raycast(game.Controller.ViewCamera.ScreenPointToRay(mouse.position.ReadValue()),out var hit,100,1<<9)){
                if(hit.collider==testerHit&&AtRepair)UseTestInstrument();
                else if(hit.collider==oldSwitchHit&&AtRepair)OpenPartInspection();
                else if(hit.collider==replacementPacketHit&&AtRepair)OpenReplacementPacket();
                else if(hit.collider==benchHit&&!AtRepair)Visit(true);
            }
        }
        void OnDestroy(){if(repairPreviewTarget)repairPreviewTarget.Release();Shader.SetGlobalVector("_WorkshopSecondarySurface",Vector4.zero);foreach(var value in owned)if(value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}}
    }
}
