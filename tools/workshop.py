def badass (s, c):
    a = -2.0
    v = 2 * (1.0 - s)
    y = 1 - 2.0 * s
    return y + (v + a * c / 2) * c

def normal (s, c):
    return 1 - s * (1.0 + c * c)

for i in range(11):
    for j in range(11):
        s=i/10.0
        c=j/10.0
        p=normal(s,c)
        print "%4.1f" % p,
    print
print

for i in range(11):
    for j in range(11):
        s=i/10.0
        c=j/10.0
        p=badass(s,c)
        print "%4.1f" % p,
    print
print

for i in range(11):
    for j in range(11):
        s=i/10.0
        c=j/10.0
        p=badass(s,c)-normal(s,c)
        print "%4.1f" % p,
    print
print

for i in range(11):
    for j in range(11):
        s=i/10.0
        c=j/10.0
        p=badass(s,c)
        p+=(1+2*s)*(1-2.7**-5)
        print "%4.1f" % p,
    print
print

for i in range(11):
    for j in range(11):
        s=i/10.0
        c=j/10.0
        p=normal(s,c)
        p+=(0.5+s)*(1-2.7**-5)
        print "%4.1f" % p,
    print
