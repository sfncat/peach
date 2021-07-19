/// <reference path="../reference.ts" />

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('PitService', () => {
		var $rootScope: ng.IRootScopeService;
		var $state: ng.ui.IStateService;
		var $httpBackend: ng.IHttpBackendService;
		var service: Peach.PitService;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $templateCache: ng.ITemplateCacheService;

			$rootScope = $injector.get(C.Angular.$rootScope);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$templateCache = $injector.get(C.Angular.$templateCache);
			$state = $injector.get(C.Angular.$state);
			service = $injector.get(C.Services.Pit);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Error, '');
			$templateCache.put(C.Templates.Pit.Configure, '');
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(service)).toBe(true);
		});

		describe('when a Pit is not selected', () => {
			it("CurrentPitId is undefined", () => {
				expect(service.CurrentPitId).toBe(undefined);
			});

			// it("IsConfigured is false", () => {
			// 	expect(service.IsConfigured).toBe(false);
			// });
		});

		describe('when a Pit is selected', () => {
			var pitId = 'PIT_GUID';
			var pitUrl = C.Api.PitUrl.replace(':id', pitId);
			var pit = {
				pitUrl: pitUrl,
				name: 'My Pit'
			};
			
			beforeEach(() => {
				$state.go(C.States.Pit, { pit: pitId });
				$rootScope.$digest();
				
				$httpBackend.whenGET(pitUrl).respond(pit);
			});

			describe('which is not configured', () => {
				beforeEach(() => {
					$httpBackend.expectGET(pitUrl);
					service.LoadPit();
					$httpBackend.flush();
				});

				// it("get IsConfigured is false", () => {
				// 	expect(service.IsConfigured).toBe(false);
				// });
			});

			describe('which is already configured', () => {
				beforeEach(() => {
					pit = {
						pitUrl: pitUrl,
						name: 'My Pit'
					};
					$httpBackend.expectGET(pitUrl).respond(pit);
					service.LoadPit();
					$httpBackend.flush();
				});

				// it("get IsConfigured is true", () => {
				// 	expect(service.IsConfigured).toBe(true);
				// });
			});
		});
	});
});
