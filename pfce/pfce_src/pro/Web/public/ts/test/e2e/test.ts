/// <reference path="reference.ts" />

'use strict';

var RandofaulterPath = ['Peach Pro Library 2015 Q1', 'Test', 'randofaulter'];

describe('Dashboard |', () => {
	var page = new Dashboard();

	beforeEach(() => {
		page.Get();
	});

	it('should have a title', () => {
		expect(browser.getTitle()).toBe('Peach Fuzzer');
	});

	describe('Select Pit modal dialog |', () => {
		it('should continue to dashboard once a Pit is selected', () => {
			expect(page.SelectPit.getText()).toBe('Select a Pit');

			var selectPitModal = new SelectPit();
			expect(selectPitModal.IsPresent).toBe(true);
			expect(selectPitModal.CanSubmit).toBe(false);

			selectPitModal.SelectNodes(RandofaulterPath);

			selectPitModal.Submit();

			var copyPitModal = new CopyPit();
			expect(copyPitModal.IsPresent).toBe(true);
			expect(copyPitModal.CanSubmit).toBe(true);
			copyPitModal.Submit();

			expect(copyPitModal.IsPresent).toBe(false);
			expect(selectPitModal.IsPresent).toBe(false);

			expect(page.SelectPit.getText()).toBe('randofaulter');
		});
	});
});

describe('Configure >> Monitoring |', () => {
	beforeEach(() => {
		//SelectPitHelper(RandofaulterPath);
	});

	it('can add agents', () => {
		var page = new CfgMonitors();
		page.Go();
		expect(page.CanSave).toBe(true);
	});
});
