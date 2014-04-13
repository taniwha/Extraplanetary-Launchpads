#! /bin/sh

full_version=`git describe --tags --abbrev=4 --match="v*" | sed -e s/^v//`
version=`echo $full_version | sed -e 's/-/\t/' | cut -f 1`

sed -e "s/@FULL_VERSION@/$full_version/" -e "s/@VERSION@/$version/" AssemblyInfo.in > AssemblyInfo.cs-
sed -e "s/@FULL_VERSION@/$full_version/" -e "s/@VERSION@/$version/" AssemblyInfoToolbar.in > AssemblyInfoToolbar.cs-

cmp -s AssemblyInfo.cs AssemblyInfo.cs- || mv AssemblyInfo.cs- AssemblyInfo.cs
cmp -s AssemblyInfoToolbar.cs AssemblyInfoToolbar.cs- || mv AssemblyInfoToolbar.cs- AssemblyInfoToolbar.cs

rm -f *.cs-
