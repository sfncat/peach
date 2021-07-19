 /// <reference path="../reference.ts" />

namespace Peach {
	export const JOB_INTERVAL = 3000;
	
	export class JobService {
		static $inject = [
			C.Angular.$rootScope,
			C.Angular.$q,
			C.Angular.$http,
			C.Angular.$uibModal,
			C.Angular.$state,
			C.Angular.$timeout,
			C.Services.Pit
		];

		constructor(
			private $rootScope: ng.IRootScopeService,
			private $q: ng.IQService,
			private $http: ng.IHttpService,
			private $modal: ng.ui.bootstrap.IModalService,
			private $state: ng.ui.IStateService,
			private $timeout: ng.ITimeoutService
		) {
		}

		private jobs: IJob[] = [];
		private poller: ng.IPromise<any>;
		private job: IJob;
		private faults: IFaultSummary[] = [];
		private pending: ng.IPromise<IJob>;
		private isActive: boolean;
		
		public OnEnter(id: string): void {
			this.isActive = true;
			this.onPoll(C.Api.JobUrl.replace(':id', id));
		}
		
		public OnExit(): void {
			this.isActive = false;

			if (this.poller) {
				this.$timeout.cancel(this.poller);
				this.poller = undefined;
			}

			this.job = undefined;
			this.$rootScope['job'] = undefined;
			this.faults = [];
		}
		
		public get Jobs(): IJob[]{
			return this.jobs;
		}
		
		public get Job(): IJob {
			return this.job;
		}

		public get Faults(): IFaultSummary[] {
			return this.faults;
		}

		public get IsRunning(): boolean {
			return this.Job && this.Job.status === JobStatus.Running;
		}

		public get IsPaused(): boolean {
			return this.Job && this.Job.status === JobStatus.Paused;
		}

		public get CanContinue(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Paused;
		}

		public get CanPause(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Running;
		}

		public get CanStop(): boolean {
			return this.isControlable && (
				this.Job.status === JobStatus.Starting ||
				this.Job.status === JobStatus.Running ||
				this.Job.status === JobStatus.Paused ||
				this.Job.status === JobStatus.Stopping
			);
		}

		public get CanKill(): boolean {
			return this.isControlable && this.Job.status === JobStatus.Stopping;
		}

		private get isControlable(): boolean {
			return this.Job && !_.isUndefined(this.Job.commands);
		}

		public get RunningTime(): string {
			if (_.isUndefined(this.Job)) {
				return undefined;
			}

			const duration = moment.duration(this.job.runtime, 'seconds');
			const days = Math.floor(duration.asDays());
			const hours = duration.hours().toString().paddingLeft('00');
			const minutes = duration.minutes().toString().paddingLeft('00');
			const seconds = duration.seconds().toString().paddingLeft('00');

			if (duration.asDays() >= 1) {
				return `${days}d ${hours}h ${minutes}m`;
			} else {
				return `${hours}h ${minutes}m ${seconds}s`;
			}
		}

		private doLoadFaultDetail(defer: ng.IDeferred<IFaultDetail>, id: string) {
			const fault = _.find(this.faults, { iteration: id });
			if (_.isUndefined(fault)) {
				defer.reject();
			} else {
				this.$http.get(fault.faultUrl)
					.success((data: IFaultDetail) => { defer.resolve(data); })
					.error(reason => { defer.reject(reason); })
				;
			}
		}

		public LoadFaultDetail(id: string): ng.IPromise<IFaultDetail> {
			var defer = this.$q.defer<IFaultDetail>();
			if (this.pending) {
				this.pending.finally(() => { this.doLoadFaultDetail(defer, id); });
			} else {
				this.doLoadFaultDetail(defer, id);
			}
			return defer.promise;
		}

		public GetJobs(): ng.IPromise<IJob[]> {
			const params = { dryrun: false };
			const promise = this.$http.get<IJob[]>(C.Api.Jobs, { params: params });
			promise.success((jobs: IJob[]) => this.jobs = jobs);
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				if (reason.status !== 401 && reason.status !== 402) {
					this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
				}
			});
			return StripHttpPromise(this.$q, promise);
		}

		public Start(job: IJobRequest): ng.IPromise<IJob> {
			const promise = this.$http.post<IJob>(C.Api.Jobs, job);
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				const options: IAlertOptions = {
					Title: 'Error Starting Job',
					Body: 'Peach was unable to start a new job.',
				};

				// 404 = Pit not found
				// 403 = Job already running

				if (reason.status === 403) {
					options.Body += "\n\nPlease ensure another job is not running and try again.";
				} else if (reason.status === 404) {
					options.Body += '\n\nPlease ensure the specified pit exists and try again.';
				}
				else {
					console.log('JobService.StartJob().error>', reason);
					return;
				}

				Alert(this.$modal, options);
			});
			return StripHttpPromise(this.$q, promise);
		}

		public Delete(job: IJob): ng.IPromise<any> {
			return this.$http.delete(job.jobUrl)
				.then(() => { return this.GetJobs(); })
				.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
					this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
				});
		}
		
		public Continue(): void {
			this.sendCommand(
				this.CanContinue, 
				JobStatus.ContinuePending, 
				this.job.commands.continueUrl);
		}

		public Pause(): void {
			this.sendCommand(
				this.CanPause, 
				JobStatus.PausePending, 
				this.job.commands.pauseUrl);
		}

		public Stop(): void {
			this.sendCommand(
				this.CanStop, 
				JobStatus.StopPending, 
				this.job.commands.stopUrl);
		}
		
		public Kill(): void {
			this.sendCommand(
				this.CanStop,
				JobStatus.KillPending,
				this.job.commands.killUrl);
		}

		private sendCommand(check: boolean, status: string, url: string): void {
			if (check) {
				this.job.status = status;
				const promise = this.$http.get(url);
				promise.success(() => this.onPoll(this.job.jobUrl));
				promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
					this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
				});
			}
		}

		private onPoll(url: string): void {
			this.pending = this.$http.get(url)
				.then((response: ng.IHttpPromiseCallbackArg<IJob>) => {
					if (!this.isActive)
						return undefined;

					const stopPending = (this.job && this.job.status === JobStatus.StopPending);
					const killPending = (this.job && this.job.status === JobStatus.KillPending);

					const job = response.data;
					this.job = job;

					if (this.job.status !== JobStatus.Stopped) {
						if (stopPending && this.job.status !== JobStatus.Stopping) {
							this.job.status = JobStatus.StopPending;
						} else if (killPending) {
							this.job.status = JobStatus.KillPending;
						}
					}

					this.$rootScope['job'] = this.job;

					if (job.status !== JobStatus.Stopped) {
						this.poller = this.$timeout(() => { this.onPoll(url); }, JOB_INTERVAL);
					}

					if (this.faults.length !== job.faultCount) {
						var deferred = this.$q.defer<IJob>();
						this.reloadFaults()
							.success(() => { deferred.resolve(this.job); })
							.error(reason => { deferred.reject(reason); })
							.finally(() => { this.pending = undefined; })
						;
						return deferred.promise;
					}

					return undefined;
				},(response: ng.IHttpPromiseCallbackArg<IError>) => {
					if (!this.isActive)
						return undefined;
					this.$state.go(C.States.MainError, { message: response.data.errorMessage });
				})
			;
		}

		private reloadFaults(): ng.IHttpPromise<IFaultSummary[]> {
			const promise = this.$http.get<IFaultSummary[]>(this.job.faultsUrl);
			promise.success((faults: IFaultSummary[]) => {
				this.faults = faults;
			});
			promise.catch((reason: ng.IHttpPromiseCallbackArg<IError>) => {
				this.$state.go(C.States.MainError, { message: reason.data.errorMessage });
			});
			return promise;
		}

		public LoadMetric<T>(metric: string): ng.IHttpPromise<T> {
			return this.$http.get(this.Job.metrics[metric]);
		}
	}
}
