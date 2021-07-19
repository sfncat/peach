/// <reference path="../reference.ts" />

namespace Peach {
	export const ParameterInputDirective: IDirective = {
		ComponentID: C.Directives.ParameterInput,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterInput,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	export const ParameterComboDirective: IDirective = {
		ComponentID: C.Directives.ParameterCombo,
		restrict: 'E',
		controller: C.Controllers.ParameterInput,
		templateUrl: C.Templates.Directives.ParameterCombo,
		scope: {
			param: "=",
			form: "="
		}
	}

	export const ParameterSelectDirective: IDirective = {
		ComponentID: C.Directives.ParameterSelect,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterSelect,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	export const ParameterStringDirective: IDirective = {
		ComponentID: C.Directives.ParameterString,
		restrict: "E",
		templateUrl: C.Templates.Directives.ParameterString,
		controller: C.Controllers.ParameterInput,
		scope: {
			param: "=",
			form: "="
		}
	}

	interface IOption {
		key: string;
		text: string;
		description?: string;
		group: string;
	}

	export interface IParameterInputScope extends IParameterScope {
		NewChoice: Function;
	}

	export class ParameterInputController {
		static $inject = [
			C.Angular.$scope,
			C.Services.Pit
		];

		constructor(
			private $scope: IParameterInputScope,
			private pitService: PitService
		) {
			$scope.vm = this;
			$scope.NewChoice = (item: string) => this.NewChoice(item);
			this.LastValue = {
				key: this.$scope.param.value,
				text: this.$scope.param.value,
				group: "Last Value"
			};
			this.MakeChoices();
		}

		get IsRequired(): boolean {
			return _.isUndefined(this.$scope.param.optional) || !this.$scope.param.optional;
		}

		get IsReadonly() {
			return this.$scope.param.type === ParameterType.System;
		}

		get ParamTooltip() {
			return this.IsReadonly ? this.$scope.param.value : "";
		}

		get WidgetType(): string {
			switch (this.$scope.param.type) {
				case ParameterType.Enum:
				case ParameterType.Bool:
				case ParameterType.Call:
					return "select";
				case ParameterType.Hwaddr:
				case ParameterType.Iface:
				case ParameterType.Ipv4:
				case ParameterType.Ipv6:
					return "combo";
				case ParameterType.Space:
					return "space";
				default:
					return "string";
			}
		}

		private Choices: IOption[];
		private LastValue: IOption;
		private NewValue: IOption;
		private EmptyValue: IOption;

		private MakeChoices() {
			const tuples = [];
			const options = this.$scope.param.options || [];
			let group: string;
			if (this.$scope.param.type === ParameterType.Call) {
				group = "Calls";
			} else {
				group = "Choices";
			}

			options.forEach(item => {
				const option: IOption = {
					key: item,
					text: item || "<i>Undefined</i>",
					group: group
				};
				if (item === this.$scope.param.defaultValue) {
					option.group = "Default";
					tuples.unshift(option);
				} else {
					tuples.push(option);
				}
			});
			this.Choices = tuples.concat(this.Defines());

			if (!this.IsRequired && !this.$scope.param.defaultValue) {
				this.Choices.unshift({
					key: "",
					text: "<i>Undefined</i>",
					group: group
				});
			}

			if (this.LastValue && this.LastValue.key) {
				this.Choices.unshift(this.LastValue);
			}
		
			if (this.NewValue && this.NewValue.key) {
				this.Choices.unshift(this.NewValue);
			}
		}

		private Defines(): IOption[] {
			const available = this.pitService.CreateFlatDefinesView(this.pitService.Pit.definesView);
			return _.chain(available)
				.map(param => {
					const key = `##${param.key}##`;
					return <IOption>{
						key: key,
						text: key,
						description: param.description,
						group: "Defines"
					};
				})
				.orderBy(x => x.key)
				.value();
		}

		private NewChoice(item: string): IOption {
			this.NewValue = {
				key: item,
				text: item,
				group: "New Value"
			};
			this.MakeChoices();
			return this.NewValue;
		}
	}
}
