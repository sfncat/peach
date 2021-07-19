/// <reference path="../reference.ts" />

namespace Peach {
	export interface ITestResult {
		status: string;
		events: ITestEvent[];
		log: string;
	}

	export interface ITestEvent {
		id: number;
		status: string;
		short?: string;
		description: string;
		resolve: string;
	}

	export var TestStatus = {
		Active : '',
		Pass   : '',
		Fail   : ''
	};
	MakeLowerEnum(TestStatus);
}
