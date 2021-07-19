/// <reference path="../reference.ts" />

namespace Peach {
	export interface IBoundScope extends ng.IScope {
		min: IBoundFunction;
		max: IBoundFunction;
	}

	export interface IBoundFunction {
		(): number;
	}

	interface IValidatePredicate {
		(value: any): boolean;
	}

	function predicateValidation(
		name: string,
		ctrl: ng.INgModelController,
		predicate: IValidatePredicate
	) {
		ctrl.$validators[name] = (modelValue, viewValue) => {
			const value = modelValue || viewValue;
			return _.isUndefined(value)
				|| (_.isString(value) && _.isEmpty(value))
				|| predicate(value);
		};
	}

	export const RangeDirective: IDirective = {
		ComponentID: C.Directives.Range,
		restrict: 'A',
		require: C.Angular.ngModel,
		scope: {
			min: '&peachRangeMin',
			max: '&peachRangeMax'
		},
		link: (
			scope: IBoundScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			predicateValidation(C.Validation.RangeMin, ctrl, (value: string) => {
				const int = parseInt(value);
				const min = scope.min();
				return _.isUndefined(min) || (!_.isNaN(int) && int >= min);
			});
			predicateValidation(C.Validation.RangeMax, ctrl, (value: string) => {
				const int = parseInt(value);
				const max = scope.max();
				return _.isUndefined(max) || (!_.isNaN(int) && int <= max);
			});
		}
	}

	export const IntegerDirective: IDirective = {
		ComponentID: C.Directives.Integer,
		restrict: 'A',
		require: C.Angular.ngModel,
		link: (
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			const pattern = /^(\-|\+)?\d+$/;
			predicateValidation(C.Validation.Integer, ctrl,
				(value: string) => pattern.test(value)
			);
		}
	}

	export const HexDirective: IDirective = {
		ComponentID: C.Directives.HexString,
		restrict: 'A',
		require: C.Angular.ngModel,
		link: (
			scope: ng.IScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			const pattern = /^[0-9A-Fa-f]+$/;
			predicateValidation(C.Validation.HexString, ctrl,
				(value: string) => pattern.test(value)
			);
		}
	}
}
