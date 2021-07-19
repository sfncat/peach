/// <reference path="../reference.ts" />

interface ITestValidatorsScope extends ng.IScope {
	form: ng.IFormController;
	model: any;
}

describe("Peach",() => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('RangeDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();
		}));

		describe('range-min: 1', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-min': '1'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("ok: 1", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("min: 0", () => {
				modelCtrl.$setViewValue('0');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("min: -1", () => {
				modelCtrl.$setViewValue('-1');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("min: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});
		});

		describe('range-max: 10', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-max': '10'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("ok: 10", () => {
				modelCtrl.$setViewValue('10');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("max: 11", () => {
				modelCtrl.$setViewValue('11');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(true);
			});

			it("ok: -1", () => {
				modelCtrl.$setViewValue('-1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("max: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(true);
			});
		});

		describe('range-min: 1, range-max: 10', () => {
			beforeEach(() => {
				var html = pithy.form({ name: 'form' }, [
					pithy.input({
						type: 'text',
						name: 'input',
						'ng-model': 'model',
						'peach:range': 'true',
						'peach:range-min': '1',
						'peach:range-max': '10'
					})
				]).toString();

				element = $compile(html)(scope);
				modelCtrl = <ng.INgModelController> scope.form['input'];
			});

			it("ok: ''", () => {
				modelCtrl.$setViewValue('');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("ok: 1", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("ok: 10", () => {
				modelCtrl.$setViewValue('1');
				scope.$digest();
				expect(scope.form.$valid).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("min: 0", () => {
				modelCtrl.$setViewValue('0');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(false);
			});

			it("max: 11", () => {
				modelCtrl.$setViewValue('11');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(true);
			});

			it("min, max: x", () => {
				modelCtrl.$setViewValue('x');
				scope.$digest();
				expect(scope.form.$valid).toBe(false);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMin)).toBe(true);
				expect(_.has(modelCtrl.$error, C.Validation.RangeMax)).toBe(true);
			});
		});
	});

	describe('IntegerDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();

			var html = pithy.form({ name: 'form' }, [
				pithy.input({
					type: 'text',
					name: 'input',
					'ng-model': 'model',
					'peach-integer': 'true'
				})
			]).toString();

			element = $compile(html)(scope);
			modelCtrl = <ng.INgModelController> scope.form['input'];
		}));

		it("ok: ''", () => {
			modelCtrl.$setViewValue('');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(false);
		});

		it("ok: 1", () => {
			modelCtrl.$setViewValue('1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(false);
		});

		it("ok: +1", () => {
			modelCtrl.$setViewValue('+1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(false);
		});

		it("ok: -1", () => {
			modelCtrl.$setViewValue('-1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(false);
		});

		it("bad: 0x01", () => {
			modelCtrl.$setViewValue('0x01');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(true);
		});

		it("bad: 0.0", () => {
			modelCtrl.$setViewValue('0.0');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(true);
		});

		it("bad: x", () => {
			modelCtrl.$setViewValue('x');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.Integer)).toBe(true);
		});
	});

	describe('HexDirective', () => {
		var $compile: ng.ICompileService;
		var scope: ITestValidatorsScope;
		var element: ng.IAugmentedJQuery;
		var modelCtrl: ng.INgModelController;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$compile = $injector.get('$compile');
			var $rootScope = $injector.get('$rootScope');
			scope = <ITestValidatorsScope> $rootScope.$new();

			var html = pithy.form({ name: 'form' }, [
				pithy.input({
					type: 'text',
					name: 'input',
					'ng-model': 'model',
					'peach-hexstring': 'true'
				})
			]).toString();

			element = $compile(html)(scope);
			modelCtrl = <ng.INgModelController> scope.form['input'];
		}));

		it("ok: ''", () => {
			modelCtrl.$setViewValue('');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(false);
		});

		it("ok: 1", () => {
			modelCtrl.$setViewValue('1');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(false);
		});

		it("ok: A", () => {
			modelCtrl.$setViewValue('A');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(false);
		});

		it("ok: FFFFFFFF", () => {
			modelCtrl.$setViewValue('FFFFFFFF');
			scope.$digest();
			expect(scope.form.$valid).toBe(true);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(false);
		});

		it("bad: x", () => {
			modelCtrl.$setViewValue('x');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(true);
		});

		it("bad: +1", () => {
			modelCtrl.$setViewValue('+1');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(true);
		});

		it("bad: -1", () => {
			modelCtrl.$setViewValue('-1');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(true);
		});

		it("bad: 0x01", () => {
			modelCtrl.$setViewValue('0x01');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(true);
		});

		it("bad: 0.0", () => {
			modelCtrl.$setViewValue('0.0');
			scope.$digest();
			expect(scope.form.$valid).toBe(false);
			expect(_.has(modelCtrl.$error, C.Validation.HexString)).toBe(true);
		});
	});
});
