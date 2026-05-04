import bpy
import sys

filepath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

obj = bpy.data.objects['Mesh_0']
mesh = obj.data

leg_verts_x = [v.co.x for v in mesh.vertices if v.co.z < -0.2]
if leg_verts_x:
    print(f"Leg X range: {min(leg_verts_x)} to {max(leg_verts_x)}")
