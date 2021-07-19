/// <reference path="../reference.ts" />

namespace Peach {
	export const RouteDirective: IDirective = {
		ComponentID: C.Directives.Route,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Route,
		controller: C.Controllers.Route,
		scope: {
			routes: '=',
			route: '=',
			index: '='
		}
	}

	export interface IRouteScope extends IFormScope {
		routes: IWebRoute[];
		route: IWebRoute;
		index: number;
		isOpen: boolean;
		help: boolean;
		storage: IRouteStorage;
	}

	interface IRouteStorage {
		showHelp: boolean;
	}

	export class RouteController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$localStorage,
			C.Services.Pit
		];

		constructor(
			private $scope: IRouteScope,
			$localStorage,
			private pitService: PitService
		) {
			$scope.vm = this;
			$scope.isOpen = true;
			$scope.storage = $localStorage['$default']({
				showHelp: true
			});
		}

		public OnHelp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.storage.showHelp = !this.$scope.storage.showHelp;
		}

		public get HelpClass() {
			return { active: this.$scope.storage.showHelp };
		}

		public get HelpPrompt(): string {
			return this.$scope.storage.showHelp ? 'Hide' : 'Help';
		}

		public get Header(): string {
			return this.$scope.route.url === '*' ? 'Default (*)' : this.$scope.route.url;
		}

		public MutateChoices = [
			'Include',
			'Exclude'
		];

		public get CanMoveUp(): boolean {
			return this.$scope.index !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.index !== (this.$scope.routes.length - 1);
		}

		public OnMoveUp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.routes, this.$scope.index);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.routes, this.$scope.index);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.routes.splice(this.$scope.index, 1);
			this.$scope.form.$setDirty();
		}

		public OnAddHeader(): void {
			this.$scope.route.headers.push({
				name: "",
				mutate: false,
				mutateChoice: 'Exclude'
			});
			this.$scope.form.$setDirty();
		}
	}
}
