/// <reference path="../reference.ts" />

namespace Peach {
	export const DefinesDirective: IDirective = {
		ComponentID: C.Directives.Defines,
		restrict: "E",
		templateUrl: C.Templates.Directives.Defines,
		controller: C.Controllers.Defines,
		scope: { 
			form: "=",
			group: "=" 
		}
	}

	export interface IDefinesScope extends IFormScope {
		group: IParameter;
		isOpen: boolean;
	}

	export class DefinesController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IDefinesScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			if (!$scope.group.collapsed) {
				$scope.isOpen = true;
			}
		}

		public get ShowGroup(): boolean {
			return !_.isEmpty(this.$scope.group.items);
		}
	
		public get CanRemove(): boolean {
			return this.$scope.group.name === "User Defines";
		}

		public OnRemove(index: number): void {
			this.$scope.group.items.splice(index, 1);
			this.$scope.form.$setDirty();
		}
	}
}
