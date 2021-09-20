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

#ifndef OS_APIS_WINDOWS_INTEL64_ASM_BARESYSCALL_H__
#define OS_APIS_WINDOWS_INTEL64_ASM_BARESYSCALL_H__

#ifdef __cplusplus
extern "C"
{
#endif

/*
 * Indexes of specific scratch registers in array
 * that gets their respective values right after syscall execution.
 */
#define OS_SCRATCH_REG_RCX 0
#define OS_SCRATCH_REG_RDX 1
#define OS_SCRATCH_REG_R8 2
#define OS_SCRATCH_REG_R9 3
#define OS_SCRATCH_REG_R10 4
#define OS_SCRATCH_REG_R11 5
// Number of elements in the array.
#define OS_SCRATCH_REGS_NUM 6

#ifdef __cplusplus
}
#endif

#endif // file guard
