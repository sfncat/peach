/// <reference path="../reference.ts" />

namespace Peach {
	export interface IFaultTimelineMetric {
		date: Date;
		faultCount: number;
	}

	export interface IBucketTimelineMetric {
		id: number;
		label: string;
		iteration: number;
		time: Date;
		faultCount: number;
		href: string;
	}

	export interface IMutatorMetric {
		mutator: string;
		elementCount: number;
		iterationCount: number;
		bucketCount: number;
		faultCount: number;
	}

	export interface IElementMetric {
		state: string;
		action: string;
		element: string;
		iterationCount: number;
		bucketCount: number;
		faultCount: number;
	}

	export interface IStateMetric {
		state: string;
		executionCount: number;
	}

	export interface IDatasetMetric {
		dataset: string;
		iterationCount: number;
		bucketCount: number;
		faultCount: number;
	}

	export interface IBucketMetric {
		bucket: string;
		mutator: string;
		dataset: string;
		state: string;
		action: string;
		element: string;
		iterationCount: number;
		faultCount: number;
	}

	export interface IVisualizerData {
		iteration: number;
		mutatedElements: string[];
		models: IVisualizerModel[];
	}

	export interface IVisualizerModel {
		original: string;
		fuzzed: string;
		name: string;
		type: string;
		children: IVisualizerModelChild[];
	}

	export interface IVisualizerModelChild {
		name: string;
		type: string;
		children: IVisualizerModelChild[];
	}
}
