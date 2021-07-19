/// <reference path="../reference.ts" />

namespace Peach {
	export class NewConfigController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			"Title",
			"Prompt",
			"Pit",
			"OnSubmit"
		];

		constructor(
			$scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			public Title: string,
			public Prompt: string,
			public Pit: IPit,
			public OnSubmit: Function
		) {
			$scope.vm = this;
		}

		private pending: boolean = false;
		public Error: string = "";

		public Submit() {
			this.Error = "";
			this.pending = true;
			this.OnSubmit(this);
		}

		public SetError(msg: string) {
			this.pending = false;
			this.Error = msg;
		}

		public Close(result: IPit) {
			this.pending = false;
			this.$modalInstance.close(result);
		}

		public Cancel() {
			this.$modalInstance.dismiss();
		}

		public get IsSubmitDisabled(): boolean {
			return this.pending;
		}
	}
}
