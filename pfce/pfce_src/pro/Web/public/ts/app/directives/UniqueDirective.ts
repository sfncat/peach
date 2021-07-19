/// <reference path="../reference.ts" />

namespace Peach {
	export interface IUniqueScope extends ng.IScope {
		unique: Function;
		watch: string;
		channel: string;
		defaultValue: any;
		ctrl: UniqueChannelController;
		ngModel: ng.INgModelController;
		ignore: string;
	}

	export const UniqueDirective: IDirective = {
		ComponentID: C.Directives.Unique,
		restrict: 'A',
		require: C.Angular.ngModel,
		scope: {
			unique: `&${C.Directives.Unique}`,
			watch: '@peachUniqueWatch',
			defaultValue: '@peachUniqueDefault'
		},
		link: (
			scope: IUniqueScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			const validate = (modelValue, viewValue) => {
				const collection = scope.unique();
				return !_.includes(collection, viewValue || scope.defaultValue);
			};

			ctrl.$validators['unique'] = validate;

			if (scope.watch) {
				scope.$watch(scope.watch, (newVal, oldVal) => {
					if (newVal !== oldVal) {
						ctrl.$setValidity('unique', validate(ctrl.$modelValue, ctrl.$viewValue));
					}
				});
			}
		}
	}

	export const UniqueChannelDirective: IDirective = {
		ComponentID: C.Directives.UniqueChannel,
		restrict: 'A',
		require: C.Angular.ngModel,
		controller: C.Controllers.UniqueChannel,
		controllerAs: 'ctrl',
		scope: {
			channel: `@${C.Directives.UniqueChannel}`,
			defaultValue: '@peachUniqueDefault',
			ignore: '@peachUniqueIgnore'
		},

		link: (
			scope: IUniqueScope,
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) => {
			scope.ctrl.Link(element, attrs, ctrl);
		}
	}

	export class UniqueChannelController {
		static $inject = [C.Angular.$scope, C.Services.Unique];

		constructor(
			private $scope: IUniqueScope,
			private service: UniqueService
		) {
		}

		public Link(
			element: ng.IAugmentedJQuery,
			attrs: ng.IAttributes,
			ctrl: ng.INgModelController
		) {
			this.$scope.ngModel = ctrl;

			this.service.Register(this.$scope);

			const validate = value => {
				this.service.IsUnique(this.$scope, value);
				return value;
			}

			ctrl.$formatters.unshift(validate);
			ctrl.$viewChangeListeners.unshift(() => validate(ctrl.$viewValue));

			element.on('$destroy', () => {
				this.service.Unregister(this.$scope);
			});
		}
	}

	class UniqueChannel {
		[id: string]: IUniqueScope;
	}

	class UniqueChannels {
		[name: string]: UniqueChannel
	}

	export class UniqueService {
		public IsUnique(scope: IUniqueScope, value: any): boolean {
			let isUnique;
			const channel = this.getChannel(scope.channel);
			_.forEach(channel, (item: IUniqueScope, id: string) => {
				const isDuplicate = this.isDuplicate(item, item.ngModel.$modelValue);
				item.ngModel.$setValidity('unique', !isDuplicate);
				if (id === scope.$id.toString()) {
					isUnique = !isDuplicate;
				}
			});
			return isUnique;
		}

		public Register(scope: IUniqueScope) {
			const channel = this.getChannel(scope.channel);
			channel[scope.$id.toString()] = scope;
		}

		public Unregister(scope: IUniqueScope) {
			const channel = this.getChannel(scope.channel);
			delete channel[scope.$id.toString()];
			if (_.isEmpty(channel)) {
				delete this.channels[scope.channel];
			} else {
				this.IsUnique(scope, null);
			}
		}

		private isDuplicate(scope: IUniqueScope, value: any): boolean {
			const myValue = (value || scope.defaultValue);
			if (scope.ignore) {
				const reIgnore: RegExp = new RegExp(scope.ignore);
				if (reIgnore.test(myValue)) {
					return false;
				}
			} 

			const channel = this.getChannel(scope.channel);
			return _.some(channel, (other: IUniqueScope, id: string) => {
				if (scope.$id.toString() === id) {
					return false;
				}
				const otherValue = other.ngModel.$modelValue;
				return (myValue === (otherValue || other.defaultValue));
			});
		}

		private getChannel(name: string): UniqueChannel {
			let channel = this.channels[name];
			if (_.isUndefined(channel)) {
				channel = {};
				this.channels[name] = channel;
			}
			return channel;
		}

		private channels: UniqueChannels = {};
	}
}
