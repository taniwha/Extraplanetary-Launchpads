elements = {
    "H":1.00794,
    "C":12.0107,
    "O":15.9994,
    "F":18.9984032,
    "Mg":24.3050,
    "Al":26.9815386,
    "Si":28.0855,
    "Cl":35.453,
    "K":39.0983,
    "Ca":40.078,
    "Ti":47.867,
    "Fe":55.845,
}

def parse_count(formula):
    i = 0
    while i < len(formula):
        if not formula[i].isdigit():
            break
        i += 1
    if i:
        count = int(formula[:i])
        formula = formula[i:]
    else:
        count = 1
    return formula, count

def parse_element(formula):
    i = 1
    while i < len(formula):
        if not formula[i].islower():
            break
        i += 1
    element = formula[:i]
    formula = formula[i:]
    return formula, element

def add_element(molecule, element, count):
    if element not in molecule:
        molecule[element] = 0
    molecule[element] += count

def parse_molecule(formula):
    molecule_stack = []
    molecule = {}
    while formula:
        if formula[0] == '(':
            molecule_stack.append(molecule)
            molecule = {}
            formula = formula[1:]
        elif formula[0] == ')':
            formula = formula[1:]
            formula, count = parse_count(formula)
            radical = molecule
            molecule = molecule_stack.pop()
            for e in radical.keys():
                add_element(molecule, e, radical[e] * count)
        elif formula[0].isupper():
            formula, element = parse_element(formula)
            formula, count = parse_count(formula)
            add_element(molecule, element, count)
        else:
            raise SyntaxError
    if molecule_stack:
        raise SyntaxError
    return molecule 
