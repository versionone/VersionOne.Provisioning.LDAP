#!/bin/bash -xe

# ----- Variables -------------------------------------------------------------
# Variables in the build.properties file will be available to Jenkins
# build steps. Variables local to this script can be defined below.
. ./build.properties



# -----------------------------------------------------------------------------

# fix for jenkins inserting the windows-style path in $WORKSPACE
cd "$WORKSPACE"
export WORKSPACE=`pwd`



# ----- Utility functions -----------------------------------------------------

function winpath() {
  # Convert gitbash style path '/c/Users/Big John/Development' to 'c:\Users\Big John\Development',
  # via dumb substitution. Handles drive letters; incurs process creation penalty for sed.
  if [ -e /etc/bash.bashrc ] ; then
    # Cygwin specific settings
    echo "`cygpath -w $1`"
  else
    # Msysgit specific settings
    echo "$1" | sed -e 's|^/\(\w\)/|\1:\\|g;s|/|\\|g'
  fi
}

function bashpath() {
  # Convert windows style path 'c:\Users\Big John\Development' to '/c/Users/Big John/Development'
  # via dumb substitution. Handles drive letters; incurs process creation penalty for sed.
  if [ -e /etc/bash.bashrc ] ; then
    # Cygwin specific settings
    echo "`cygpath $1`"
  else
    # Msysgit specific settings
    echo "$1" | sed -e 's|\(\w\):|/\1|g;s|\\|/|g'
  fi
}

function parentwith() {  # used to find $WORKSPACE, below.
  # Starting at the current dir and progressing up the ancestors,
  # retuns the first dir containing $1. If not found returns pwd.
  SEARCHTERM="$1"
  DIR=`pwd`
  while [ ! -e "$DIR/$SEARCHTERM" ]; do
    NEWDIR=`dirname "$DIR"`
    if [ "$NEWDIR" = "$DIR" ]; then
      pwd
      return
    fi
    DIR="$NEWDIR"
  done
  echo "$DIR"
}



# ----- Default values --------------------------------------------------------
# If we aren't running under jenkins some variables will be unset.
# So set them to a reasonable value.

if [ -z "$WORKSPACE" ]; then
  export WORKSPACE=`parentwith .git`;
fi

if [ -z "$VERSION_NUMBER" ]; then
  export VERSION_NUMBER="0.0.0"
fi

if [ -z "$BUILD_NUMBER" ]; then
  # presume local workstation, use date-based build number
  export BUILD_NUMBER=`date +%H%M`  # hour + minute
fi

if [ -z "$BUILD_TAG" ]; then
  export BUILD_TAG="${VERSION_NUMBER}.${BUILD_NUMBER}"
fi

if [ -z "$NUNIT_RUNNER_NAME" ]
then
  NUNIT_RUNNER_NAME="nunit-console.exe"
fi

if [ -z "$NUNIT_XML_OUTPUT" ]
then
  NUNIT_XML_OUTPUT="nunit-result.xml"
fi



# ---- Run Tests --------------------------------------------------------------------------

# Make sure the nunit-console is available first...
NUNIT_CONSOLE_RUNNER=`/usr/bin/find packages | grep "${NUNIT_RUNNER_NAME}\$"`
if [ -z "$NUNIT_CONSOLE_RUNNER" ]
then
  echo "Could not find $NUNIT_RUNNER_NAME in the $WORKSPACE/packages folder."
  exit -1
fi

TEST_LIBS=""
for TEST_DIR in $WORKSPACE/*.Tests; do
  if [ -d $TEST_DIR ] ; then
    for TEST_DLL in $TEST_DIR/bin/$Configuration/*.Tests.dll; do
      if [ -f $TEST_DLL ] ; then
        TEST_LIBS="$TEST_LIBS `winpath $TEST_DLL`"
      fi
    done
  fi
done

if [ -e /etc/bash.bashrc ] ; then
  # Cygwin specific settings
  $NUNIT_CONSOLE_RUNNER \
    -framework:net-4.0 \
    -labels \
    -stoponerror \
    -xml=$NUNIT_XML_OUTPUT \
    $TEST_LIBS
else
  # Msysgit specific settings
  $NUNIT_CONSOLE_RUNNER \
    //framework:net-4.0 \
    //labels \
    //stoponerror \
    //xml=$NUNIT_XML_OUTPUT \
    $TEST_LIBS
fi


