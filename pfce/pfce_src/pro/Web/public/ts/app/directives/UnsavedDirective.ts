/// <reference path="../reference.ts" />

namespace Peach {
	interface IUnsavedScope extends ng.IScope {
		ctrl: UnsavedController;
	}

	export const UnsavedDirective: IDirective = {
		ComponentID: C.Directives.Unsaved,
		restrict: 'A',
		require: '^form',
		controller: C.Controllers.Unsaved,
		controllerAs: 'ctrl',
		scope: {},
		link: (
			scope: IUnsavedScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			form: ng.IFormController
		) => {
			scope.ctrl.Link(form);
		}
	}

	export class UnsavedController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModal,
			C.Angular.$state
		];	

		constructor(
			private $scope: ng.IScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private $state: ng.ui.IStateService
		) {
		}

		public Link(form: ng.IFormController) {
			const onRouteChangeOff = this.$scope.$root.$on(C.Angular.$stateChangeStart, (
				event: ng.IAngularEvent,
				toState: ng.ui.IState,
				toParams: any,
				fromState: ng.ui.IState,
				fromParams: any
			) => {
				if (!form.$dirty) {
					onRouteChangeOff();
					return;
				}

				event.preventDefault();
				
				const options: IConfirmOptions = {
					Title: 'Unsaved Changes',
					Body: 'You have unsaved changes. Do you want to leave the page?',
					SubmitPrompt: 'Ignore Changes'
				};

				Confirm(this.$modal, options).result
					.then(result => {
						if (result === 'ok') {
							onRouteChangeOff();
							this.$state.transitionTo(toState.name, toParams);
						}
					})
				;
			});
		}
	}
}
