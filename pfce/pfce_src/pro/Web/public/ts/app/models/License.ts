/// <reference path="../reference.ts" />

namespace Peach {
	export interface ILicense {
		status: string;
		errorText: string;
		expiration: string;
		eulaAccepted: boolean;
		eula: string;
		version: string;
	}

	export var LicenseStatus = {
		Missing   : '',
		Expired   : '',
		Invalid   : '',
		Valid     : ''
	};
	MakeLowerEnum(LicenseStatus);
}
