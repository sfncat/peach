/// <reference path="../reference.ts" />

namespace Peach {
	export class EulaController {
		static $inject = [
			C.Angular.$uibModalInstance
		];

		constructor(
			private $modalInstance: ng.ui.bootstrap.IModalServiceInstance
		) {
		}

		public OnSubmit() {
			this.$modalInstance.close();
		}
	}
}
