#!/bin/bash

last=$(git describe --match ${TAG_PREFIX}${RELEASE_VERSION}.* --abbrev=0 | cut -f3 -d.)
next=$((last+1))

echo BUILDTAG=${RELEASE_VERSION}.${next}
