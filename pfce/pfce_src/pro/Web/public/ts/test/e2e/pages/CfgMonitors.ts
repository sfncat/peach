/// <reference path="../reference.ts" />

"use strict";

class CfgMonitors {
	constructor() {
		this.saveButton = element(by.buttonText('Save'));
		this.addAgentButton = element(by.partialButtonText('Add Agent'));
	}

	private saveButton: protractor.ElementFinder;
	private addAgentButton: protractor.ElementFinder;

	public Go() {
		Menu.GetItem('Configuration').SubItem('Monitoring');
	}

	public get CanSave() {
		return this.saveButton.isEnabled();
	}

	public Save() {
		this.saveButton.click();
	}

	public AddAgent() {
		this.addAgentButton.click();
	}
}
