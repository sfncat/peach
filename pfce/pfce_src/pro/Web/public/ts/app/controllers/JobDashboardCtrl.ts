/// <reference path="../reference.ts" />

namespace Peach {
	export class DashboardController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private jobService: JobService
		) {
		}

		public get ShowLimited(): boolean {
			return onlyIf(this.Job, () => _.isEmpty(this.Job.pitUrl));
		}

		public get ShowStatus(): boolean {
			return !_.isUndefined(this.Job);
		}

		public get ShowCommands(): boolean {
			return this.JobStatus !== JobStatus.Stopped;
		}

		public get JobStatus(): string {
			return onlyIf(this.Job, () => this.Job.status);
		}

		public get JobMode(): string {
			return this.Job.mode;
		}

		public get Job(): IJob {
			return this.jobService.Job;
		}

		public get RunningTime(): string {
			return this.jobService.RunningTime;
		}

		public get CanPause(): boolean {
			return this.jobService.CanPause;
		}

		public get CanContinue(): boolean {
			return this.jobService.CanContinue;
		}

		public get CanStop(): boolean {
			return this.jobService.CanStop || this.jobService.CanKill;
		}

		public Pause(): void {
			this.jobService.Pause();
		}

		public Stop(): void {
			if (this.Job.status === JobStatus.Stopping) {
				this.jobService.Kill();
			} else {
				this.jobService.Stop();
			}
		}
		
		public Continue(): void {
			this.jobService.Continue();
		}

		public get StatusClass(): any {
			if (!_.isUndefined(this.Job) && !_.isUndefined(this.Job.result)) {
				return 'alert-danger';
			}
			return 'alert-info';
		}

		public ValueOr(value, alt): any {
			return _.isUndefined(value) ? alt : value;
		}

		public get IsEditDisabled(): boolean {
			return this.ShowLimited;
		}

		public get IsReplayDisabled(): boolean {
			return this.ShowLimited;
		}

		public OnEdit(): void {
			const pitId = _.last(this.Job.pitUrl.split('/'));
			this.$state.go(C.States.Pit, { pit: pitId });
		}

		public OnReplay(): void {
			const pitId = _.last(this.Job.pitUrl.split('/'));
			this.$state
				.go(C.States.Pit, {
					pit: pitId,
					seed: this.Job.seed,
					rangeStart: this.Job.rangeStart,
					rangeStop: this.Job.rangeStop
				})
				.catch(reason => {
					console.log('failed to go', reason);
				});
		}

		public get StopPrompt(): string {
			return (this.Job && this.Job.status === JobStatus.Stopping) ?
				"Abort" :
				"Stop";
		}

		public get StopIcon(): string {
			return (this.Job && this.Job.status === JobStatus.Stopping) ?
				"fa-power-off" :
				"fa-stop";
		}
	}
}
