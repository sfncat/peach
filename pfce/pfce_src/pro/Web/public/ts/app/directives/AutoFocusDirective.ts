/// <reference path="../reference.ts" />

namespace Peach {
	export const AutoFocusDirective: IDirective = {
		ComponentID: C.Directives.AutoFocus,
		restrict: 'AC',
		link: (scope: ng.IScope, element: ng.IAugmentedJQuery) => {
			_.delay(() => {
				element[0].focus();
			}, 100);
		}
	}
}
