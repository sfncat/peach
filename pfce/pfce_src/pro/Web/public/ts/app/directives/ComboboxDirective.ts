/// <reference path="../reference.ts" />

namespace Peach {
	export interface IComboboxScope extends ng.IScope {
		vm: ComboboxController;
		data: any;
		options: string[];
		showOptions: boolean;
		placeholder: string;
		selected: string;
		highlighted?: number;
		$model: ng.INgModelController;
		$element: ng.IAugmentedJQuery;
		description: string;
	}

	const KEY = {
		TAB: 9,
		ENTER: 13,
		ESC: 27,
		UP: 38,
		DOWN: 40
	};

	export const ComboboxDirective: IDirective = {
		ComponentID: C.Directives.Combobox,
		restrict: 'E',
		require: [C.Directives.Combobox, C.Angular.ngModel],
		replace: true,
		controller: C.Controllers.Combobox,
		controllerAs: 'vm',
		templateUrl: C.Templates.Directives.Combobox,
		scope: {
			data: '=',
			placeholder: '&'
		},
		link: (
			scope: IComboboxScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrls: any
		) => {
			const ctrl: ComboboxController = ctrls[0];
			ctrl.Link(element, attrs, ctrls[1]);
		}
	}

	export class ComboboxController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$document
		];

		constructor(
			private $scope: IComboboxScope,
			private $document: ng.IDocumentService
		) {
		}

		private $element: ng.IAugmentedJQuery;
		private $model: ng.INgModelController;

		public Link(
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			this.$element = element;
			this.$model = ctrl;

			this.$scope.showOptions = false;
			this.$scope.options = [];
			this.$scope.highlighted = null;

			// Listen for the data to change and update options
			this.$scope.$watchCollection('data', (newVal, oldVal) => {
				if (newVal !== oldVal) {
					this.buildOptions();
				}
			});

			// Listen for the input value to change and handle any side effects
			this.$scope.$watch('selected', (newVal) => {
				if (this.$model.$viewValue !== newVal) {
					this.$model.$setViewValue(newVal);
					this.buildOptions(newVal);
				}
			});

			// model -> view
			this.$model.$formatters.unshift(value => {
				this.setSelected(value);
				return value;
			});

			// view -> model
			this.$model.$viewChangeListeners.unshift(() => {
				this.setSelected(this.$model.$viewValue);
			});

			const hideOptions = this.hideOptions.bind(this);
			this.$document.on('click', hideOptions);
			this.$element.on('$destroy', () => {
				this.$document.off('click', hideOptions);
			});
		}

		public SelectOption(option: string) {
			this.$scope.showOptions = false;
			this.$model.$setViewValue(option);
		}

		public OnKeyDown(event: KeyboardEvent) {
			if (event.keyCode === KEY.ENTER ||
				event.keyCode === KEY.TAB) {
				if (!_.isNull(this.$scope.highlighted)) {
					this.SelectOption(this.$scope.options[this.$scope.highlighted]);
					this.$scope.highlighted = null;
					event.preventDefault();
					event.stopPropagation();
				}
			}
		}

		public OnKeyUp(event: KeyboardEvent) {
			if (event.keyCode === KEY.ESC ||
				event.keyCode === KEY.TAB ||
				event.keyCode === KEY.ENTER) {
				this.$scope.showOptions = false;
				this.$scope.highlighted = null;
				return;
			}

			if (event.keyCode === KEY.DOWN) {
				if (this.$scope.highlighted == null) {
					this.$scope.highlighted = 0;
					if (!this.$scope.showOptions) {
						this.buildOptions();
					}
				} else if (this.$scope.highlighted < (this.$scope.options.length - 1)) {
					this.$scope.highlighted++;
				}
			} else if (event.keyCode === KEY.UP) {
				if (this.$scope.highlighted > 0) {
					this.$scope.highlighted--;
				}
			}

			this.$scope.showOptions = true;
		}

		// Open/close the options when the open button is clicked
		public ToggleOptions() {
			this.buildOptions();
			this.$scope.showOptions = !this.$scope.showOptions;
			this.$element.find('input').focus();
		}

		private buildOptions(filter?: string) {
			this.$scope.options = [];

			filter = filter || '';
			filter = filter.toLowerCase();

			if (this.$scope.data) {
				_.each(this.$scope.data, (item: string) => {
					// If the item text matches the current input text, push it to the options
					if (item.toLowerCase().indexOf(filter) >= 0) {
						this.$scope.options.push(item);
					}
				});
			}
		}

		private setSelected(value: string) {
			this.$scope.selected = value;
		}

		private hideOptions(event) {
			const isChild = this.$element.has(event.target).length > 0;
			const isSelf = this.$element[0] === event.target;
			const isInside = isChild || isSelf;

			if (!isInside) {
				this.$scope.$apply(() => {
					this.$scope.showOptions = false;
				});
			}
		}
	}
}
