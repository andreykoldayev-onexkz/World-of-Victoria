import bpy
import sys

filepath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

obj = bpy.data.objects['Mesh_0']
mesh = obj.data

center_verts = [v.co.x for v in mesh.vertices if abs(v.co.x) < 0.01 and v.co.z < 0.0]
print(f"Center verts count: {len(center_verts)}")
if center_verts:
    print(f"Center verts X: {center_verts[:10]}")
