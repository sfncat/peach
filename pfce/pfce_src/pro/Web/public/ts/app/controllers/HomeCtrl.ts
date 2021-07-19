/// <reference path="../reference.ts" />

namespace Peach {
	export class HomeController {
		static $inject = [
			C.Angular.$scope
		];

		constructor(
			$scope: IViewModelScope
		) {
		}
	}
}
