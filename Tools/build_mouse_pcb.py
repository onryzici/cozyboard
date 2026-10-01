"""Author the mouse circuit board using the keyboard's Blender -> native Unity mesh pipeline.
Run with Blender --background --python Tools/build_mouse_pcb.py.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
colors=[('PCB',(.24,.42,.32)),('Graphite',(.18,.24,.22)),('Copper',(.77,.67,.38)),('Steel',(.61,.66,.61)),('Silkscreen',(.89,.84,.69))]
mats=[]
for name,color in colors:
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);mats.append(m)
objects=[]
def part(name,center,size,mat,bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=(center[0],center[2],center[1]));o=bpy.context.object;o.name=name;o.dimensions=(size[0],size[2],size[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Moulded edge','BEVEL');mod.width=min(bevel,min(size)*.28);mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(mats[mat]);objects.append(o);return o
def route(name,points,radius,mat):
 curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=radius;curve.bevel_resolution=1
 poly=curve.splines.new('POLY');poly.points.add(len(points)-1)
 for p,c in zip(poly.points,points):p.co=(c[0],c[2],c[1],1)
 o=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(o);curve.materials.append(mats[mat]);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False);objects.append(o)
def pad(x,z,r=.038):
 bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=.007,location=(x,z,.028));o=bpy.context.object;o.data.materials.append(mats[2]);objects.append(o)
# Rounded shoulder outline, with routed board edge rather than a flat rectangular tray.
outline=[(-.48,-1.02),(-.60,-.82),(-.60,.68),(-.52,.96),(-.37,1.05),(.37,1.05),(.52,.96),(.60,.68),(.60,-.82),(.48,-1.02)]
v=[(x,z,h) for h in (-.0225,.0225) for x,z in outline];n=len(outline);f=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('Mouse routed board');mesh.from_pydata(v,[],f);mesh.update();board=bpy.data.objects.new('Mouse_PCB',mesh);bpy.context.collection.objects.link(board);board.data.materials.append(mats[0]);objects.append(board)
bpy.context.view_layer.objects.active=board;board.select_set(True)
bevel=board.modifiers.new('Routed edge','BEVEL');bevel.width=.017;bevel.segments=3;bpy.ops.object.modifier_apply(modifier=bevel.name);board.select_set(False)
# Actual cut-out accepts the optical module; it is not a printed square.
cut=part('Optical service opening',(0,0,0),(.42,.20,.40),1,.025);objects.remove(cut)
bpy.context.view_layer.objects.active=board;mod=board.modifiers.new('Optical module socket','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cut;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
# Controller with individually soldered legs, resonator, ceramic components, USB connector.
part('Controller IC',(-.10,.059,-.52),(.27,.065,.32),1,.015)
for side in (-1,1):
 for i in range(6):part('Controller solder leg',(-.10+side*.17,.031,-.65+i*.052),(.068,.017,.025),3,.002)
part('Quartz resonator',(.33,.048,-.55),(.17,.048,.26),3,.020)
for x,z in [(.31,-.18),(-.36,-.22),(.33,.35),(-.34,.43),(.15,-.83),(-.29,-.84)]:
 part('Ceramic SMD body',(x,.041,z),(.07,.035,.13),4,.004)
 for end in (-1,1):part('SMD end contact',(x,.041,z+end*.052),(.075,.036,.025),3,.002)
part('USB socket body',(0,.054,.91),(.30,.065,.19),4,.018)
part('USB socket recess',(0,.089,.91),(.20,.012,.07),1,.005)
for x in (-.085,-.028,.028,.085):part('USB terminal',(x,.096,.91),(.025,.008,.07),2,.002)
# Micro-switch solder footprints; matching the actual left and right switch mounting slots.
for side in (-1,1):
 for z in (.59,.74,.89):pad(side*.40,z,.029)
 for x,z in [(side*.4,.59),(side*.4,.89)]:
  route('Signal trace',[(x,.027,z),(x-side*.09,.027,z-.08),(side*.24,.027,z-.08),(side*.24,.027,-.36),(-.1+side*.17,.027,-.44)],.0055,2)
# Short signal routes and vias lead between actual components instead of parallel stripes.
for side in (-1,1):
 for i in range(4):
  x=-.10+side*.17;z=-.65+i*.052;endx=side*(.34+i*.046);endz=-.88+i*.09
  route('Controller fanout',[(x,.027,z),(x+side*.05,.027,z),(endx,.027,z-.09),(endx,.027,endz)],.004,2);pad(endx,endz,.018)
for x,z in [(-.52,-.76),(.52,-.76),(-.50,.43),(.50,.43)]:
 pad(x,z,.052);part('Mounting dark centre',(x,.033,z),(.042,.008,.042),1,.015)
# Four board support bosses match the base's seating height, as in the existing keyboard.
for x,z in [(-.49,-.74),(.49,-.74),(-.48,.39),(.48,.39)]:part('PCB standoff',(x,-.048,z),(.12,.055,.12),4,.035)
# Underside socket reaches the sensor's pins; four walls leave its aperture clear.
for x in (-.20,.20):part('Optical connector rail',(x,-.065,0),(.03,.12,.34),1,.005)
for z in (-.18,.18):part('Optical connector brace',(0,-.065,z),(.40,.12,.03),1,.005)
for side in (-1,1):
 for i in range(4):part('Sensor spring contact',(side*.17,-.082,-.12+i*.08),(.036,.092,.021),3,.003)
# Legible silkscreen uses geometry and the same material slot as the keyboard's PCB print.
def text(value,x,z,size=.055):
 curve=bpy.data.curves.new('PCB legend','FONT');curve.body=value;curve.size=size;curve.extrude=.0006
 o=bpy.data.objects.new('PCB legend '+value,curve);bpy.context.collection.objects.link(o);o.location=(x,z,.028);curve.materials.append(mats[4]);bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o.select_set(False);objects.append(o)
text('AVUC / REV A',-.44,-.96,.060);text('USB',-.10,.78);text('L',-.48,.99);text('R',.38,.99);text('U1',-.25,-.32,.042)
# Apply smooth bevel normals and export evaluated loop triangles, preserving material boundaries.
verts=[];normals=[];uvs=[];subs=[{'indices':[]} for _ in mats]
for o in objects:
 if o.type!='MESH':continue
 o.data.calc_loop_triangles();ns=o.data.corner_normals
 for tri in o.data.loop_triangles:
  start=len(verts)//3;mat=mats.index(o.data.materials[tri.material_index])
  for li in tri.loops:
   p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;normal=o.matrix_world.to_3x3()@ns[li].vector;normal.normalize()
   verts.extend((round(p.x,7),round(p.z,7),round(p.y,7)));normals.extend((round(normal.x,7),round(normal.z,7),round(normal.y,7)));uvs.extend((p.x/1.24+.5,p.y/2.15+.5))
  subs[mat]['indices'].extend((start,start+2,start+1))
out=ROOT/'Assets/CozyBoard/SourceData/mouse-pcb.json';out.write_text(json.dumps({'vertices':verts,'normals':normals,'uv':uvs,'submeshes':subs},separators=(',',':')))
source=ROOT/'SourceArt';source.mkdir(exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(source/'mouse-pcb.blend'))
print('MOUSE_PCB_AUTHORED',len(verts)//3,'vertices',len(objects),'components')

# Moulded enclosure parts use the same authoring/export pipeline, with real openings.
def half_width(z):
 if z<-.8:return .80*math.sqrt(max(0,1-((z+.8)/.62)**2))
 if z>.95:return .73*math.sqrt(max(0,1-((z-.95)/.47)**2))
 return .80+(.73-.80)*(z+.80)/1.75-.035*math.exp(-((z+.15)/.42)**2)
def shell_height(x,z):
 hump=max(0,math.sin((z+1.42)/2.84*math.pi))**.8
 return .16+.50*hump*math.sqrt(max(0,1-(x/max(.02,half_width(z)))**2))
def export_one(o,name,slot=(0,0,0)):
 o.data.calc_loop_triangles();v=[];n=[];uv=[];t=[]
 for tri in o.data.loop_triangles:
  a=len(v)//3
  for li in tri.loops:
   p=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;no=o.matrix_world.to_3x3()@o.data.corner_normals[li].vector;no.normalize()
   v.extend((p.x-slot[0],p.z-slot[1],p.y-slot[2]));n.extend((no.x,no.z,no.y));uv.extend((p.x/1.7+.5,p.y/2.84+.5))
  t.extend((a,a+2,a+1))
 (ROOT/'Assets/CozyBoard/SourceData'/('mouse-'+name+'.json')).write_text(json.dumps({'vertices':v,'normals':n,'uv':uv,'submeshes':[{'indices':t}]},separators=(',',':')))
 print('MOUSE_ENCLOSURE_AUTHORED',name,len(v)//3)
def boolean(o,cut,name):
 bpy.context.view_layer.objects.active=o;m=o.modifiers.new(name,'BOOLEAN');m.operation='DIFFERENCE';m.object=cut;bpy.ops.object.modifier_apply(modifier=m.name);bpy.data.objects.remove(cut,do_unlink=True)
# A real service bay, a cable exit and four recessed screw bores in the bottom moulding.
n=96;outline=[]
for i in range(n):
 a=i*math.pi*2/n;z=math.sin(a)*1.42;x=(-1 if math.cos(a)<0 else 1)*half_width(z);outline.append((x,z))
v=[(x,z,y) for y in (.012,.13) for x,z in outline];f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('Moulded base');mesh.from_pydata(v,[],f);mesh.update();base=bpy.data.objects.new('Mouse_Base',mesh);bpy.context.collection.objects.link(base);base.data.materials.append(mats[4]);
# Recalculate the outward face orientation before doing solid Boolean operations.
import bmesh
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=1,depth=.5,location=(0,0,.06));hole=bpy.context.object;hole.scale=(.30,.33,1);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);boolean(base,hole,'Optical service bay')
cut=part('Cable exit cutter',(0,.10,1.32),(.19,.30,.47),1,.012);objects.remove(cut);boolean(base,cut,'Cable strain relief socket')
for x in (-.45,.45):
 for z in (-.91,.87):
  bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.075,depth=.18,location=(x,z,.0));boolean(base,bpy.context.object,'Screw recess')
bpy.context.view_layer.objects.active=base;bevel=base.modifiers.new('Moulded edge roll','BEVEL');bevel.width=.009;bevel.segments=3;bpy.ops.object.modifier_apply(modifier=bevel.name)
export_one(base,'base')
# Continuous compound-curved upper shell and individually moving buttons.
for name,minz,maxz,side,slot in [('palm',-1.416,.26,0,(0,.14,-.57)),('left',.285,1.416,-1,(-.43,.30,.64)),('right',.285,1.416,1,(.43,.30,.64))]:
 rows=44;cols=28;v=[];f=[]
 for r in range(rows+1):
  z=minz+(maxz-minz)*r/rows;w=half_width(z)-.006
  gap=.012+.108*math.sqrt(max(0,1-((z-.76)/.37)**2))
  # A low cable socket leaves the upper seam thin and uninterrupted.
  lo=gap if side==1 else -w;hi=-gap if side==-1 else w
  for c in range(cols+1):
   x=lo+(hi-lo)*c/cols;v.append((x,z,shell_height(x,z)))
 for r in range(rows):
  for c in range(cols):
   a=r*(cols+1)+c;b=a+cols+1;f.append((a,a+1,b+1,b))
 # Outer skirts seat on the bottom shell, rather than leaving floating panel edges.
 edges=[]
 if side<=0:edges.append([r*(cols+1) for r in range(rows+1)])
 if side>=0:edges.append([r*(cols+1)+cols for r in range(rows+1)])
 if side==0:edges.append(list(range(cols+1)))
 else:edges.append([rows*(cols+1)+c for c in range(cols+1)])
 for edge in edges:
  low=[]
  for idx in edge:
   x,z,h=v[idx];low.append(len(v));v.append((x,z,.133))
  for i in range(len(edge)-1):f.append((edge[i],low[i],low[i+1],edge[i+1]))
 mesh=bpy.data.meshes.new(name+' moulded surface');mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new('Mouse_'+name,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(mats[4]);
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
 bpy.context.view_layer.objects.active=o
 solid=o.modifiers.new('Real shell thickness','SOLIDIFY');solid.thickness=.035;solid.offset=-1;bpy.ops.object.modifier_apply(modifier=solid.name)
 bevel=o.modifiers.new('Soft panel lip','BEVEL');bevel.width=.005;bevel.segments=2;bevel.limit_method='ANGLE';bevel.angle_limit=.55;bpy.ops.object.modifier_apply(modifier=bevel.name)
 for poly in o.data.polygons:poly.use_smooth=True
 export_one(o,name,slot)
bpy.ops.wm.save_as_mainfile(filepath=str(source/'mouse-components.blend'))
