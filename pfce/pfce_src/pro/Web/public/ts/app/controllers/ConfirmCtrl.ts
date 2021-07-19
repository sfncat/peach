/// <reference path="../reference.ts" />

namespace Peach {
	export interface IConfirmOptions {
		Title?: string;
		Body?: string;
		SubmitPrompt: string;
		CancelPrompt?: string;
	}

	export function Confirm(
		$modal: ng.ui.bootstrap.IModalService,
		options: IConfirmOptions
	) {
		return $modal.open({
			templateUrl: C.Templates.Modal.Confirm,
			controller: ConfirmController,
			controllerAs: C.ViewModel,
			resolve: { Options: () => options }
		});
	}

	class ConfirmController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			'Options'
		];

		constructor(
			private $scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			public Options: IConfirmOptions
		) {
		}

		public OnCancel() {
			this.$modalInstance.dismiss();
		}

		public OnSubmit() {
			this.$modalInstance.close('ok');
		}
	}
}
