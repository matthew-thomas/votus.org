#!/usr/bin/env bash

echo Starting build dependency provisioning!

set -o errexit
set -o nounset
set -o pipefail

echo
echo Bash shell configured!
echo

PACKAGES_FOLDER='packages'
PACKAGES_PATH="$(readlink -f $PACKAGES_FOLDER)"
SHELL_SCRIPTS_PATH="$(readlink -f ./src/shell-scripts)"
NODE_TOOLS_PATH="$(readlink -f $SHELL_SCRIPTS_PATH/node.sh)"
source $NODE_TOOLS_PATH
echo Script $NODE_TOOLS_PATH loaded!
mkdir -p $PACKAGES_PATH
echo Folder \'$PACKAGES_PATH\' created/exists!
echo

provision_node 'v6.3.1' $PACKAGES_PATH

echo $node $($node --version)
echo $npm $($npm --version)