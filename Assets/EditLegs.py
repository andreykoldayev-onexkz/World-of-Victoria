import bpy
import sys

filepath = sys.argv[-2]
outpath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

obj = bpy.data.objects['Mesh_0']
mesh = obj.data

# Find bounding box
min_z = min([v.co.z for v in mesh.vertices])
max_z = max([v.co.z for v in mesh.vertices])
min_x = min([v.co.x for v in mesh.vertices])
max_x = max([v.co.x for v in mesh.vertices])

print(f"Z range: {min_z} to {max_z}")
print(f"X range: {min_x} to {max_x}")

z_threshold = min_z + (max_z - min_z) * 0.45

for v in mesh.vertices:
    if v.co.z < z_threshold:
        weight = 1.0 - (v.co.z - min_z) / (z_threshold - min_z)
        weight = max(0.0, min(1.0, weight))
        
        # Move apart by 0.15 units max
        move_amount = 0.15 * weight
        
        if v.co.x > 0.05:
            v.co.x += move_amount
        elif v.co.x < -0.05:
            v.co.x -= move_amount

# Export
bpy.ops.export_scene.gltf(filepath=outpath, export_format='GLB')
print("Exported successfully.")
