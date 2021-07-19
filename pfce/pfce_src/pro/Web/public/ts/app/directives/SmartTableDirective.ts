/// <reference path="../reference.ts" />

namespace Peach {
	export interface IRatioScope extends ng.IScope {
		stRatio: number;
	}

	export const SmartTableRatioDirective: IDirective = {
		ComponentID: C.Directives.Ratio,
		restrict: 'A',
		scope: {
			stRatio: '='
		},
		link: (scope: IRatioScope, element: ng.IAugmentedJQuery) => {
			element.css('width', `${scope.stRatio}%`);
		}
	}
}
