/// <reference path="../reference.ts" />

namespace Peach {
	export const JobsDirective: IDirective = {
		ComponentID: C.Directives.Jobs,
		restrict: 'E',
		templateUrl: C.Templates.Directives.Jobs,
		controller: C.Controllers.Jobs,
		scope: { limit: '=' }
	}

	export interface IJobsDirectiveScope extends IViewModelScope {
		limit?: number;
	}

	export class JobsDirectiveController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$uibModal,
			C.Angular.$window,
			C.Services.Job
		];

		constructor(
			private $scope: IJobsDirectiveScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private $window: ng.IWindowService,
			private jobService: JobService
		) {
			$scope.vm = this;
			this.refresh(this.jobService.GetJobs());
		}

		public Jobs: IJob[] = [];
		public AllJobs: IJob[] = [];
		private pendingDelete: IJob;

		public OnJobSelected(job: IJob): void {
			this.$state.go(C.States.Job, { job: job.id });
		}

		public IsReportDisabled(job: IJob): boolean {
			return !_.isUndefined(this.pendingDelete) || !_.isString(job.reportUrl);
		}

		public IsRemoveDisabled(job: IJob): boolean {
			return !_.isUndefined(this.pendingDelete) || job.status !== JobStatus.Stopped;
		}

		public IsActive(job: IJob): boolean {
			return job.status !== JobStatus.Stopped;
		}

		public OnRemove($event: ng.IAngularEvent, job: IJob): void {
			$event.preventDefault();
			$event.stopPropagation();
			
			const options: IConfirmOptions = {
				SubmitPrompt: 'Delete Job'
			};
			Confirm(this.$modal, options).result
				.then(result => {
					if (result === 'ok') {
						this.pendingDelete = job;
						this.refresh(this.jobService.Delete(job));
					}
				})
			;
		}

		public OnViewReport($event: ng.IAngularEvent, job: IJob): void {
			$event.preventDefault();
			$event.stopPropagation();
			this.$window.open(job.reportUrl);
		}
		
		private refresh(promise: ng.IPromise<IJob[]>): void {
			promise.then((jobs: IJob[]) => {
				if (this.$scope.limit) {
					this.AllJobs = _.takeRight(jobs, this.$scope.limit);
				} else {
					this.AllJobs = jobs;
				}
				this.pendingDelete = undefined;
			});
			promise.catch(() => {
				this.pendingDelete = undefined;
			});
		}
		
		public RemoveIconClass(job: IJob): any {
			return job === this.pendingDelete ? 
				'fa-spin fa-refresh' : 
				'fa-remove';
		}
	}
}
