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

#include "os-apis.h"

#ifdef __cplusplus
extern "C"
{
#endif

    typedef OS_RETURN_CODE (*OS_FnPtrWriteFD)(NATIVE_FD fd, const void* buffer, USIZE* size);
    typedef OS_RETURN_CODE (*OS_FnPtrReadFD)(NATIVE_FD fd, USIZE* size, void* buffer);
    typedef OS_RETURN_CODE (*OS_FnPtrIsConsoleFD)(NATIVE_FD fd, INT* isConsole);
    typedef UINT32 (*OS_FnNtCreateFile)(NATIVE_FD* hFile, const CHAR* fileName, UINT32 accessMask, UINT32 objAttributes,
                                        UINT32 fileAttributes, UINT32 shareAccess, UINT32 createDisposition,
                                        UINT32 createOptions);
    typedef UINT32 (*OS_FnNtQueryAttributesFile)(const CHAR* fileName, UINT32 objAttributes, void* fbi);
    typedef UINT32 (*OS_FnRemoveFile)(const CHAR* fileName, UINT32 objAttributes);
    typedef UINT32 (*OS_FnNtAllocateVirtualMemory)(VOID** baseAddress, ADDRINT zeroBits, ADDRINT* regionSize,
                                                   UINT32 allocationType, UINT32 protect);
    typedef UINT32 (*OS_FnNtProtectVirtualMemory)(VOID** baseAddress, ADDRINT* regionSize, UINT32 newProtect, UINT32* oldProtect);
    typedef int (*CheckBrokerExists)();

    typedef struct _FileApiOverrides
    {
        OS_FnPtrWriteFD writeFd;
        OS_FnPtrReadFD readFd;
        OS_FnPtrIsConsoleFD isConsoleFd;
        OS_FnNtCreateFile ntCreateFile;
        OS_FnNtQueryAttributesFile ntQueryAttributesFile;
        OS_FnRemoveFile RemoveFile;
        OS_FnNtAllocateVirtualMemory ntAllocateVirtualMemory;
        OS_FnNtProtectVirtualMemory ntProtectVirtualMemory;
        CheckBrokerExists brokerExists;
    } FileApiOverrides;

    VOID OS_SetFileApiOverrides(FileApiOverrides* overrides);
    FileApiOverrides* OS_GetFileApiOverrides();

    extern OS_FnPtrCreateProcess pOS_CreateProcess;

#ifdef __cplusplus
}
#endif
