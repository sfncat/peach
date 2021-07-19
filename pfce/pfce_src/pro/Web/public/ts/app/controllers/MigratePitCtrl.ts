/// <reference path="../reference.ts" />

namespace Peach {
	export interface IMigratePitScope extends IViewModelScope {
		search: string;
	}

	export class MigratePitController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			C.Services.Pit,
			"Lib",
			"Pit"
		];

		constructor(
			private $scope: IMigratePitScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			private pitService: PitService,
			public Lib: PitLibrary,
			public Pit: IPit
		) {
			$scope.vm = this;
		}

		private pending: boolean = false;
		private selected: PitEntry;
		public Error: string = "";

		public get CanAccept(): boolean {
			return !_.isUndefined(this.selected) && !this.pending;
		}

		public OnSelect(item: PitEntry): void {
			this.selected = item;
		}

		public OnCustom(): void {
			this.MigratePit(this.Pit);
		}

		public Accept(): void {
			this.MigratePit(this.selected.Pit);
		}

		private MigratePit(originalPit: IPit): void {
			this.Error = "";
			this.pending = true;

			this.pitService.MigratePit(this.Pit, originalPit)
				.then((response: ng.IHttpPromiseCallbackArg<IPit>) => {
					this.pending = false;
					this.$modalInstance.close(response.data);
				},
				(response: ng.IHttpPromiseCallbackArg<any>) => {
					this.pending = false;
					switch (response.status) {
						case 400:
							this.Error = `${this.Pit.name} already exists.`;
							break;
						default:
							this.Error = `Error: ${response.statusText}`;
							break;
					}
				});
		}

		public Cancel(): void {
			this.$modalInstance.dismiss();
		}

		public filterCategory(search: string) {
			return (category: PitCategory) => {
				if (_.isEmpty(search)) {
					return true;
				}
				return _.some(category.Pits, entry => {
					return _.includes(entry.Pit.name.toLowerCase(), search.toLowerCase());
				});
			}
		}
	}
}
