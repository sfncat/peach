/// <reference path="../reference.ts" />

namespace Peach {
	export class ConfigureWebProxyController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModal,
			C.Services.Pit
		];

		constructor(
			private $scope: IFormScope,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.Routes = pit.webProxy.routes;
				for (let route of this.Routes) {
					route.faultOnStatusCodesText = _.join(route.faultOnStatusCodes, ',');
					route.mutateChoice = route.mutate ? 'Include' : 'Exclude';
					if (route.headers) {
						for (let header of route.headers) {
							header.mutateChoice = header.mutate ? 'Include' : 'Exclude';
						}
					}
				}
				this.hasLoaded = true;
			});
		}

		private hasLoaded = false;
		private isSaved = false;
		public Routes: IWebRoute[];

		public get ShowLoading(): boolean {
			return !this.hasLoaded;
		}

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowError(): boolean {
			return this.$scope.form.$invalid;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public OnSave(): void {
			for (let route of this.Routes) {
				route.faultOnStatusCodes = _.map(
					_.split(route.faultOnStatusCodesText, ','),
					_.parseInt
				);
				route.mutate = route.mutateChoice === 'Include';
				if (route.headers) {
					for (let header of route.headers) {
						header.mutate = header.mutateChoice === 'Include';
					}
				}
			}
			const promise = this.pitService.SaveWebProxy({ routes: this.Routes });
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		public OnAdd(): void {
			this.Routes.unshift({
				url: "",
				swagger: "",
				script: "",
				mutate: false,
				baseUrl: "",
				faultOnStatusCodes: [500, 501],
				headers: [
					{
						name: "*",
						mutate: true,
						mutateChoice: 'Include'
					}
				],
				faultOnStatusCodesText: "500,501",
				mutateChoice: 'Include'
			});
			this.$scope.form.$setDirty();
		}
	}
}
