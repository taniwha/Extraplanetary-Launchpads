#! /bin/sh

full_version=`./tools/git-version-gen --prefix v .tarball-version`
version=`echo $full_version | cut -d '-' -f 1`

sed -e "s/@FULL_VERSION@/$full_version/" -e "s/@VERSION@/$version/" assembly/AssemblyInfo.in > assembly/AssemblyInfo.cs-

cmp -s assembly/AssemblyInfo.cs assembly/AssemblyInfo.cs- || mv assembly/AssemblyInfo.cs- assembly/AssemblyInfo.cs

rm -f assembly/*.cs-

MAJOR=`echo $full_version | cut -f 1 -d .`
MINOR=`echo $full_version | cut -f 2 -d .`
PATCH=`echo $full_version | cut -f 3 -d .`
BUILD=`echo $full_version | cut -f 4 -d . | cut -f 1 -d '-'`
if test -z "$BUILD"; then
	BUILD=0
fi

set `head -20 $KSPDIR/readme.txt | grep ^Version | sed -e 's/\./ /g'`
KSPMAJOR=$2
KSPMINOR=$3
KSPPATCH=$4

mkdir -p bin
cat > bin/EL.version <<EOF
{
	"NAME":"Extraplanetary Launchpads",
	"URL":"http://taniwha.org/~bill/EL.version",
	"DOWNLOAD":"http://taniwha.org/~bill/Extraplanetary_Launchpads_v$full_version.zip",
	"VERSION":{"MAJOR":$MAJOR,"MINOR":$MINOR,"PATCH":$PATCH,"BUILD":$BUILD},
	"KSP_VERSION":{"MAJOR":$KSPMAJOR,"MINOR":$KSPMINOR,"PATCH":$KSPPATCH}
}
EOF
