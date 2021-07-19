/// <reference path="../reference.ts" />

describe("Peach", () => {
	var C = Peach.C;
	beforeEach(module('Peach'));

	describe('PitTestController', () => {
		var $httpBackend: ng.IHttpBackendService;
		var $timeout: ng.ITimeoutService;
		var ctrl: Peach.PitTestController;

		var pitId = 'PIT_GUID';
		var pitUrl = C.Api.PitUrl.replace(':id', pitId);
		var pit: Peach.IPit = {
			id: pitId,
			pitUrl: pitUrl,
			name: 'My Pit',
			config: [],
			agents: [],
			metadata: {
				defines: [],
				monitors: []
			}
		};

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			var $state = <ng.ui.IStateService> $injector.get(C.Angular.$state);
			var $rootScope = <ng.IRootScopeService> $injector.get(C.Angular.$rootScope);
			var $controller = <ng.IControllerService> $injector.get(C.Angular.$controller);
			var $templateCache = <ng.ITemplateCacheService> $injector.get(C.Angular.$templateCache);

			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$timeout = $injector.get(C.Angular.$timeout);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Configure, '');

			$state.go(C.States.Pit, { pit: pitId });
			$rootScope.$digest();

			$httpBackend.whenGET(pitUrl).respond(pit);
			ctrl = $controller('PitTestController', {
				$scope: $rootScope.$new()
			});
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		it("new", () => {
			expect(_.isObject(ctrl)).toBe(true);
		});

		it("can begin a test", () => {
			var testUrl = '/p/my/test/url';
			var req: Peach.IJobRequest = {
				pitUrl: pitUrl,
				dryRun: true
			};
			var job: Peach.IJob = {
				id: 'JOB_ID',
				pitUrl: pitUrl,
				jobUrl: '/p/jobs/JOB_ID',
				firstNodeUrl: testUrl
			};
			var result1: Peach.ITestResult = {
				status: 'active',
				log: '',
				events: []
			};

			ctrl.OnBeginTest();
			$httpBackend.expectPOST(Peach.C.Api.Jobs, req).respond(job);
			$httpBackend.expectGET(testUrl).respond(result1);
			$httpBackend.flush();
			
			expect(ctrl.CanBeginTest).toBe(false);
			expect(ctrl.CanContinue).toBe(false);

			var result2: Peach.ITestResult = {
				status: 'pass',
				log: '',
				events: []
			};

			$httpBackend.expectGET(testUrl).respond(result2);
			$httpBackend.expectDELETE(job.jobUrl).respond({});
			$timeout.flush(Peach.TEST_INTERVAL);
			$httpBackend.flush();

			expect(ctrl.CanBeginTest).toBe(true);
			expect(ctrl.CanContinue).toBe(true);
		});
	});
});
