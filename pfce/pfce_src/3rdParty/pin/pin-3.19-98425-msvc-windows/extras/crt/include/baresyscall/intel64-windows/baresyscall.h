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

#ifndef OS_APIS_WINDOWS_INTEL64_BARESYSCALL_H__
#define OS_APIS_WINDOWS_INTEL64_BARESYSCALL_H__

#ifdef __cplusplus
extern "C"
{
#endif

#include "types.h"
#include "baresyscall/intel64-windows/asm-baresyscall.h"

#define REG_SIZE HEX(8)
// 8 callee-saved registers
#define CALLEE_SAVED_REG HEX(8)
// System call arguments stack offset
// (shadow stack (0x20) + return address (0x8))
#define SYSCALL_ARG_STACK_OFFSET HEX(28)

    /*!
* Set of raw return values from a system call. Return value and scratch register values upon syscall execution.
*/
    typedef struct /*<POD>*/
    {
        long _status;
        ADDRINT _regs[OS_SCRATCH_REGS_NUM];
    } OS_SYSCALLRETURN;

#ifdef __cplusplus
}
#endif

#endif // file guard
