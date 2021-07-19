/// <reference path="../reference.ts" />

namespace Peach {
	export interface IAlertOptions {
		Title?: string;
		Body?: string;
		ButtonText?: string;
	}

	export function Alert(
		$modal: ng.ui.bootstrap.IModalService,
		options: IAlertOptions
	) {
		return $modal.open({
			templateUrl: C.Templates.Modal.Alert,
			controller: AlertController,
			controllerAs: C.ViewModel,
			resolve: { Options: () => options }
		});
	}

	class AlertController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			'Options'
		];

		constructor(
			private $scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			public Options: IAlertOptions
		) {
		}

		public OnSubmit() {
			this.$modalInstance.dismiss();
		}
	}
}
