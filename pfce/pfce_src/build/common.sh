#!/bin/bash

umask 000

requires() {
    # verifies that a particular command exists on the system
    command -v "$1" >/dev/null 2>&1 || { echo "'$1' is required but it's not installed.  Aborting." >&2; exit 1; }
}
