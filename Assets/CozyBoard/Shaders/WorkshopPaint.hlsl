#ifndef COZY_WORKSHOP_PAINT
#define COZY_WORKSHOP_PAINT
float paintHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float paintNoise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(paintHash(q),paintHash(q+float2(1,0)),f.x),lerp(paintHash(q+float2(0,1)),paintHash(q+1),f.x),f.y);}
float pigment(float2 p){
 float2 brush=float2(p.x*.86+p.y*.5,p.y*.86-p.x*.5);
 return (paintNoise(brush*float2(3,14))-.5)*.016+(paintNoise(brush*float2(14,60))-.5)*.004+(paintNoise(p*.8)-.5)*.019;
}
// Both desks receive the same painted window light in their own local space.
// Zero bounds leave the original single-desk appearance unchanged.
float4 _WorkshopSecondarySurface;
float windowShade(float2 p){
 float2 local=p-_WorkshopSecondarySurface.xy;
 if(_WorkshopSecondarySurface.z>0 && abs(local.x)<_WorkshopSecondarySurface.z && abs(local.y)<_WorkshopSecondarySurface.w) p=local;
 // A single unseen window to the upper left. Parallel mullions, soft painted edges.
 float2 q=float2(p.x*.79-p.y*.61,p.x*.61+p.y*.79);
 float wobble=(paintNoise(p*3)-.5)*.035;
 float outer=smoothstep(3.8,3.94,q.x+wobble);
 float frame=(1-smoothstep(.07,.14,abs(q.x+5.6+wobble)))*.40;
 float cross=(1-smoothstep(.045,.11,abs(q.y-2.7+wobble)))*.33;
 float left=1-smoothstep(-8.7,-8.55,q.x);
 return max(max(outer*.63,left*.57),max(frame,cross));
}
half3 windowColor(half3 color,float2 p){return lerp(color,color*half3(.67,.55,.55),windowShade(p));}
#endif
