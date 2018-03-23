from chemistry import *

class Resource:
    def __init__(self, name, formula, state, density):
        self.name = name
        self.formula = formula
        self.state = state
        self.density = density
        self.molecule = parse_molecule(formula)
    def mass(self):
        m = 0.0
        for e in self.molecule.keys():
            m += self.molecule[e] * elements[e]
        return m

lines = open("resources.txt", "rt").readlines()
for l in lines:
    l = l.strip()
    if not l:
        continue
    print(l)
    formula, name, state, density = l.split()
    r = Resource(name, formula, state, density)
    print(r.name, r.formula, r.mass())
