/*++

Copyright (c) Michael Eddington

Module Name:

    peach.c

Abstract:

    Purpose of this driver is to provide a method for fuzzing
	in kernel land with Peach.  This is a sample driver that
	performs the I/O interface with Peach.

Environment:

    Kernel mode only.

--*/


//
// Include files.
//

#include <ntddk.h>          // various NT definitions
#include <string.h>

#include "peach.h"

#define TAG 'caep'
#define NT_DEVICE_NAME      L"\\Device\\Peach"
#define DOS_DEVICE_NAME     L"\\DosDevices\\Peach"

#if DBG
#define PEACH_KDPRINT(_x_) \
                DbgPrint("PEACH.SYS: ");\
                DbgPrint _x_;

#else
#define PEACH_KDPRINT(_x_)
#endif

//
// Static variables
//

static char started = FALSE;
static char* methodName = NULL;
static char* propertyName = NULL;
static char* data = NULL;

//
// Device driver routine declarations.
//

DRIVER_INITIALIZE DriverEntry;

__drv_dispatchType(IRP_MJ_CREATE)
__drv_dispatchType(IRP_MJ_CLOSE)
DRIVER_DISPATCH PeachCreateClose;

__drv_dispatchType(IRP_MJ_DEVICE_CONTROL)
DRIVER_DISPATCH PeachDeviceControl;

DRIVER_UNLOAD PeachUnloadDriver;

VOID
PrintIrpInfo(
    PIRP Irp
    );
VOID
PrintChars(
    __in_ecount(CountChars) PCHAR BufferAddress,
    __in size_t CountChars
    );

#ifdef ALLOC_PRAGMA
#pragma alloc_text( INIT, DriverEntry )
#pragma alloc_text( PAGE, PeachCreateClose)
#pragma alloc_text( PAGE, PeachDeviceControl)
#pragma alloc_text( PAGE, PeachUnloadDriver)
#pragma alloc_text( PAGE, PrintIrpInfo)
#pragma alloc_text( PAGE, PrintChars)
#endif // ALLOC_PRAGMA


NTSTATUS
DriverEntry(
    __in PDRIVER_OBJECT   DriverObject,
    __in PUNICODE_STRING      RegistryPath
    )
/*++

Routine Description:
    This routine is called by the Operating System to initialize the driver.

    It creates the device object, fills in the dispatch entry points and
    completes the initialization.

Arguments:
    DriverObject - a pointer to the object that represents this device
    driver.

    RegistryPath - a pointer to our Services key in the registry.

Return Value:
    STATUS_SUCCESS if initialized; an error otherwise.

--*/

{
    NTSTATUS        ntStatus;
    UNICODE_STRING  ntUnicodeString;    // NT Device Name "\Device\PEACH"
    UNICODE_STRING  ntWin32NameString;    // Win32 Name "\DosDevices\IoctlTest"
    PDEVICE_OBJECT  deviceObject = NULL;    // ptr to device object

    UNREFERENCED_PARAMETER(RegistryPath);

    RtlInitUnicodeString( &ntUnicodeString, NT_DEVICE_NAME );

    ntStatus = IoCreateDevice(
        DriverObject,                   // Our Driver Object
        0,                              // We don't use a device extension
        &ntUnicodeString,               // Device name "\Device\Peach"
        FILE_DEVICE_UNKNOWN,            // Device type
        FILE_DEVICE_SECURE_OPEN,     // Device characteristics
        FALSE,                          // Not an exclusive device
        &deviceObject );                // Returned ptr to Device Object

    if ( !NT_SUCCESS( ntStatus ) )
    {
        PEACH_KDPRINT(("Couldn't create the device object\n"));
        return ntStatus;
    }

	// Init variables
	methodName = NULL;
	propertyName = NULL;
	data = NULL;
	started = FALSE;

    //
    // Initialize the driver object with this driver's entry points.
    //

    DriverObject->MajorFunction[IRP_MJ_CREATE] = PeachCreateClose;
    DriverObject->MajorFunction[IRP_MJ_CLOSE] = PeachCreateClose;
    DriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = PeachDeviceControl;
    DriverObject->DriverUnload = PeachUnloadDriver;

    //
    // Initialize a Unicode String containing the Win32 name
    // for our device.
    //

    RtlInitUnicodeString( &ntWin32NameString, DOS_DEVICE_NAME );

    //
    // Create a symbolic link between our device name  and the Win32 name
    //

    ntStatus = IoCreateSymbolicLink(
                        &ntWin32NameString, &ntUnicodeString );

    if ( !NT_SUCCESS( ntStatus ) )
    {
        //
        // Delete everything that this routine has allocated.
        //
        PEACH_KDPRINT(("Couldn't create symbolic link\n"));
        IoDeleteDevice( deviceObject );
    }


    return ntStatus;
}


NTSTATUS
PeachCreateClose(
    PDEVICE_OBJECT DeviceObject,
    PIRP Irp
    )
/*++

Routine Description:

    This routine is called by the I/O system when the PEACH is opened or
    closed.

    No action is performed other than completing the request successfully.

Arguments:

    DeviceObject - a pointer to the object that represents the device
    that I/O is to be done on.

    Irp - a pointer to the I/O Request Packet for this request.

Return Value:

    NT status code

--*/

{
    UNREFERENCED_PARAMETER(DeviceObject);

    PAGED_CODE();

	// Free any memory
	if(methodName)
		ExFreePoolWithTag(methodName, TAG);
	if(propertyName)
		ExFreePoolWithTag(propertyName, TAG);
	if(data)
		ExFreePoolWithTag(data, TAG);

	methodName = NULL;
	propertyName = NULL;
	data = NULL;

    Irp->IoStatus.Status = STATUS_SUCCESS;
    Irp->IoStatus.Information = 0;

    IoCompleteRequest( Irp, IO_NO_INCREMENT );

    return STATUS_SUCCESS;
}

VOID
PeachUnloadDriver(
    __in PDRIVER_OBJECT DriverObject
    )
/*++

Routine Description:

    This routine is called by the I/O system to unload the driver.

    Any resources previously allocated must be freed.

Arguments:

    DriverObject - a pointer to the object that represents our driver.

Return Value:

    None
--*/

{
    PDEVICE_OBJECT deviceObject = DriverObject->DeviceObject;
    UNICODE_STRING uniWin32NameString;

    PAGED_CODE();

    //
    // Create counted string version of our Win32 device name.
    //

    RtlInitUnicodeString( &uniWin32NameString, DOS_DEVICE_NAME );


    //
    // Delete the link from our device name to a name in the Win32 namespace.
    //

    IoDeleteSymbolicLink( &uniWin32NameString );

    if ( deviceObject != NULL )
    {
        IoDeleteDevice( deviceObject );
    }



}

NTSTATUS
PeachDeviceControl(
    PDEVICE_OBJECT DeviceObject,
    PIRP Irp
    )

/*++

Routine Description:

    This routine is called by the I/O system to perform a device I/O
    control function.

Arguments:

    DeviceObject - a pointer to the object that represents the device
        that I/O is to be done on.

    Irp - a pointer to the I/O Request Packet for this request.

Return Value:

    NT status code

--*/

{
    PIO_STACK_LOCATION  irpSp;// Pointer to current stack location
    NTSTATUS            ntStatus = STATUS_SUCCESS;// Assume success
    ULONG               inBufLength; // Input buffer length
    ULONG               outBufLength; // Output buffer length
	char				ret;

    UNREFERENCED_PARAMETER(DeviceObject);

    PAGED_CODE();

    irpSp = IoGetCurrentIrpStackLocation( Irp );
    inBufLength = irpSp->Parameters.DeviceIoControl.InputBufferLength;
    outBufLength = irpSp->Parameters.DeviceIoControl.OutputBufferLength;

    if (!inBufLength || !outBufLength)
    {
        ntStatus = STATUS_INVALID_PARAMETER;
        goto End;
    }

    //
    // Determine which I/O control code was specified.
    //

    switch ( irpSp->Parameters.DeviceIoControl.IoControlCode )
    {
	/* Sent by Peach to say "Starting iteration" */
	case IOCTL_PEACH_METHOD_START:

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_START\n"));

		started = TRUE;

		// TODO: Put any code here that runs on start.

		break;

	/* Sent by Peach to say "Stopping iteration" */
	case IOCTL_PEACH_METHOD_STOP:

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_STOP\n"));

		started = FALSE;

		// TODO: Put any code here that runs on STOP

		break;

	/* Sent by Peach to indicate a "call" data is "method name".
	 * This will be followed by one or more DATA ioctls to deliver
	 * each parameter's data.
	 */
	case IOCTL_PEACH_METHOD_CALL:

		if(methodName != NULL)
		{
			ExFreePoolWithTag(methodName, TAG);
			methodName = NULL;
		}

		methodName = ExAllocatePoolWithTag(NonPagedPool, inBufLength, TAG);
		RtlCopyBytes(methodName, Irp->AssociatedIrp.SystemBuffer, inBufLength);

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_CALL: [%s]\nh", methodName));

		break;

	/* Sent by Peach to indicate a "property" set, data is "property name".
	 * This will be followed by a single DATA ioctl. */
	case IOCTL_PEACH_METHOD_PROPERTY:

		if(propertyName != NULL)
		{
			ExFreePoolWithTag(propertyName, TAG);
			propertyName = NULL;
		}

		propertyName = ExAllocatePoolWithTag(NonPagedPool, inBufLength, TAG);
		RtlCopyBytes(propertyName, Irp->AssociatedIrp.SystemBuffer, inBufLength);

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_PROPERTY: [%s]\n", propertyName));

		break;

	/* Sent by Peach to query if driver is ready for next test.  Driver
	 * should send back TRUE (1) or FALSE (0) */
	case IOCTL_PEACH_METHOD_NEXT:

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_NEXT\n"));

		// TODO: Change ret based on our status
		ret = 1;

		RtlCopyBytes(Irp->AssociatedIrp.SystemBuffer, &ret, 1);
        Irp->IoStatus.Information = 1;

		break;

	/* Sent by Peach to deliver DATA to Kernel */
    case IOCTL_PEACH_METHOD_DATA:

		PEACH_KDPRINT(("IOCTL_PEACH_METHOD_DATA: Length: %d\n", inBufLength));

		if(data != NULL)
		{
			ExFreePoolWithTag(data, TAG);
			data = NULL;
		}

		data = ExAllocatePoolWithTag(NonPagedPool, inBufLength, TAG);
		RtlCopyBytes(data, Irp->AssociatedIrp.SystemBuffer, inBufLength);

		// TODO: Place code here to fuzz something!!

		break;

    default:

        //
        // The specified I/O control code is unrecognized by this driver.
        //

        ntStatus = STATUS_INVALID_DEVICE_REQUEST;
        PEACH_KDPRINT(("ERROR: unrecognized IOCTL %x\n",
            irpSp->Parameters.DeviceIoControl.IoControlCode));
        break;
    }

End:
    //
    // Finish the I/O operation by simply completing the packet and returning
    // the same status as in the packet itself.
    //

    Irp->IoStatus.Status = ntStatus;

    IoCompleteRequest( Irp, IO_NO_INCREMENT );

    return ntStatus;
}

VOID
PrintIrpInfo(
    PIRP Irp)
{
    PIO_STACK_LOCATION  irpSp;
    irpSp = IoGetCurrentIrpStackLocation( Irp );

    PAGED_CODE();

    PEACH_KDPRINT(("\tIrp->AssociatedIrp.SystemBuffer = 0x%p\n",
        Irp->AssociatedIrp.SystemBuffer));
    PEACH_KDPRINT(("\tIrp->UserBuffer = 0x%p\n", Irp->UserBuffer));
    PEACH_KDPRINT(("\tirpSp->Parameters.DeviceIoControl.Type3InputBuffer = 0x%p\n",
        irpSp->Parameters.DeviceIoControl.Type3InputBuffer));
    PEACH_KDPRINT(("\tirpSp->Parameters.DeviceIoControl.InputBufferLength = %d\n",
        irpSp->Parameters.DeviceIoControl.InputBufferLength));
    PEACH_KDPRINT(("\tirpSp->Parameters.DeviceIoControl.OutputBufferLength = %d\n",
        irpSp->Parameters.DeviceIoControl.OutputBufferLength ));
    return;
}

VOID
PrintChars(
    __in_ecount(CountChars) PCHAR BufferAddress,
    __in size_t CountChars
    )
{
    PAGED_CODE();

    if (CountChars) {

        while (CountChars--) {

            if (*BufferAddress > 31
                 && *BufferAddress != 127) {

                KdPrint (( "%c", *BufferAddress) );

            } else {

                KdPrint(( ".") );

            }
            BufferAddress++;
        }
        KdPrint (("\n"));
    }
    return;
}


