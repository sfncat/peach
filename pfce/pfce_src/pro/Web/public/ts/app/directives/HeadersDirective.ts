/// <reference path="../reference.ts" />

namespace Peach {
	export const HeadersDirective: IDirective = {
		ComponentID: C.Directives.Headers,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Headers,
		controller: C.Controllers.Headers,
		scope: {
			route: '=',
			form: '='
		}
	}

	export interface IHeadersScope extends IFormScope {
		route: IWebRoute;
	}

	export class HeadersController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IHeadersScope,
			private pitService: PitService
		) {
			$scope.vm = this;
		}

		public MutateChoices = [
			'Include',
			'Exclude'
		];

		public CanMoveUp(index: number): boolean {
			return index !== 0;
		}

		public CanMoveDown(index: number): boolean {
			return index !== (this.$scope.route.headers.length - 1);
		}

		public OnMoveUp($event: ng.IAngularEvent, index: number): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.route.headers, index);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent, index: number): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.route.headers, index);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent, index: number): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.route.headers.splice(index, 1);
			this.$scope.form.$setDirty();
		}
	}
}
