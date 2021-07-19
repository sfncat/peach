/// <reference path="../reference.ts" />

namespace Peach {
	export class ConfigureDefinesController {
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
			const UserDefinesName = "User Defines";
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.View = pit.definesView;

				for (let group of this.View) {
					if (group.type === ParameterType.Group && 
						group.name === UserDefinesName) {
						this.UserDefines = group;
					}
				};

				if (!this.UserDefines) {
					this.UserDefines = {
						type: ParameterType.Group,
						name: UserDefinesName,
						items: []
					};
				
					let systemDefines = this.View.pop();
					this.View.push(this.UserDefines);
					this.View.push(systemDefines);
				}

				this.hasLoaded = true;
			});
		}

		private hasLoaded = false;
		private isSaved = false;
		public View: IParameter[];
		public UserDefines: IParameter;

		public get ShowLoading(): boolean {
			return !this.hasLoaded;
		}

		public get ShowSaved(): boolean {
			return !this.$scope.form.$dirty && this.isSaved;
		}

		public get ShowRequired(): boolean {
			return this.$scope.form.$pristine && this.$scope.form.$invalid;
		}

		public get ShowValidation(): boolean {
			return this.$scope.form.$dirty && this.$scope.form.$invalid;
		}

		public get CanSave(): boolean {
			return this.$scope.form.$dirty && !this.$scope.form.$invalid;
		}

		public OnSave(): void {
			const promise = this.pitService.SaveDefines(this.View);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		public OnAdd(): void {
			const modal = this.$modal.open({
				templateUrl: C.Templates.Modal.NewVar,
				controller: NewVarController
			});

			modal.result.then((param: IParameter) => {
				this.UserDefines.items.push(param);
				this.$scope.form.$setDirty();
			});
		}
	}
}
