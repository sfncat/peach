"use strict";
String.prototype.startsWith = function (prefix) {
    return this.slice(0, prefix.length) === prefix;
};
String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};
String.prototype.paddingLeft = function (pattern) {
    return String(pattern + this).slice(-pattern.length);
};
var Peach;
(function (Peach) {
    function MakeEnum(obj) {
        Object.keys(obj).map(function (key) { return obj[key] = key; });
    }
    Peach.MakeEnum = MakeEnum;
    function MakeLowerEnum(obj) {
        Object.keys(obj).map(function (key) { return obj[key] = key[0].toLowerCase() + key.substr(1); });
    }
    Peach.MakeLowerEnum = MakeLowerEnum;
    function onlyWith(obj, fn) {
        if (!_.isUndefined(obj)) {
            return fn(obj);
        }
        return undefined;
    }
    Peach.onlyWith = onlyWith;
    function onlyIf(preds, fn) {
        if (!_.isArray(preds)) {
            preds = [preds];
        }
        if (_.every(preds)) {
            return fn();
        }
        return undefined;
    }
    Peach.onlyIf = onlyIf;
    function ArrayItemUp(array, i) {
        if (i > 0) {
            var x = array[i - 1];
            array[i - 1] = array[i];
            array[i] = x;
        }
        return array;
    }
    Peach.ArrayItemUp = ArrayItemUp;
    function ArrayItemDown(array, i) {
        if (i < array.length - 1) {
            var x = array[i + 1];
            array[i + 1] = array[i];
            array[i] = x;
        }
        return array;
    }
    Peach.ArrayItemDown = ArrayItemDown;
    function StripHttpPromise($q, promise) {
        var deferred = $q.defer();
        promise.success(function (data) {
            deferred.resolve(data);
        });
        promise.error(function (reason) {
            deferred.reject(reason);
        });
        return deferred.promise;
    }
    Peach.StripHttpPromise = StripHttpPromise;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var C;
    (function (C) {
        var Angular;
        (function (Angular) {
            Angular.$compile = '$compile';
            Angular.$controller = '$controller';
            Angular.$document = '$document';
            Angular.$http = '$http';
            Angular.$httpBackend = '$httpBackend';
            Angular.$httpProvider = '$httpProvider';
            Angular.$interpolate = '$interpolate';
            Angular.$interval = '$interval';
            Angular.$location = '$location';
            Angular.$provide = '$provide';
            Angular.$q = '$q';
            Angular.$rootScope = '$rootScope';
            Angular.$scope = '$scope';
            Angular.$templateCache = '$templateCache';
            Angular.$timeout = '$timeout';
            Angular.$window = '$window';
            Angular.ngModel = 'ngModel';
            Angular.$uibModal = '$uibModal';
            Angular.$uibModalInstance = '$uibModalInstance';
            Angular.$state = '$state';
            Angular.$stateChangeStart = '$stateChangeStart';
            Angular.$stateChangeSuccess = '$stateChangeSuccess';
            Angular.$stateParams = '$stateParams';
            Angular.$stateProvider = '$stateProvider';
            Angular.$urlRouterProvider = '$urlRouterProvider';
            Angular.$breadcrumbProvider = '$breadcrumbProvider';
            Angular.$localStorage = '$localStorage';
            Angular.$sessionStorage = '$sessionStorage';
        })(Angular = C.Angular || (C.Angular = {}));
    })(C = Peach.C || (Peach.C = {}));
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var C;
    (function (C) {
        C.ViewModel = 'vm';
        var Vendor;
        (function (Vendor) {
            Vendor.VisDataSet = 'VisDataSet';
        })(Vendor = C.Vendor || (C.Vendor = {}));
        var Events;
        (function (Events) {
            Events.PitChanged = 'PitChanged';
        })(Events = C.Events || (C.Events = {}));
        var Directives;
        (function (Directives) {
            Directives.Agent = 'peachAgent';
            Directives.AutoFocus = 'peachAutoFocus';
            Directives.Combobox = 'peachCombobox';
            Directives.Defines = 'peachDefines';
            Directives.Faults = 'peachFaults';
            Directives.FaultAssets = 'peachFaultAssets';
            Directives.FaultFiles = 'peachFaultFiles';
            Directives.Jobs = 'peachJobs';
            Directives.Monitor = 'peachMonitor';
            Directives.Route = 'peachRoute';
            Directives.Headers = 'peachHeaders';
            Directives.Parameter = 'peachParameter';
            Directives.ParameterInput = 'peachParameterInput';
            Directives.ParameterCombo = 'peachParameterCombo';
            Directives.ParameterSelect = 'peachParameterSelect';
            Directives.ParameterString = 'peachParameterString';
            Directives.Test = 'peachTest';
            Directives.Unique = 'peachUnique';
            Directives.UniqueChannel = 'peachUniqueChannel';
            Directives.Unsaved = 'peachUnsaved';
            Directives.Range = 'peachRange';
            Directives.Integer = 'peachInteger';
            Directives.HexString = 'peachHexstring';
            Directives.Ratio = 'stRatio';
        })(Directives = C.Directives || (C.Directives = {}));
        var Validation;
        (function (Validation) {
            Validation.HexString = 'hexstring';
            Validation.Integer = 'integer';
            Validation.RangeMax = 'rangeMax';
            Validation.RangeMin = 'rangeMin';
        })(Validation = C.Validation || (C.Validation = {}));
        var Controllers;
        (function (Controllers) {
            Controllers.Agent = 'AgentController';
            Controllers.Combobox = 'ComboboxController';
            Controllers.Defines = 'DefinesController';
            Controllers.Faults = 'FaultsDirectiveController';
            Controllers.FaultAssets = 'FaultAssetsController';
            Controllers.FaultFiles = 'FaultFilesController';
            Controllers.Jobs = 'JobsDirectiveController';
            Controllers.Monitor = 'MonitorController';
            Controllers.Route = 'RouteController';
            Controllers.Headers = 'HeadersController';
            Controllers.Parameter = 'ParameterController';
            Controllers.ParameterInput = 'ParameterInputController';
            Controllers.Test = 'TestController';
            Controllers.UniqueChannel = 'UniqueChannelController';
            Controllers.Unsaved = 'UnsavedController';
        })(Controllers = C.Controllers || (C.Controllers = {}));
        var Services;
        (function (Services) {
            Services.Eula = 'EulaService';
            Services.Pit = 'PitService';
            Services.Unique = 'UniqueService';
            Services.HttpError = 'HttpErrorService';
            Services.Job = 'JobService';
            Services.Test = 'TestService';
        })(Services = C.Services || (C.Services = {}));
        var Api;
        (function (Api) {
            Api.License = '/p/license';
            Api.Libraries = '/p/libraries';
            Api.Pits = '/p/pits';
            Api.PitUrl = '/p/pits/:id';
            Api.Jobs = '/p/jobs';
            Api.JobUrl = '/p/jobs/:id';
        })(Api = C.Api || (C.Api = {}));
        var Tracks;
        (function (Tracks) {
            Tracks.Intro = 'intro';
            Tracks.Vars = 'Vars';
            Tracks.Fault = 'fault';
            Tracks.Data = 'data';
            Tracks.Auto = 'auto';
            Tracks.Test = 'test';
        })(Tracks = C.Tracks || (C.Tracks = {}));
        var Metrics;
        (function (Metrics) {
            Metrics.BucketTimeline = {
                id: 'bucketTimeline', name: 'Bucket Timeline'
            };
            Metrics.FaultTimeline = {
                id: 'faultTimeline', name: 'Faults Over Time'
            };
            Metrics.Mutators = {
                id: 'mutators', name: 'Mutators'
            };
            Metrics.Elements = {
                id: 'elements', name: 'Elements'
            };
            Metrics.States = {
                id: 'states', name: 'States'
            };
            Metrics.Dataset = {
                id: 'dataset', name: 'Datasets'
            };
            Metrics.Buckets = {
                id: 'buckets', name: 'Buckets'
            };
        })(Metrics = C.Metrics || (C.Metrics = {}));
        C.MetricsList = [
            Metrics.BucketTimeline,
            Metrics.FaultTimeline,
            Metrics.Mutators,
            Metrics.Elements,
            Metrics.States,
            Metrics.Dataset,
            Metrics.Buckets
        ];
        var Templates;
        (function (Templates) {
            Templates.UiView = '<div ui-view></div>';
            Templates.Home = 'html/home.html';
            Templates.Jobs = 'html/jobs.html';
            Templates.Library = 'html/library.html';
            Templates.Error = 'html/error.html';
            var Job;
            (function (Job) {
                Job.Dashboard = 'html/job/dashboard.html';
                var Faults;
                (function (Faults) {
                    Faults.Summary = 'html/job/faults/summary.html';
                    Faults.Detail = 'html/job/faults/detail.html';
                })(Faults = Job.Faults || (Job.Faults = {}));
                Job.MetricPage = 'html/job/metrics/:metric.html';
                Job.BucketTimelineItem = 'bucketTimelineItem.html';
            })(Job = Templates.Job || (Templates.Job = {}));
            var Pit;
            (function (Pit) {
                Pit.Configure = 'html/pit/configure.html';
                var Advanced;
                (function (Advanced) {
                    Advanced.Variables = 'html/pit/advanced/variables.html';
                    Advanced.Monitoring = 'html/pit/advanced/monitoring.html';
                    Advanced.Tuning = 'html/pit/advanced/tuning.html';
                    Advanced.WebProxy = 'html/pit/advanced/webproxy.html';
                    Advanced.Test = 'html/pit/advanced/test.html';
                })(Advanced = Pit.Advanced || (Pit.Advanced = {}));
            })(Pit = Templates.Pit || (Templates.Pit = {}));
            var Modal;
            (function (Modal) {
                Modal.Confirm = 'html/modal/Confirm.html';
                Modal.Alert = 'html/modal/Alert.html';
                Modal.NewConfig = 'html/modal/NewConfig.html';
                Modal.MigratePit = 'html/modal/MigratePit.html';
                Modal.NewVar = 'html/modal/NewVar.html';
                Modal.PitLibrary = 'html/modal/PitLibrary.html';
                Modal.License = 'html/modal/License.html';
                Modal.StartJob = 'html/modal/StartJob.html';
                Modal.AddMonitor = 'html/modal/AddMonitor.html';
            })(Modal = Templates.Modal || (Templates.Modal = {}));
            var Directives;
            (function (Directives) {
                Directives.Agent = 'html/directives/agent.html';
                Directives.Combobox = 'html/directives/combobox.html';
                Directives.Defines = 'html/directives/defines.html';
                Directives.Faults = 'html/directives/faults.html';
                Directives.FaultAssets = 'html/directives/fault/assets.html';
                Directives.FaultFiles = 'html/directives/fault/files.html';
                Directives.Jobs = 'html/directives/jobs.html';
                Directives.Monitor = 'html/directives/monitor.html';
                Directives.Route = 'html/directives/route.html';
                Directives.Headers = 'html/directives/headers.html';
                Directives.Parameter = 'html/directives/parameter.html';
                Directives.Question = 'html/directives/question.html';
                Directives.Test = 'html/directives/test.html';
                Directives.ParameterCombo = 'html/directives/parameter/combo.html';
                Directives.ParameterInput = 'html/directives/parameter/input.html';
                Directives.ParameterSelect = 'html/directives/parameter/select.html';
                Directives.ParameterString = 'html/directives/parameter/string.html';
            })(Directives = Templates.Directives || (Templates.Directives = {}));
        })(Templates = C.Templates || (C.Templates = {}));
        var States;
        (function (States) {
            States.Main = 'main';
            States.MainHome = 'main.home';
            States.MainJobs = 'main.jobs';
            States.MainLibrary = 'main.library';
            States.MainError = 'main.error';
            States.Job = 'job';
            States.JobFaults = 'job.faults';
            States.JobFaultsDetail = 'job.faults.detail';
            States.JobMetrics = 'job.metrics';
            States.Pit = 'pit';
            States.PitAdvanced = 'pit.advanced';
            States.PitAdvancedVariables = 'pit.advanced.variables';
            States.PitAdvancedMonitoring = 'pit.advanced.monitoring';
            States.PitAdvancedTuning = 'pit.advanced.tuning';
            States.PitAdvancedWebProxy = 'pit.advanced.webproxy';
            States.PitAdvancedTest = 'pit.advanced.test';
        })(States = C.States || (C.States = {}));
    })(C = Peach.C || (Peach.C = {}));
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var AddMonitorController = (function () {
        function AddMonitorController($scope, $modalInstance, pitService) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.pitService = pitService;
            $scope.vm = this;
        }
        Object.defineProperty(AddMonitorController.prototype, "Monitors", {
            get: function () {
                if (this.$scope.search) {
                    var search_1 = this.$scope.search.toLowerCase();
                    var monitors = [];
                    for (var _i = 0, _a = this.pitService.Pit.metadata.monitors; _i < _a.length; _i++) {
                        var group = _a[_i];
                        if (_.some(group.items, function (item) {
                            var name = item.name.toLowerCase();
                            var pos = name.indexOf(search_1);
                            return pos !== -1;
                        })) {
                            monitors.push(group);
                        }
                    }
                    return monitors;
                }
                return this.pitService.Pit.metadata.monitors;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(AddMonitorController.prototype, "CanAccept", {
            get: function () {
                return !_.isUndefined(this.selected);
            },
            enumerable: false,
            configurable: true
        });
        AddMonitorController.prototype.OnSelect = function (item) {
            this.selected = item;
        };
        AddMonitorController.prototype.Accept = function () {
            this.$modalInstance.close(this.selected);
        };
        AddMonitorController.prototype.Cancel = function () {
            this.$modalInstance.dismiss();
        };
        AddMonitorController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            Peach.C.Services.Pit
        ];
        return AddMonitorController;
    }());
    Peach.AddMonitorController = AddMonitorController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    function Alert($modal, options) {
        return $modal.open({
            templateUrl: Peach.C.Templates.Modal.Alert,
            controller: AlertController,
            controllerAs: Peach.C.ViewModel,
            resolve: { Options: function () { return options; } }
        });
    }
    Peach.Alert = Alert;
    var AlertController = (function () {
        function AlertController($scope, $modalInstance, Options) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.Options = Options;
        }
        AlertController.prototype.OnSubmit = function () {
            this.$modalInstance.dismiss();
        };
        AlertController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            'Options'
        ];
        return AlertController;
    }());
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    function Confirm($modal, options) {
        return $modal.open({
            templateUrl: Peach.C.Templates.Modal.Confirm,
            controller: ConfirmController,
            controllerAs: Peach.C.ViewModel,
            resolve: { Options: function () { return options; } }
        });
    }
    Peach.Confirm = Confirm;
    var ConfirmController = (function () {
        function ConfirmController($scope, $modalInstance, Options) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.Options = Options;
        }
        ConfirmController.prototype.OnCancel = function () {
            this.$modalInstance.dismiss();
        };
        ConfirmController.prototype.OnSubmit = function () {
            this.$modalInstance.close('ok');
        };
        ConfirmController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            'Options'
        ];
        return ConfirmController;
    }());
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var ErrorController = (function () {
        function ErrorController($scope, $state) {
            this.Message = $state.params['message'] || 'An unknown error has occured.';
        }
        ErrorController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state
        ];
        return ErrorController;
    }());
    Peach.ErrorController = ErrorController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var EulaController = (function () {
        function EulaController($modalInstance) {
            this.$modalInstance = $modalInstance;
        }
        EulaController.prototype.OnSubmit = function () {
            this.$modalInstance.close();
        };
        EulaController.$inject = [
            Peach.C.Angular.$uibModalInstance
        ];
        return EulaController;
    }());
    Peach.EulaController = EulaController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var HomeController = (function () {
        function HomeController($scope) {
        }
        HomeController.$inject = [
            Peach.C.Angular.$scope
        ];
        return HomeController;
    }());
    Peach.HomeController = HomeController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var DashboardController = (function () {
        function DashboardController($scope, $state, jobService) {
            this.$state = $state;
            this.jobService = jobService;
        }
        Object.defineProperty(DashboardController.prototype, "ShowLimited", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.Job, function () { return _.isEmpty(_this.Job.pitUrl); });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "ShowStatus", {
            get: function () {
                return !_.isUndefined(this.Job);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "ShowCommands", {
            get: function () {
                return this.JobStatus !== Peach.JobStatus.Stopped;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "JobStatus", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.Job, function () { return _this.Job.status; });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "JobMode", {
            get: function () {
                return this.Job.mode;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "Job", {
            get: function () {
                return this.jobService.Job;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "RunningTime", {
            get: function () {
                return this.jobService.RunningTime;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "CanPause", {
            get: function () {
                return this.jobService.CanPause;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "CanContinue", {
            get: function () {
                return this.jobService.CanContinue;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "CanStop", {
            get: function () {
                return this.jobService.CanStop || this.jobService.CanKill;
            },
            enumerable: false,
            configurable: true
        });
        DashboardController.prototype.Pause = function () {
            this.jobService.Pause();
        };
        DashboardController.prototype.Stop = function () {
            if (this.Job.status === Peach.JobStatus.Stopping) {
                this.jobService.Kill();
            }
            else {
                this.jobService.Stop();
            }
        };
        DashboardController.prototype.Continue = function () {
            this.jobService.Continue();
        };
        Object.defineProperty(DashboardController.prototype, "StatusClass", {
            get: function () {
                if (!_.isUndefined(this.Job) && !_.isUndefined(this.Job.result)) {
                    return 'alert-danger';
                }
                return 'alert-info';
            },
            enumerable: false,
            configurable: true
        });
        DashboardController.prototype.ValueOr = function (value, alt) {
            return _.isUndefined(value) ? alt : value;
        };
        Object.defineProperty(DashboardController.prototype, "IsEditDisabled", {
            get: function () {
                return this.ShowLimited;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "IsReplayDisabled", {
            get: function () {
                return this.ShowLimited;
            },
            enumerable: false,
            configurable: true
        });
        DashboardController.prototype.OnEdit = function () {
            var pitId = _.last(this.Job.pitUrl.split('/'));
            this.$state.go(Peach.C.States.Pit, { pit: pitId });
        };
        DashboardController.prototype.OnReplay = function () {
            var pitId = _.last(this.Job.pitUrl.split('/'));
            this.$state
                .go(Peach.C.States.Pit, {
                pit: pitId,
                seed: this.Job.seed,
                rangeStart: this.Job.rangeStart,
                rangeStop: this.Job.rangeStop
            })
                .catch(function (reason) {
                console.log('failed to go', reason);
            });
        };
        Object.defineProperty(DashboardController.prototype, "StopPrompt", {
            get: function () {
                return (this.Job && this.Job.status === Peach.JobStatus.Stopping) ?
                    "Abort" :
                    "Stop";
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DashboardController.prototype, "StopIcon", {
            get: function () {
                return (this.Job && this.Job.status === Peach.JobStatus.Stopping) ?
                    "fa-power-off" :
                    "fa-stop";
            },
            enumerable: false,
            configurable: true
        });
        DashboardController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Services.Job
        ];
        return DashboardController;
    }());
    Peach.DashboardController = DashboardController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    function FaultsTitle(bucket) {
        return (bucket === "all") ? 'Faults' : 'Bucket: ' + bucket;
    }
    var FaultsDetailController = (function () {
        function FaultsDetailController($scope, $state, jobService) {
            var _this = this;
            this.Assets = {
                TestData: [],
                MonitorAssets: [],
                Other: []
            };
            this.InitialAssets = {
                TestData: [],
                MonitorAssets: [],
                Other: []
            };
            this.HasInitialAssets = false;
            $scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);
            var id = $state.params['id'];
            $scope.FaultDetailTitle = 'Test Case: ' + id;
            var promise = jobService.LoadFaultDetail(id);
            promise.then(function (detail) {
                _this.Fault = detail;
                for (var _i = 0, _a = detail.files; _i < _a.length; _i++) {
                    var file = _a[_i];
                    var assets = file.initial ? _this.InitialAssets : _this.Assets;
                    _this.organizeFile(file, assets);
                }
            }, function () {
                $state.go(Peach.C.States.MainHome);
            });
        }
        FaultsDetailController.prototype.organizeFile = function (file, assets) {
            if (assets === this.InitialAssets) {
                this.HasInitialAssets = true;
            }
            if (file.type === Peach.FaultFileType.Asset) {
                if (_.isEmpty(file.agentName) || _.isEmpty(file.monitorClass) || _.isEmpty(file.monitorName)) {
                    assets.Other.push(file);
                }
                else {
                    var lastAgent = _.last(assets.MonitorAssets);
                    if (_.isUndefined(lastAgent) || lastAgent.agentName !== file.agentName) {
                        lastAgent = {
                            agentName: file.agentName,
                            children: []
                        };
                        assets.MonitorAssets.push(lastAgent);
                    }
                    var lastMonitor = _.last(lastAgent.children);
                    if (_.isUndefined(lastMonitor) || lastMonitor.monitorName !== file.monitorName) {
                        lastMonitor = {
                            monitorName: file.monitorName,
                            monitorClass: file.monitorClass,
                            children: []
                        };
                        lastAgent.children.push(lastMonitor);
                    }
                    lastMonitor.children.push(file);
                }
            }
            else {
                assets.TestData.push(file);
            }
        };
        FaultsDetailController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Services.Job
        ];
        return FaultsDetailController;
    }());
    Peach.FaultsDetailController = FaultsDetailController;
    var FaultsController = (function () {
        function FaultsController($scope, $state) {
            $scope.FaultSummaryTitle = FaultsTitle($state.params['bucket']);
        }
        FaultsController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state
        ];
        return FaultsController;
    }());
    Peach.FaultsController = FaultsController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var MetricsController = (function () {
        function MetricsController($scope, $state, $interpolate, $templateCache, $timeout, VisDataSet, jobService) {
            var _this = this;
            this.$scope = $scope;
            this.$state = $state;
            this.$interpolate = $interpolate;
            this.$templateCache = $templateCache;
            this.$timeout = $timeout;
            this.VisDataSet = VisDataSet;
            this.jobService = jobService;
            this.MutatorData = [];
            this.AllMutatorData = [];
            this.ElementData = [];
            this.AllElementData = [];
            this.DatasetData = [];
            this.AllDatasetData = [];
            this.StateData = [];
            this.AllStateData = [];
            this.BucketData = [];
            this.AllBucketData = [];
            this.FaultsOverTimeLabels = [
                moment(Date.now()).format("M/D h a")
            ];
            this.FaultsOverTimeData = [
                [0]
            ];
            this.BucketTimelineData = undefined;
            this.BucketTimelineLoaded = false;
            this.BucketTimelineOptions = {
                showCurrentTime: true,
                selectable: false,
                type: "box",
                template: function (item) {
                    if (item.content) {
                        return item.content;
                    }
                    var html = _this.$templateCache.get(Peach.C.Templates.Job.BucketTimelineItem);
                    return _this.$interpolate(html)({ item: item });
                }
            };
            this.$scope.metric = $state.params['metric'];
            if (this.jobService.Job) {
                this.update();
            }
            else {
                var unwatch_1 = $scope.$watch(function () { return jobService.Job; }, function (newVal, oldVal) {
                    if (newVal !== oldVal) {
                        _this.update();
                        unwatch_1();
                    }
                });
            }
        }
        MetricsController.prototype.update = function () {
            var _this = this;
            var promise = this.jobService.LoadMetric(this.$scope.metric);
            switch (this.$scope.metric) {
                case Peach.C.Metrics.BucketTimeline.id:
                    var items_1 = new this.VisDataSet();
                    if (_.isUndefined(this.BucketTimelineData)) {
                        this.BucketTimelineData = {
                            items: items_1
                        };
                    }
                    promise.success(function (data) {
                        data.forEach(function (item) {
                            item.href = _this.$state.href(Peach.C.States.JobFaults, { bucket: item.label });
                            items_1.add({
                                id: item.iteration,
                                content: undefined,
                                start: item.time,
                                data: item
                            });
                        });
                        items_1.add({
                            id: 0,
                            style: "color: green",
                            content: "Job Start",
                            start: _this.jobService.Job.startDate
                        });
                        if (_this.jobService.Job.stopDate) {
                            items_1.add({
                                id: -1,
                                style: "color: red",
                                content: "Job End",
                                start: _this.jobService.Job.stopDate
                            });
                        }
                        _this.BucketTimelineData = {
                            items: items_1
                        };
                        _this.BucketTimelineLoaded = true;
                    });
                    break;
                case Peach.C.Metrics.FaultTimeline.id:
                    promise.success(function (data) {
                        if (data.length === 0) {
                            _this.FaultsOverTimeLabels = [moment(Date.now()).format("M/D h a")];
                            _this.FaultsOverTimeData = [[0]];
                        }
                        else {
                            _this.FaultsOverTimeLabels = data.map(function (x) { return moment(x.date).format("M/D h a"); }),
                                _this.FaultsOverTimeData = [_.map(data, 'faultCount')];
                        }
                    });
                    break;
                case Peach.C.Metrics.Mutators.id:
                    promise.success(function (data) {
                        _this.AllMutatorData = data;
                    });
                    break;
                case Peach.C.Metrics.Elements.id:
                    promise.success(function (data) {
                        _this.AllElementData = data;
                    });
                    break;
                case Peach.C.Metrics.Dataset.id:
                    promise.success(function (data) {
                        _this.AllDatasetData = data;
                    });
                    break;
                case Peach.C.Metrics.States.id:
                    promise.success(function (data) {
                        _this.AllStateData = data;
                    });
                    break;
                case Peach.C.Metrics.Buckets.id:
                    promise.success(function (data) {
                        _this.AllBucketData = data;
                    });
                    break;
            }
        };
        MetricsController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Angular.$interpolate,
            Peach.C.Angular.$templateCache,
            Peach.C.Angular.$timeout,
            Peach.C.Vendor.VisDataSet,
            Peach.C.Services.Job
        ];
        return MetricsController;
    }());
    Peach.MetricsController = MetricsController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var JobsController = (function () {
        function JobsController($scope) {
        }
        JobsController.$inject = [
            Peach.C.Angular.$scope
        ];
        return JobsController;
    }());
    Peach.JobsController = JobsController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var LibText = {
        Pits: 'The Pits section contains all of your licensed test modules. Custom Pits will also be shown in this section.',
        Configurations: 'The Configurations section contains existing Peach Pit configurations. Selecting an existing configuration allows editing the configuration and starting a new fuzzing job.',
        Legacy: 'The Legacy section contains configurations that have not yet been upgraded to Peach’s new configuration format. To upgrade a legacy configuration simply click on the configuration link below.'
    };
    var PitLibrary = (function () {
        function PitLibrary(Name) {
            this.Name = Name;
            this.Categories = [];
            this.Text = LibText[Name];
        }
        return PitLibrary;
    }());
    Peach.PitLibrary = PitLibrary;
    var PitCategory = (function () {
        function PitCategory(Name) {
            this.Name = Name;
            this.Pits = [];
        }
        return PitCategory;
    }());
    Peach.PitCategory = PitCategory;
    var PitEntry = (function () {
        function PitEntry(Library, Pit) {
            this.Library = Library;
            this.Pit = Pit;
        }
        return PitEntry;
    }());
    Peach.PitEntry = PitEntry;
    var LibraryController = (function () {
        function LibraryController($scope, $state, $modal, pitService) {
            this.$state = $state;
            this.$modal = $modal;
            this.pitService = pitService;
            this.Libs = [];
            this.refresh();
        }
        LibraryController.prototype.refresh = function () {
            var _this = this;
            var promise = this.pitService.LoadLibrary();
            promise.then(function (data) {
                _this.Libs = [];
                for (var _i = 0, data_1 = data; _i < data_1.length; _i++) {
                    var lib = data_1[_i];
                    var pitLib = new PitLibrary(lib.name);
                    var hasPits = false;
                    for (var _a = 0, _b = lib.versions; _a < _b.length; _a++) {
                        var version = _b[_a];
                        for (var _c = 0, _d = version.pits; _c < _d.length; _c++) {
                            var pit = _d[_c];
                            var category = _.find(pit.tags, function (tag) {
                                return tag.name.startsWith("Category");
                            }).values[1];
                            var pitCategory = _.find(pitLib.Categories, { 'Name': category });
                            if (!pitCategory) {
                                pitCategory = new PitCategory(category);
                                pitLib.Categories.push(pitCategory);
                            }
                            pitCategory.Pits.push(new PitEntry(lib, pit));
                            hasPits = true;
                        }
                        ;
                    }
                    ;
                    if (pitLib.Name !== 'Legacy' || hasPits) {
                        _this.Libs.push(pitLib);
                    }
                }
            });
        };
        LibraryController.prototype.ShowActions = function (entry) {
            return !entry.Library.locked;
        };
        LibraryController.prototype.OnSelectPit = function (entry) {
            var _this = this;
            if (entry.Library.versions[0].version === 1) {
                this.$modal.open({
                    templateUrl: Peach.C.Templates.Modal.MigratePit,
                    controller: Peach.MigratePitController,
                    resolve: {
                        Lib: function () { return _.find(_this.Libs, { Name: 'Pits' }); },
                        Pit: function () { return angular.copy(entry.Pit); }
                    }
                }).result.then(function (newPit) {
                    _this.GoToPit(newPit);
                });
            }
            else if (entry.Library.locked) {
                this.$modal.open({
                    templateUrl: Peach.C.Templates.Modal.NewConfig,
                    controller: Peach.NewConfigController,
                    resolve: {
                        Title: function () { return 'New Pit Configuration'; },
                        Prompt: function () {
                            return "This will create a new configuration for the <code>" + entry.Pit.name + "</code> pit. " +
                                'You will then be able to edit the configuration and start a new fuzzing job.';
                        },
                        Pit: function () { return angular.copy(entry.Pit); },
                        OnSubmit: function () { return function (modal) { return _this.DoNewConfig(modal); }; }
                    }
                }).result.then(function (copied) {
                    _this.GoToPit(copied);
                });
            }
            else {
                this.GoToPit(entry.Pit);
            }
        };
        LibraryController.prototype.DoNewConfig = function (modal) {
            this.pitService.NewConfig(modal.Pit)
                .then(function (response) {
                modal.Close(response.data);
            }, function (response) {
                switch (response.status) {
                    case 400:
                        modal.SetError(modal.Pit.name + " already exists, please choose a new name.");
                        break;
                    default:
                        modal.SetError("Error: " + response.statusText);
                        break;
                }
            });
        };
        LibraryController.prototype.OnCopyPit = function (entry) {
            var _this = this;
            this.$modal.open({
                templateUrl: Peach.C.Templates.Modal.NewConfig,
                controller: Peach.NewConfigController,
                resolve: {
                    Title: function () { return 'Copy Pit Configuration'; },
                    Prompt: function () { return "This will create a copy from the <code>" + entry.Pit.name + "</code> pit configuration. "; },
                    Pit: function () { return angular.copy(entry.Pit); },
                    OnSubmit: function () { return function (modal) { return _this.DoNewConfig(modal); }; }
                }
            }).result.then(function () {
                _this.refresh();
            });
        };
        LibraryController.prototype.OnEditPit = function (entry) {
            var _this = this;
            this.$modal.open({
                templateUrl: Peach.C.Templates.Modal.NewConfig,
                controller: Peach.NewConfigController,
                resolve: {
                    Title: function () { return 'Edit Pit Configuration'; },
                    Prompt: function () { return ''; },
                    Pit: function () { return angular.copy(entry.Pit); },
                    OnSubmit: function () { return function (modal) {
                        _this.pitService.EditConfig(modal.Pit)
                            .then(function () {
                            modal.Close(null);
                        }, function (response) {
                            switch (response.status) {
                                case 400:
                                    modal.SetError(modal.Pit.name + " already exists, please choose a new name.");
                                    break;
                                default:
                                    modal.SetError("Error: " + response.statusText);
                                    break;
                            }
                        });
                    }; }
                }
            }).result.finally(function () {
                _this.refresh();
            });
        };
        LibraryController.prototype.OnDeletePit = function (entry) {
            var _this = this;
            var options = {
                Title: 'Delete Pit Configuration',
                SubmitPrompt: 'Delete'
            };
            Peach.Confirm(this.$modal, options).result
                .then(function (result) {
                if (result === 'ok') {
                    _this.pitService.DeletePit(entry.Pit).then(function () {
                        _this.refresh();
                    }, function (response) {
                        Peach.Alert(_this.$modal, {
                            Title: 'Failed to delete pit configuration',
                            Body: response.statusText
                        }).result.finally(function () {
                            _this.refresh();
                        });
                    });
                }
            });
        };
        LibraryController.prototype.GoToPit = function (pit) {
            this.$state.go(Peach.C.States.Pit, { pit: pit.id });
        };
        LibraryController.prototype.filterCategory = function (search) {
            return function (category) {
                if (_.isEmpty(search)) {
                    return true;
                }
                return _.some(category.Pits, function (entry) {
                    return _.includes(entry.Pit.name.toLowerCase(), search.toLowerCase());
                });
            };
        };
        LibraryController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Angular.$uibModal,
            Peach.C.Services.Pit
        ];
        return LibraryController;
    }());
    Peach.LibraryController = LibraryController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var LicenseController = (function () {
        function LicenseController($scope, $modalInstance, Options) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.Options = Options;
        }
        LicenseController.prototype.OnSubmit = function () {
            this.$modalInstance.close();
        };
        LicenseController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            'Options'
        ];
        return LicenseController;
    }());
    Peach.LicenseController = LicenseController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var MainController = (function () {
        function MainController($scope, $state, $modal, $window, eulaService, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.$state = $state;
            this.$modal = $modal;
            this.$window = $window;
            this.eulaService = eulaService;
            this.pitService = pitService;
            this.showSidebar = false;
            this.isMenuMin = false;
            this.Metrics = Peach.C.MetricsList;
            this.configStepsDefault = [
                { id: Peach.C.States.PitAdvancedVariables, name: 'Variables' },
                { id: Peach.C.States.PitAdvancedMonitoring, name: 'Monitoring' },
                { id: Peach.C.States.PitAdvancedTuning, name: 'Tuning' },
                { id: Peach.C.States.PitAdvancedTest, name: 'Test' }
            ];
            this.configStepsWebProxy = [
                { id: Peach.C.States.PitAdvancedWebProxy, name: 'Web Proxy' },
                { id: Peach.C.States.PitAdvancedVariables, name: 'Variables' },
                { id: Peach.C.States.PitAdvancedMonitoring, name: 'Monitoring' },
                { id: Peach.C.States.PitAdvancedTest, name: 'Test' }
            ];
            this.subMenus = [
                { state: Peach.C.States.JobMetrics, collapsed: true },
                { state: Peach.C.States.PitAdvanced, collapsed: true },
                { state: 'help', collapsed: true }
            ];
            $scope.vm = this;
            $scope.$root.$on(Peach.C.Angular.$stateChangeSuccess, function () {
                _this.subMenus.forEach(function (item) {
                    if ($state.includes(item.state)) {
                        item.collapsed = false;
                    }
                    else {
                        item.collapsed = true;
                    }
                });
            });
            this.eulaService.Verify().then(function (license) {
                _this.license = license;
                _this.Version = license.version;
            });
        }
        Object.defineProperty(MainController.prototype, "LicenseExpiration", {
            get: function () {
                if (_.isUndefined(this.license)) {
                    return moment().add({ days: 60 });
                }
                return moment(this.license.expiration);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "LicenseExpirationDiff", {
            get: function () {
                return this.LicenseExpiration.diff(moment());
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "LicenseExpirationFromNow", {
            get: function () {
                return this.LicenseExpiration.fromNow();
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "JobId", {
            get: function () {
                return this.$state.params['job'];
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "PitId", {
            get: function () {
                return this.$state.params['pit'];
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "ConfigSteps", {
            get: function () {
                return this.pitService.IsWebProxy ? this.configStepsWebProxy : this.configStepsDefault;
            },
            enumerable: false,
            configurable: true
        });
        MainController.prototype.OnItemClick = function (event, enabled) {
            if (!enabled) {
                event.preventDefault();
                event.stopPropagation();
            }
        };
        MainController.prototype.IsCollapsed = function (state) {
            var subMenu = _.find(this.subMenus, { state: state });
            return subMenu.collapsed;
        };
        MainController.prototype.OnSubClick = function (event, state) {
            event.preventDefault();
            this.subMenus.forEach(function (item) {
                if (item.state === state) {
                    item.collapsed = !item.collapsed;
                }
                else {
                    item.collapsed = true;
                }
            });
        };
        Object.defineProperty(MainController.prototype, "FaultCount", {
            get: function () {
                var count = 0;
                if (this.$scope.job) {
                    count = this.$scope.job.faultCount;
                }
                return count || '';
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "IsMenuMinimized", {
            get: function () {
                return this.isMenuMin;
            },
            enumerable: false,
            configurable: true
        });
        MainController.prototype.OnToggleSidebar = function () {
            this.isMenuMin = !this.isMenuMin;
        };
        Object.defineProperty(MainController.prototype, "SidebarClass", {
            get: function () {
                return {
                    'menu-min': this.IsMenuMinimized,
                    'display': this.showSidebar
                };
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MainController.prototype, "MenuTogglerClass", {
            get: function () {
                return {
                    'display': this.showSidebar
                };
            },
            enumerable: false,
            configurable: true
        });
        MainController.prototype.OnMenuToggle = function () {
            this.showSidebar = !this.showSidebar;
        };
        MainController.prototype.ShowMenu = function (name) {
            return this.$state.includes(name);
        };
        MainController.prototype.ShowQuickStart = function () {
            return this.ShowMenu('pit') && !this.pitService.IsWebProxy;
        };
        MainController.prototype.MetricUrl = function (metric) {
            var state = [Peach.C.States.JobMetrics, metric.id].join('.');
            var params = { job: this.JobId };
            return this.$state.href(state, params);
        };
        MainController.prototype.MetricActive = function (metric) {
            var state = [Peach.C.States.JobMetrics, metric.id].join('.');
            var params = { job: this.JobId };
            if (this.$state.is(state, params)) {
                return 'active';
            }
            return undefined;
        };
        MainController.prototype.ShortcutClass = function (section) {
            return '';
        };
        MainController.prototype.OnHelp = function () {
            this.$window.open('/docs/user', '_blank');
        };
        Object.defineProperty(MainController.prototype, "ShowLicenseWarning", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.LicenseExpiration, function () {
                    return _this.LicenseExpiration.diff(moment(), 'days') < 30;
                });
            },
            enumerable: false,
            configurable: true
        });
        MainController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Angular.$uibModal,
            Peach.C.Angular.$window,
            Peach.C.Services.Eula,
            Peach.C.Services.Pit
        ];
        return MainController;
    }());
    Peach.MainController = MainController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var MigratePitController = (function () {
        function MigratePitController($scope, $modalInstance, pitService, Lib, Pit) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.pitService = pitService;
            this.Lib = Lib;
            this.Pit = Pit;
            this.pending = false;
            this.Error = "";
            $scope.vm = this;
        }
        Object.defineProperty(MigratePitController.prototype, "CanAccept", {
            get: function () {
                return !_.isUndefined(this.selected) && !this.pending;
            },
            enumerable: false,
            configurable: true
        });
        MigratePitController.prototype.OnSelect = function (item) {
            this.selected = item;
        };
        MigratePitController.prototype.OnCustom = function () {
            this.MigratePit(this.Pit);
        };
        MigratePitController.prototype.Accept = function () {
            this.MigratePit(this.selected.Pit);
        };
        MigratePitController.prototype.MigratePit = function (originalPit) {
            var _this = this;
            this.Error = "";
            this.pending = true;
            this.pitService.MigratePit(this.Pit, originalPit)
                .then(function (response) {
                _this.pending = false;
                _this.$modalInstance.close(response.data);
            }, function (response) {
                _this.pending = false;
                switch (response.status) {
                    case 400:
                        _this.Error = _this.Pit.name + " already exists.";
                        break;
                    default:
                        _this.Error = "Error: " + response.statusText;
                        break;
                }
            });
        };
        MigratePitController.prototype.Cancel = function () {
            this.$modalInstance.dismiss();
        };
        MigratePitController.prototype.filterCategory = function (search) {
            return function (category) {
                if (_.isEmpty(search)) {
                    return true;
                }
                return _.some(category.Pits, function (entry) {
                    return _.includes(entry.Pit.name.toLowerCase(), search.toLowerCase());
                });
            };
        };
        MigratePitController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            Peach.C.Services.Pit,
            "Lib",
            "Pit"
        ];
        return MigratePitController;
    }());
    Peach.MigratePitController = MigratePitController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var NewConfigController = (function () {
        function NewConfigController($scope, $modalInstance, Title, Prompt, Pit, OnSubmit) {
            this.$modalInstance = $modalInstance;
            this.Title = Title;
            this.Prompt = Prompt;
            this.Pit = Pit;
            this.OnSubmit = OnSubmit;
            this.pending = false;
            this.Error = "";
            $scope.vm = this;
        }
        NewConfigController.prototype.Submit = function () {
            this.Error = "";
            this.pending = true;
            this.OnSubmit(this);
        };
        NewConfigController.prototype.SetError = function (msg) {
            this.pending = false;
            this.Error = msg;
        };
        NewConfigController.prototype.Close = function (result) {
            this.pending = false;
            this.$modalInstance.close(result);
        };
        NewConfigController.prototype.Cancel = function () {
            this.$modalInstance.dismiss();
        };
        Object.defineProperty(NewConfigController.prototype, "IsSubmitDisabled", {
            get: function () {
                return this.pending;
            },
            enumerable: false,
            configurable: true
        });
        NewConfigController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            "Title",
            "Prompt",
            "Pit",
            "OnSubmit"
        ];
        return NewConfigController;
    }());
    Peach.NewConfigController = NewConfigController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var NewVarController = (function () {
        function NewVarController($scope, $modalInstance, pitService) {
            this.$scope = $scope;
            this.$modalInstance = $modalInstance;
            this.pitService = pitService;
            $scope.vm = this;
            this.Param = {
                key: "",
                value: "",
                name: "",
                description: 'User-defined variable',
                type: Peach.ParameterType.User
            };
        }
        Object.defineProperty(NewVarController.prototype, "ParamKeys", {
            get: function () {
                return _.map(this.pitService.Pit.config, 'key');
            },
            enumerable: false,
            configurable: true
        });
        NewVarController.prototype.Cancel = function () {
            this.$modalInstance.dismiss();
        };
        NewVarController.prototype.Accept = function () {
            this.$modalInstance.close(this.Param);
        };
        NewVarController.prototype.OnNameBlur = function () {
            this.hasBlurred = true;
        };
        NewVarController.prototype.OnNameChanged = function () {
            var value = this.Param.name;
            if (!this.hasBlurred) {
                if (_.isString(value)) {
                    this.Param.key = value.replace(new RegExp(' ', 'g'), '');
                }
                else {
                    this.Param.key = undefined;
                }
            }
        };
        NewVarController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModalInstance,
            Peach.C.Services.Pit
        ];
        return NewVarController;
    }());
    Peach.NewVarController = NewVarController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var ConfigureDefinesController = (function () {
        function ConfigureDefinesController($scope, $modal, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.$modal = $modal;
            this.pitService = pitService;
            this.hasLoaded = false;
            this.isSaved = false;
            var UserDefinesName = "User Defines";
            var promise = pitService.LoadPit();
            promise.then(function (pit) {
                _this.View = pit.definesView;
                for (var _i = 0, _a = _this.View; _i < _a.length; _i++) {
                    var group = _a[_i];
                    if (group.type === Peach.ParameterType.Group &&
                        group.name === UserDefinesName) {
                        _this.UserDefines = group;
                    }
                }
                ;
                if (!_this.UserDefines) {
                    _this.UserDefines = {
                        type: Peach.ParameterType.Group,
                        name: UserDefinesName,
                        items: []
                    };
                    var systemDefines = _this.View.pop();
                    _this.View.push(_this.UserDefines);
                    _this.View.push(systemDefines);
                }
                _this.hasLoaded = true;
            });
        }
        Object.defineProperty(ConfigureDefinesController.prototype, "ShowLoading", {
            get: function () {
                return !this.hasLoaded;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureDefinesController.prototype, "ShowSaved", {
            get: function () {
                return !this.$scope.form.$dirty && this.isSaved;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureDefinesController.prototype, "ShowRequired", {
            get: function () {
                return this.$scope.form.$pristine && this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureDefinesController.prototype, "ShowValidation", {
            get: function () {
                return this.$scope.form.$dirty && this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureDefinesController.prototype, "CanSave", {
            get: function () {
                return this.$scope.form.$dirty && !this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        ConfigureDefinesController.prototype.OnSave = function () {
            var _this = this;
            var promise = this.pitService.SaveDefines(this.View);
            promise.then(function () {
                _this.isSaved = true;
                _this.$scope.form.$setPristine();
            });
        };
        ConfigureDefinesController.prototype.OnAdd = function () {
            var _this = this;
            var modal = this.$modal.open({
                templateUrl: Peach.C.Templates.Modal.NewVar,
                controller: Peach.NewVarController
            });
            modal.result.then(function (param) {
                _this.UserDefines.items.push(param);
                _this.$scope.form.$setDirty();
            });
        };
        ConfigureDefinesController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModal,
            Peach.C.Services.Pit
        ];
        return ConfigureDefinesController;
    }());
    Peach.ConfigureDefinesController = ConfigureDefinesController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var ConfigureMonitorsController = (function () {
        function ConfigureMonitorsController($scope, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.pitService = pitService;
            this.hasLoaded = false;
            this.isSaved = false;
            var promise = pitService.LoadPit();
            promise.then(function (pit) {
                _this.Agents = pit.agents;
                _this.hasLoaded = true;
            });
        }
        Object.defineProperty(ConfigureMonitorsController.prototype, "ShowLoading", {
            get: function () {
                return !this.hasLoaded;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureMonitorsController.prototype, "ShowSaved", {
            get: function () {
                return !this.$scope.form.$dirty && this.isSaved;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureMonitorsController.prototype, "ShowError", {
            get: function () {
                return this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureMonitorsController.prototype, "ShowMissingAgents", {
            get: function () {
                return this.hasLoaded && this.NumAgents === 0;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureMonitorsController.prototype, "CanSave", {
            get: function () {
                return this.$scope.form.$dirty && !this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        ConfigureMonitorsController.prototype.AddAgent = function () {
            this.Agents.push({
                name: "",
                agentUrl: "",
                monitors: []
            });
            this.$scope.form.$setDirty();
        };
        ConfigureMonitorsController.prototype.Save = function () {
            var _this = this;
            var promise = this.pitService.SaveAgents(this.Agents);
            promise.then(function () {
                _this.isSaved = true;
                _this.$scope.form.$setPristine();
            });
        };
        Object.defineProperty(ConfigureMonitorsController.prototype, "NumAgents", {
            get: function () {
                return (this.Agents && this.Agents.length) || 0;
            },
            enumerable: false,
            configurable: true
        });
        ConfigureMonitorsController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return ConfigureMonitorsController;
    }());
    Peach.ConfigureMonitorsController = ConfigureMonitorsController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var SHIFT_WIDTH = 20;
    var MAX_NODES = 2000;
    var DELAY = 500;
    function defaultWeight(node) {
        return _.isUndefined(node.weight) ? 3 : node.weight;
    }
    function flatten(nodes, depth, prefix, parent, result) {
        nodes.forEach(function (node) {
            var here = "" + prefix + node.id;
            var flat = {
                node: node,
                parent: parent,
                id: node.id,
                fullId: here,
                depth: depth,
                style: { 'margin-left': depth * SHIFT_WIDTH },
                showExpander: !_.isEmpty(node.fields),
                display: node.id
            };
            result.nodes.push(flat);
            result.total++;
            flatten(node.fields, depth + 1, flat.fullId + ".", flat, result);
        });
    }
    function includeNode(node) {
        if (_.isNull(node) || node.include)
            return;
        node.include = true;
        includeNode(node.parent);
    }
    function expandNode(node) {
        if (_.isNull(node) || node.node.expanded)
            return;
        node.node.expanded = true;
        expandNode(node.parent);
    }
    function matchWeight(node, weight) {
        return (defaultWeight(node) === weight) ||
            _.some(node.fields, function (field) { return matchWeight(field, weight); });
    }
    function selectWeight(node, weight) {
        node.weight = weight;
        var fields = node.fields || [];
        fields.forEach(function (field) { return selectWeight(field, weight); });
    }
    function applyWeights(weights, fields) {
        for (var _i = 0, weights_1 = weights; _i < weights_1.length; _i++) {
            var rule = weights_1[_i];
            var parts = rule.id.split('.');
            applyWeight(fields, parts, rule.weight);
        }
    }
    function applyWeight(fields, parts, weight) {
        var next = parts.shift();
        for (var _i = 0, fields_1 = fields; _i < fields_1.length; _i++) {
            var node = fields_1[_i];
            if (node.id === next) {
                if (parts.length === 0) {
                    node.weight = weight;
                }
                else {
                    applyWeight(node.fields, parts, weight);
                }
            }
        }
    }
    function extractWeights(prefix, tree, collect) {
        for (var _i = 0, tree_1 = tree; _i < tree_1.length; _i++) {
            var node = tree_1[_i];
            var here = "" + prefix + node.id;
            if (defaultWeight(node) !== 3) {
                collect.push({ id: here, weight: node.weight });
            }
            extractWeights(here + ".", node.fields, collect);
        }
    }
    function cloneFields(fields) {
        return fields.map(function (item) { return ({
            id: item.id,
            fields: cloneFields(item.fields)
        }); });
    }
    var ConfigureTuningController = (function () {
        function ConfigureTuningController($scope, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.pitService = pitService;
            this.pit = null;
            this.isSaved = false;
            this.tree = [];
            this.source = [];
            this.total = 0;
            this.nodeHover = null;
            this.hovers = [
                false,
                false,
                false,
                false,
                false,
                false
            ];
            this.$scope.search = '';
            this.$scope.lastSearch = '';
            this.$scope.hasLoaded = false;
            this.$scope.hasData = false;
            this.$scope.isTruncated = false;
            this.$scope.MAX_NODES = MAX_NODES;
            this.DelayedOnSearch = _.debounce(function () { return _this.OnSearch(); }, DELAY);
            var promise = pitService.LoadPit();
            promise.then(function (pit) {
                _this.pit = pit;
                if (pit.metadata.fields) {
                    _this.init();
                    _this.update();
                    _this.$scope.hasData = true;
                }
                _this.$scope.hasLoaded = true;
            });
        }
        ConfigureTuningController.prototype.init = function () {
            this.tree = cloneFields(this.pit.metadata.fields);
            applyWeights(this.pit.weights, this.tree);
            var result = {
                nodes: [],
                total: 0
            };
            flatten(this.tree, 0, '', null, result);
            this.source = result.nodes;
            this.total = result.total;
        };
        ConfigureTuningController.prototype.update = function () {
            this.source.forEach(function (node) {
                var parent = node.parent;
                var inner = node.node;
                node.visible = !parent || (parent.expanded && parent.visible);
                node.expanded = _.isUndefined(inner.expanded) ?
                    node.depth < 2 :
                    inner.expanded;
                node.expanderIcon = node.expanded ? 'fa-minus' : 'fa-plus';
                node.weight = defaultWeight(inner);
                node.weightIcons = _.range(6).map(function (i) { return ((node.weight === i) ? 'fa-circle' :
                    (!node.expanded && matchWeight(inner, i)) ?
                        'fa-dot-circle-o' :
                        'fa-circle-thin'); });
            });
            var visible = _.filter(this.source, 'visible');
            this.$scope.isTruncated = (visible.length > MAX_NODES);
            this.$scope.flat = _.take(visible, MAX_NODES);
        };
        ConfigureTuningController.prototype.search = function (search) {
            this.init();
            var parts = search.split('.').reverse();
            var lastPart = parts.shift();
            var partial = new RegExp("(" + lastPart + ")", 'gi');
            var starting = new RegExp("^(" + lastPart + ")", 'i');
            this.source.forEach(function (node) {
                var id = node.id.toLowerCase();
                var fullId = node.fullId.toLowerCase();
                if (_.isEmpty(parts)) {
                    if (_.includes(fullId, lastPart)) {
                        includeNode(node);
                    }
                    if (_.includes(id, lastPart)) {
                        expandNode(node.parent);
                        node.display = node.id.replace(partial, '<strong>$1</strong>');
                    }
                }
                else {
                    if (_.startsWith(id, lastPart)) {
                        var cur = node.parent;
                        for (var _i = 0, parts_1 = parts; _i < parts_1.length; _i++) {
                            var part = parts_1[_i];
                            if (_.isNull(cur)) {
                                return;
                            }
                            if (cur.id.toLowerCase() !== part) {
                                return;
                            }
                            cur = cur.parent;
                        }
                        includeNode(node);
                        expandNode(node.parent);
                        node.display = node.id.replace(starting, '<strong>$1</strong>');
                        cur = node.parent;
                        for (var _a = 0, parts_2 = parts; _a < parts_2.length; _a++) {
                            var part = parts_2[_a];
                            cur.display = "<strong>" + cur.id + "</strong>";
                            cur = cur.parent;
                        }
                    }
                }
            });
            this.source = _.filter(this.source, 'include');
        };
        ConfigureTuningController.prototype.DelayedApply = function () {
            var _this = this;
            setTimeout(function () { return _this.$scope.$apply(); }, 100);
        };
        ConfigureTuningController.prototype.LegendText = function (i) {
            return this.hovers[i] ? 'text bold' : 'text';
        };
        ConfigureTuningController.prototype.LegendIcon = function (i) {
            return this.hovers[i] ? 'fa-circle' : 'fa-circle-thin';
        };
        ConfigureTuningController.prototype.OnLegendEnter = function (i) {
            this.hovers[i] = true;
        };
        ConfigureTuningController.prototype.OnLegendLeave = function (i) {
            this.hovers[i] = false;
        };
        ConfigureTuningController.prototype.isHovered = function (node) {
            return !_.isNull(this.nodeHover) && (node.node === this.nodeHover.node);
        };
        ConfigureTuningController.prototype.RowHover = function (node) {
            return this.isHovered(node) ? 'tuning-row-hover' : '';
        };
        ConfigureTuningController.prototype.OnRowEnter = function (node) {
            this.nodeHover = node;
        };
        ConfigureTuningController.prototype.OnRowLeave = function (node) {
            this.nodeHover = null;
        };
        ConfigureTuningController.prototype.OnToggleExpand = function (node) {
            var _this = this;
            if (!node.showExpander)
                return;
            node.node.expanded = !node.expanded;
            node.expanderIcon = 'fa-spin fa-clock-o';
            setTimeout(function () {
                _this.update();
                _this.DelayedApply();
            });
        };
        ConfigureTuningController.prototype.OnSelectWeight = function (node, weight) {
            var _this = this;
            node.weightIcons[weight] = 'fa-spin fa-clock-o';
            selectWeight(node.node, weight);
            setTimeout(function () {
                _this.update();
                _this.$scope.form.$setDirty();
                _this.DelayedApply();
            });
        };
        Object.defineProperty(ConfigureTuningController.prototype, "ShowSaved", {
            get: function () {
                return !this.$scope.form.$dirty && this.isSaved;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureTuningController.prototype, "CanSave", {
            get: function () {
                return this.$scope.form.$dirty;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureTuningController.prototype, "CanSearch", {
            get: function () {
                return this.$scope.hasData;
            },
            enumerable: false,
            configurable: true
        });
        ConfigureTuningController.prototype.OnSave = function () {
            var _this = this;
            var weights = [];
            extractWeights('', this.tree, weights);
            var promise = this.pitService.SaveWeights(weights);
            promise.then(function () {
                _this.isSaved = true;
                _this.$scope.form.$setPristine();
            });
        };
        ConfigureTuningController.prototype.OnSearch = function () {
            var _this = this;
            this.$scope.hasLoaded = false;
            setTimeout(function () {
                var search = _this.$scope.search.toLowerCase();
                _this.$scope.lastSearch = search;
                if (_.isEmpty(search)) {
                    _this.init();
                }
                else {
                    _this.search(search);
                }
                _this.update();
                _this.$scope.hasLoaded = true;
                _this.DelayedApply();
            });
        };
        ConfigureTuningController.prototype.OnSearchChange = function () {
            if (this.total < MAX_NODES) {
                this.DelayedOnSearch();
            }
        };
        ConfigureTuningController.prototype.OnSearchKeyPress = function (event) {
            if (event.which === 13) {
                this.OnSearch();
            }
        };
        ConfigureTuningController.prototype.DirtySearch = function () {
            return this.$scope.search.toLowerCase() === this.$scope.lastSearch ?
                '' :
                'dirty-search';
        };
        ConfigureTuningController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return ConfigureTuningController;
    }());
    Peach.ConfigureTuningController = ConfigureTuningController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var ConfigureWebProxyController = (function () {
        function ConfigureWebProxyController($scope, $modal, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.$modal = $modal;
            this.pitService = pitService;
            this.hasLoaded = false;
            this.isSaved = false;
            var promise = pitService.LoadPit();
            promise.then(function (pit) {
                _this.Routes = pit.webProxy.routes;
                for (var _i = 0, _a = _this.Routes; _i < _a.length; _i++) {
                    var route = _a[_i];
                    route.faultOnStatusCodesText = _.join(route.faultOnStatusCodes, ',');
                    route.mutateChoice = route.mutate ? 'Include' : 'Exclude';
                    if (route.headers) {
                        for (var _b = 0, _c = route.headers; _b < _c.length; _b++) {
                            var header = _c[_b];
                            header.mutateChoice = header.mutate ? 'Include' : 'Exclude';
                        }
                    }
                }
                _this.hasLoaded = true;
            });
        }
        Object.defineProperty(ConfigureWebProxyController.prototype, "ShowLoading", {
            get: function () {
                return !this.hasLoaded;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureWebProxyController.prototype, "ShowSaved", {
            get: function () {
                return !this.$scope.form.$dirty && this.isSaved;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureWebProxyController.prototype, "ShowError", {
            get: function () {
                return this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureWebProxyController.prototype, "CanSave", {
            get: function () {
                return this.$scope.form.$dirty && !this.$scope.form.$invalid;
            },
            enumerable: false,
            configurable: true
        });
        ConfigureWebProxyController.prototype.OnSave = function () {
            var _this = this;
            for (var _i = 0, _a = this.Routes; _i < _a.length; _i++) {
                var route = _a[_i];
                route.faultOnStatusCodes = _.map(_.split(route.faultOnStatusCodesText, ','), _.parseInt);
                route.mutate = route.mutateChoice === 'Include';
                if (route.headers) {
                    for (var _b = 0, _c = route.headers; _b < _c.length; _b++) {
                        var header = _c[_b];
                        header.mutate = header.mutateChoice === 'Include';
                    }
                }
            }
            var promise = this.pitService.SaveWebProxy({ routes: this.Routes });
            promise.then(function () {
                _this.isSaved = true;
                _this.$scope.form.$setPristine();
            });
        };
        ConfigureWebProxyController.prototype.OnAdd = function () {
            this.Routes.unshift({
                url: "",
                swagger: "",
                script: "",
                mutate: false,
                baseUrl: "",
                faultOnStatusCodes: [500, 501],
                headers: [
                    {
                        name: "*",
                        mutate: true,
                        mutateChoice: 'Include'
                    }
                ],
                faultOnStatusCodesText: "500,501",
                mutateChoice: 'Include'
            });
            this.$scope.form.$setDirty();
        };
        ConfigureWebProxyController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModal,
            Peach.C.Services.Pit
        ];
        return ConfigureWebProxyController;
    }());
    Peach.ConfigureWebProxyController = ConfigureWebProxyController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var ConfigureController = (function () {
        function ConfigureController($scope, $state, $localStorage, pitService, jobService) {
            var _this = this;
            this.$state = $state;
            this.pitService = pitService;
            this.jobService = jobService;
            this.storage = $localStorage['$default']({
                showCfgHelp: true,
                showStartHelp: true
            });
            this.pitService.LoadPit().then(function (pit) {
                _this.Pit = pit;
                _this.Job = {
                    pitUrl: pit.pitUrl,
                    seed: $state.params['seed'],
                    rangeStart: $state.params['rangeStart'] || undefined,
                    rangeStop: $state.params['rangeStop']
                };
            });
        }
        Object.defineProperty(ConfigureController.prototype, "ShowReady", {
            get: function () {
                return this.pitService.HasMonitors && this.CanStart;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "ShowNotConfigured", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pitService.Pit, function () { return !_this.pitService.IsConfigured; });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "ShowNoMonitors", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pitService.Pit, function () {
                    return _this.pitService.IsConfigured && !_this.pitService.HasMonitors;
                });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "ShowWebProxy", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pitService.Pit, function () { return _this.pitService.IsWebProxy; });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "CanStart", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pitService.Pit, function () { return _this.pitService.IsConfigured; });
            },
            enumerable: false,
            configurable: true
        });
        ConfigureController.prototype.Start = function () {
            var _this = this;
            this.jobService.Start(this.Job)
                .then(function (job) {
                _this.$state.go(Peach.C.States.Job, { job: job.id });
            });
        };
        ConfigureController.prototype.OnCfgHelp = function () {
            this.storage.showCfgHelp = !this.storage.showCfgHelp;
        };
        ConfigureController.prototype.OnStartHelp = function () {
            this.storage.showStartHelp = !this.storage.showStartHelp;
        };
        Object.defineProperty(ConfigureController.prototype, "StartHelpClass", {
            get: function () {
                return { active: this.storage.showStartHelp };
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "CfgHelpClass", {
            get: function () {
                return { active: this.storage.showCfgHelp };
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "StartHelpPrompt", {
            get: function () {
                return this.storage.showStartHelp ? 'Hide' : 'Help';
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ConfigureController.prototype, "CfgHelpPrompt", {
            get: function () {
                return this.storage.showCfgHelp ? 'Hide' : 'Help';
            },
            enumerable: false,
            configurable: true
        });
        ConfigureController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Angular.$localStorage,
            Peach.C.Services.Pit,
            Peach.C.Services.Job
        ];
        return ConfigureController;
    }());
    Peach.ConfigureController = ConfigureController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var PitTestController = (function () {
        function PitTestController($scope, $state, pitService, testService) {
            this.$state = $state;
            this.pitService = pitService;
            this.testService = testService;
            this.Title = 'Test';
            $scope.Title = "Test";
            this.pitService.LoadPit();
        }
        Object.defineProperty(PitTestController.prototype, "ShowNotConfigured", {
            get: function () {
                return !this.pitService.IsConfigured;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitTestController.prototype, "ShowNoMonitors", {
            get: function () {
                return this.pitService.IsConfigured && !this.pitService.HasMonitors;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitTestController.prototype, "CanBeginTest", {
            get: function () {
                return this.pitService.IsConfigured && this.testService.CanBeginTest;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitTestController.prototype, "CanContinue", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.testService.TestResult, function () {
                    return _this.testService.CanBeginTest &&
                        _this.testService.TestResult.status === Peach.TestStatus.Pass;
                }) || false;
            },
            enumerable: false,
            configurable: true
        });
        PitTestController.prototype.OnBeginTest = function () {
            this.testService.BeginTest();
        };
        PitTestController.prototype.OnNextTrack = function () {
            this.$state.go(Peach.C.States.Pit);
        };
        PitTestController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Services.Pit,
            Peach.C.Services.Test
        ];
        return PitTestController;
    }());
    Peach.PitTestController = PitTestController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.AgentDirective = {
        ComponentID: Peach.C.Directives.Agent,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Agent,
        controller: Peach.C.Controllers.Agent,
        scope: {
            agents: '=',
            agent: '=',
            agentIndex: '='
        }
    };
    var AgentController = (function () {
        function AgentController($scope, $modal, pitService) {
            this.$scope = $scope;
            this.$modal = $modal;
            this.pitService = pitService;
            $scope.vm = this;
            $scope.isOpen = true;
        }
        Object.defineProperty(AgentController.prototype, "Header", {
            get: function () {
                var url = this.$scope.agent.agentUrl || 'local://';
                var name = this.$scope.agent.name ? "(" + this.$scope.agent.name + ")" : '';
                return url + " " + name;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(AgentController.prototype, "CanMoveUp", {
            get: function () {
                return this.$scope.agentIndex !== 0;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(AgentController.prototype, "CanMoveDown", {
            get: function () {
                return this.$scope.agentIndex !== (this.$scope.agents.length - 1);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(AgentController.prototype, "ShowMissingMonitors", {
            get: function () {
                return this.$scope.agent.monitors.length === 0;
            },
            enumerable: false,
            configurable: true
        });
        AgentController.prototype.OnMoveUp = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemUp(this.$scope.agents, this.$scope.agentIndex);
            this.$scope.form.$setDirty();
        };
        AgentController.prototype.OnMoveDown = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemDown(this.$scope.agents, this.$scope.agentIndex);
            this.$scope.form.$setDirty();
        };
        AgentController.prototype.OnRemove = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$scope.agents.splice(this.$scope.agentIndex, 1);
            this.$scope.form.$setDirty();
        };
        AgentController.prototype.AddMonitor = function () {
            var _this = this;
            var modal = this.$modal.open({
                templateUrl: Peach.C.Templates.Modal.AddMonitor,
                controller: Peach.AddMonitorController
            });
            modal.result.then(function (selected) {
                var monitor = _this.pitService.CreateMonitor(selected);
                _this.$scope.agent.monitors.push(monitor);
                _this.$scope.form.$setDirty();
            });
        };
        AgentController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModal,
            Peach.C.Services.Pit
        ];
        return AgentController;
    }());
    Peach.AgentController = AgentController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.AutoFocusDirective = {
        ComponentID: Peach.C.Directives.AutoFocus,
        restrict: 'AC',
        link: function (scope, element) {
            _.delay(function () {
                element[0].focus();
            }, 100);
        }
    };
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var KEY = {
        TAB: 9,
        ENTER: 13,
        ESC: 27,
        UP: 38,
        DOWN: 40
    };
    Peach.ComboboxDirective = {
        ComponentID: Peach.C.Directives.Combobox,
        restrict: 'E',
        require: [Peach.C.Directives.Combobox, Peach.C.Angular.ngModel],
        replace: true,
        controller: Peach.C.Controllers.Combobox,
        controllerAs: 'vm',
        templateUrl: Peach.C.Templates.Directives.Combobox,
        scope: {
            data: '=',
            placeholder: '&'
        },
        link: function (scope, element, attrs, ctrls) {
            var ctrl = ctrls[0];
            ctrl.Link(element, attrs, ctrls[1]);
        }
    };
    var ComboboxController = (function () {
        function ComboboxController($scope, $document) {
            this.$scope = $scope;
            this.$document = $document;
        }
        ComboboxController.prototype.Link = function (element, attrs, ctrl) {
            var _this = this;
            this.$element = element;
            this.$model = ctrl;
            this.$scope.showOptions = false;
            this.$scope.options = [];
            this.$scope.highlighted = null;
            this.$scope.$watchCollection('data', function (newVal, oldVal) {
                if (newVal !== oldVal) {
                    _this.buildOptions();
                }
            });
            this.$scope.$watch('selected', function (newVal) {
                if (_this.$model.$viewValue !== newVal) {
                    _this.$model.$setViewValue(newVal);
                    _this.buildOptions(newVal);
                }
            });
            this.$model.$formatters.unshift(function (value) {
                _this.setSelected(value);
                return value;
            });
            this.$model.$viewChangeListeners.unshift(function () {
                _this.setSelected(_this.$model.$viewValue);
            });
            var hideOptions = this.hideOptions.bind(this);
            this.$document.on('click', hideOptions);
            this.$element.on('$destroy', function () {
                _this.$document.off('click', hideOptions);
            });
        };
        ComboboxController.prototype.SelectOption = function (option) {
            this.$scope.showOptions = false;
            this.$model.$setViewValue(option);
        };
        ComboboxController.prototype.OnKeyDown = function (event) {
            if (event.keyCode === KEY.ENTER ||
                event.keyCode === KEY.TAB) {
                if (!_.isNull(this.$scope.highlighted)) {
                    this.SelectOption(this.$scope.options[this.$scope.highlighted]);
                    this.$scope.highlighted = null;
                    event.preventDefault();
                    event.stopPropagation();
                }
            }
        };
        ComboboxController.prototype.OnKeyUp = function (event) {
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
                }
                else if (this.$scope.highlighted < (this.$scope.options.length - 1)) {
                    this.$scope.highlighted++;
                }
            }
            else if (event.keyCode === KEY.UP) {
                if (this.$scope.highlighted > 0) {
                    this.$scope.highlighted--;
                }
            }
            this.$scope.showOptions = true;
        };
        ComboboxController.prototype.ToggleOptions = function () {
            this.buildOptions();
            this.$scope.showOptions = !this.$scope.showOptions;
            this.$element.find('input').focus();
        };
        ComboboxController.prototype.buildOptions = function (filter) {
            var _this = this;
            this.$scope.options = [];
            filter = filter || '';
            filter = filter.toLowerCase();
            if (this.$scope.data) {
                _.each(this.$scope.data, function (item) {
                    if (item.toLowerCase().indexOf(filter) >= 0) {
                        _this.$scope.options.push(item);
                    }
                });
            }
        };
        ComboboxController.prototype.setSelected = function (value) {
            this.$scope.selected = value;
        };
        ComboboxController.prototype.hideOptions = function (event) {
            var _this = this;
            var isChild = this.$element.has(event.target).length > 0;
            var isSelf = this.$element[0] === event.target;
            var isInside = isChild || isSelf;
            if (!isInside) {
                this.$scope.$apply(function () {
                    _this.$scope.showOptions = false;
                });
            }
        };
        ComboboxController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$document
        ];
        return ComboboxController;
    }());
    Peach.ComboboxController = ComboboxController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.DefinesDirective = {
        ComponentID: Peach.C.Directives.Defines,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.Defines,
        controller: Peach.C.Controllers.Defines,
        scope: {
            form: "=",
            group: "="
        }
    };
    var DefinesController = (function () {
        function DefinesController($scope, pitService) {
            this.$scope = $scope;
            this.pitService = pitService;
            $scope.vm = this;
            if (!$scope.group.collapsed) {
                $scope.isOpen = true;
            }
        }
        Object.defineProperty(DefinesController.prototype, "ShowGroup", {
            get: function () {
                return !_.isEmpty(this.$scope.group.items);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(DefinesController.prototype, "CanRemove", {
            get: function () {
                return this.$scope.group.name === "User Defines";
            },
            enumerable: false,
            configurable: true
        });
        DefinesController.prototype.OnRemove = function (index) {
            this.$scope.group.items.splice(index, 1);
            this.$scope.form.$setDirty();
        };
        DefinesController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return DefinesController;
    }());
    Peach.DefinesController = DefinesController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.FaultAssetsDirective = {
        ComponentID: Peach.C.Directives.FaultAssets,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.FaultAssets,
        controller: Peach.C.Controllers.FaultAssets,
        scope: {
            assets: "="
        }
    };
    var FaultAssetsController = (function () {
        function FaultAssetsController($scope) {
            this.$scope = $scope;
            $scope.vm = this;
        }
        FaultAssetsController.$inject = [
            Peach.C.Angular.$scope
        ];
        return FaultAssetsController;
    }());
    Peach.FaultAssetsController = FaultAssetsController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.FaultFilesDirective = {
        ComponentID: Peach.C.Directives.FaultFiles,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.FaultFiles,
        controller: Peach.C.Controllers.FaultFiles,
        scope: {
            files: "="
        }
    };
    var FaultFilesController = (function () {
        function FaultFilesController($scope) {
            this.$scope = $scope;
            $scope.vm = this;
        }
        FaultFilesController.prototype.Name = function (i, file) {
            if (file.type === Peach.FaultFileType.Asset) {
                return file.name;
            }
            var dir = (file.type === Peach.FaultFileType.Input) ? 'RX' : 'TX';
            return "#" + (i + 1) + " - " + dir + " - " + file.name;
        };
        FaultFilesController.$inject = [
            Peach.C.Angular.$scope
        ];
        return FaultFilesController;
    }());
    Peach.FaultFilesController = FaultFilesController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.FaultsDirective = {
        ComponentID: Peach.C.Directives.Faults,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Faults,
        controller: Peach.C.Controllers.Faults,
        scope: {
            limit: '='
        }
    };
    var FaultsDirectiveController = (function () {
        function FaultsDirectiveController($scope, $state, jobService) {
            var _this = this;
            this.$scope = $scope;
            this.$state = $state;
            this.jobService = jobService;
            this.Faults = [];
            this.AllFaults = [];
            $scope.vm = this;
            this.bucket = $state.params['bucket'] || 'all';
            $scope.$watch(function () { return jobService.Faults.length; }, function (newVal, oldVal) {
                if (newVal !== oldVal) {
                    _this.RefreshFaults();
                }
            });
            this.RefreshFaults();
        }
        Object.defineProperty(FaultsDirectiveController.prototype, "DefaultSort", {
            get: function () {
                return this.$scope.limit ? 'reverse' : 'forward';
            },
            enumerable: false,
            configurable: true
        });
        FaultsDirectiveController.prototype.OnFaultSelected = function (fault) {
            var params = {
                bucket: this.bucket,
                id: fault.iteration
            };
            this.$state.go(Peach.C.States.JobFaultsDetail, params);
        };
        FaultsDirectiveController.prototype.RefreshFaults = function () {
            var _this = this;
            var faults;
            if (this.bucket === 'all') {
                faults = this.jobService.Faults;
            }
            else {
                faults = _.filter(this.jobService.Faults, function (fault) {
                    return _this.bucket === (fault.majorHash + "_" + fault.minorHash);
                });
            }
            if (this.$scope.limit) {
                this.AllFaults = _.takeRight(faults, this.$scope.limit);
            }
            else {
                this.AllFaults = faults;
            }
        };
        FaultsDirectiveController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Services.Job
        ];
        return FaultsDirectiveController;
    }());
    Peach.FaultsDirectiveController = FaultsDirectiveController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.HeadersDirective = {
        ComponentID: Peach.C.Directives.Headers,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Headers,
        controller: Peach.C.Controllers.Headers,
        scope: {
            route: '=',
            form: '='
        }
    };
    var HeadersController = (function () {
        function HeadersController($scope, pitService) {
            this.$scope = $scope;
            this.pitService = pitService;
            this.MutateChoices = [
                'Include',
                'Exclude'
            ];
            $scope.vm = this;
        }
        HeadersController.prototype.CanMoveUp = function (index) {
            return index !== 0;
        };
        HeadersController.prototype.CanMoveDown = function (index) {
            return index !== (this.$scope.route.headers.length - 1);
        };
        HeadersController.prototype.OnMoveUp = function ($event, index) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemUp(this.$scope.route.headers, index);
            this.$scope.form.$setDirty();
        };
        HeadersController.prototype.OnMoveDown = function ($event, index) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemDown(this.$scope.route.headers, index);
            this.$scope.form.$setDirty();
        };
        HeadersController.prototype.OnRemove = function ($event, index) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$scope.route.headers.splice(index, 1);
            this.$scope.form.$setDirty();
        };
        HeadersController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return HeadersController;
    }());
    Peach.HeadersController = HeadersController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.JobsDirective = {
        ComponentID: Peach.C.Directives.Jobs,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Jobs,
        controller: Peach.C.Controllers.Jobs,
        scope: { limit: '=' }
    };
    var JobsDirectiveController = (function () {
        function JobsDirectiveController($scope, $state, $modal, $window, jobService) {
            this.$scope = $scope;
            this.$state = $state;
            this.$modal = $modal;
            this.$window = $window;
            this.jobService = jobService;
            this.Jobs = [];
            this.AllJobs = [];
            $scope.vm = this;
            this.refresh(this.jobService.GetJobs());
        }
        JobsDirectiveController.prototype.OnJobSelected = function (job) {
            this.$state.go(Peach.C.States.Job, { job: job.id });
        };
        JobsDirectiveController.prototype.IsReportDisabled = function (job) {
            return !_.isUndefined(this.pendingDelete) || !_.isString(job.reportUrl);
        };
        JobsDirectiveController.prototype.IsRemoveDisabled = function (job) {
            return !_.isUndefined(this.pendingDelete) || job.status !== Peach.JobStatus.Stopped;
        };
        JobsDirectiveController.prototype.IsActive = function (job) {
            return job.status !== Peach.JobStatus.Stopped;
        };
        JobsDirectiveController.prototype.OnRemove = function ($event, job) {
            var _this = this;
            $event.preventDefault();
            $event.stopPropagation();
            var options = {
                SubmitPrompt: 'Delete Job'
            };
            Peach.Confirm(this.$modal, options).result
                .then(function (result) {
                if (result === 'ok') {
                    _this.pendingDelete = job;
                    _this.refresh(_this.jobService.Delete(job));
                }
            });
        };
        JobsDirectiveController.prototype.OnViewReport = function ($event, job) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$window.open(job.reportUrl);
        };
        JobsDirectiveController.prototype.refresh = function (promise) {
            var _this = this;
            promise.then(function (jobs) {
                if (_this.$scope.limit) {
                    _this.AllJobs = _.takeRight(jobs, _this.$scope.limit);
                }
                else {
                    _this.AllJobs = jobs;
                }
                _this.pendingDelete = undefined;
            });
            promise.catch(function () {
                _this.pendingDelete = undefined;
            });
        };
        JobsDirectiveController.prototype.RemoveIconClass = function (job) {
            return job === this.pendingDelete ?
                'fa-spin fa-refresh' :
                'fa-remove';
        };
        JobsDirectiveController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$state,
            Peach.C.Angular.$uibModal,
            Peach.C.Angular.$window,
            Peach.C.Services.Job
        ];
        return JobsDirectiveController;
    }());
    Peach.JobsDirectiveController = JobsDirectiveController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.MonitorDirective = {
        ComponentID: Peach.C.Directives.Monitor,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Monitor,
        controller: Peach.C.Controllers.Monitor,
        scope: {
            monitors: '=',
            monitor: '=',
            agentIndex: '=',
            monitorIndex: '='
        }
    };
    var MonitorController = (function () {
        function MonitorController($scope, pitService) {
            this.$scope = $scope;
            this.pitService = pitService;
            $scope.vm = this;
            if ($scope.monitorIndex === ($scope.monitors.length - 1)) {
                $scope.isOpen = true;
            }
        }
        Object.defineProperty(MonitorController.prototype, "Header", {
            get: function () {
                var monitor = this.$scope.monitor.monitorClass;
                var name = this.$scope.monitor.name ? "(" + this.$scope.monitor.name + ")" : '';
                return monitor + " " + name;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MonitorController.prototype, "CanMoveUp", {
            get: function () {
                return this.$scope.monitorIndex !== 0;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(MonitorController.prototype, "CanMoveDown", {
            get: function () {
                return this.$scope.monitorIndex !== (this.$scope.monitors.length - 1);
            },
            enumerable: false,
            configurable: true
        });
        MonitorController.prototype.OnMoveUp = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemUp(this.$scope.monitors, this.$scope.monitorIndex);
            this.$scope.form.$setDirty();
        };
        MonitorController.prototype.OnMoveDown = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemDown(this.$scope.monitors, this.$scope.monitorIndex);
            this.$scope.form.$setDirty();
        };
        MonitorController.prototype.OnRemove = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$scope.monitors.splice(this.$scope.monitorIndex, 1);
            this.$scope.form.$setDirty();
        };
        MonitorController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return MonitorController;
    }());
    Peach.MonitorController = MonitorController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.ParameterDirective = {
        ComponentID: Peach.C.Directives.Parameter,
        restrict: "E",
        replace: true,
        templateUrl: Peach.C.Templates.Directives.Parameter,
        controller: Peach.C.Controllers.Parameter,
        scope: { param: "=" }
    };
    var ParameterController = (function () {
        function ParameterController($scope, pitService) {
            this.$scope = $scope;
            this.pitService = pitService;
            $scope.vm = this;
            if (!$scope.param.collapsed) {
                $scope.isOpen = true;
            }
        }
        ParameterController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return ParameterController;
    }());
    Peach.ParameterController = ParameterController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.ParameterInputDirective = {
        ComponentID: Peach.C.Directives.ParameterInput,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.ParameterInput,
        controller: Peach.C.Controllers.ParameterInput,
        scope: {
            param: "=",
            form: "="
        }
    };
    Peach.ParameterComboDirective = {
        ComponentID: Peach.C.Directives.ParameterCombo,
        restrict: 'E',
        controller: Peach.C.Controllers.ParameterInput,
        templateUrl: Peach.C.Templates.Directives.ParameterCombo,
        scope: {
            param: "=",
            form: "="
        }
    };
    Peach.ParameterSelectDirective = {
        ComponentID: Peach.C.Directives.ParameterSelect,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.ParameterSelect,
        controller: Peach.C.Controllers.ParameterInput,
        scope: {
            param: "=",
            form: "="
        }
    };
    Peach.ParameterStringDirective = {
        ComponentID: Peach.C.Directives.ParameterString,
        restrict: "E",
        templateUrl: Peach.C.Templates.Directives.ParameterString,
        controller: Peach.C.Controllers.ParameterInput,
        scope: {
            param: "=",
            form: "="
        }
    };
    var ParameterInputController = (function () {
        function ParameterInputController($scope, pitService) {
            var _this = this;
            this.$scope = $scope;
            this.pitService = pitService;
            $scope.vm = this;
            $scope.NewChoice = function (item) { return _this.NewChoice(item); };
            this.LastValue = {
                key: this.$scope.param.value,
                text: this.$scope.param.value,
                group: "Last Value"
            };
            this.MakeChoices();
        }
        Object.defineProperty(ParameterInputController.prototype, "IsRequired", {
            get: function () {
                return _.isUndefined(this.$scope.param.optional) || !this.$scope.param.optional;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ParameterInputController.prototype, "IsReadonly", {
            get: function () {
                return this.$scope.param.type === Peach.ParameterType.System;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ParameterInputController.prototype, "ParamTooltip", {
            get: function () {
                return this.IsReadonly ? this.$scope.param.value : "";
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(ParameterInputController.prototype, "WidgetType", {
            get: function () {
                switch (this.$scope.param.type) {
                    case Peach.ParameterType.Enum:
                    case Peach.ParameterType.Bool:
                    case Peach.ParameterType.Call:
                        return "select";
                    case Peach.ParameterType.Hwaddr:
                    case Peach.ParameterType.Iface:
                    case Peach.ParameterType.Ipv4:
                    case Peach.ParameterType.Ipv6:
                        return "combo";
                    case Peach.ParameterType.Space:
                        return "space";
                    default:
                        return "string";
                }
            },
            enumerable: false,
            configurable: true
        });
        ParameterInputController.prototype.MakeChoices = function () {
            var _this = this;
            var tuples = [];
            var options = this.$scope.param.options || [];
            var group;
            if (this.$scope.param.type === Peach.ParameterType.Call) {
                group = "Calls";
            }
            else {
                group = "Choices";
            }
            options.forEach(function (item) {
                var option = {
                    key: item,
                    text: item || "<i>Undefined</i>",
                    group: group
                };
                if (item === _this.$scope.param.defaultValue) {
                    option.group = "Default";
                    tuples.unshift(option);
                }
                else {
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
        };
        ParameterInputController.prototype.Defines = function () {
            var available = this.pitService.CreateFlatDefinesView(this.pitService.Pit.definesView);
            return _.chain(available)
                .map(function (param) {
                var key = "##" + param.key + "##";
                return {
                    key: key,
                    text: key,
                    description: param.description,
                    group: "Defines"
                };
            })
                .orderBy(function (x) { return x.key; })
                .value();
        };
        ParameterInputController.prototype.NewChoice = function (item) {
            this.NewValue = {
                key: item,
                text: item,
                group: "New Value"
            };
            this.MakeChoices();
            return this.NewValue;
        };
        ParameterInputController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Pit
        ];
        return ParameterInputController;
    }());
    Peach.ParameterInputController = ParameterInputController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.RouteDirective = {
        ComponentID: Peach.C.Directives.Route,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Route,
        controller: Peach.C.Controllers.Route,
        scope: {
            routes: '=',
            route: '=',
            index: '='
        }
    };
    var RouteController = (function () {
        function RouteController($scope, $localStorage, pitService) {
            this.$scope = $scope;
            this.pitService = pitService;
            this.MutateChoices = [
                'Include',
                'Exclude'
            ];
            $scope.vm = this;
            $scope.isOpen = true;
            $scope.storage = $localStorage['$default']({
                showHelp: true
            });
        }
        RouteController.prototype.OnHelp = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$scope.storage.showHelp = !this.$scope.storage.showHelp;
        };
        Object.defineProperty(RouteController.prototype, "HelpClass", {
            get: function () {
                return { active: this.$scope.storage.showHelp };
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(RouteController.prototype, "HelpPrompt", {
            get: function () {
                return this.$scope.storage.showHelp ? 'Hide' : 'Help';
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(RouteController.prototype, "Header", {
            get: function () {
                return this.$scope.route.url === '*' ? 'Default (*)' : this.$scope.route.url;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(RouteController.prototype, "CanMoveUp", {
            get: function () {
                return this.$scope.index !== 0;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(RouteController.prototype, "CanMoveDown", {
            get: function () {
                return this.$scope.index !== (this.$scope.routes.length - 1);
            },
            enumerable: false,
            configurable: true
        });
        RouteController.prototype.OnMoveUp = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemUp(this.$scope.routes, this.$scope.index);
            this.$scope.form.$setDirty();
        };
        RouteController.prototype.OnMoveDown = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            Peach.ArrayItemDown(this.$scope.routes, this.$scope.index);
            this.$scope.form.$setDirty();
        };
        RouteController.prototype.OnRemove = function ($event) {
            $event.preventDefault();
            $event.stopPropagation();
            this.$scope.routes.splice(this.$scope.index, 1);
            this.$scope.form.$setDirty();
        };
        RouteController.prototype.OnAddHeader = function () {
            this.$scope.route.headers.push({
                name: "",
                mutate: false,
                mutateChoice: 'Exclude'
            });
            this.$scope.form.$setDirty();
        };
        RouteController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$localStorage,
            Peach.C.Services.Pit
        ];
        return RouteController;
    }());
    Peach.RouteController = RouteController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.SmartTableRatioDirective = {
        ComponentID: Peach.C.Directives.Ratio,
        restrict: 'A',
        scope: {
            stRatio: '='
        },
        link: function (scope, element) {
            element.css('width', scope.stRatio + "%");
        }
    };
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.TestDirective = {
        ComponentID: Peach.C.Directives.Test,
        restrict: 'E',
        templateUrl: Peach.C.Templates.Directives.Test,
        controller: Peach.C.Controllers.Test,
        controllerAs: 'vm',
        scope: {}
    };
    var TestController = (function () {
        function TestController($scope, testService) {
            this.$scope = $scope;
            this.testService = testService;
        }
        Object.defineProperty(TestController.prototype, "IsAvailable", {
            get: function () {
                return this.testService.IsAvailable;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "TestEvents", {
            get: function () {
                return this.testService.TestResult.events;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "TestStatus", {
            get: function () {
                return this.testService.TestResult.status;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "TestLog", {
            get: function () {
                return this.testService.TestResult.log;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "TestTime", {
            get: function () {
                return this.testService.TestTime;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "ShowTestPending", {
            get: function () {
                return this.testService.IsPending;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "ShowTestPass", {
            get: function () {
                return this.testService.TestResult.status === Peach.TestStatus.Pass;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestController.prototype, "ShowTestFail", {
            get: function () {
                return this.testService.TestResult.status === Peach.TestStatus.Fail;
            },
            enumerable: false,
            configurable: true
        });
        TestController.prototype.StatusClass = function (row) {
            return _.isNull(row)
                ? this.statusClassFor(this.TestStatus)
                : this.statusClassFor(row.status);
        };
        TestController.prototype.statusClassFor = function (status) {
            return {
                'fa fa-spinner fa-pulse': _.isEmpty(status) || status === Peach.TestStatus.Active,
                'fa fa-check green': status === Peach.TestStatus.Pass,
                'fa fa-ban red': status === Peach.TestStatus.Fail
            };
        };
        TestController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Services.Test
        ];
        return TestController;
    }());
    Peach.TestController = TestController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.UniqueDirective = {
        ComponentID: Peach.C.Directives.Unique,
        restrict: 'A',
        require: Peach.C.Angular.ngModel,
        scope: {
            unique: "&" + Peach.C.Directives.Unique,
            watch: '@peachUniqueWatch',
            defaultValue: '@peachUniqueDefault'
        },
        link: function (scope, element, attrs, ctrl) {
            var validate = function (modelValue, viewValue) {
                var collection = scope.unique();
                return !_.includes(collection, viewValue || scope.defaultValue);
            };
            ctrl.$validators['unique'] = validate;
            if (scope.watch) {
                scope.$watch(scope.watch, function (newVal, oldVal) {
                    if (newVal !== oldVal) {
                        ctrl.$setValidity('unique', validate(ctrl.$modelValue, ctrl.$viewValue));
                    }
                });
            }
        }
    };
    Peach.UniqueChannelDirective = {
        ComponentID: Peach.C.Directives.UniqueChannel,
        restrict: 'A',
        require: Peach.C.Angular.ngModel,
        controller: Peach.C.Controllers.UniqueChannel,
        controllerAs: 'ctrl',
        scope: {
            channel: "@" + Peach.C.Directives.UniqueChannel,
            defaultValue: '@peachUniqueDefault',
            ignore: '@peachUniqueIgnore'
        },
        link: function (scope, element, attrs, ctrl) {
            scope.ctrl.Link(element, attrs, ctrl);
        }
    };
    var UniqueChannelController = (function () {
        function UniqueChannelController($scope, service) {
            this.$scope = $scope;
            this.service = service;
        }
        UniqueChannelController.prototype.Link = function (element, attrs, ctrl) {
            var _this = this;
            this.$scope.ngModel = ctrl;
            this.service.Register(this.$scope);
            var validate = function (value) {
                _this.service.IsUnique(_this.$scope, value);
                return value;
            };
            ctrl.$formatters.unshift(validate);
            ctrl.$viewChangeListeners.unshift(function () { return validate(ctrl.$viewValue); });
            element.on('$destroy', function () {
                _this.service.Unregister(_this.$scope);
            });
        };
        UniqueChannelController.$inject = [Peach.C.Angular.$scope, Peach.C.Services.Unique];
        return UniqueChannelController;
    }());
    Peach.UniqueChannelController = UniqueChannelController;
    var UniqueChannel = (function () {
        function UniqueChannel() {
        }
        return UniqueChannel;
    }());
    var UniqueChannels = (function () {
        function UniqueChannels() {
        }
        return UniqueChannels;
    }());
    var UniqueService = (function () {
        function UniqueService() {
            this.channels = {};
        }
        UniqueService.prototype.IsUnique = function (scope, value) {
            var _this = this;
            var isUnique;
            var channel = this.getChannel(scope.channel);
            _.forEach(channel, function (item, id) {
                var isDuplicate = _this.isDuplicate(item, item.ngModel.$modelValue);
                item.ngModel.$setValidity('unique', !isDuplicate);
                if (id === scope.$id.toString()) {
                    isUnique = !isDuplicate;
                }
            });
            return isUnique;
        };
        UniqueService.prototype.Register = function (scope) {
            var channel = this.getChannel(scope.channel);
            channel[scope.$id.toString()] = scope;
        };
        UniqueService.prototype.Unregister = function (scope) {
            var channel = this.getChannel(scope.channel);
            delete channel[scope.$id.toString()];
            if (_.isEmpty(channel)) {
                delete this.channels[scope.channel];
            }
            else {
                this.IsUnique(scope, null);
            }
        };
        UniqueService.prototype.isDuplicate = function (scope, value) {
            var myValue = (value || scope.defaultValue);
            if (scope.ignore) {
                var reIgnore = new RegExp(scope.ignore);
                if (reIgnore.test(myValue)) {
                    return false;
                }
            }
            var channel = this.getChannel(scope.channel);
            return _.some(channel, function (other, id) {
                if (scope.$id.toString() === id) {
                    return false;
                }
                var otherValue = other.ngModel.$modelValue;
                return (myValue === (otherValue || other.defaultValue));
            });
        };
        UniqueService.prototype.getChannel = function (name) {
            var channel = this.channels[name];
            if (_.isUndefined(channel)) {
                channel = {};
                this.channels[name] = channel;
            }
            return channel;
        };
        return UniqueService;
    }());
    Peach.UniqueService = UniqueService;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.UnsavedDirective = {
        ComponentID: Peach.C.Directives.Unsaved,
        restrict: 'A',
        require: '^form',
        controller: Peach.C.Controllers.Unsaved,
        controllerAs: 'ctrl',
        scope: {},
        link: function (scope, element, attrs, form) {
            scope.ctrl.Link(form);
        }
    };
    var UnsavedController = (function () {
        function UnsavedController($scope, $modal, $state) {
            this.$scope = $scope;
            this.$modal = $modal;
            this.$state = $state;
        }
        UnsavedController.prototype.Link = function (form) {
            var _this = this;
            var onRouteChangeOff = this.$scope.$root.$on(Peach.C.Angular.$stateChangeStart, function (event, toState, toParams, fromState, fromParams) {
                if (!form.$dirty) {
                    onRouteChangeOff();
                    return;
                }
                event.preventDefault();
                var options = {
                    Title: 'Unsaved Changes',
                    Body: 'You have unsaved changes. Do you want to leave the page?',
                    SubmitPrompt: 'Ignore Changes'
                };
                Peach.Confirm(_this.$modal, options).result
                    .then(function (result) {
                    if (result === 'ok') {
                        onRouteChangeOff();
                        _this.$state.transitionTo(toState.name, toParams);
                    }
                });
            });
        };
        UnsavedController.$inject = [
            Peach.C.Angular.$scope,
            Peach.C.Angular.$uibModal,
            Peach.C.Angular.$state
        ];
        return UnsavedController;
    }());
    Peach.UnsavedController = UnsavedController;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    function predicateValidation(name, ctrl, predicate) {
        ctrl.$validators[name] = function (modelValue, viewValue) {
            var value = modelValue || viewValue;
            return _.isUndefined(value)
                || (_.isString(value) && _.isEmpty(value))
                || predicate(value);
        };
    }
    Peach.RangeDirective = {
        ComponentID: Peach.C.Directives.Range,
        restrict: 'A',
        require: Peach.C.Angular.ngModel,
        scope: {
            min: '&peachRangeMin',
            max: '&peachRangeMax'
        },
        link: function (scope, element, attrs, ctrl) {
            predicateValidation(Peach.C.Validation.RangeMin, ctrl, function (value) {
                var int = parseInt(value);
                var min = scope.min();
                return _.isUndefined(min) || (!_.isNaN(int) && int >= min);
            });
            predicateValidation(Peach.C.Validation.RangeMax, ctrl, function (value) {
                var int = parseInt(value);
                var max = scope.max();
                return _.isUndefined(max) || (!_.isNaN(int) && int <= max);
            });
        }
    };
    Peach.IntegerDirective = {
        ComponentID: Peach.C.Directives.Integer,
        restrict: 'A',
        require: Peach.C.Angular.ngModel,
        link: function (scope, element, attrs, ctrl) {
            var pattern = /^(\-|\+)?\d+$/;
            predicateValidation(Peach.C.Validation.Integer, ctrl, function (value) { return pattern.test(value); });
        }
    };
    Peach.HexDirective = {
        ComponentID: Peach.C.Directives.HexString,
        restrict: 'A',
        require: Peach.C.Angular.ngModel,
        link: function (scope, element, attrs, ctrl) {
            var pattern = /^[0-9A-Fa-f]+$/;
            predicateValidation(Peach.C.Validation.HexString, ctrl, function (value) { return pattern.test(value); });
        }
    };
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.FaultFileType = {
        Asset: '',
        Output: '',
        Input: ''
    };
    Peach.MakeLowerEnum(Peach.FaultFileType);
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.JobStatus = {
        StartPending: '',
        PausePending: '',
        ContinuePending: '',
        StopPending: '',
        KillPending: '',
        Stopped: '',
        Starting: '',
        Running: '',
        Paused: '',
        Stopping: ''
    };
    Peach.MakeLowerEnum(Peach.JobStatus);
    Peach.JobMode = {
        Preparing: '',
        Fuzzing: '',
        Searching: '',
        Reproducing: '',
        Reporting: ''
    };
    Peach.MakeLowerEnum(Peach.JobMode);
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.LicenseStatus = {
        Missing: '',
        Expired: '',
        Invalid: '',
        Valid: ''
    };
    Peach.MakeLowerEnum(Peach.LicenseStatus);
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.ParameterType = {
        String: '',
        Hex: '',
        Range: '',
        Ipv4: '',
        Ipv6: '',
        Hwaddr: '',
        Iface: '',
        Enum: '',
        Bool: '',
        User: '',
        System: '',
        Call: '',
        Group: '',
        Space: '',
        Monitor: ''
    };
    Peach.MakeLowerEnum(Peach.ParameterType);
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.TestStatus = {
        Active: '',
        Pass: '',
        Fail: ''
    };
    Peach.MakeLowerEnum(Peach.TestStatus);
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var EulaService = (function () {
        function EulaService($q, $http, $modal, $state) {
            this.$q = $q;
            this.$http = $http;
            this.$modal = $modal;
            this.$state = $state;
        }
        EulaService.prototype.Verify = function () {
            var _this = this;
            return this.LoadLicense().then(function (license) {
                return _this.VerifyLicense(license);
            });
        };
        EulaService.prototype.LoadLicense = function () {
            var _this = this;
            var promise = this.$http.get(Peach.C.Api.License);
            promise.catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        EulaService.prototype.LicenseStatusTitle = function (license) {
            switch (license.status) {
                case Peach.LicenseStatus.Missing:
                    return 'Missing License Detected';
                case Peach.LicenseStatus.Expired:
                    return 'Expired License Detected';
                case Peach.LicenseStatus.Invalid:
                    return 'Invalid License Detected';
            }
        };
        EulaService.prototype.VerifyLicense = function (license) {
            var _this = this;
            if (license.status != Peach.LicenseStatus.Valid) {
                return this.LicenseError({
                    Title: this.LicenseStatusTitle(license),
                    Body: license.errorText.split('\n')
                }).then(function () {
                    return _this.Verify();
                });
            }
            if (license.eulaAccepted) {
                var ret = this.$q.defer();
                ret.resolve(license);
                return ret.promise;
            }
            var promise = this.DisplayEula(license.eula);
            return promise.then(function () {
                return _this.AcceptEula();
            });
        };
        EulaService.prototype.DisplayEula = function (type) {
            return this.$modal.open({
                templateUrl: "html/eula/" + type + ".html",
                controller: Peach.EulaController,
                controllerAs: Peach.C.ViewModel,
                backdrop: 'static',
                keyboard: false,
                size: 'lg'
            }).result;
        };
        EulaService.prototype.AcceptEula = function () {
            var _this = this;
            var promise = this.$http.post(Peach.C.Api.License, {});
            promise.then(function () {
                _this.$state.reload();
            });
            promise.catch(function (reason) {
                if (reason.status >= 500) {
                    _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
                }
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        EulaService.prototype.LicenseError = function (options) {
            return this.$modal.open({
                templateUrl: Peach.C.Templates.Modal.License,
                controller: Peach.LicenseController,
                controllerAs: Peach.C.ViewModel,
                backdrop: 'static',
                keyboard: false,
                resolve: { Options: function () { return options; } }
            }).result;
        };
        EulaService.$inject = [
            Peach.C.Angular.$q,
            Peach.C.Angular.$http,
            Peach.C.Angular.$uibModal,
            Peach.C.Angular.$state
        ];
        return EulaService;
    }());
    Peach.EulaService = EulaService;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.JOB_INTERVAL = 3000;
    var JobService = (function () {
        function JobService($rootScope, $q, $http, $modal, $state, $timeout) {
            this.$rootScope = $rootScope;
            this.$q = $q;
            this.$http = $http;
            this.$modal = $modal;
            this.$state = $state;
            this.$timeout = $timeout;
            this.jobs = [];
            this.faults = [];
        }
        JobService.prototype.OnEnter = function (id) {
            this.isActive = true;
            this.onPoll(Peach.C.Api.JobUrl.replace(':id', id));
        };
        JobService.prototype.OnExit = function () {
            this.isActive = false;
            if (this.poller) {
                this.$timeout.cancel(this.poller);
                this.poller = undefined;
            }
            this.job = undefined;
            this.$rootScope['job'] = undefined;
            this.faults = [];
        };
        Object.defineProperty(JobService.prototype, "Jobs", {
            get: function () {
                return this.jobs;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "Job", {
            get: function () {
                return this.job;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "Faults", {
            get: function () {
                return this.faults;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "IsRunning", {
            get: function () {
                return this.Job && this.Job.status === Peach.JobStatus.Running;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "IsPaused", {
            get: function () {
                return this.Job && this.Job.status === Peach.JobStatus.Paused;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "CanContinue", {
            get: function () {
                return this.isControlable && this.Job.status === Peach.JobStatus.Paused;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "CanPause", {
            get: function () {
                return this.isControlable && this.Job.status === Peach.JobStatus.Running;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "CanStop", {
            get: function () {
                return this.isControlable && (this.Job.status === Peach.JobStatus.Starting ||
                    this.Job.status === Peach.JobStatus.Running ||
                    this.Job.status === Peach.JobStatus.Paused ||
                    this.Job.status === Peach.JobStatus.Stopping);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "CanKill", {
            get: function () {
                return this.isControlable && this.Job.status === Peach.JobStatus.Stopping;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "isControlable", {
            get: function () {
                return this.Job && !_.isUndefined(this.Job.commands);
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(JobService.prototype, "RunningTime", {
            get: function () {
                if (_.isUndefined(this.Job)) {
                    return undefined;
                }
                var duration = moment.duration(this.job.runtime, 'seconds');
                var days = Math.floor(duration.asDays());
                var hours = duration.hours().toString().paddingLeft('00');
                var minutes = duration.minutes().toString().paddingLeft('00');
                var seconds = duration.seconds().toString().paddingLeft('00');
                if (duration.asDays() >= 1) {
                    return days + "d " + hours + "h " + minutes + "m";
                }
                else {
                    return hours + "h " + minutes + "m " + seconds + "s";
                }
            },
            enumerable: false,
            configurable: true
        });
        JobService.prototype.doLoadFaultDetail = function (defer, id) {
            var fault = _.find(this.faults, { iteration: id });
            if (_.isUndefined(fault)) {
                defer.reject();
            }
            else {
                this.$http.get(fault.faultUrl)
                    .success(function (data) { defer.resolve(data); })
                    .error(function (reason) { defer.reject(reason); });
            }
        };
        JobService.prototype.LoadFaultDetail = function (id) {
            var _this = this;
            var defer = this.$q.defer();
            if (this.pending) {
                this.pending.finally(function () { _this.doLoadFaultDetail(defer, id); });
            }
            else {
                this.doLoadFaultDetail(defer, id);
            }
            return defer.promise;
        };
        JobService.prototype.GetJobs = function () {
            var _this = this;
            var params = { dryrun: false };
            var promise = this.$http.get(Peach.C.Api.Jobs, { params: params });
            promise.success(function (jobs) { return _this.jobs = jobs; });
            promise.catch(function (reason) {
                if (reason.status !== 401 && reason.status !== 402) {
                    _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
                }
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        JobService.prototype.Start = function (job) {
            var _this = this;
            var promise = this.$http.post(Peach.C.Api.Jobs, job);
            promise.catch(function (reason) {
                var options = {
                    Title: 'Error Starting Job',
                    Body: 'Peach was unable to start a new job.',
                };
                if (reason.status === 403) {
                    options.Body += "\n\nPlease ensure another job is not running and try again.";
                }
                else if (reason.status === 404) {
                    options.Body += '\n\nPlease ensure the specified pit exists and try again.';
                }
                else {
                    console.log('JobService.StartJob().error>', reason);
                    return;
                }
                Peach.Alert(_this.$modal, options);
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        JobService.prototype.Delete = function (job) {
            var _this = this;
            return this.$http.delete(job.jobUrl)
                .then(function () { return _this.GetJobs(); })
                .catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
        };
        JobService.prototype.Continue = function () {
            this.sendCommand(this.CanContinue, Peach.JobStatus.ContinuePending, this.job.commands.continueUrl);
        };
        JobService.prototype.Pause = function () {
            this.sendCommand(this.CanPause, Peach.JobStatus.PausePending, this.job.commands.pauseUrl);
        };
        JobService.prototype.Stop = function () {
            this.sendCommand(this.CanStop, Peach.JobStatus.StopPending, this.job.commands.stopUrl);
        };
        JobService.prototype.Kill = function () {
            this.sendCommand(this.CanStop, Peach.JobStatus.KillPending, this.job.commands.killUrl);
        };
        JobService.prototype.sendCommand = function (check, status, url) {
            var _this = this;
            if (check) {
                this.job.status = status;
                var promise = this.$http.get(url);
                promise.success(function () { return _this.onPoll(_this.job.jobUrl); });
                promise.catch(function (reason) {
                    _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
                });
            }
        };
        JobService.prototype.onPoll = function (url) {
            var _this = this;
            this.pending = this.$http.get(url)
                .then(function (response) {
                if (!_this.isActive)
                    return undefined;
                var stopPending = (_this.job && _this.job.status === Peach.JobStatus.StopPending);
                var killPending = (_this.job && _this.job.status === Peach.JobStatus.KillPending);
                var job = response.data;
                _this.job = job;
                if (_this.job.status !== Peach.JobStatus.Stopped) {
                    if (stopPending && _this.job.status !== Peach.JobStatus.Stopping) {
                        _this.job.status = Peach.JobStatus.StopPending;
                    }
                    else if (killPending) {
                        _this.job.status = Peach.JobStatus.KillPending;
                    }
                }
                _this.$rootScope['job'] = _this.job;
                if (job.status !== Peach.JobStatus.Stopped) {
                    _this.poller = _this.$timeout(function () { _this.onPoll(url); }, Peach.JOB_INTERVAL);
                }
                if (_this.faults.length !== job.faultCount) {
                    var deferred = _this.$q.defer();
                    _this.reloadFaults()
                        .success(function () { deferred.resolve(_this.job); })
                        .error(function (reason) { deferred.reject(reason); })
                        .finally(function () { _this.pending = undefined; });
                    return deferred.promise;
                }
                return undefined;
            }, function (response) {
                if (!_this.isActive)
                    return undefined;
                _this.$state.go(Peach.C.States.MainError, { message: response.data.errorMessage });
            });
        };
        JobService.prototype.reloadFaults = function () {
            var _this = this;
            var promise = this.$http.get(this.job.faultsUrl);
            promise.success(function (faults) {
                _this.faults = faults;
            });
            promise.catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
            return promise;
        };
        JobService.prototype.LoadMetric = function (metric) {
            return this.$http.get(this.Job.metrics[metric]);
        };
        JobService.$inject = [
            Peach.C.Angular.$rootScope,
            Peach.C.Angular.$q,
            Peach.C.Angular.$http,
            Peach.C.Angular.$uibModal,
            Peach.C.Angular.$state,
            Peach.C.Angular.$timeout,
            Peach.C.Services.Pit
        ];
        return JobService;
    }());
    Peach.JobService = JobService;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    var PitService = (function () {
        function PitService($rootScope, $q, $http, $state) {
            this.$rootScope = $rootScope;
            this.$q = $q;
            this.$http = $http;
            this.$state = $state;
        }
        Object.defineProperty(PitService.prototype, "CurrentPitId", {
            get: function () {
                return this.$state.params['pit'];
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitService.prototype, "Pit", {
            get: function () {
                return this.pit;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitService.prototype, "IsWebProxy", {
            get: function () {
                return !_.isUndefined(this.pit) && !_.isUndefined(this.pit.webProxy);
            },
            enumerable: false,
            configurable: true
        });
        PitService.prototype.LoadLibrary = function () {
            var _this = this;
            var promise = this.$http.get(Peach.C.Api.Libraries);
            promise.catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        PitService.prototype.LoadPit = function () {
            var _this = this;
            var url = Peach.C.Api.PitUrl.replace(':id', this.CurrentPitId);
            var promise = this.$http.get(url);
            promise.success(function (pit) { return _this.OnSuccess(pit, false); });
            promise.catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        PitService.prototype.SavePit = function () {
            var _this = this;
            var config = [];
            var view = this.CreateFlatDefinesView(this.pit.config);
            for (var _i = 0, view_1 = view; _i < view_1.length; _i++) {
                var param = view_1[_i];
                if (param.type === Peach.ParameterType.User) {
                    config.push({
                        name: param.name,
                        description: param.description,
                        key: param.key,
                        value: param.value
                    });
                }
                else if (param.type !== Peach.ParameterType.System) {
                    config.push({
                        key: param.key,
                        value: param.value
                    });
                }
            }
            var agents = [];
            for (var _a = 0, _b = this.pit.agents; _a < _b.length; _a++) {
                var agent = _b[_a];
                var monitors = [];
                for (var _c = 0, _d = agent.monitors; _c < _d.length; _c++) {
                    var monitor = _d[_c];
                    var map = [];
                    this.Visit(monitor.view, function (param) {
                        if (!_.isUndefined(param.value)) {
                            map.push({
                                key: param.key,
                                value: param.value
                            });
                        }
                    });
                    monitors.push({
                        monitorClass: monitor.monitorClass,
                        name: monitor.name,
                        map: map
                    });
                }
                agents.push({
                    name: agent.name,
                    agentUrl: agent.agentUrl,
                    monitors: monitors
                });
            }
            var dto = {
                id: this.pit.id,
                pitUrl: this.pit.pitUrl,
                name: this.pit.name,
                description: this.pit.description,
                config: config,
                agents: agents,
                weights: this.pit.weights,
                webProxy: this.pit.webProxy
            };
            var promise = this.$http.post(this.pit.pitUrl, dto);
            promise.success(function (pit) { return _this.OnSuccess(pit, true); });
            promise.catch(function (reason) {
                _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
            });
            return Peach.StripHttpPromise(this.$q, promise);
        };
        PitService.prototype.SaveDefines = function (config) {
            this.pit.config = config;
            return this.SavePit();
        };
        PitService.prototype.SaveAgents = function (agents) {
            this.pit.agents = agents;
            return this.SavePit();
        };
        PitService.prototype.SaveWeights = function (weights) {
            this.pit.weights = weights;
            return this.SavePit();
        };
        PitService.prototype.SaveWebProxy = function (webProxy) {
            this.pit.webProxy = webProxy;
            return this.SavePit();
        };
        PitService.prototype.NewConfig = function (pit) {
            var request = {
                pitUrl: pit.pitUrl,
                name: pit.name,
                description: pit.description
            };
            return this.DoNewPit(request);
        };
        PitService.prototype.EditConfig = function (pit) {
            var _this = this;
            return this.$http.get(pit.pitUrl).then(function (response) {
                var fullPit = response.data;
                if (pit.name === fullPit.name) {
                    fullPit.description = pit.description;
                    return _this.$http.post(pit.pitUrl, fullPit).then(function () { });
                }
                return _this.NewConfig(pit).then(function () {
                    return _this.DeletePit(fullPit).then(function () { });
                });
            });
        };
        PitService.prototype.MigratePit = function (legacyPit, originalPit) {
            var request = {
                legacyPitUrl: legacyPit.pitUrl,
                pitUrl: originalPit.pitUrl
            };
            return this.DoNewPit(request);
        };
        PitService.prototype.DeletePit = function (pit) {
            return this.$http.delete(pit.pitUrl);
        };
        PitService.prototype.DoNewPit = function (request) {
            var _this = this;
            var promise = this.$http.post(Peach.C.Api.Pits, request);
            promise.success(function (pit) { return _this.OnSuccess(pit, true); });
            promise.catch(function (reason) {
                if (reason.status >= 500) {
                    _this.$state.go(Peach.C.States.MainError, { message: reason.data.errorMessage });
                }
            });
            return promise;
        };
        Object.defineProperty(PitService.prototype, "IsConfigured", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pit, function () { return _.every(_this.CreateFlatDefinesView(_this.CreateDefinesView()), function (param) {
                    return param.optional || param.value !== "";
                }); });
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(PitService.prototype, "HasMonitors", {
            get: function () {
                var _this = this;
                return Peach.onlyIf(this.pit, function () { return _.some(_this.pit.agents, function (agent) {
                    return agent.monitors.length > 0;
                }); });
            },
            enumerable: false,
            configurable: true
        });
        PitService.prototype.OnSuccess = function (pit, saved) {
            var oldPit = this.pit;
            this.pit = pit;
            this.$rootScope['pit'] = pit;
            if (saved || (oldPit && oldPit.id !== pit.id)) {
                this.$rootScope.$emit(Peach.C.Events.PitChanged, pit);
            }
            for (var _i = 0, _a = pit.agents; _i < _a.length; _i++) {
                var agent = _a[_i];
                for (var _b = 0, _c = agent.monitors; _b < _c.length; _b++) {
                    var monitor = _c[_b];
                    monitor.view = this.CreateMonitorView(monitor);
                }
            }
            if (pit.metadata) {
                pit.definesView = this.CreateDefinesView();
            }
        };
        PitService.prototype.CreateMonitor = function (param) {
            var monitor = {
                monitorClass: param.key,
                name: param.name,
                map: angular.copy(param.items),
                description: param.description
            };
            monitor.view = this.CreateMonitorView(monitor);
            return monitor;
        };
        PitService.prototype.CreateDefinesView = function () {
            var view = angular.copy(this.pit.metadata.defines);
            for (var _i = 0, view_2 = view; _i < view_2.length; _i++) {
                var group = view_2[_i];
                if (group.items) {
                    for (var _a = 0, _b = group.items; _a < _b.length; _a++) {
                        var param = _b[_a];
                        var config = _.find(this.pit.config, { key: param.key });
                        if (config && config.value) {
                            param.value = config.value;
                        }
                    }
                }
            }
            return view;
        };
        PitService.prototype.CreateFlatDefinesView = function (src) {
            var skip = [
                Peach.ParameterType.Group,
                Peach.ParameterType.Monitor,
                Peach.ParameterType.Space
            ];
            var view = [];
            this.Visit(src, function (param) {
                if (_.includes(skip, param.type)) {
                    return;
                }
                view.push(param);
            });
            return view;
        };
        PitService.prototype.CreateMonitorView = function (monitor) {
            var metadata = this.FindMonitorMetadata(monitor.monitorClass);
            if (!metadata) {
                var view_3 = [];
                for (var _i = 0, _a = monitor.map; _i < _a.length; _i++) {
                    var param = _a[_i];
                    view_3.push({
                        key: param.key,
                        name: param.key,
                        value: param.value
                    });
                }
                return view_3;
            }
            var view = angular.copy(metadata.items);
            this.Visit(view, function (param) {
                var kv = _.find(monitor.map, { key: param.key });
                if (kv && kv.value) {
                    param.value = kv.value;
                }
            });
            return view;
        };
        PitService.prototype.FindMonitorMetadata = function (key) {
            for (var _i = 0, _a = this.pit.metadata.monitors; _i < _a.length; _i++) {
                var monitor = _a[_i];
                var ret = this._FindByTypeKey(monitor, Peach.ParameterType.Monitor, key);
                if (ret) {
                    return ret;
                }
            }
            return null;
        };
        PitService.prototype._FindByTypeKey = function (param, type, key) {
            if (param.type === type) {
                if (param.key === key) {
                    return param;
                }
            }
            for (var _i = 0, _a = _.get(param, 'items', []); _i < _a.length; _i++) {
                var item = _a[_i];
                var ret = this._FindByTypeKey(item, type, key);
                if (ret) {
                    return ret;
                }
            }
            return null;
        };
        PitService.prototype.Visit = function (params, fn) {
            for (var _i = 0, params_1 = params; _i < params_1.length; _i++) {
                var param = params_1[_i];
                fn(param);
                this.Visit(param.items || [], fn);
            }
        };
        PitService.$inject = [
            Peach.C.Angular.$rootScope,
            Peach.C.Angular.$q,
            Peach.C.Angular.$http,
            Peach.C.Angular.$state
        ];
        return PitService;
    }());
    Peach.PitService = PitService;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    Peach.TEST_INTERVAL = 1000;
    var TestService = (function () {
        function TestService($rootScope, $q, $http, $timeout, pitService) {
            var _this = this;
            this.$rootScope = $rootScope;
            this.$q = $q;
            this.$http = $http;
            this.$timeout = $timeout;
            this.pitService = pitService;
            this.isPending = false;
            $rootScope.$on(Peach.C.Events.PitChanged, function () {
                _this.Reset();
            });
        }
        Object.defineProperty(TestService.prototype, "IsPending", {
            get: function () {
                return this.isPending;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestService.prototype, "TestResult", {
            get: function () {
                return this.testResult;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestService.prototype, "TestTime", {
            get: function () {
                return this.testTime;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestService.prototype, "CanBeginTest", {
            get: function () {
                return !this.isPending;
            },
            enumerable: false,
            configurable: true
        });
        Object.defineProperty(TestService.prototype, "IsAvailable", {
            get: function () {
                return !_.isEmpty(this.testTime);
            },
            enumerable: false,
            configurable: true
        });
        TestService.prototype.BeginTest = function () {
            var _this = this;
            this.Reset();
            this.pendingResult = this.$q.defer();
            this.isPending = true;
            this.testTime = moment().format("h:mm a");
            var request = {
                pitUrl: this.pitService.Pit.pitUrl,
                dryRun: true
            };
            this.$http.post(Peach.C.Api.Jobs, request)
                .success(function (job) {
                _this.OnPoll(job);
            })
                .catch(function (response) {
                if (response.status === 403) {
                    _this.SetFailure('Peach was unable to start the test. Please make sure another there are no other running tests or jobs and try again.');
                }
                else if (response.status === 404) {
                    _this.SetFailure('Peach was unable to start the test. Please make sure the pit exists and try again.');
                }
                else {
                    _this.SetFailure(response.data.errorMessage);
                }
                _this.pendingResult.reject();
            });
            return this.pendingResult.promise;
        };
        TestService.prototype.Reset = function () {
            this.testTime = "";
            this.testResult = {
                status: "",
                log: "",
                events: []
            };
        };
        TestService.prototype.OnPoll = function (job) {
            var _this = this;
            this.$http.get(job.firstNodeUrl)
                .success(function (data) {
                _this.testResult = data;
                if (data.status === Peach.TestStatus.Active) {
                    _this.$timeout(function () { _this.OnPoll(job); }, Peach.TEST_INTERVAL);
                }
                else {
                    if (data.status === Peach.TestStatus.Pass) {
                        _this.pendingResult.resolve();
                    }
                    else {
                        _this.pendingResult.reject();
                    }
                    _this.isPending = false;
                    _this.$http.delete(job.jobUrl);
                }
            })
                .catch(function (response) {
                _this.SetFailure(response.data.errorMessage);
                _this.pendingResult.reject();
            });
        };
        TestService.prototype.SetFailure = function (reason) {
            this.isPending = false;
            this.testResult.status = Peach.TestStatus.Fail;
            this.testResult.events.push({
                id: this.testResult.events.length + 1,
                status: Peach.TestStatus.Fail,
                description: 'Test execution failure.',
                resolve: reason
            });
        };
        TestService.$inject = [
            Peach.C.Angular.$rootScope,
            Peach.C.Angular.$q,
            Peach.C.Angular.$http,
            Peach.C.Angular.$timeout,
            Peach.C.Services.Pit
        ];
        return TestService;
    }());
    Peach.TestService = TestService;
})(Peach || (Peach = {}));
var Peach;
(function (Peach) {
    function getComponentName(name, component) {
        var id = component.ComponentID;
        return _.isUndefined(id) ? name : id;
    }
    function registerModule(ns, app) {
        _.forOwn(ns, function (component, key) {
            if (typeof (key) !== 'string') {
                return;
            }
            var name = getComponentName(key, component);
            if (key.endsWith('Controller')) {
                app.controller(name, component);
            }
            if (key.endsWith('Directive')) {
                app.directive(name, function () {
                    return component;
                });
            }
            if (key.endsWith('Service')) {
                app.service(name, component);
            }
        });
    }
    var p = angular.module("Peach", [
        "angular-loading-bar",
        "chart.js",
        "ncy-angular-breadcrumb",
        "ngMessages",
        "ngSanitize",
        "ngStorage",
        "ngVis",
        "smart-table",
        "ui.bootstrap",
        "ui.router",
        "ui.select"
    ]);
    registerModule(Peach, p);
    p.config(function ($provide) {
        $provide.decorator('$uiViewScroll', function () {
            return function () {
                window.scrollTo(0, 0);
            };
        });
    });
    p.config([
        Peach.C.Angular.$breadcrumbProvider,
        function ($breadcrumbProvider) {
            $breadcrumbProvider.setOptions({
                prefixStateName: Peach.C.States.MainHome,
                includeAbstract: true
            });
        }
    ]);
    p.config([
        Peach.C.Angular.$stateProvider,
        Peach.C.Angular.$urlRouterProvider, function ($stateProvider, $urlRouterProvider) {
            $urlRouterProvider.when('', '/');
            $urlRouterProvider.otherwise('/error');
            $stateProvider
                .state(Peach.C.States.Main, {
                abstract: true,
                template: Peach.C.Templates.UiView,
                ncyBreadcrumb: { skip: true }
            })
                .state(Peach.C.States.MainHome, {
                url: '/',
                templateUrl: Peach.C.Templates.Home,
                controller: Peach.HomeController,
                controllerAs: Peach.C.ViewModel,
                ncyBreadcrumb: { label: 'Home' }
            })
                .state(Peach.C.States.MainLibrary, {
                url: '/library',
                templateUrl: Peach.C.Templates.Library,
                controller: Peach.LibraryController,
                controllerAs: Peach.C.ViewModel,
                ncyBreadcrumb: { label: 'Library' }
            })
                .state(Peach.C.States.MainJobs, {
                url: '/jobs',
                templateUrl: Peach.C.Templates.Jobs,
                controller: Peach.JobsController,
                controllerAs: Peach.C.ViewModel,
                ncyBreadcrumb: { label: 'Jobs' }
            })
                .state(Peach.C.States.MainError, {
                url: '/error',
                templateUrl: Peach.C.Templates.Error,
                controller: Peach.ErrorController,
                controllerAs: Peach.C.ViewModel,
                params: { message: undefined },
                ncyBreadcrumb: { label: 'Oops!' }
            })
                .state(Peach.C.States.Job, {
                url: '/job/:job',
                templateUrl: Peach.C.Templates.Job.Dashboard,
                controller: Peach.DashboardController,
                controllerAs: Peach.C.ViewModel,
                ncyBreadcrumb: {
                    label: '{{job.name}}',
                    parent: Peach.C.States.MainJobs
                },
                onEnter: [
                    Peach.C.Services.Job,
                    Peach.C.Angular.$stateParams,
                    function (jobService, $stateParams) {
                        jobService.OnEnter($stateParams.job);
                    }
                ],
                onExit: [Peach.C.Services.Job, function (jobService) {
                        jobService.OnExit();
                    }]
            })
                .state(Peach.C.States.JobFaults, {
                url: '/faults/:bucket',
                params: { bucket: 'all' },
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Job.Faults.Summary,
                        controller: Peach.FaultsController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: '{{FaultSummaryTitle}}' }
            })
                .state(Peach.C.States.JobFaultsDetail, {
                url: '/{id:int}',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Job.Faults.Detail,
                        controller: Peach.FaultsDetailController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: '{{FaultDetailTitle}}' }
            })
                .state(Peach.C.States.JobMetrics, {
                url: '/metrics',
                abstract: true,
                ncyBreadcrumb: { label: 'Metrics' }
            })
                .state(Peach.C.States.Pit, {
                url: '/pit/:pit',
                templateUrl: Peach.C.Templates.Pit.Configure,
                controller: Peach.ConfigureController,
                controllerAs: Peach.C.ViewModel,
                params: {
                    seed: undefined,
                    rangeStart: undefined,
                    rangeStop: undefined
                },
                ncyBreadcrumb: {
                    label: '{{pit.name}}',
                    parent: Peach.C.States.MainLibrary
                }
            })
                .state(Peach.C.States.PitAdvanced, {
                abstract: true,
                url: '/advanced',
                ncyBreadcrumb: { label: 'Configure' }
            })
                .state(Peach.C.States.PitAdvancedVariables, {
                url: '/variables',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Pit.Advanced.Variables,
                        controller: Peach.ConfigureDefinesController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: 'Variables' }
            })
                .state(Peach.C.States.PitAdvancedMonitoring, {
                url: '/monitoring',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Pit.Advanced.Monitoring,
                        controller: Peach.ConfigureMonitorsController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: 'Monitoring' }
            })
                .state(Peach.C.States.PitAdvancedTuning, {
                url: '/tuning',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Pit.Advanced.Tuning,
                        controller: Peach.ConfigureTuningController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: 'Tuning' }
            })
                .state(Peach.C.States.PitAdvancedWebProxy, {
                url: '/webproxy',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Pit.Advanced.WebProxy,
                        controller: Peach.ConfigureWebProxyController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: 'Web Proxy' }
            })
                .state(Peach.C.States.PitAdvancedTest, {
                url: '/test',
                views: {
                    '@': {
                        templateUrl: Peach.C.Templates.Pit.Advanced.Test,
                        controller: Peach.PitTestController,
                        controllerAs: Peach.C.ViewModel
                    }
                },
                ncyBreadcrumb: { label: 'Test' }
            });
            _.forEach(Peach.C.MetricsList, function (metric) {
                var state = [Peach.C.States.JobMetrics, metric.id].join('.');
                $stateProvider.state(state, {
                    url: "/" + metric.id,
                    views: {
                        '@': {
                            templateUrl: Peach.C.Templates.Job.MetricPage.replace(':metric', metric.id),
                            controller: Peach.MetricsController,
                            controllerAs: Peach.C.ViewModel
                        }
                    },
                    params: { metric: metric.id },
                    ncyBreadcrumb: { label: metric.name }
                });
            });
        }
    ]);
    p.filter('filesize', function () {
        var units = [
            'bytes',
            'KB',
            'MB',
            'GB',
            'TB',
            'PB'
        ];
        return function (bytes, precision) {
            if (bytes === 0) {
                return '0 bytes';
            }
            if (isNaN(parseFloat(bytes)) || !isFinite(bytes)) {
                return "?";
            }
            if (_.isUndefined(precision)) {
                precision = 1;
            }
            var unit = 0;
            while (bytes >= 1024) {
                bytes /= 1024;
                unit++;
            }
            var value = bytes.toFixed(precision);
            return (value.match(/\.0*$/) ? value.substr(0, value.indexOf('.')) : value) + ' ' + units[unit];
        };
    });
    p.filter('peachParameterName', function () {
        return function (value) {
            return value.substr(0).replace(/[A-Z]/g, ' $&');
        };
    });
    p.filter('capitalize', function () {
        return function (value) {
            return _.capitalize(value);
        };
    });
    p.filter('peachPitName', function () {
        return function (value) {
            return value.replace(/_/g, ' ');
        };
    });
    function Startup() {
        var version = getHtmlVer();
        if (version < 5) {
            alert("This application requires an HTML 5 and ECMAScript 5 capable browser. " +
                "Please upgrade your browser to a more recent version.");
        }
        function getHtmlVer() {
            var cName = navigator.appCodeName;
            var uAgent = navigator.userAgent;
            var htmlVer = 0.0;
            uAgent = uAgent.substring((uAgent + cName).toLowerCase().indexOf(cName.toLowerCase()));
            uAgent = uAgent.substring(cName.length);
            while (uAgent.substring(0, 1) === " " || uAgent.substring(0, 1) === "/") {
                uAgent = uAgent.substring(1);
            }
            var pointer = 0;
            while ("0123456789.+-".indexOf((uAgent + "?").substring(pointer, pointer + 1)) >= 0) {
                pointer++;
            }
            uAgent = uAgent.substring(0, pointer);
            if (!isNaN(uAgent)) {
                if (uAgent > 0) {
                    htmlVer = uAgent;
                }
            }
            return parseFloat(htmlVer);
        }
    }
    Peach.Startup = Startup;
})(Peach || (Peach = {}));
//# sourceMappingURL=app.js.map