/// <reference path="../reference.ts" />

describe("Peach", () => {
	let C = Peach.C;
	beforeEach(module('Peach'));

	interface ITestCase {
		id: string;
		url: string;
		pit: Peach.IPit;
		post: Peach.IPit;
	}

	const key = "Key";
	const name = "Name";
	const value = "Value";

	const SystemDefines = {
		type: Peach.ParameterType.Group,
		name: 'System',
		items: [
			{
				key: 'Peach.OS',
				name: 'Peach OS',
				value: 'windows',
				type: Peach.ParameterType.System
			}
		]
	};

	const AllMonitors = {
		type: Peach.ParameterType.Group,
		name: 'All',
		items: [
			{
				type: Peach.ParameterType.Monitor,
				key: 'PageHeap',
				items: []
			},
			{
				type: Peach.ParameterType.Monitor,
				key: 'WindowsDebugger',
				items: []
			},
			{
				type: Peach.ParameterType.Monitor,
				key: 'FooMonitor',
				items: []
			},
			{
				type: Peach.ParameterType.Monitor,
				key: 'BarMonitor',
				items: []
			}
		]
	};

	function MakeTestCase(id: string, pit: Peach.IPit, post: Peach.IPit): ITestCase {
		const url = C.Api.PitUrl.replace(':id', id);
		pit.id = id;
		pit.pitUrl = url;
		post.id = id;
		post.pitUrl = url;
		return {
			id: id,
			url: url,
			pit: pit,
			post: post
		};
	}

	const noVarsTest = MakeTestCase('NO_VARS', {
		id: null,
		name: 'My Pit',
		description: 'description',
		pitUrl: null,
		tags: [],
		config: [],
		agents: [],
		metadata: {
			defines: [ SystemDefines ],
			monitors: [ AllMonitors ]
		}
	}, {
		id: null,
		name: 'My Pit',
		pitUrl: null,
		config: [],
		agents: []
	});

	const withVarsTest = MakeTestCase('WITH_VARS', {
		id: null,
		name: 'My Pit',
		description: 'description',
		pitUrl: null,
		tags: [],
		config: [],
		agents: [],
		metadata: {
			defines: [ 
				SystemDefines,
				{
					type: Peach.ParameterType.Group,
					name: 'Group',
					items: [
						{
							key: key,
							name: name,
							type: Peach.ParameterType.String
						}
					]
				}
			],
			monitors: [ AllMonitors ]
		}
	}, {
		id: null,
		name: 'My Pit',
		pitUrl: null,
		config: [
			{ key: key, value: value }
		],
		agents: []
	});

	const WizardController = 'WizardController';

	describe(WizardController, () => {
		let $controller: ng.IControllerService;
		let $httpBackend: ng.IHttpBackendService;
		let $rootScope: ng.IRootScopeService;
		let $state: ng.ui.IStateService;

		let ctrl: Peach.WizardController;
		let scope: Peach.IWizardScope;
		let wizardService: Peach.WizardService;

		beforeEach(inject(($injector: ng.auto.IInjectorService) => {
			$controller = $injector.get(C.Angular.$controller);
			$httpBackend = $injector.get(C.Angular.$httpBackend);
			$rootScope = $injector.get(C.Angular.$rootScope);
			$state = $injector.get(C.Angular.$state);
			const $templateCache: ng.ITemplateCacheService = $injector.get(C.Angular.$templateCache);
			const pitService: Peach.PitService = $injector.get(C.Services.Pit);
			wizardService = $injector.get(C.Services.Wizard);

			$templateCache.put(C.Templates.Home, '');
			$templateCache.put(C.Templates.Pit.Configure, '');
			$templateCache.put(C.Templates.Pit.Wizard.Intro, '');
			$templateCache.put(C.Templates.Pit.Wizard.Track, '');
			$templateCache.put(C.Templates.Pit.Wizard.Question, '');

			let tracks = [
				C.Tracks.Vars,
				C.Tracks.Fault,
				C.Tracks.Data
			];
			for (let track of tracks) {
				$templateCache.put(
					C.Templates.Pit.Wizard.TrackIntro.replace(':track', track.toLowerCase()), ''
				);
				$templateCache.put(
					C.Templates.Pit.Wizard.TrackDone.replace(':track', track.toLowerCase()), ''
				);
			}
			
			$state.go(C.States.Pit, { pit: noVarsTest.id });
			$rootScope.$digest();

			$httpBackend.whenGET(noVarsTest.url).respond(noVarsTest.pit);
			$httpBackend.whenGET(withVarsTest.url).respond(withVarsTest.pit);
			pitService.LoadPit();
			$httpBackend.flush();
		}));

		afterEach(() => {
			$httpBackend.verifyNoOutstandingExpectation();
			$httpBackend.verifyNoOutstandingRequest();
		});

		function NewCtrl(track) {
			$state.current.data = { track: track };
			scope = <Peach.IWizardScope> $rootScope.$new();
			ctrl = $controller(WizardController, {
				$scope: scope
			});
			$rootScope.$digest();
		}

		function Next() {
			ctrl.Next();
			$rootScope.$digest();
		}

		function NextTrack(track) {
			ctrl.OnNextTrack();
			NewCtrl(track);
		}

		function expectState(testCase: ITestCase, state, id?) {
			let actual = $state.is(state, { pit: testCase.id, id: id });
			expect(actual).toBe(true);
			if (!actual) {
				console.error('expectState', state, id);
				console.error('state is', $state.current.name, JSON.stringify($state.params));
			}
		}

		describe("'vars' track", () => {
			describe("without variables to configure", () => {
				beforeEach(() => {
					$state.go(Peach.WizardTrackIntro(C.Tracks.Vars), { pit: noVarsTest.id });
					NewCtrl(C.Tracks.Vars);
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Vars));
				});

				it("Next() will move to review", () => {
					$httpBackend.expectPOST(noVarsTest.url, noVarsTest.post).respond(noVarsTest.pit);
					Next();
					$httpBackend.flush();
					expectState(noVarsTest, Peach.WizardTrackReview(C.Tracks.Vars));
				});

				it("OnNextTrack() moves to the next track", () => {
					NextTrack(C.Tracks.Fault);
					expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Fault));
				});
			});

			describe("with variables to configure", () => {
				beforeEach(() => {
					$state.go(C.States.Pit, { pit: withVarsTest.id });
					$rootScope.$digest();
					$state.go(Peach.WizardTrackIntro(C.Tracks.Vars), { pit: withVarsTest.id });
					NewCtrl(C.Tracks.Vars);
				});

				it("starts clean", () => {
					expect(_.isObject(ctrl)).toBe(true);
					expect(scope.Question.id).toBe(0);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
					expectState(withVarsTest, Peach.WizardTrackIntro(C.Tracks.Vars));
				});

				it("should walk thru wizard", () => {
					expectState(withVarsTest, Peach.WizardTrackIntro(C.Tracks.Vars));
					$httpBackend.expectGET(withVarsTest.url).respond(withVarsTest.pit);
					Next();
					$httpBackend.flush();
					expectState(withVarsTest, Peach.WizardTrackSteps(C.Tracks.Vars), 1);

					scope.Question.value = value;
					expect(scope.Question.shortName).toBe(name);
					expect(scope.Question.key).toBe(key);
					expect(scope.Question.type).toBe(Peach.QuestionTypes.String);

					let response = angular.copy(withVarsTest.post);
					response.config = [
						{ key: key, value: value }
					];
					$httpBackend.expectPOST(withVarsTest.url, withVarsTest.post).respond(response);
					Next();
					$httpBackend.flush();

					expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);
					expectState(withVarsTest, Peach.WizardTrackReview(C.Tracks.Vars));
				});
			});
		});

		describe("'fault' track", () => {
			beforeEach(() => {
				$state.go(Peach.WizardTrackIntro(C.Tracks.Fault));

				NewCtrl(C.Tracks.Fault);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Fault));
			});

			it("should walk thru wizard", () => {
				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Fault));

				$httpBackend.expectGET(noVarsTest.url).respond(noVarsTest.pit);
				Next();
				$httpBackend.flush();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1);
				expect(scope.Question.key).toBe("AgentScheme");
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 100);
				expect(scope.Question.id).toBe(100);
				scope.Question.value = 0;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1100);
				expect(scope.Question.id).toBe(1100);
				scope.Question.value = 0;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1110);
				expect(scope.Question.id).toBe(1110);
				scope.Question.value = "C:\\some\\program.exe";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1111);
				expect(scope.Question.id).toBe(1111);
				scope.Question.value = "/args";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1140);
				expect(scope.Question.id).toBe(1140);
				scope.Question.value = "StartOnEachIteration";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1141);
				expect(scope.Question.id).toBe(1141);
				scope.Question.value = "";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1142);
				expect(scope.Question.id).toBe(1142);
				scope.Question.value = true;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Fault), 1143);
				expect(scope.Question.id).toBe(1143);
				scope.Question.value = true;
				Next();

				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected: Peach.IAgent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "PageHeap",
								"path": [1110],
								"map": [
									{ "key": "Executable", "value": "C:\\some\\program.exe" },
									{ "key": "WinDbgPath", "value": "" }
								],
								"description": "Enable page heap debugging options for an executable.",
								"view": []
							}, {
								"monitorClass": "WindowsDebugger",
								"path": [1100],
								"map": [
									{ "key": "Executable", "value": "C:\\some\\program.exe" },
									{ "key": "Arguments", "value": "/args" },
									{ "key": "ProcessName" },
									{ "key": "Service" },
									{ "key": "WinDbgPath", "value": "" },
									{ "key": "RestartOnEachTest", "value": "false" },
									{ "key": "IgnoreFirstChanceGuardPage", "value": "true" }
								],
								"description": "Enable Windows debugging.",
								"view": []
							}
						]
					}
				];
				
				let actual = wizardService.GetTrack("fault").agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});
		});

		describe("mocked 'data' track", () => {
			let mockTemplate: Peach.IWizardTemplate = {
				qa: [
					{
						id: 0,
						type: "intro",
						next: 1
					},
					{
						id: 1,
						type: "choice",
						key: "AgentScheme",
						choice: [
							{ value: "local", next: 3 },
							{ value: "tcp", next: 2 }
						]
					},
					{
						id: 2,
						type: "string",
						key: "AgentHost",
						next: 3
					},
					{
						id: 3,
						type: "choice",
						choice: [
							{ value: 0, next: 1000 },
							{ value: 1, next: 2000 }
						]
					},
					{
						id: 1000,
						type: "string",
						key: "FooParam"
					},
					{
						id: 2000,
						type: "string",
						key: "BarParam"
					}
				],
				monitors: [
					{
						monitorClass: "FooMonitor",
						path: [1000],
						map: [
							{ key: "FooParam", name: "Param" }
						],
						description: "Foo monitor."
					},
					{
						monitorClass: "BarMonitor",
						path: [2000],
						map: [
							{ key: "BarParam", name: "Param" }
						],
						description: "Bar monitor."
					}
				]
			};

			beforeEach(() => {
				wizardService.SetTrackTemplate(C.Tracks.Data, mockTemplate);

				$state.go(Peach.WizardTrackIntro(C.Tracks.Data));

				NewCtrl(C.Tracks.Data);
			});

			afterEach(() => {
				// restore the old template for the sake of other unit tests
				wizardService.SetTrackTemplate(C.Tracks.Data, Peach.Wizards.Data);
			});

			it("starts clean", () => {
				expect(_.isObject(ctrl)).toBe(true);
				expect(scope.Question.id).toBe(0);
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);
				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Data));
			});

			it("should walk thru wizard", () => {
				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(noVarsTest.url).respond(noVarsTest.pit);
				Next();
				$httpBackend.flush();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(noVarsTest, Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected: Peach.IAgent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "key": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor.",
								"view": []
							}
						]
					}
				];

				let actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});

			it("should allow adding multiple agents", () => {
				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(noVarsTest.url).respond(noVarsTest.pit);
				Next();
				$httpBackend.flush();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "local";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 0;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 1000);
				expect(scope.Question.id).toBe(1000);
				scope.Question.value = "FooValue";
				Next();

				expectState(noVarsTest, Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected1: Peach.IAgent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "key": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor.",
								"view": []
							}
						]
					}
				];

				let actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected1));

				ctrl.OnRestart();
				$rootScope.$digest();

				expectState(noVarsTest, Peach.WizardTrackIntro(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Intro);

				$httpBackend.expectGET(noVarsTest.url).respond(noVarsTest.pit);
				Next();
				$httpBackend.flush();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 1);
				expect(scope.Question.id).toBe(1);
				scope.Question.value = "tcp";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 2);
				expect(scope.Question.id).toBe(2);
				scope.Question.value = "host";
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 3);
				expect(scope.Question.id).toBe(3);
				scope.Question.value = 1;
				Next();

				expectState(noVarsTest, Peach.WizardTrackSteps(C.Tracks.Data), 2000);
				expect(scope.Question.id).toBe(2000);
				scope.Question.value = "BarValue";
				Next();

				expectState(noVarsTest, Peach.WizardTrackReview(C.Tracks.Data));
				expect(scope.Question.type).toBe(Peach.QuestionTypes.Done);

				let expected2: Peach.IAgent[] = [
					{
						"name": "",
						"agentUrl": "local://",
						"monitors": [
							{
								"monitorClass": "FooMonitor",
								"path": [1000],
								"map": [
									{ "key": "Param", "value": "FooValue" }
								],
								"description": "Foo monitor.",
								"view": []
							}
						]
					},
					{
						"name": "",
						"agentUrl": "tcp://host",
						"monitors": [
							{
								"monitorClass": "BarMonitor",
								"path": [2000],
								"map": [
									{ "key": "Param", "value": "BarValue" }
								],
								"description": "Bar monitor.",
								"view": []
							}
						]
					}
				];

				actual = wizardService.GetTrack(C.Tracks.Data).agents;
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected2));
			});
		});

		describe("merge agents", () => {
			it("works", () => {
				const agents: Peach.IAgent[] = [
					{
						name: "",
						agentUrl: "local://",
						monitors: [
							{
								monitorClass: "PageHeap",
								map: [
									{ key: "Executable", value: "Foo.exe" }
								]
							},
							{
								monitorClass: "WindowsDebugger",
								map: [
									{ key: "Executable", value: "Foo.exe" }
								]
							}
						]
					},
					{
						name: "",
						agentUrl: "tcp://remote",
						monitors: [
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							}
						]
					},
					{
						name: "",
						agentUrl: "local://",
						monitors: [
							{
								monitorClass: "CanaKit",
								map: [
									{ key: "SerialPort", value: "COM1" }
								]
							}
						]
					},
					{
						name: "",
						agentUrl: "tcp://remote2",
						monitors: [
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							}
						]
					},
					{
						name: "",
						agentUrl: "tcp://remote",
						monitors: [
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							}
						]
					},
					{
						name: "",
						agentUrl: "tcp://remote",
						monitors: [
							{
								monitorClass: "CanaKit",
								map: [
									{ key: "SerialPort", value: "COM1" }
								]
							}
						]
					}
				];

				const expected: Peach.IAgent[] = [
					{
						name: "Agent1",
						agentUrl: "local://",
						monitors: [
							{
								monitorClass: "PageHeap",
								map: [
									{ key: "Executable", value: "Foo.exe" }
								]
							},
							{
								monitorClass: "WindowsDebugger",
								map: [
									{ key: "Executable", value: "Foo.exe" }
								]
							},
							{
								monitorClass: "CanaKit",
								map: [
									{ key: "SerialPort", value: "COM1" }
								]
							}
						]
					},
					{
						name: "Agent2",
						agentUrl: "tcp://remote",
						monitors: [
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							},
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							},
							{
								monitorClass: "CanaKit",
								map: [
									{ key: "SerialPort", value: "COM1" }
								]
							}
						]
					},
					{
						name: "Agent3",
						agentUrl: "tcp://remote2",
						monitors: [
							{
								monitorClass: "Pcap",
								map: [
									{ key: "Device", value: "eth0" }
								]
							}
						]
					}
				];

				const actual = wizardService.MergeAgents(agents);
				expect(JSON.stringify(actual)).toEqual(JSON.stringify(expected));
			});
		});
	});
});
