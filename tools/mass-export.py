import bpy
import os
from io_object_mu.export_mu import export_object, strip_nnn
from io_object_mu.export_mu import enable_collections, restore_collections

textures = set()

blend_filepath = bpy.context.blend_data.filepath
blend_filepath = os.path.dirname(blend_filepath)
print(blend_filepath)
collections = enable_collections()
try:
    for obj in bpy.data.objects:
        if not obj.hide_render and not obj.parent and obj.children:
            name = strip_nnn(obj.name)+".mu"
            filepath = os.path.join(blend_filepath, name)
            print(name, filepath)

            mu = export_object (obj, filepath)
            for m in mu.messages:
                print(m)
            for tex in mu.textures:
                textures.add(tex.name)
finally:
    restore_collections(collections)

for tex in textures:
    if tex not in bpy.data.images:
        continue
    image = bpy.data.images[tex]
    name = tex + ".png"
    path = os.path.join(blend_filepath, name)
    print(tex, image.type, path)
    image.filepath = "//" + name
    image.save()
