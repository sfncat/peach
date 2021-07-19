/// <reference path="../reference.ts" />

namespace Peach {
	export class ConfigureMonitorsController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private pitService: PitService
		) {
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.Agents = pit.agents;
				this.hasLoaded = true;
			});
		}

		private hasLoaded: boolean = false;
		private isSaved: boolean = false;

		public Agents: IAgent[];

		public get ShowLoading(): boolean {
			return !this.hasLoaded;
		}

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowError(): boolean {
			return this.$scope.form.$invalid;
		}

		public get ShowMissingAgents(): boolean {
			return this.hasLoaded && this.NumAgents === 0;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public AddAgent(): void {
			this.Agents.push({
				name: "",
				agentUrl: "",
				monitors: []
			});
			this.$scope.form.$setDirty();
		}

		public Save(): void {
			const promise = this.pitService.SaveAgents(this.Agents);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		private get NumAgents(): number {
			return (this.Agents && this.Agents.length) || 0;
		}
	}
}
