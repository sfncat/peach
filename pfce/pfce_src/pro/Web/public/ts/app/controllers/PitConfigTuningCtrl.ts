/// <reference path="../reference.ts" />

namespace Peach {
	const SHIFT_WIDTH = 20;
	const MAX_NODES = 2000;
	const DELAY = 500;

	export interface FlatNode {
		node: IPitFieldNode;
		parent: FlatNode;
		id: string;
		fullId: string;
		depth: number;
		style: any;
		showExpander: boolean;
		display: string;
		weight?: number;
		visible?: boolean;
		expanded?: boolean;
		weightIcons?: string[];
		expanderIcon?: string;
		include?: boolean;
	}

	function defaultWeight(node: IPitFieldNode) {
		return _.isUndefined(node.weight) ? 3 : node.weight;
	}

	interface Result {
		nodes: FlatNode[];
		total: number;
	}

	function flatten(
		nodes: IPitFieldNode[], 
		depth: number, 
		prefix: string,
		parent: FlatNode, 
		result: Result) {
		nodes.forEach(node => {
			const here = `${prefix}${node.id}`;
			const flat: FlatNode = {
				node: node,
				parent: parent,
				id: node.id,
				fullId: here,
				depth: depth,
				style: { 'margin-left': depth * SHIFT_WIDTH },
				showExpander: !_.isEmpty(node.fields),
				display: node.id
			};

			result.nodes.push(flat);
			result.total++;

			flatten(node.fields, depth + 1, `${flat.fullId}.`, flat, result);
		});
	}

	function includeNode(node: FlatNode) {
		if (_.isNull(node) || node.include)
			return;
		node.include = true;
		includeNode(node.parent);
	}

	function expandNode(node: FlatNode) {
		if (_.isNull(node) || node.node.expanded)
			return;
		node.node.expanded = true;
		expandNode(node.parent);
	}

	function matchWeight(node: IPitFieldNode, weight: number) {
		return (defaultWeight(node) === weight) ||
			_.some(node.fields, field => matchWeight(field, weight));
	}

	function selectWeight(node: IPitFieldNode, weight: number) {
		node.weight = weight;
		const fields = node.fields || [];
		fields.forEach(field => selectWeight(field, weight));
	}

	function applyWeights(weights: IPitWeight[], fields: IPitFieldNode[]) {
		// console.time('applyWeights');
		for (const rule of weights) {
			const parts = rule.id.split('.');
			applyWeight(fields, parts, rule.weight);
		}
		// console.timeEnd('applyWeights');
	}

	function applyWeight(fields: IPitFieldNode[], parts: string[], weight: number) {
		const next = parts.shift();
		for (const node of fields) {
			if (node.id === next) {
				if (parts.length === 0) {
					node.weight = weight;
				} else {
					applyWeight(node.fields, parts, weight);
				}
			}
		}
	}

	function extractWeights(prefix: string, tree: IPitFieldNode[], collect: IPitWeight[]) {
		for (const node of tree) {
			const here = `${prefix}${node.id}`;
			if (defaultWeight(node) !== 3) {
				collect.push({id: here, weight: node.weight});
			}
			extractWeights(`${here}.`, node.fields, collect);
		}
	}

	function cloneFields(fields: IPitFieldNode[]): IPitFieldNode[] {
		return fields.map(item => ({ 
			id: item.id, 
			fields: cloneFields(item.fields) 
		}));
	}

	export interface ITuningScope extends IFormScope {
		flat: FlatNode[];
		hasLoaded: boolean;
		hasData: boolean;
		isTruncated: boolean;
		MAX_NODES: number;
		search: string;
		lastSearch: string;
	}
	
	export class ConfigureTuningController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		private DelayedOnSearch: Function;
		private pit: IPit = null;
		private isSaved = false;
		private tree: IPitFieldNode[] = [];
		private source: FlatNode[] = [];
		private total: number = 0;
		private nodeHover: FlatNode = null;
		private hovers: boolean[] = [
			false,
			false,
			false,
			false,
			false,
			false
		];

		constructor(
			private $scope: ITuningScope,
			private pitService: PitService
		) {
			this.$scope.search = '';
			this.$scope.lastSearch = '';
			this.$scope.hasLoaded = false;
			this.$scope.hasData = false;
			this.$scope.isTruncated = false;
			this.$scope.MAX_NODES = MAX_NODES;
			this.DelayedOnSearch = _.debounce(() => this.OnSearch(), DELAY);

			// console.time('load');
			const promise = pitService.LoadPit();
			promise.then((pit: IPit) => {
				this.pit = pit;
				if (pit.metadata.fields) {
					this.init();
					this.update();
					this.$scope.hasData = true;
				}
				this.$scope.hasLoaded = true;
				// setTimeout(() => console.timeEnd('load'));
			});
		}

		init() {
			// console.time('clone');
			this.tree = cloneFields(this.pit.metadata.fields);
			// console.timeEnd('clone');
			applyWeights(this.pit.weights, this.tree);
		
			const result: Result = {
				nodes: [],
				total: 0
			};
			// console.time('flatten');
			flatten(this.tree, 0, '', null, result);
			// console.timeEnd('flatten');

			this.source = result.nodes;
			this.total = result.total;
		}

		update() {
			// console.time('update');

			this.source.forEach(node => {
				const parent = node.parent;
				const inner = node.node;
				node.visible = !parent || (parent.expanded && parent.visible);
				node.expanded = _.isUndefined(inner.expanded) ?
					node.depth < 2 :
					inner.expanded;
				node.expanderIcon = node.expanded ? 'fa-minus' : 'fa-plus';
				node.weight = defaultWeight(inner);
				node.weightIcons = _.range(6).map(i => (
					(node.weight === i) ? 'fa-circle' :
						(!node.expanded && matchWeight(inner, i)) ?
							'fa-dot-circle-o' :
							'fa-circle-thin'
				));
			});

			const visible = _.filter(this.source, 'visible');
			this.$scope.isTruncated = (visible.length > MAX_NODES);
			this.$scope.flat = _.take(visible, MAX_NODES);

			// console.timeEnd('update');
		
			// console.log('nodes', this.total, this.$scope.flat.length);
		}

		search(search: string) {
			// console.time('search');

			this.init();

			const parts = search.split('.').reverse();
			const lastPart = parts.shift();
			const partial = new RegExp(`(${lastPart})`, 'gi');
			const starting = new RegExp(`^(${lastPart})`, 'i');

			this.source.forEach(node => {
				const id = node.id.toLowerCase();
				const fullId = node.fullId.toLowerCase();
				if (_.isEmpty(parts)) {
					// use partial search of leaf
					if (_.includes(fullId, lastPart)) {
						includeNode(node);
					}
					if (_.includes(id, lastPart)) {
						expandNode(node.parent);
						node.display = node.id.replace(partial, '<strong>$1</strong>');
					}
				} else {
					// a dot appears in the search, match structure
					if (_.startsWith(id, lastPart)) {
						// match parents
						let cur = node.parent;
						for (const part of parts) {
							if (_.isNull(cur)) {
								return;
							}
							if (cur.id.toLowerCase() !== part) {
								return;
							}
							cur = cur.parent;
						}

						// if we get this far, we've found a full match
						includeNode(node);
						expandNode(node.parent);

						// highlight the leaf
						node.display = node.id.replace(starting, '<strong>$1</strong>');

						// highlight parents
						cur = node.parent;
						for (const part of parts) {
							cur.display = `<strong>${cur.id}</strong>`;
							cur = cur.parent;
						}
					}
				}
			});

			this.source = _.filter(this.source, 'include');

			// console.timeEnd('search');
		}

		DelayedApply() {
			setTimeout(() => this.$scope.$apply(), 100);
		}

		LegendText(i: number) {
			return this.hovers[i] ? 'text bold' : 'text';
		}

		LegendIcon(i: number) {
			return this.hovers[i] ? 'fa-circle' : 'fa-circle-thin';
		}

		OnLegendEnter(i: number) {
			this.hovers[i] = true;
		}

		OnLegendLeave(i: number) {
			this.hovers[i] = false;
		}

		isHovered(node: FlatNode) {
			return !_.isNull(this.nodeHover) && (node.node === this.nodeHover.node);
		}
		
		RowHover(node: FlatNode) {
			return this.isHovered(node) ? 'tuning-row-hover' : '';
		}

		OnRowEnter(node: FlatNode) {
			this.nodeHover = node;
		}

		OnRowLeave(node: FlatNode) {
			this.nodeHover = null;
		}

		OnToggleExpand(node: FlatNode) {
			if (!node.showExpander)
				return;

			node.node.expanded = !node.expanded;
			node.expanderIcon = 'fa-spin fa-clock-o';
			setTimeout(() => {
				this.update();
				this.DelayedApply();
			});
		}

		OnSelectWeight(node: FlatNode, weight: number) {
			node.weightIcons[weight] = 'fa-spin fa-clock-o';
			selectWeight(node.node, weight);
			setTimeout(() => {
				this.update();
				this.$scope.form.$setDirty();
				this.DelayedApply();
			});
		}

		get ShowSaved(): boolean {
			return !this.$scope.form.$dirty  && this.isSaved;
		}

		get CanSave(): boolean {
			return this.$scope.form.$dirty;
		}

		get CanSearch(): boolean {
			return this.$scope.hasData;
		}

		OnSave(): void {
			const weights = [];
			// console.time('extractWeights');
			extractWeights('', this.tree, weights);
			// console.timeEnd('extractWeights');
			// console.log(weights.length);

			const promise = this.pitService.SaveWeights(weights);
			promise.then(() => {
				this.isSaved = true;
				this.$scope.form.$setPristine();
			});
		}

		OnSearch(): void {
			this.$scope.hasLoaded = false;
			setTimeout(() => {
				const search = this.$scope.search.toLowerCase();
				this.$scope.lastSearch = search;
				if (_.isEmpty(search)) {
					this.init();
				} else {
					this.search(search);
				}

				this.update();

				this.$scope.hasLoaded = true;
				this.DelayedApply();
			});
		}

		OnSearchChange(): void {
			if (this.total < MAX_NODES) {
				this.DelayedOnSearch();
			}
		}

		OnSearchKeyPress(event: KeyboardEvent): void {
			if (event.which === 13) {
				this.OnSearch();
			}
		}

		DirtySearch(): string {
			return this.$scope.search.toLowerCase() === this.$scope.lastSearch ? 
				'' : 
				'dirty-search';
		}
	}
}
