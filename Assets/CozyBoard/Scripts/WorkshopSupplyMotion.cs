using System.Collections;
using UnityEngine;
namespace CozyBoard {
 public sealed class WorkshopSupplyMotion:MonoBehaviour {
  Vector3 home,scale;Coroutine motion;bool desired,known;WorkshopGameMode game;
  public bool Arrived{get;private set;}
  public void Initialize(WorkshopGameMode owner){game=owner;home=transform.localPosition;scale=transform.localScale;}
  public void Show(bool visible){if(known&&desired==visible)return;known=true;desired=visible;if(motion!=null)StopCoroutine(motion);Arrived=false;
   if(!Application.isPlaying){transform.localPosition=home;transform.localScale=scale;gameObject.SetActive(visible);Arrived=visible;return;}
   if(!gameObject.activeSelf&&!visible)return;
   if(visible&&!gameObject.activeSelf){transform.localPosition=home+new Vector3(home.x<0?-2.5f:2.5f,.45f,0);transform.localScale=scale*.92f;gameObject.SetActive(true);}
   motion=StartCoroutine(Animate(visible));
  }
  IEnumerator Animate(bool visible){var start=transform.localPosition;var startScale=transform.localScale;var end=visible?home:home+new Vector3(home.x<0?-3.5f:3.5f,.18f,0);float duration=visible?.65f:.48f;
   for(float time=0;time<duration;time+=Time.unscaledDeltaTime){float t=time/duration;float eased=visible?1-Mathf.Pow(1-t,3):t*t;transform.localPosition=Vector3.Lerp(start,end,eased)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.10f;transform.localScale=Vector3.Lerp(startScale,visible?scale:scale*.88f,eased);yield return null;}
   transform.localPosition=end;transform.localScale=visible?scale:scale*.88f;motion=null;Arrived=visible;if(!visible)gameObject.SetActive(false);else game.Audio.Play(game.Audio.Place,.20f);game.Refresh();
  }
 }
}
