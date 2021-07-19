 /// <reference path="../reference.ts" />

namespace Peach {
	export interface IFaultSummary {
		faultUrl: string;
		archiveUrl: string;
		reproducible: boolean;
		iteration: number;
		timeStamp: string;
		source: string;
		exploitability: string;
		majorHash: string;
		minorHash: string;
	}

	export interface IFaultDetail extends IFaultSummary {
		nodeUrl: string;
		targetUrl: string;
		targetConfigUrl: string;
		pitUrl: string;
		peachUrl: string;

		title: string;
		description: string;
		seed: number;
		files: IFaultFile[];
		mutations: IFaultMutation[];

		// range of search when fault was found
		iterationStart: number;
		iterationStop: number;
	}

	export interface IFaultMutation {
		iteration: number;
		state: string;
		action: string;
		element: string;
		mutator: string;
		dataset: string;
	}

	export interface IFaultFile {
		name?: string;
		fullName?: string;
		fileUrl?: string;
		size?: number;
		initial?: boolean;
		type?: string;
		agentName?: string;
		monitorName?: string;
		monitorClass?: string;

		// client-side only
		displayName?: string;
		children?: IFaultFile[];
	}

	export var FaultFileType = {
		Asset: '',
		Output: '',
		Input: ''
	}
	MakeLowerEnum(FaultFileType);

}
