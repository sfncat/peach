 /// <reference path="../reference.ts" />

namespace Peach {
	export var ParameterType = {
		String  : '',
		Hex     : '',
		Range   : '',
		Ipv4    : '',
		Ipv6    : '',
		Hwaddr  : '',
		Iface   : '',
		Enum    : '',
		Bool    : '',
		User    : '',
		System  : '',
		Call    : '',
		Group   : '',
		Space   : '',
		Monitor : ''
	};
	MakeLowerEnum(ParameterType);

	export interface IParameter {
		key?: string;
		value?: any;
		name?: string;
		type?: string;
		items?: IParameter[];
		options?: string[];
		defaultValue?: string;
		description?: string;
		min?: number;
		max?: number;
		optional?: boolean;
		collapsed?: boolean;
	}

	export interface IAgent {
		name: string;
		agentUrl: string;
		monitors: IMonitor[];
	}

	export interface IMonitor {
		monitorClass: string;
		name?: string;
		map: IParameter[];
		description?: string;

		// for use by the wizard
		path?: number[];

		// only used by client-side
		view?: IParameter[];
	}

	export interface IPitFieldNode {
		id: string;
		fields: IPitFieldNode[];
		weight?: number;
		expanded?: boolean;
	}

	export interface IPitMetadata {
		defines: IParameter[];
		monitors: IParameter[];
		fields: IPitFieldNode[];
	}

	export interface IPitWeight {
		id: string;
		weight: number;
	}
	
	export interface IPit {
		id: string;
		pitUrl: string;
		name: string;
		description?: string;
		tags?: ITag[];

		// details, not available from collection at /p/pits
		config: IParameter[];
		agents: IAgent[];
		weights?: IPitWeight[];
		metadata?: IPitMetadata;
		webProxy?: IWebProxy;
	
		// only used by client-side
		definesView?: IParameter[];
	}

	export interface IPitCopy {
		legacyPitUrl?: string;
		pitUrl: string;
		name?: string;
		description?: string;
	}

	export interface IWebProxy {
		routes: IWebRoute[];
	}

	export interface IWebRoute {
		url: string;
		swagger: string;
		script: string;
		mutate: boolean;
		baseUrl: string;
		faultOnStatusCodes: number[];
		headers: IWebHeader[];

		// only used by front-end
		mutateChoice: string;
		faultOnStatusCodesText: string;
	}

	export interface IWebHeader {
		name: string;
		mutate: boolean;

		// only used by front-end
		mutateChoice: string;
	}
}
