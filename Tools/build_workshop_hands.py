"""Original CozyBoard glove poses and stoneware screw dish. No third-party assets.
Blender --background --python Tools/build_workshop_hands.py
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
def sphere(name,p,scale):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=(p[0],p[2],p[1]))
 o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return o
def tube(name,points,r):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=3;c.resolution_u=12
 s=c.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
 for b,p in zip(s.bezier_points,points):b.co=(p[0],p[2],p[1]);b.handle_left_type='AUTO';b.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,c);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False)
 return [o,sphere(name+' tip',points[-1],(r,r,r))]
def export(name,objects):
 v=[];n=[];uv=[];ids=[[],[]] if name=="dish" else [[]]
 for o in objects:
  for p in o.data.polygons:p.use_smooth=True
  o.data.calc_loop_triangles();ns=o.data.corner_normals
  for tri in o.data.loop_triangles:
   k=len(v)//3
   for li in tri.loops:
    p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;normal=o.matrix_world.to_3x3()@ns[li].vector;normal.normalize()
    v.extend((p.x,p.z,p.y));n.extend((normal.x,normal.z,normal.y));uv.extend((p.x+.5,p.y+.5))
   ids[tri.material_index].extend((k,k+2,k+1))
 (ROOT/'Assets/CozyBoard/SourceData'/('workshop-'+name+'.json')).write_text(json.dumps(dict(vertices=v,normals=n,uv=uv,submeshes=[dict(indices=x) for x in ids]),separators=(',',':')))
 print(name,len(v)//3)
def hand(name,pinch=False,driver=False):
 parts=[sphere('Soft palm',(.42,.03,0),(.24,.18,.285)),sphere('Rounded wrist',(.68,.018,-.075),(.25,.15,.20))] if not driver else [sphere('Power grip palm',(.26,.015,-.09),(.18,.19,.32)),sphere('Palm heel',(.17,-.015,-.37),(.20,.17,.23)),sphere('Wrist behind handle',(.03,-.02,-.64),(.22,.16,.23))]
 for i,z in enumerate([-.215,-.075,.068,.195]):
  if driver:pts=[(.28,.025,z),(.22,-.17,z),(.015,-.265,z),(-.205,-.18,z),(-.265,.015,z),(-.19,.19,z)]
  elif pinch and i==3:pts=[(.29,.03,z),(.16,.015,.20),(.04,-.025,.16),(-.005,-.075,.055)]
  else:pts=[(.30,.01,z),(.20,-.16,z),(0,-.27,z),(-.22,-.16,z),(-.26,.07,z),(-.18,.21,z)]
  parts+=tube('Curled finger '+str(i),pts,.065 if i else .061)
 thumb=[(.28,.16,.08),(.16,.285,.24),(-.08,.27,.245),(-.22,.115,.19)] if driver else [(.45,.17,.23),(.30,.30,.245),(.085,.29,.21),(-.15,.245,.16)] if not pinch else [(.43,.16,.23),(.28,.20,.22),(.12,.17,.10),(.002,.12,.052)]
 parts+=tube('Opposing thumb',thumb,.082)
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=parts[0];o.name=name
 rem=o.modifiers.new('Continuous cloth surface','REMESH');rem.mode='VOXEL';rem.voxel_size=.014;bpy.ops.object.modifier_apply(modifier=rem.name)
 sm=o.modifiers.new('Soft padded glove','SMOOTH');sm.factor=.62;sm.iterations=5;bpy.ops.object.modifier_apply(modifier=sm.name)
 export(name,[o]);return o
hand('grip');hand('pinch',True);hand('driver-grip',driver=True)
# Revolved profile: a real concave dish with a rolled lip and a closed underside.
profile=[(0,.014),(.50,.014),(.64,.030),(.73,.105),(.77,.148),(.76,.173),(.73,.184),(.70,.174),(.66,.12),(.58,.07),(.46,.056),(0,.056)]
v=[];f=[];steps=80
for r,h in profile:
 for i in range(steps):a=i*math.tau/steps;v.append((math.cos(a)*r,math.sin(a)*r*.72,h))
for j in range(len(profile)-1):
 for i in range(steps):a=j*steps+i;b=j*steps+(i+1)%steps;f.append((a,a+steps,b+steps,b))
m=bpy.data.meshes.new('Thrown ceramic dish');m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new('Screw dish',m);bpy.context.collection.objects.link(o)
import bmesh
bm=bmesh.new();bm.from_mesh(m);bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=.0001);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(m);bm.free()
for i in range(2):o.data.materials.append(bpy.data.materials.new('Ceramic rim' if i==0 else 'Sage magnetic liner'))
for poly in m.polygons:poly.material_index=1 if poly.center.z<.075 and poly.normal.z>.3 else 0
export('dish',[o])
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SourceArt/workshop-hands.blend'))
