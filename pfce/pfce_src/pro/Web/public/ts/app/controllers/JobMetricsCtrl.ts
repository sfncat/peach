/// <reference path="../reference.ts" />

namespace Peach {
	export interface ITimelineItem {
		className?: string;
		content: string;
		end?: Date;
		group?: any;
		id?: number;
		start: Date;
		title?: string;
		type?: string;
		data?: IBucketTimelineMetric
	}

	export interface ITimelineOptions {
		align?: string;
		autoResize?: boolean;
		clickToUse?: boolean;
		dataAttributes?: string[];
		editable?: any;
		end?: any;
		groupOrder?: any;
		height?: any;
		locale?: string;
		locales?: Object;
		margin?: Object;
		max?: any;
		maxHeight?: any;
		min?: any;
		minHeight?: any;
		onAdd?: Function;
		onUpdate?: Function;
		onMove?: Function;
		onMoving?: Function;
		onRemove?: Function;
		orientation?: string;
		padding?: number;
		selectable?: boolean;
		showCurrentTime?: boolean;
		showCustomTime?: boolean;
		showMajorLabels?: boolean;
		showMinorLabels?: boolean;
		stack?: boolean;
		start?: any;
		template?: Function;
		type?: string;
		width?: string;
		zoomable?: boolean;
		zoomMax?: number;
		zoomMin?: number;
	}

	export interface IMetricsScope extends IViewModelScope {
		metric: string;
	}

	export class MetricsController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$interpolate,
			C.Angular.$templateCache,
			C.Angular.$timeout,
			C.Vendor.VisDataSet,
			C.Services.Job
		];

		constructor(
			private $scope: IMetricsScope,
			private $state: ng.ui.IStateService,
			private $interpolate: ng.IInterpolateService,
			private $templateCache: ng.ITemplateCacheService,
			private $timeout: ng.ITimeoutService,
			private VisDataSet,
			private jobService: JobService
		) {
			this.$scope.metric = $state.params['metric'];

			if (this.jobService.Job) {
				this.update();
			} else {
				const unwatch = $scope.$watch(() => jobService.Job,(newVal, oldVal) => {
					if (newVal !== oldVal) {
						this.update();
						unwatch();
					}
				});
			}
		}

		public MutatorData: IMutatorMetric[] = [];
		public AllMutatorData: IMutatorMetric[] = [];

		public ElementData: IElementMetric[] = [];
		public AllElementData: IElementMetric[] = [];

		public DatasetData: IDatasetMetric[] = [];
		public AllDatasetData: IDatasetMetric[] = [];

		public StateData: IStateMetric[] = [];
		public AllStateData: IStateMetric[] = [];

		public BucketData: IBucketMetric[] = [];
		public AllBucketData: IBucketMetric[] = [];

		public FaultsOverTimeLabels: string[] = [
			moment(Date.now()).format("M/D h a")
		];

		public FaultsOverTimeData: number[][] = [
			[0]
		];

		public BucketTimelineData = undefined;
		public BucketTimelineLoaded: boolean = false;

		public BucketTimelineOptions: ITimelineOptions = {
			showCurrentTime: true,
			selectable: false,
			type: "box",
			template: (item: ITimelineItem) => {
				if (item.content) {
					return item.content;
				}
				const html = this.$templateCache.get(C.Templates.Job.BucketTimelineItem);
				return this.$interpolate(html)({ item: item });
			}
		}

		private update(): void {
			const promise = this.jobService.LoadMetric(this.$scope.metric);
			switch (this.$scope.metric) {
			case C.Metrics.BucketTimeline.id:
				const items = new this.VisDataSet();

				if (_.isUndefined(this.BucketTimelineData)) {
					this.BucketTimelineData = {
						items: items
					};
				}
				
				promise.success((data: IBucketTimelineMetric[]) => {
					data.forEach((item: IBucketTimelineMetric) => {
						item.href = this.$state.href(C.States.JobFaults, { bucket: item.label });
						items.add({
							id: item.iteration,
							content: undefined,
							start: item.time,
							data: item
						});
					});

					items.add({
						id: 0,
						style: "color: green",
						content: "Job Start",
						start: this.jobService.Job.startDate
					});

					if (this.jobService.Job.stopDate) {
						items.add({
							id: -1,
							style: "color: red",
							content: "Job End",
							start: this.jobService.Job.stopDate
						});
					}
					
					this.BucketTimelineData = {
						items: items
					};
					
					this.BucketTimelineLoaded = true;
				});
				break;
			case C.Metrics.FaultTimeline.id:
				promise.success((data: IFaultTimelineMetric[]) => {
					if (data.length === 0) {
						this.FaultsOverTimeLabels = [moment(Date.now()).format("M/D h a")];
						this.FaultsOverTimeData = [[0]];
					} else {
						this.FaultsOverTimeLabels = data.map(x => moment(x.date).format("M/D h a")),
						this.FaultsOverTimeData = [_.map<IFaultTimelineMetric, number>(data, 'faultCount')];
					}
				});
				break;
			case C.Metrics.Mutators.id:
				promise.success((data: IMutatorMetric[]) => {
					this.AllMutatorData = data;
				});
				break;
			case C.Metrics.Elements.id:
				promise.success((data: IElementMetric[]) => {
					this.AllElementData = data;
				});
				break;
			case C.Metrics.Dataset.id:
				promise.success((data: IDatasetMetric[]) => {
					this.AllDatasetData = data;
				});
				break;
			case C.Metrics.States.id:
				promise.success((data: IStateMetric[]) => {
					this.AllStateData = data;
				});
				break;
			case C.Metrics.Buckets.id:
				promise.success((data: IBucketMetric[]) => {
					this.AllBucketData = data;
				});
				break;
			}
		}
	}
}
