/// <reference path="../reference.ts" />

namespace Peach {
	export interface IPitTestScope extends IViewModelScope {
		Title: string;
	}

	export class PitTestController {

		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Pit,
			C.Services.Test
		];

		constructor(
			$scope: IPitTestScope,
			private $state: ng.ui.IStateService,
			private pitService: PitService,
			private testService: TestService
		) {
			$scope.Title = "Test";
			this.pitService.LoadPit();
		}

		public Title = 'Test';

		public get ShowNotConfigured(): boolean {
			return !this.pitService.IsConfigured;
		}

		public get ShowNoMonitors(): boolean {
			return this.pitService.IsConfigured && !this.pitService.HasMonitors;
		}

		public get CanBeginTest(): boolean {
			return this.pitService.IsConfigured && this.testService.CanBeginTest;
		}

		public get CanContinue(): boolean {
			return onlyIf(this.testService.TestResult, () => { 
				return this.testService.CanBeginTest &&
					this.testService.TestResult.status === TestStatus.Pass;
			}) || false;
		}

		public OnBeginTest() {
			this.testService.BeginTest();
		}

		public OnNextTrack() {
			this.$state.go(C.States.Pit);
		}
	}
}
