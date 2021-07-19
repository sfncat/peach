/// <reference path="../reference.ts" />

"use strict";

class Menu {
	public static GetItem(name): MenuItem {
		return new MenuItem(name);
	}
}

class MenuItem {
	constructor(name: string) {
		this.item = $('.sidebar .nav')
			.element(by.cssContainingText('li', name));
	}

	private item: protractor.ElementFinder;

	public Open() {
		this.item.click();
	}

	public SubItem(name) {
		this.Open();

		this.item.$('.submenu')
			.element(by.linkText(name))
			.click();
	}
}
