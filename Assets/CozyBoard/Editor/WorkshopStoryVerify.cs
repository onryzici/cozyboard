using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CozyBoard.Editor {
    public static class WorkshopStoryVerify {
        const string Scene="Assets/CozyBoard/Scenes/Workbench.unity";
        public static void VerifyAndBuild(){Run();WorkshopPresentationVerify.Run();BuildWindowsForSmoke();}
        public static void BuildWindowsForSmoke(){
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="Builds/StoryVerification/CozyBoard.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded||report.summary.totalErrors!=0)throw new Exception("Story verification build failed");
            Debug.Log("COZY_STORY_BUILD_SUCCEEDED");
        }
        [MenuItem("Cozy Board/Verify story and order selection")]
        public static void Run(){
            Directory.CreateDirectory("Logs");EditorSceneManager.OpenScene(Scene);
            var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();controller.SendMessage("Start");
            var game=controller.Game;game.Experience.Initialize(game);game.VerificationSavePath=Path.GetFullPath("Logs/story-verification-save.json");
            var report=new List<string>();
            void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add("PASS "+message);}
            void Fits(Transform root){Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();var rect=text.rectTransform.rect;bool within=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible).All(x=>x.bottomLeft.x>=rect.xMin-1&&x.topRight.x<=rect.xMax+1&&x.bottomLeft.y>=rect.yMin-1&&x.topRight.y<=rect.yMax+1);Check(!text.isTextOverflowing&&within,"Text fits: "+text.name);}}
            try{
                game.Experience.NewGame();
                Check(game.Story.enabled&&!game.Story.openingRead&&game.StoryUI.Open,"New game shows Nermin's opening note");
                Check(game.Experience.Blocking&&game.WaitingForOrder&&game.Stock.Length==0,"Introduction blocks interaction and no stock appears before acceptance");
                Check(!game.CanInstall(controller.Lookup["PCB"]),"Unaccepted offers cannot be assembled");
                Check(!game.AcceptOrder(WorkshopStory.First.Id),"An offer cannot be accepted through the opening overlay");
                Fits(GameObject.Find("Notebook paper").transform);Capture(controller,"Logs/story-opening.png");
                game.StoryUI.CloseNotebook();
                Check(game.Story.openingRead&&game.Shop.IsOpen&&!game.StoryUI.Open,"Closing the opening saves it and opens first offer");
                Check(WorkshopStory.Offers(game.Story).Length==1,"Only Ece's guided first job is offered");
                Fits(GameObject.Find("Available orders").transform);Capture(controller,"Logs/story-first-offer.png");
                int stock=game.Shop.Data.stock.Sum();Check(game.AcceptOrder(WorkshopStory.First.Id),"First offer can be accepted");
                Check(game.Shop.Data.stock.Sum()==stock&&game.CurrentRequest.Name=="Ece","Acceptance keeps stock and binds Ece by customer ID");
                Check(!game.AcceptOrder("mina-keyboard"),"A second job cannot overwrite an active job");
                Check(game.Menu.OrderPanel.activeSelf&&game.Stock.Length==1,"Accepted job shows its card and PCB supply");
                Fits(game.Menu.OrderPanel.transform);
                string activeId=game.ActiveOrder.instanceId;
                var position=controller.Lookup["Case"].transform.position;game.TrySaveQuiet();
                Check(game.Menu.OrderPanel.activeSelf&&controller.Lookup["Case"].transform.position==position,"Quiet save does not close panels or move the board");
                string initial=game.SerializeProgress();game.StartStory();game.RestoreProgress(initial);
                Check(game.ActiveOrder.instanceId==activeId&&game.Story.openingRead,"Reload preserves accepted instance and opening acknowledgement");
                game.Experience.Continue();Check(!game.StoryUI.Open,"Reload does not replay acknowledged opening");
                game.Menu.ClosePanels();
                foreach(var item in controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))controller.Attach(item);
                game.Refresh();Check(game.Completed,"Existing assembly works for a story order");
                Check(!game.FinishDelivery(),"Unchecked keyboard cannot be delivered");
                game.Testing.Restore(controller.Items.Where(x=>x.Stage==4).Select(x=>x.Id).ToArray(),null,true);game.Tools.Tightened=15;
                int money=game.Shop.Data.credits;Check(game.FinishDelivery(),"Checked and screwed first job delivers successfully");
                Check(game.Story.FirstDelivered&&game.Story.events.Length==1&&game.WaitingForOrder,"Delivery records its story event once and returns to selection");
                Check(game.Shop.Data.credits==money+300&&game.DeliveryMail.Count==1,"First matching order pays exactly once");
                Check(!game.FinishDelivery()&&game.Shop.Data.credits==money+300,"Repeated delivery cannot repeat reward");
                Check(!game.Story.Record(WorkshopStory.FirstEvent)&&game.Story.events.Length==1,"Repeated narrative event is idempotent");
                var receipt=game.LastDelivery;
                Check(receipt.CustomerId=="ece"&&receipt.orderId==activeId&&receipt.Letter.Contains("pencere"),"Receipt snapshots customer, accepted instance and story letter");
                Check(WorkshopStory.Offers(game.Story).Length==3,"Three selectable keyboard offers unlock after first delivery");
                game.Shop.OpenOrders();Fits(GameObject.Find("Available orders").transform);Capture(controller,"Logs/story-orders.png");
                Check(game.AcceptOrder("mina-keyboard")&&game.OrderNumber==2,"Player can choose Mina for delivery number two");
                Check(game.Shop.Purchase(5),"Mina's requested switch set can be purchased");Check(game.CurrentRequest.Name=="Mina"&&game.CurrentFit(5).Contains("Uygun ses"),"Shop requirements follow selected customer, not old rotation");
                game.Menu.ShowOrder();Fits(game.Menu.OrderPanel.transform);
                var minaReceipt=game.CreateReceipt();Check(minaReceipt.matches==2&&minaReceipt.CustomerId=="mina","Evaluation uses Mina's request even on order two");
                game.Shop.OpenMail();Fits(GameObject.Find("Customer mailbox").transform);
                Check(GameObject.Find("Sender").GetComponent<TMP_Text>().text.StartsWith("Ece"),"Old mail still belongs to Ece while Mina is active");
                int paid=game.Shop.Data.credits;game.Shop.ReadMail(0);Check(game.Shop.Data.credits==paid,"Reading mail does not pay again");
                game.Shop.Close();game.StoryUI.OpenNotebook();Fits(GameObject.Find("Notebook paper").transform);Capture(controller,"Logs/story-notebook.png");game.StoryUI.CloseNotebook();
                string selected=game.SerializeProgress();game.RestoreProgress(selected);
                Check(game.CustomerId=="mina"&&game.Story.FirstDelivered&&game.DeliveryMail.Count==1,"Reload retains independent customer, story and mail state");
                game.StartStory();game.Story.openingRead=true;
                string waiting=game.SerializeProgress();game.RestoreProgress(waiting);Check(game.WaitingForOrder&&game.Stock.Length==0,"Unaccepted offer state survives reload without spawning materials");
                Directory.CreateDirectory("Logs/story-unwritable-target");game.VerificationSavePath=Path.GetFullPath("Logs/story-unwritable-target");
                Check(!game.AcceptOrder(WorkshopStory.First.Id)&&game.WaitingForOrder,"Failed acceptance save rolls back active job");
                game.VerificationSavePath=Path.GetFullPath("Logs/story-verification-save.json");game.AcceptOrder(WorkshopStory.First.Id);game.Menu.ClosePanels();
                WorkshopPaintVerify.Run(game,Check);
                // Paint verification finishes with a legacy migration. Its physical state is useful for all old save versions.
                var legacy=JsonUtility.FromJson<WorkshopGameMode.SaveData>(game.SerializeProgress());legacy.order=9;
                for(int version=1;version<=10;version++){
                    legacy.version=version;legacy.story=null;legacy.activeOrder=null;game.RestoreProgress(JsonUtility.ToJson(legacy));
                    Check(!game.Story.enabled&&!game.StoryUI.Open&&game.Completed&&game.OrderNumber==9&&game.CurrentRequest.Name=="Mina","Version "+version+" preserves legacy order and assembly without forced opening");
                }
                var oldMail=WorkshopOrders.Evaluate(2,4);Check(oldMail.CustomerId=="deniz"&&oldMail.Subject=="Klavyem geldi!","Legacy receipts retain original customer and generic reaction");
                var mismatch=WorkshopOrders.EvaluateCustomer(1,5,WorkshopStory.First.Accept());Check(!mismatch.Letter.Contains("Tuşları duymamış"),"Story letter does not claim silent typing for a mismatched switch");
                // Verify the complete delivery rollback, including wallet, board and narrative event.
                game.StartStory();game.Story.openingRead=true;game.AcceptOrder(WorkshopStory.First.Id);game.Menu.ClosePanels();
                foreach(var item in controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))controller.Attach(item);
                game.Testing.Restore(controller.Items.Where(x=>x.Stage==4).Select(x=>x.Id).ToArray(),null,true);game.Tools.Tightened=15;
                int beforeFailure=game.Shop.Data.credits;string beforeId=game.ActiveOrder.instanceId;game.VerificationSavePath=Path.GetFullPath("Logs/story-unwritable-target");
                Check(!game.FinishDelivery()&&game.Completed&&game.ActiveOrder.instanceId==beforeId&&!game.Story.FirstDelivered&&game.DeliveryMail.Count==0&&game.Shop.Data.credits==beforeFailure,"Failed delivery save restores assembly, wallet, mail and story transaction");
                File.WriteAllLines("Logs/story-checks.txt",report);Debug.Log("COZY_STORY_VERIFIED "+report.Count);
            }finally{game.VerificationSavePath=null;game.Painter.ResetPaint();EditorSceneManager.OpenScene(Scene);}
        }
        static void Capture(WorkshopController controller,string path){
            controller.Game.Experience.SendMessage("Update");controller.Game.Shop.SendMessage("Update");controller.Game.Menu.ToastLabel.gameObject.SetActive(false);controller.SendMessage("LateUpdate");
            var camera=controller.ViewCamera;var target=new RenderTexture(1920,1080,24){antiAliasing=4};target.Create();camera.targetTexture=target;camera.aspect=16f/9;controller.UpdateCamera();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        }
    }
}
