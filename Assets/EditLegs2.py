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

z_threshold = min_z + (max_z - min_z) * 0.45

# We want to create a gap.
# Let's find the center of each leg.
# Left leg is roughly X > 0, Right leg is X < 0.
# To create a gap, we need to move the inner vertices of the legs inwards (towards the center of the leg), or just move the whole leg outwards.
# If we move the whole leg outwards, the crotch stretches.
# To fix the stretch, we can move the inner vertices even more, or we can just scale the legs down on the X axis.

for v in mesh.vertices:
    if v.co.z < z_threshold:
        # Weight based on Z (1.0 at bottom, 0.0 at crotch)
        weight = 1.0 - (v.co.z - min_z) / (z_threshold - min_z)
        weight = max(0.0, min(1.0, weight))
        
        # If we just scale the X coordinate of the legs relative to their centers:
        # Let's say leg centers are at X = 0.2 and X = -0.2
        if v.co.x > 0.01:
            # Right leg (from character's perspective, or left on screen)
            leg_center = 0.25
            # Move towards leg center to make leg thinner, creating a gap
            v.co.x = v.co.x + (leg_center - v.co.x) * 0.3 * weight
            # And move the whole leg slightly outwards
            v.co.x += 0.05 * weight
        elif v.co.x < -0.01:
            # Left leg
            leg_center = -0.25
            v.co.x = v.co.x + (leg_center - v.co.x) * 0.3 * weight
            v.co.x -= 0.05 * weight

# Export
bpy.ops.export_scene.gltf(filepath=outpath, export_format='GLB')
print("Exported successfully.")
