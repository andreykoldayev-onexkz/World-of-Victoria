import bpy
import sys

filepath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

objects = []
for obj in bpy.context.scene.objects:
    objects.append(f"{obj.name} ({obj.type})")

print("OBJECTS:", objects)
