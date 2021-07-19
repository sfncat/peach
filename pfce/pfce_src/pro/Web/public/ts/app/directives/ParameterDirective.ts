/// <reference path="../reference.ts" />

namespace Peach {
	export const ParameterDirective: IDirective = {
		ComponentID: C.Directives.Parameter,
		restrict: "E",
		replace: true,
		templateUrl: C.Templates.Directives.Parameter,
		controller: C.Controllers.Parameter,
		scope: { param: "=" }
	}

	export interface IParameterScope extends IFormScope {
		param: IParameter;
		isOpen: boolean;
	}

	export class ParameterController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IParameterScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			if (!$scope.param.collapsed) {
				$scope.isOpen = true;
			}
		}
	}
}
