import bpy
import bmesh
import sys

filepath = sys.argv[-2]
outpath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

obj = bpy.data.objects['Mesh_0']
mesh = obj.data

bm = bmesh.new()
bm.from_mesh(mesh)

faces = set(bm.faces)
components = []
while faces:
    f = faces.pop()
    comp = {f}
    queue = [f]
    while queue:
        curr = queue.pop(0)
        for e in curr.edges:
            for linked_f in e.link_faces:
                if linked_f in faces:
                    faces.remove(linked_f)
                    comp.add(linked_f)
                    queue.append(linked_f)
    components.append(comp)

# Move components
for comp in components:
    # Find center of component
    verts = set()
    for f in comp:
        for v in f.verts:
            verts.add(v)
    
    center_x = sum(v.co.x for v in verts) / len(verts)
    center_z = sum(v.co.z for v in verts) / len(verts)
    
    # If it's a leg component
    if center_z < -0.1:
        # Move it
        if center_x > 0.01:
            # Right leg
            for v in verts:
                v.co.x += 0.08
        elif center_x < -0.01:
            # Left leg
            for v in verts:
                v.co.x -= 0.08

bm.to_mesh(mesh)
bm.free()

# Now we need to fix the gap between the torso and the legs.
# The torso is probably the component with center_z > -0.1
# We can find the vertices of the torso that are close to the legs and move them to match the legs, or we can just scale the legs up slightly on the X axis so they touch the torso again.
# Actually, the user said "Сделай явный просвет между ногами модели. Более ничего не изменяй, оставь как есть."
# This means we should only move the inner vertices of the legs, not the whole leg components.

# Let's revert the component moving and just move the inner vertices.
