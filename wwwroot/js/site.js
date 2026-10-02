// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
	const scrollStorageKey = 'Zentro.scrollPosition';
	const getScrollElement = () => {
		const content = document.querySelector('.hmi-content');
		return content && window.getComputedStyle(content).overflowY !== 'visible'
			? content
			: document.scrollingElement;
	};

	document.addEventListener('submit', (event) => {
		if (!(event.target instanceof HTMLFormElement)) return;
		const scrollElement = getScrollElement();
		sessionStorage.setItem(scrollStorageKey, JSON.stringify({
			path: window.location.pathname,
			scrollTop: scrollElement?.scrollTop ?? window.scrollY,
		}));
	});

	const savedScroll = sessionStorage.getItem(scrollStorageKey);
	if (savedScroll !== null) {
		sessionStorage.removeItem(scrollStorageKey);
		try {
			const savedPosition = JSON.parse(savedScroll);
			const scrollElement = getScrollElement();
			if (savedPosition.path === window.location.pathname && Number.isFinite(savedPosition.scrollTop) && scrollElement) {
				window.requestAnimationFrame(() => { scrollElement.scrollTop = savedPosition.scrollTop; });
			}
		} catch { }
	}
})();

(() => {
	const backdrop = document.getElementById('hmi-confirm');
	if (!backdrop) return;

	const message = document.getElementById('hmi-confirm-message');
	const cancel = document.getElementById('hmi-confirm-cancel');
	const submit = document.getElementById('hmi-confirm-submit');
	let targetControl = null;

	const close = () => {
		backdrop.hidden = true;
		targetControl = null;
	};

	document.querySelectorAll('[data-confirm]').forEach((control) => {
		control.addEventListener('click', (event) => {
			event.preventDefault();
			event.stopImmediatePropagation();
			targetControl = control;
			message.textContent = control.dataset.confirm;
			backdrop.hidden = false;
			cancel.focus();
		});
	});

	cancel.addEventListener('click', close);
	submit.addEventListener('click', () => {
		if (!targetControl) return;
		const sourceForm = targetControl.closest('form');
		if (sourceForm) {
			close();
			sourceForm.requestSubmit();
			return;
		}
		const form = document.createElement('form');
		form.method = 'post';
		form.action = targetControl.href;
		const token = document.querySelector('#post-command-token input[name="__RequestVerificationToken"]');
		if (token) form.append(token.cloneNode());
		document.body.append(form);
		form.requestSubmit();
	});
	backdrop.addEventListener('click', (event) => {
		if (event.target === backdrop) close();
	});
	document.addEventListener('keydown', (event) => {
		if (!backdrop.hidden && event.key === 'Escape') close();
	});
})();
