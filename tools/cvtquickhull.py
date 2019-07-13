from mu import Mu, MuObject, MuTransform, MuMesh, MuTagLayer

def read_vertices(input):
    count = input.read_int()
    verts = [None] * count
    for i in range(count):
        verts[i] = input.read_vector()
    return verts

class Face:
    pass

def read_face(input):
    f = Face()
    a, b, c = input.read_int(3)
    if not a:
        f.tri = a, c, b
    else:
        f.tri = c, b, a
    f.highest = input.read_int()
    count = input.read_int()
    f.vispoints = input.read_int(count)
    return f

def read_facelist(input):
    count = input.read_int()
    faces = [None] * count
    for i in range(count):
        faces[i] = read_face(input)
    return faces

def make_transform(name):
    transform = MuTransform()
    transform.name = name
    transform.localPosition = (0, 0, 0)
    transform.localRotation = (1, 0, 0, 0)
    transform.localScale = (1, 1, 1)
    return transform

def make_empty(name):
    obj = MuObject()
    obj.transform = make_transform(name)
    obj.tag_and_layer = MuTagLayer()
    obj.tag_and_layer.tag = ""
    obj.tag_and_layer.layer = 0
    return obj

def make_tris(faces):
    tris = []
    for f in faces:
        tris.append(f.tri)
    return tris

def make_mesh(name, verts, faces):
    obj = make_empty(name)
    mesh = MuMesh()
    mesh.verts = verts
    mesh.submeshes = [make_tris(faces)]
    obj.shared_mesh = mesh
    return obj

i = 0
input = Mu()
while True:
    name = f"quickhull-{i:#05d}"
    print(name)
    input.file = open(name + ".bin", "rb");
    verts = read_vertices(input)
    faces = read_facelist(input)
    final_faces = read_facelist(input)
    point = input.read_int()
    lit_faces = read_facelist(input)
    new_faces = read_facelist(input)
    output = Mu()
    output.materials = []
    output.textures = []
    output.obj = make_empty(name)
    output.obj.children.append(make_mesh("faces", verts, faces))
    output.obj.children.append(make_mesh("final_faces", verts, final_faces))
    output.obj.children.append(make_mesh("lit_faces", verts, lit_faces))
    output.obj.children.append(make_mesh("new_faces", verts, new_faces))
    if (point >= 0):
        p = make_empty("point")
        p.transform.localPosition = verts[point]
        output.obj.children.append(p)
    output.write(name+".mu")
    i+=1
