/// <reference path="../reference.ts" />

namespace Peach {
	export interface IFaultDetailScope extends IFaultSummaryScope {
		FaultDetailTitle: string;
	}

	function FaultsTitle(bucket: string) {
		return (bucket === "all") ? 'Faults' : 'Bucket: ' + bucket;
	}

	export class FaultsDetailController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Services.Job
		];

		constructor(
			$scope: IFaultDetailScope,
			$state: ng.ui.IStateService,
			jobService: JobService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);

			const id = $state.params['id'];
			$scope.FaultDetailTitle = 'Test Case: ' + id;
			const promise = jobService.LoadFaultDetail(id);
			promise.then((detail: IFaultDetail) => {
				this.Fault = detail;
				for (const file of detail.files) {
					const assets = file.initial ? this.InitialAssets : this.Assets;
					this.organizeFile(file, assets);
				}
			}, () => {
				$state.go(C.States.MainHome);
			});
		}

		public Fault: IFaultDetail;
		
		public Assets: IFaultAssets = {
			TestData: [],
			MonitorAssets: [],
			Other: []
		};
		
		public InitialAssets: IFaultAssets = {
			TestData: [],
			MonitorAssets: [],
			Other: []
		};

		public HasInitialAssets: boolean = false;

		organizeFile(file: IFaultFile, assets: IFaultAssets) {
			if (assets === this.InitialAssets) {
				this.HasInitialAssets = true;
			}

			if (file.type === FaultFileType.Asset) {
				if (_.isEmpty(file.agentName) || _.isEmpty(file.monitorClass) || _.isEmpty(file.monitorName)) {
					assets.Other.push(file);
				} else {
					let lastAgent = _.last(assets.MonitorAssets);
					if (_.isUndefined(lastAgent) || lastAgent.agentName !== file.agentName) {
						lastAgent = {
							agentName: file.agentName,
							children: []
						}

						assets.MonitorAssets.push(lastAgent);
					}

					let lastMonitor = _.last(lastAgent.children);
					if (_.isUndefined(lastMonitor) || lastMonitor.monitorName !== file.monitorName) {
						lastMonitor = {
							monitorName: file.monitorName,
							monitorClass: file.monitorClass,
							children: []
						}

						lastAgent.children.push(lastMonitor);
					}

					lastMonitor.children.push(file);
				}
			} else {
				assets.TestData.push(file);
			}
		}
	}

	export interface IFaultSummaryScope extends IViewModelScope {
		FaultSummaryTitle: string;
	}

	export class FaultsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state
		];

		constructor(
			$scope: IFaultSummaryScope,
			$state: ng.ui.IStateService
		) {
			$scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);
		}
	}
}
