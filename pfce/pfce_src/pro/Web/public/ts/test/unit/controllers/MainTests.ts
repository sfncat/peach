/// <reference path="../reference.ts" />

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('Main controller', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $state: ng.ui.IStateService;
		var ctrl: Peach.MainController;
		var service: Peach.PitService;
		var pitUrl = '/p/pits/PIT_GUID';

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $controller: ng.IControllerService;
			var $rootScope: ng.IRootScopeService;
			var $templateCache: ng.ITemplateCacheService;

			$controller = $injector.get(C.Angular.$controller);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$rootScope = $injector.get(C.Angular.$rootScope);
			$state = $injector.get(C.Angular.$state);
			$templateCache = $injector.get(C.Angular.$templateCache);
			service = $injector.get(C.Services.Pit);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Wizard.Intro, '');

			$httpBackend.expectGET(C.Api.Jobs).respond([]);
			ctrl = $controller('MainController', {
				$scope: $rootScope.$new()
			});
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});
	});
});
