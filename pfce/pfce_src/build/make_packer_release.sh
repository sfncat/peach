#!/bin/bash

set -ex

. build/common.sh

usage() {
    echo "USAGE: make_packer_release.sh --buildtag 1.2.3 --settings /path/to/packersecrets/file --filesdir /path/to/files [--publish true|false]"
}

# packer comes from: https://www.packer.io/downloads.html
# ovatool comes from: https://www.vmware.com/support/developer/ovf/
# ovftool is implicitly run via packer when building on esxi
# ovftool is run by this script when building via vmware

requires "packer"
requires "jq"
requires "ovftool"

while [[ $# -gt 0 ]]; do
    key="$1"

    case $key in
        --buildtag)
            export BUILDTAG="$2"
            shift
        ;;
        --except)
            export NO_BUILD="$2"
            shift
        ;;
        --filesdir)
            export FILES_DIR="$2"
            shift
        ;;
        --publish)
            export PUBLISH_AMI="$2"
            shift
        ;;    
        --settings)
            export PACKER_SECRETS="$2"
            shift
        ;;
        *)
            usage
            echo "Unknown Option: $key"
            exit 1
        ;;
    esac
    shift
done

if [ -z "$BUILDTAG" ]; then
    echo "Missing --buildtag."
    usage
    exit 1
fi

if [ -z "$PUBLISH_AMI" ]; then
    echo "--publish not set, defaulting to false"
    export PUBLISH_AMI="false"
fi

if [ -z "$PACKER_SECRETS" ]; then
    echo "warning: --settings not used, packer secrets must be set from environment variables or errors may occur"
fi

if [ "$PUBLISH_AMI" == "true" ]; then
    export BUILD_SUFFIX="prod"
else
    export BUILD_SUFFIX="test"
fi

if [ -z "$FILES_DIR" ]; then
    echo "Missing --filesdir"
    usage
    exit 1
fi

echo ""
echo "Running packer"
echo ""


ovadir="$(pwd)/output/release/${BUILDTAG}"
pushd packer


if [ -z "$PACKER_SECRETS" ]; then
    rm -rvf "output-vmware-iso ${ovadir}/peach-targetvm-${BUILDTAG}.ova" 2>/dev/null || {}
    remote_var=""
    packer_vars=""
else
    rm -rvf "peach-targetvm" 2>/dev/null || {}
    remote_var="-var remote_type=esx5"
    packer_vars="-var-file=${PACKER_SECRETS}"
fi

# Ensure tmp directory for .tar files is clean
rm -rvf "tmp" 2>/dev/null || {}
mkdir tmp

# If packer is unable to connect over VNC to the esxi server
# ensure that the firewall settings did not get reset
# https://nickcharlton.net/posts/using-packer-esxi-6.html

export AWS_DEFAULT_REGION="us-west-1"
export PACKER_CACHE_DIR="${JENKINS_HOME}/.packer_cache"

packer build \
    -var "buildtag=${BUILDTAG}" \
    -var "ami_name_suffix=${BUILD_SUFFIX}" \
    -var "files_dir=${FILES_DIR}" \
    ${packer_vars} \
    ${remote_var} \
    template.json

if [ -z "$PACKER_SECRETS" ]; then
    ovftool "output-vmware-iso/peach-targetvm.vmx" "${ovadir}/peach-targetvm-${BUILDTAG}.ova"
else
    mv "output-vmware-iso/peach-targetvm.ova" "${ovadir}/peach-targetvm-${BUILDTAG}.ova"
fi

# last step is to modify the release.json to include the .ova as a file
jq --arg OVANAME peach-targetvm-${BUILDTAG}.ova '.files += [$OVANAME]' ${ovadir}/release.json > ${ovadir}/release.json.tmp && mv ${ovadir}/release.json.tmp ${ovadir}/release.json

popd

echo ""
echo "Successfully created peach-targetvm-${BUILDTAG}"
