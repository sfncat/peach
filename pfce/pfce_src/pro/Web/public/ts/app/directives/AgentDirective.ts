/// <reference path="../reference.ts" />

namespace Peach {
	export const AgentDirective: IDirective = {
		ComponentID: C.Directives.Agent,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Agent,
		controller: C.Controllers.Agent,
		scope: {
			agents: '=',
			agent: '=',
			agentIndex: '='
		}
	}

	export interface ISelectable<T> {
		selected: T;
	}

	export interface IAgentScope extends IFormScope {
		agents: IAgent[];
		agent: IAgent;
		agentIndex: number;
		isOpen: boolean;
	}

	export class AgentController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModal,
			C.Services.Pit
		];

		constructor(
			private $scope: IAgentScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			$scope.vm = this;
			$scope.isOpen = true;
		}

		public get Header(): string {
			const url = this.$scope.agent.agentUrl || 'local://';
			const name = this.$scope.agent.name ? `(${this.$scope.agent.name})` : '';
			return `${url} ${name}`;
		}

		public get CanMoveUp(): boolean {
			return this.$scope.agentIndex !== 0;
		}

		public get CanMoveDown(): boolean {
			return this.$scope.agentIndex !== (this.$scope.agents.length - 1);
		}

		public get ShowMissingMonitors(): boolean {
			return this.$scope.agent.monitors.length === 0;
		}

		public OnMoveUp($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemUp(this.$scope.agents, this.$scope.agentIndex);
			this.$scope.form.$setDirty();
		}

		public OnMoveDown($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			ArrayItemDown(this.$scope.agents, this.$scope.agentIndex);
			this.$scope.form.$setDirty();
		}

		public OnRemove($event: ng.IAngularEvent): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$scope.agents.splice(this.$scope.agentIndex, 1);
			this.$scope.form.$setDirty();
		}

		public AddMonitor(): void {
			const modal = this.$modal.open({
				templateUrl: C.Templates.Modal.AddMonitor,
				controller: AddMonitorController
			});

			modal.result.then((selected: IParameter) => {
				const monitor = this.pitService.CreateMonitor(selected);
				this.$scope.agent.monitors.push(monitor);
				this.$scope.form.$setDirty();
			});
		}
	}
}
