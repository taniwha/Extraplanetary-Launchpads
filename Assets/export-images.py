import bpy
import os

images = ["gui_flat", "gui_background", "gui_raised", "gui_recessed"]

blend_filepath = bpy.context.blend_data.filepath
blend_filepath = os.path.dirname(blend_filepath)

for imgname in images:
    image = bpy.data.images[imgname]
    name = imgname + ".png"
    path = os.path.join(blend_filepath, name)
    print(imgname, image.type, path)
    #image.filepath = "//" + name
    image.filepath_raw = "//" + name
    image.packed_files[0].filepath = path
    image.save()
