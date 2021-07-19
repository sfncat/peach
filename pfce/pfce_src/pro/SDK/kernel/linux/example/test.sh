#!/bin/sh
sudo ./scullv_unload 
sudo ./scullv_load 
../peach_call start
../peach_call data ../peach.h 
