/// <reference path="../reference.ts" />

namespace Peach {
	interface IConfigureStorage {
		showCfgHelp: boolean;
		showStartHelp: boolean;
	}

	export class ConfigureController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$localStorage,
			C.Services.Pit,
			C.Services.Job
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			$localStorage,
			private pitService: PitService,
			private jobService: JobService
		) {
			this.storage = $localStorage['$default']({
				showCfgHelp: true,
				showStartHelp: true
			});

			this.pitService.LoadPit().then((pit: IPit) => {
				this.Pit = pit;
				this.Job = {
					pitUrl: pit.pitUrl,
					seed: $state.params['seed'],
					rangeStart: $state.params['rangeStart'] || undefined,
					rangeStop: $state.params['rangeStop']
				};
			});
		}

		public Job: IJobRequest;
		public Pit: IPit;
		private storage: IConfigureStorage;

		public get ShowReady(): boolean {
			return this.pitService.HasMonitors && this.CanStart;
		}

		public get ShowNotConfigured(): boolean {
			return onlyIf(this.pitService.Pit, () => !this.pitService.IsConfigured);
		}

		public get ShowNoMonitors(): boolean {
			return onlyIf(this.pitService.Pit, () =>
				this.pitService.IsConfigured && !this.pitService.HasMonitors
			);
		}

		public get ShowWebProxy(): boolean {
			return onlyIf(this.pitService.Pit, () => this.pitService.IsWebProxy);
		}

		public get CanStart(): boolean {
			return onlyIf(this.pitService.Pit, () => this.pitService.IsConfigured);
		}

		public Start(): void {
			this.jobService.Start(this.Job)
				.then((job: IJob) => {
					this.$state.go(C.States.Job, { job: job.id });
				})
				;
		}

		public OnCfgHelp(): void {
			this.storage.showCfgHelp = !this.storage.showCfgHelp;
		}

		public OnStartHelp(): void {
			this.storage.showStartHelp = !this.storage.showStartHelp;
		}

		public get StartHelpClass() {
			return { active: this.storage.showStartHelp };
		}

		public get CfgHelpClass() {
			return { active: this.storage.showCfgHelp };
		}

		public get StartHelpPrompt(): string {
			return this.storage.showStartHelp ? 'Hide' : 'Help';
		}

		public get CfgHelpPrompt(): string {
			return this.storage.showCfgHelp ? 'Hide' : 'Help';
		}
	}
}
