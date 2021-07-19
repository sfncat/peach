/// <reference path="../reference.ts" />

namespace Peach {
	export interface ILicenseOptions {
		Title: string;
		Body: string[];
	}

	export class LicenseController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$uibModalInstance,
			'Options'
		];

		constructor(
			private $scope: IViewModelScope,
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance,
			public Options: ILicenseOptions
		) {
		}

		public OnSubmit() {
			this.$modalInstance.close();
		}
	}
}
