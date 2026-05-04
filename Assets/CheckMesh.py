import bpy
import bmesh
import sys

filepath = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=filepath)

obj = bpy.data.objects['Mesh_0']
mesh = obj.data

bm = bmesh.new()
bm.from_mesh(mesh)

# Check if there are non-manifold edges
non_manifold = [e for e in bm.edges if not e.is_manifold]
print(f"Non-manifold edges: {len(non_manifold)}")

# Check if the mesh is a single connected component
components = []
faces = set(bm.faces)
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

print(f"Connected components: {len(components)}")
