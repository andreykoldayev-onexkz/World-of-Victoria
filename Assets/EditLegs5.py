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

# We want to create a gap between the legs.
# The legs are roughly below Z = -0.1
# The inner vertices of the legs are roughly between X = -0.1 and X = 0.1
# We can just move the inner vertices outwards (away from the center).

for v in mesh.vertices:
    if v.co.z < -0.1:
        # If it's an inner vertex of the right leg (X > 0 but close to 0)
        if 0.0 < v.co.x < 0.15:
            # Move it further right
            v.co.x += 0.05
        # If it's an inner vertex of the left leg (X < 0 but close to 0)
        elif -0.15 < v.co.x < 0.0:
            # Move it further left
            v.co.x -= 0.05

# Export
bpy.ops.export_scene.gltf(filepath=outpath, export_format='GLB')
print("Exported successfully.")
