/// <reference path="../reference.ts" />

namespace Peach {
	export const MonitorDirective: IDirective = {
		ComponentID: C.Directives.Monitor,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Monitor,
		controller: C.Controllers.Monitor,
		scope: {
			monitors: '=',
			monitor: '=',
			agentIndex: '=',
			monitorIndex: '='
		}
	}

	export interface IMonitorScope extends IFormScope {
		monitors: IMonitor[];
		monitor: IMonitor;
		agentIndex: number;
		monitorIndex: number;
		isOpen: boolean;
	}

	export class MonitorController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IMonitorScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			if ($scope.monitorIndex === ($scope.monitors.length - 1)) {
				$scope.isOpen = true;
			}
		}

		public get Header(): string {
			const monitor = this.$scope.monitor.monitorClass;
			const name = this.$scope.monitor.name ? `(${this.$scope.monitor.name})` : '';
			return `${monitor} ${name}`;
		}

		public get CanMoveUp(): boolean {
			return this.$scope.monitorIndex !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.monitorIndex !== (this.$scope.monitors.length - 1);
		}

		public OnMoveUp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.monitors, this.$scope.monitorIndex);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.monitors, this.$scope.monitorIndex);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.monitors.splice(this.$scope.monitorIndex, 1);
			this.$scope.form.$setDirty();
		}
	}
}
