/// <reference path="../reference.ts" />

namespace Peach {
	const LibText = {
		Pits: 'The Pits section contains all of your licensed test modules. Custom Pits will also be shown in this section.',
		Configurations: 'The Configurations section contains existing Peach Pit configurations. Selecting an existing configuration allows editing the configuration and starting a new fuzzing job.',
		Legacy: 'The Legacy section contains configurations that have not yet been upgraded to Peach’s new configuration format. To upgrade a legacy configuration simply click on the configuration link below.'
	};

	export class PitLibrary {
		constructor(
			public Name: string
		) {
			this.Text = LibText[Name];
		}

		public Text: string;
		public Categories: PitCategory[] = [];
	}

	export class PitCategory {
		constructor(
			public Name: string
		) {}
		
		public Pits: PitEntry[] = [];
	}

	export class PitEntry {
		constructor(
			public Library: ILibrary,
			public Pit: IPit
		) {}
	}
	
	export class LibraryController {
		static $inject = [
			C.Angular.$scope,
			C.Angular.$state,
			C.Angular.$uibModal,
			C.Services.Pit
		];

		constructor(
			$scope: IViewModelScope,
			private $state: ng.ui.IStateService,
			private $modal: ng.ui.bootstrap.IModalService,
			private pitService: PitService
		) {
			this.refresh();
		}

		private Libs: PitLibrary[] = [];
		
		private refresh() {
			const promise = this.pitService.LoadLibrary();
			promise.then((data: ILibrary[]) => {
				this.Libs = [];
				for (const lib of data) {
					const pitLib = new PitLibrary(lib.name);
					let hasPits = false;
					
					for (const version of lib.versions) {
						for (const pit of version.pits) {
							const category = _.find(pit.tags, (tag: ITag) =>
								tag.name.startsWith("Category")
							).values[1];
							
							let pitCategory = _.find(pitLib.Categories, { 'Name': category });
							if (!pitCategory) {
								pitCategory = new PitCategory(category);
								pitLib.Categories.push(pitCategory);
							}
							
							pitCategory.Pits.push(new PitEntry(lib, pit));
							hasPits = true;
						};
					};

					if (pitLib.Name !== 'Legacy' || hasPits) {
						this.Libs.push(pitLib);
					}
				}
			});
		}

		private ShowActions(entry: PitEntry): boolean {
			return !entry.Library.locked;
		}

		private OnSelectPit(entry: PitEntry) {
			if (entry.Library.versions[0].version === 1) {
				this.$modal.open({
					templateUrl: C.Templates.Modal.MigratePit,
					controller: MigratePitController,
					resolve: {
						Lib: () => _.find(this.Libs, { Name: 'Pits' }),
						Pit: () => angular.copy(entry.Pit)
					}
				}).result.then((newPit: IPit) => {
					this.GoToPit(newPit);
				});
			} else if (entry.Library.locked) {
				this.$modal.open({
					templateUrl: C.Templates.Modal.NewConfig,
					controller: NewConfigController,
					resolve: { 
						Title: () => 'New Pit Configuration',
						Prompt: () => 
							`This will create a new configuration for the <code>${entry.Pit.name}</code> pit. ` + 
							'You will then be able to edit the configuration and start a new fuzzing job.',
						Pit: () => angular.copy(entry.Pit),
						OnSubmit: () => modal => this.DoNewConfig(modal)
					}
				}).result.then((copied: IPit) => {
					this.GoToPit(copied);
				});
			} else {
				this.GoToPit(entry.Pit);
			}
		}

		private DoNewConfig(modal: NewConfigController) {
			this.pitService.NewConfig(modal.Pit)
				.then((response: ng.IHttpPromiseCallbackArg<IPit>) => {
					modal.Close(response.data);
				},
				(response: ng.IHttpPromiseCallbackArg<any>) => {
					switch (response.status) {
						case 400:
							modal.SetError(`${modal.Pit.name} already exists, please choose a new name.`);
							break;
						default:
							modal.SetError(`Error: ${response.statusText}`);
							break;
					}
				});
		}

		private OnCopyPit(entry: PitEntry) {
			this.$modal.open({
				templateUrl: C.Templates.Modal.NewConfig,
				controller: NewConfigController,
				resolve: { 
					Title: () => 'Copy Pit Configuration',
					Prompt: () => `This will create a copy from the <code>${entry.Pit.name}</code> pit configuration. `,
					Pit: () => angular.copy(entry.Pit),
					OnSubmit: () => modal => this.DoNewConfig(modal)
				}
			}).result.then(() => {
				this.refresh();
			});
		}

		private OnEditPit(entry: PitEntry) {
			this.$modal.open({
				templateUrl: C.Templates.Modal.NewConfig,
				controller: NewConfigController,
				resolve: { 
					Title: () => 'Edit Pit Configuration',
					Prompt: () => '',
					Pit: () => angular.copy(entry.Pit),
					OnSubmit: () => (modal: NewConfigController) => {
						this.pitService.EditConfig(modal.Pit)
							.then(() => {
								modal.Close(null);
							},
							(response: ng.IHttpPromiseCallbackArg<any>) => {
								switch (response.status) {
									case 400:
										modal.SetError(`${modal.Pit.name} already exists, please choose a new name.`);
										break;
									default:
										modal.SetError(`Error: ${response.statusText}`);
										break;
								}
							});
					}
				}
			}).result.finally(() => {
				this.refresh();
			});
		}

		private OnDeletePit(entry: PitEntry) {
			const options: IConfirmOptions = {
				Title: 'Delete Pit Configuration',
				SubmitPrompt: 'Delete'
			};
			Confirm(this.$modal, options).result
				.then(result => {
					if (result === 'ok') {
						this.pitService.DeletePit(entry.Pit).then(() => {
							this.refresh();
						}, (response: ng.IHttpPromiseCallbackArg<void>) => {
							Alert(this.$modal, {
								Title: 'Failed to delete pit configuration',
								Body: response.statusText
							}).result.finally(() => {
								this.refresh();
							})
						});
					}
				})
			;
		}

		private GoToPit(pit: IPit) {
			this.$state.go(C.States.Pit, { pit: pit.id });
		}

		private filterCategory(search: string) {
			return (category: PitCategory) => {
				if (_.isEmpty(search)) {
					return true;
				}
				return _.some(category.Pits, entry => {
					return _.includes(entry.Pit.name.toLowerCase(), search.toLowerCase());
				});
			}
		}
	}
}
