/// <reference path="../reference.ts" />

interface ITestItem {
	key: string;
	value: string;
}

interface ITestUniqueScope extends ng.IScope {
	model: string;
	model1: string;
	model2: string;
	model3: string;
	values: string[];
	items: ITestItem[];
	form: ng.IFormController;
}

function getInputs(doc) {
	var inputs = doc.find('input');
	return _.map(inputs, el => angular.element(el).controller('ngModel'));
}

describe("Peach", () => {
	beforeEach(module('Peach'));

	describe('UniqueDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestUniqueScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestUniqueScope> $rootScope.$new();
			scope.values = ['duplicate'];
		}));

		describe('without default', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:unique': 'values'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("validates unique values", () => {
				modelCtrl.$setViewValue('unique');
				scope.$digest();
				expect(scope.model).toEqual('unique');
				expect(modelCtrl.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl.$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model).toEqual(undefined);
				expect(modelCtrl.$valid).toBe(false);
			});
		});

		describe('with default', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:unique': 'values',
						'peach:unique-default': 'duplicate'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("validates unique values", () => {
				modelCtrl.$setViewValue('unique');
				scope.$digest();
				expect(scope.model).toEqual('unique');
				expect(modelCtrl.$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrl.$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model).toEqual(undefined);
				expect(modelCtrl.$valid).toBe(false);
			});

			it("uses default value for comparison", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.model).toEqual(undefined);
				expect(modelCtrl.$valid).toBe(false);
			});
		});
	});

	describe('UniqueChannelDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestUniqueScope;
		var modelCtrls: ng.INgModelController[] = [];

		describe('without default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel'
					}),
					pithy.input({
						type: 'text',
						name: 'input3',
						'ng-model': 'model3',
						'peach:unique-channel': 'channel'
					})
				]).toString();

				var element = $compile(html)(scope);
				modelCtrls = getInputs(element);
			}));

			it("validates unique values", () => {
				modelCtrls[0].$setViewValue('unique1');
				modelCtrls[1].$setViewValue('unique2');
				modelCtrls[2].$setViewValue('unique3');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(scope.model3).toEqual('unique3');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrls[0].$setViewValue('duplicate');
				modelCtrls[1].$setViewValue('duplicate');
				modelCtrls[2].$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(false);
				expect(modelCtrls[2].$valid).toBe(true);

				modelCtrls[0].$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(false);

				modelCtrls[0].$setViewValue('unique1');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);
			});
		});

		describe('with default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default1'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default2'
					}),
					pithy.input({
						type: 'text',
						name: 'input3',
						'ng-model': 'model3',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default3'
					})
				]).toString();

				var element = $compile(html)(scope);
				modelCtrls = getInputs(element);
			}));

			it("validates unique values", () => {
				modelCtrls[0].$setViewValue('unique1');
				modelCtrls[1].$setViewValue('unique2');
				modelCtrls[2].$setViewValue('unique3');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(scope.model3).toEqual('unique3');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrls[0].$setViewValue('duplicate');
				modelCtrls[1].$setViewValue('duplicate');
				modelCtrls[2].$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(false);
				expect(modelCtrls[2].$valid).toBe(true);

				modelCtrls[0].$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(false);

				modelCtrls[0].$setViewValue('unique1');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('duplicate');
				expect(scope.model3).toEqual('unique');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);
			});

			it("uses default value for comparison", () => {
				modelCtrls[0].$setViewValue('');
				modelCtrls[1].$setViewValue('value2');
				modelCtrls[2].$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('value2');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);

				modelCtrls[0].$setViewValue('');
				modelCtrls[1].$setViewValue('default1');
				modelCtrls[2].$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('default1');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(false);
				expect(modelCtrls[2].$valid).toBe(true);

				modelCtrls[0].$setViewValue('');
				modelCtrls[1].$setViewValue('default1');
				modelCtrls[2].$setViewValue('default1');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('default1');
				expect(scope.model3).toEqual('default1');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(false);
				expect(modelCtrls[2].$valid).toBe(false);
			});

			it("each element can specify a different default value", () => {
				modelCtrls[0].$setViewValue('');
				modelCtrls[1].$setViewValue('');
				modelCtrls[2].$setViewValue('value3');
				scope.$digest();
				expect(scope.model1).toEqual('');
				expect(scope.model2).toEqual('');
				expect(scope.model3).toEqual('value3');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
				expect(modelCtrls[2].$valid).toBe(true);
			});
		});

		describe('with ignore & default', () => {
			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				var $rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();

				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input1',
						'ng-model': 'model1',
						'peach:unique-channel': 'channel',
						'peach:unique-ignore': '^starts'
					}),
					pithy.input({
						type: 'text',
						name: 'input2',
						'ng-model': 'model2',
						'peach:unique-channel': 'channel',
						'peach:unique-default': 'default2',
						'peach:unique-ignore': 'default2'
					})
				]).toString();

				var element = $compile(html)(scope);
				modelCtrls = getInputs(element);
			}));

			it("validates unique values", () => {
				modelCtrls[0].$setViewValue('unique1');
				modelCtrls[1].$setViewValue('unique2');
				scope.$digest();
				expect(scope.model1).toEqual('unique1');
				expect(scope.model2).toEqual('unique2');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
			});

			it("invalidates duplicate values", () => {
				modelCtrls[0].$setViewValue('duplicate');
				modelCtrls[1].$setViewValue('duplicate');
				scope.$digest();
				expect(scope.model1).toEqual('duplicate');
				expect(scope.model2).toEqual('duplicate');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(false);

				modelCtrls[0].$setViewValue('unique');
				scope.$digest();
				expect(scope.model1).toEqual('unique');
				expect(scope.model2).toEqual('duplicate');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(true);
			});

			it("honors ignore", () => {
				modelCtrls[0].$setViewValue('starts-12345');
				modelCtrls[1].$setViewValue('starts-12345');
				scope.$digest();
				expect(scope.model1).toEqual('starts-12345');
				expect(scope.model2).toEqual('starts-12345');
				expect(modelCtrls[0].$valid).toBe(true);
				expect(modelCtrls[1].$valid).toBe(false);
			});

			it("honors ignore & default", () => {
				modelCtrls[0].$setViewValue('default2');
				modelCtrls[1].$setViewValue('');
				scope.$digest();
				expect(scope.model1).toEqual('default2');
				expect(scope.model2).toEqual('');
				expect(modelCtrls[0].$valid).toBe(false);
				expect(modelCtrls[1].$valid).toBe(true);
			});
		});

		describe('ng-repeat', () => {
			var $rootScope: ng.IRootScopeService;

			beforeEach(inject(($injector: ng.auto.IInjectorService) => {
				$compile = $injector.get('$compile');
				$rootScope = $injector.get('$rootScope');
				scope = <ITestUniqueScope> $rootScope.$new();
				scope.items = [
					{ key: 'key1', value: 'value1' },
					{ key: 'key2', value: 'value2' }
				];

				var html = pithy.form({ name: 'form' }, [
					pithy.div({ 'ng-repeat': 'item in items track by $index' }, [
						pithy.div({ 'ng-form': 'inner' }, [
							pithy.input({
								name: 'item',
								type: 'text',
								'ng-model': 'item.value',
								'peach:unique-channel': 'channel'
							})
						])
					])
				]).toString();

				var element = $compile(html)(scope);
				$rootScope.$digest();
				modelCtrls = getInputs(element);
			}));

			it("validates unique values", () => {
				expect(_.pluck(scope.items, 'value')).toEqual(['value1', 'value2']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([true, true]);
			});

			it("invalidates duplicate values", () => {
				modelCtrls[0].$setViewValue('duplicate');
				modelCtrls[1].$setViewValue('duplicate');
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['duplicate', 'duplicate']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([false, false]);

				modelCtrls[0].$setViewValue('unique');
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['unique', 'duplicate']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([true, true]);
			});

			it("handles modifying collection order", () => {
				Peach.ArrayItemDown(scope.items, 0);
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['value2', 'value1']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([true, true]);

				Peach.ArrayItemDown(scope.items, 0);
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['value1', 'value2']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([true, true]);

				modelCtrls[0].$setViewValue('duplicate');
				modelCtrls[1].$setViewValue('duplicate');
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['duplicate', 'duplicate']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([false, false]);

				Peach.ArrayItemDown(scope.items, 0);
				$rootScope.$digest();
				expect(_.pluck(scope.items, 'value')).toEqual(['duplicate', 'duplicate']);
				expect(_.pluck(modelCtrls, '$valid')).toEqual([false, false]);
			});
		});
	});
});
