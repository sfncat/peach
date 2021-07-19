/// <reference path="../reference.ts" />

namespace Peach {
	export const FaultFilesDirective: IDirective = {
		ComponentID: C.Directives.FaultFiles,
		restrict: "E",
		templateUrl: C.Templates.Directives.FaultFiles,
		controller: C.Controllers.FaultFiles,
		scope: {
			files: "="
		}
	}

	export interface IFaultFilesScope extends IFormScope {
		files: IFaultFile[];
	}

	export class FaultFilesController {
		static $inject = [
			C.Angular.$scope
		];

		constructor(
			private $scope: IFaultFilesScope
		) {
			$scope.vm = this;
		}

		public Name(i: number, file: IFaultFile): string {
			if (file.type === FaultFileType.Asset) {
				return file.name;
			}

			const dir = (file.type === FaultFileType.Input) ? 'RX' : 'TX'
			return `#${i+1} - ${dir} - ${file.name}`;
		}
	}
}
