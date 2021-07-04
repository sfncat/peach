/*++

Copyright (c) Michael Eddington

Module Name:

    controller.c

Abstract:

Environment:

    Win32 console multi-threaded application

--*/
#include <windows.h>
#include <winioctl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strsafe.h>
#include "..\driver\peach.h"


BOOLEAN
ManageDriver(
    __in LPCTSTR  DriverName,
    __in LPCTSTR  ServiceName,
    __in USHORT   Function
    );

BOOLEAN
SetupDriverName(
    __inout_bcount_full(BufferLength) PCHAR DriverLocation,
    __in ULONG BufferLength
    );

VOID __cdecl
main(
    __in ULONG argc,
    __in_ecount(argc) PCHAR argv[]
    )
{
    TCHAR driverLocation[MAX_PATH];
	PCHAR cmd;

    UNREFERENCED_PARAMETER(argc);
    UNREFERENCED_PARAMETER(argv);

	printf("| Peach Windows Driver Controller\n");
	printf("| Copyright (c) Michael Eddington\n\n");

	printf("IOCTL_PEACH_METHOD_START: %u\n", IOCTL_PEACH_METHOD_START);
	printf("IOCTL_PEACH_METHOD_STOP: %u\n", IOCTL_PEACH_METHOD_STOP);
	printf("IOCTL_PEACH_METHOD_DATA: %u\n", IOCTL_PEACH_METHOD_DATA);
	printf("IOCTL_PEACH_METHOD_CALL: %u\n", IOCTL_PEACH_METHOD_CALL);
	printf("IOCTL_PEACH_METHOD_PROPERTY: %u\n", IOCTL_PEACH_METHOD_PROPERTY);
	printf("IOCTL_PEACH_METHOD_NEXT: %u\n", IOCTL_PEACH_METHOD_NEXT);

	if(argc != 2)
	{
		printf("Syntax: controller.exe <install|uninstall>\n\n");
		return;
	}

	cmd = argv[1];

	if(!strcmp(cmd, "install"))
	{

        //
        // The driver is not started yet so let us the install the driver.
        // First setup full path to driver name.
        //

        if (!SetupDriverName(driverLocation, sizeof(driverLocation)))
		{
            return ;
        }

        if (!ManageDriver(DRIVER_NAME,
                          driverLocation,
                          DRIVER_FUNC_INSTALL
                          )) {

            printf("Unable to install driver. \n");

            //
            // Error - remove driver.
            //

            ManageDriver(DRIVER_NAME,
                         driverLocation,
                         DRIVER_FUNC_REMOVE
                         );

            return;
        }
	}
	else if(!strcmp(cmd, "uninstall"))
	{
		//
		// Unload the driver.  Ignore any errors.
		//

		ManageDriver(DRIVER_NAME,
					 driverLocation,
					 DRIVER_FUNC_REMOVE
					 );

	}
}


