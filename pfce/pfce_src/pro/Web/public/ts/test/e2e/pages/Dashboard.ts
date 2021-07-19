/// <reference path="../reference.ts" />

"use strict";

class Dashboard {
	constructor() {
		this.selectPit = $('.navbar .nav li');
	}

	private selectPit: protractor.ElementFinder;
	public get SelectPit(): protractor.ElementFinder {
		return this.selectPit;
	}

	public Get() {
		browser.get('/');
	}
}

class SelectPit {
	constructor() {
		this.modal = element(by.cssContainingText('.modal-dialog', 'Select a Pit'));
		this.submit = this.modal.$('button[type=submit]');
		this.tree = this.modal.$('div[treecontrol]');
	}

	private modal: protractor.ElementFinder;
	private submit: protractor.ElementFinder;
	private tree: protractor.ElementFinder;

	public get IsPresent() {
		return this.modal.isPresent();
	}

	public get CanSubmit() {
		return this.submit.isEnabled();
	}

	public ClickTreeNode(text) {
		var node = this.tree.element(by.cssContainingText('li div', text));
		node.click();
	}

	public Submit() {
		this.submit.click();
	}

	public SelectNodes(nodes: string[]) {
		nodes.forEach((node, i) => {
			this.ClickTreeNode(node);
			var isLeaf = (i === (nodes.length - 1));
			expect(this.CanSubmit).toBe(isLeaf);
		});
	}
}

class CopyPit {
	constructor() {
		this.modal = element(by.cssContainingText('.modal-dialog', 'Copy Pit'));
		this.submit = this.modal.$('button[type=submit]');
	}

	private modal: protractor.ElementFinder;
	private submit: protractor.ElementFinder;

	public get IsPresent() {
		return this.modal.isPresent();
	}

	public get CanSubmit() {
		return this.submit.isEnabled();
	}

	public Submit() {
		this.submit.click();
	}
}

function SelectPitHelper(path: string[]) {
	var page = new Dashboard();
	page.Get();
	var modal = new SelectPit();
	modal.SelectNodes(path);
	modal.Submit();
}
