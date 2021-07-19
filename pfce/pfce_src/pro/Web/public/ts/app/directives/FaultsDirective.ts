/// <reference path="../reference.ts" />

namespace Peach {
	export const FaultsDirective: IDirective = {
		ComponentID: C.Directives.Faults,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Faults,
		controller: C.Controllers.Faults,
		scope: {
			limit: '='
		}
	}

	export interface IFaultsDirectiveScope extends IViewModelScope {
		limit?: number;
	}

	export class FaultsDirectiveController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			private $scope: IFaultsDirectiveScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService
		) {
			$scope.vm = this;

			this.bucket = $state.params['bucket'] || 'all';

			$scope.$watch(() => jobService.Faults.length, (newVal, oldVal) => {
				if (newVal !== oldVal) {
					this.RefreshFaults();
				}
			});

			this.RefreshFaults();
		}

		private bucket: string;
		public Faults: IFaultSummary[] = [];
		public AllFaults: IFaultSummary[] = [];

		public get DefaultSort() {
			return this.$scope.limit ? 'reverse' : 'forward';
		} 

		public OnFaultSelected(fault: IFaultSummary) {
			const params = {
				bucket: this.bucket,
				id: fault.iteration
			};
			this.$state.go(C.States.JobFaultsDetail, params);
		}

		private RefreshFaults() {
			let faults: IFaultSummary[];
			if (this.bucket === 'all') {
				faults = this.jobService.Faults;
			} else {
				faults = _.filter(this.jobService.Faults, (fault: IFaultSummary) => {
					return this.bucket === (`${fault.majorHash}_${fault.minorHash}`);
				});
			}

			if (this.$scope.limit) {
				this.AllFaults = _.takeRight(faults, this.$scope.limit);
			} else {
				this.AllFaults = faults;
			}
		}
	}
}
 