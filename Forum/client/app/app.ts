﻿import { BBCode } from './services/bbcode';
import { EasterEgg } from './services/easter-egg';
import { Navigation } from './services/navigation';
import { PassedTimeMonitor } from './services/passed-time-monitor';
import { SmileySelector } from './services/smiley-selector';

import { TopicIndex } from './pages/topic-index';
import { TopicDisplay } from './pages/topic-display';
import { ManageBoards } from './pages/manage-boards';
import { MessageCreate } from './pages/message-create';

window.onload = function () {
	let app = new App();
	app.boot();
};

export class App {
	bbCode: BBCode;
	easterEgg: EasterEgg;
	navigation: Navigation;
	passedTimeMonitor: PassedTimeMonitor;
	smileySelector: SmileySelector;

	constructor() {
		this.bbCode = new BBCode(document);
		this.easterEgg = new EasterEgg(document);
		this.navigation = new Navigation(document);
		this.passedTimeMonitor = new PassedTimeMonitor(document);
		this.smileySelector = new SmileySelector(document);
	}

	boot() {
		this.bbCode.init();
		this.easterEgg.init();
		this.navigation.addListeners();
		this.passedTimeMonitor.init();
		this.smileySelector.init();

		let pageActions = (<any>window).pageActions;

		switch (pageActions) {
			case 'manage-boards':
				let manageBoards = new ManageBoards(document, this);
				manageBoards.init();
				break;

			case 'message-create':
				let messageCreate = new MessageCreate(document, this);
				messageCreate.init();
				break;

			case 'topic-display':
				let topicDisplay = new TopicDisplay(document, this);
				topicDisplay.init();
				break;

			case 'topic-index':
				let topicIndex = new TopicIndex(document, this);
				topicIndex.init();
				break;
		}
	}
}