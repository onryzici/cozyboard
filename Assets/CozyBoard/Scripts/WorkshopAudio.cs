using System.Collections;
using UnityEngine;
namespace CozyBoard {
    public sealed class WorkshopAudio : MonoBehaviour {
        public AudioSource MusicSource,EffectsSource;
        public AudioClip Music,Pickup,Snap,Place,Complete;
        public AudioClip[] Keys;
        public float MusicVolume=.32f,EffectsVolume=.8f;
        public int SwitchVoice;
        AudioLowPassFilter tone;
        int previous=-1;
        AudioSource assemblySource;
        AudioClip screwTurn,screwStop;
        void Awake(){
            var assembly=new GameObject("Assembly feedback");assembly.transform.SetParent(transform,false);
            assemblySource=assembly.AddComponent<AudioSource>();
            assemblySource.playOnAwake=false;assemblySource.spatialBlend=0;
            if(EffectsSource)assemblySource.outputAudioMixerGroup=EffectsSource.outputAudioMixerGroup;
            screwTurn=Resources.Load<AudioClip>("Assembly/ScrewTurn");
            screwStop=Resources.Load<AudioClip>("Assembly/ScrewStop");
        }
        void AssemblySound(AudioClip clip,float volume){
            if(!Application.isPlaying||!assemblySource||!clip)return;
            assemblySource.mute=EffectsSource&&EffectsSource.mute;
            assemblySource.volume=EffectsVolume;assemblySource.pitch=1;
            assemblySource.PlayOneShot(clip,volume);
        }
        public void AssemblyContact()=>AssemblySound(Pickup,.12f);
        public void AssemblySeat(int stage)=>AssemblySound(stage==3?Snap:stage==4?Place:Pickup,stage==3?.58f:.32f);
        public void ScrewTurn()=>AssemblySound(screwTurn,.42f);
        public void ScrewStop(bool tightened)=>AssemblySound(tightened?screwStop:Pickup,tightened?.52f:.20f);
        public void StopAssembly(){if(assemblySource)assemblySource.Stop();}
        void OnDisable()=>StopAssembly();
        void Start(){SetMusic(PlayerPrefs.GetFloat("CozyBoard.MusicVolume",MusicVolume));SetEffects(PlayerPrefs.GetFloat("CozyBoard.EffectsVolume",EffectsVolume));var atelierMusic=Resources.Load<AudioClip>("Music/WarmFireplace");if(atelierMusic)Music=atelierMusic;MusicSource.clip=Music;MusicSource.loop=true;MusicSource.Play();StartCoroutine(FadeMusic());}
        IEnumerator FadeMusic(){for(float t=0;t<1;t+=Time.deltaTime/3){MusicSource.volume=Mathf.SmoothStep(0,MusicVolume*.42f,t);yield return null;}MusicSource.volume=MusicVolume*.42f;}
        public void SetMusic(float v){MusicVolume=Mathf.Clamp01(v);if(MusicSource)MusicSource.volume=MusicVolume*.42f;PlayerPrefs.SetFloat("CozyBoard.MusicVolume",MusicVolume);}
        public void SetEffects(float v){EffectsVolume=Mathf.Clamp01(v);if(EffectsSource)EffectsSource.volume=EffectsVolume;if(assemblySource)assemblySource.volume=EffectsVolume;PlayerPrefs.SetFloat("CozyBoard.EffectsVolume",EffectsVolume);}
        public void Play(AudioClip clip,float volume=1){if(!Application.isPlaying||!EffectsSource||!clip)return;EffectsSource.pitch=1;EffectsSource.PlayOneShot(clip,volume);}
        public void Key(float volume=.5f){if(Keys==null||Keys.Length==0)return;int index=Random.Range(0,Keys.Length);if(index==previous)index=(index+1)%Keys.Length;previous=index;Play(Keys[index],volume*(SwitchVoice==0?.72f:1));if(EffectsSource){if(!tone)tone=EffectsSource.gameObject.GetComponent<AudioLowPassFilter>();if(!tone)tone=EffectsSource.gameObject.AddComponent<AudioLowPassFilter>();tone.cutoffFrequency=SwitchVoice==0?2600:SwitchVoice==1?6500:18000;EffectsSource.pitch=SwitchVoice==0?.88f:SwitchVoice==1?1.02f:1.19f;if(SwitchVoice==2&&Snap)EffectsSource.PlayOneShot(Snap,volume*.18f);}}
    }
}
