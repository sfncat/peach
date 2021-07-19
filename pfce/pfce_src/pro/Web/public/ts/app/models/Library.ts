/// <reference path="../reference.ts" />

namespace Peach {
	export interface ILibrary {
		libraryUrl: string;
		name: string;
		description: string;
		locked: boolean;
		versions: ILibraryVersion[];
		groups: IGroup[];
		user: string;
		timeStamp: Date;
	}

	export interface ILibraryVersion {
		version: number;
		locked: boolean;
		pits: IPit[];
	}

	export interface IGroup {
		groupUrl: string;
		access: string;
	}

	export interface ITag {
		name: string;
		values: string[];
	}

	export interface IError {
		errorMessage: string;
	}
}
