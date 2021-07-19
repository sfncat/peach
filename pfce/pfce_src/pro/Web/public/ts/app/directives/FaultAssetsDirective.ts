/// <reference path="../reference.ts" />

namespace Peach {
	export const FaultAssetsDirective: IDirective = {
		ComponentID: C.Directives.FaultAssets,
		restrict: "E",
		templateUrl: C.Templates.Directives.FaultAssets,
		controller: C.Controllers.FaultAssets,
		scope: {
			assets: "="
		}
	}

	export interface IFaultAssets {
		// Is list of test i/o
		TestData: IFaultFile[];

		// List of Agents
		// Children is List of Monitors
		// Children[0].Children is files for a given monitor
		MonitorAssets: IFaultFile[];

		// Is list of test i/o
		Other: IFaultFile[];
	}

	export interface IFaultAssetsScope extends IFormScope {
		assets: IFaultAssets;
	}

	export class FaultAssetsController {
		static $inject = [
			C.Angular.$scope
		];

		constructor(
			private $scope: IFaultAssetsScope
		) {
			$scope.vm = this;
		}
	}
}
