import { throwIfNull } from '../helpers';
import { HttpMethod } from '../definitions/http-method';
import { App } from '../app';
import { Navigation } from '../services/navigation';
import { Xhr } from '../services/xhr';

import { XhrOptions } from '../models/xhr-options';
import { TopicIndexSettings } from '../models/topic-index-settings';

export class TopicIndex {
	private settings: TopicIndexSettings;

	constructor(private doc: Document, private app: App) {
		throwIfNull(doc, 'doc');
		this.settings = new TopicIndexSettings(window);
	}

	init(): void {
		if (this.app.hub) {
			this.bindHubActions();
		}
	}

	bindHubActions = () => {
		if (!this.app.hub) {
			throw new Error('Hub not defined.');
		}

		this.app.hub.on('new-reply', this.hubNewReply);
	}

	hubNewReply = () => {
		if (this.settings.currentPage == 1) {
			this.loadTopicsPage(this.settings.boardId, 1, this.settings.unreadFilter);
		}
	}

	loadTopicsPage = (boardId: number, pageId: number, unread: number) => {
		let self = this;

		let requestOptions = new XhrOptions({
			method: HttpMethod.Get,
			url: `/Topics/IndexPartial/${boardId}/${pageId}?unread=${unread}`,
			responseType: 'document'
		});

		Xhr.request(requestOptions)
			.then((xhrResult) => {
				let resultDocument = <HTMLElement>(<Document>xhrResult.response).documentElement;
				let resultBody = <HTMLBodyElement>resultDocument.querySelector('body');
				let resultBodyElements = resultBody.childNodes;
				let topicList = <Element>self.doc.querySelector('#topic-list');

				resultBodyElements.forEach(node => {
					let element = node as Element;

					if (element && element.tagName) {
						if (element.tagName.toLowerCase() == 'script') {
							eval(element.textContent || '');
							new Navigation(self.doc).addListenerClickableLinkParent();
						}
						else if (element.tagName.toLowerCase() == 'section') {
							topicList.after(element);
							topicList.remove();
						}
					}
				});

				let partialSettings = new TopicIndexSettings(window);

				self.settings.currentPage = partialSettings.currentPage;
				self.settings.totalPages = partialSettings.totalPages;
			})
			.catch(Xhr.logRejected);
	}
}
