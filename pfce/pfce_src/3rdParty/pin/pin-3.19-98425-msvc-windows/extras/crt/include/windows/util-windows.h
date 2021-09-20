/*
 * Copyright 2002-2020 Intel Corporation.
 * 
 * This software and the related documents are Intel copyrighted materials, and your
 * use of them is governed by the express license under which they were provided to
 * you ("License"). Unless the License provides otherwise, you may not use, modify,
 * copy, publish, distribute, disclose or transmit this software or the related
 * documents without Intel's prior written permission.
 * 
 * This software and the related documents are provided as is, with no express or
 * implied warranties, other than those that are expressly stated in the License.
 */

// <COMPONENT>: os-apis
// <FILE-TYPE>: component public header

#ifndef OS_APIS_UITL_WINDOWS_H_
#define OS_APIS_UITL_WINDOWS_H_

#include "os-apis.h"
#include "types.h"
#include "baresyscall/baresyscall.h"
#include "win_syscalls.h"

void OS_SetSysCallTable(SYSCALL_NUMBER_T* input);

void OS_SetIfItIsWow64();

/*
 * Returns TRUE when Pin OS API uses native kernel32.dll functions
 * instead of internal implementation.
 */
BOOL_T UseKernel32();

#endif // file guard
