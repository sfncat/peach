declare module pithy {

	interface ISafeString {
		toString(): string;
		valueOf(): string;
	}

	interface ElementFn {
		(attrs?: any, contents?: ISafeString[]): ISafeString;
	}

	interface IPithyStatic {
		html: ElementFn;
		head: ElementFn;
		title: ElementFn;
		base: ElementFn;
		link: ElementFn;
		meta: ElementFn;
		style: ElementFn;
		script: ElementFn;

		noscript: ElementFn;
		body: ElementFn;
		section: ElementFn;
		nav: ElementFn;
		article: ElementFn;
		aside: ElementFn;
		h1: ElementFn;
		h2: ElementFn;

		h3: ElementFn;
		h4: ElementFn;
		h5: ElementFn;
		h6: ElementFn;
		hgroup: ElementFn;
		header: ElementFn;
		footer: ElementFn;
		address: ElementFn;
		main: ElementFn;

		p: ElementFn;
		hr: ElementFn;
		pre: ElementFn;
		blockquote: ElementFn;
		ol: ElementFn;
		ul: ElementFn;
		li: ElementFn;
		dl: ElementFn;
		dt: ElementFn;
		dd: ElementFn;

		figure: ElementFn;
		figcaption: ElementFn;
		div: ElementFn;
		a: ElementFn;
		em: ElementFn;
		strong: ElementFn;
		small: ElementFn;
		s: ElementFn;
		cite: ElementFn;

		q: ElementFn;
		dfn: ElementFn;
		abbr: ElementFn;
		data: ElementFn;
		time: ElementFn;
		code: ElementFn;
		var: ElementFn;
		samp: ElementFn;
		kbd: ElementFn;
		sub: ElementFn;

		sup: ElementFn;
		i: ElementFn;
		b: ElementFn;
		u: ElementFn;
		mark: ElementFn;
		ruby: ElementFn;
		rt: ElementFn;
		rp: ElementFn;
		bdi: ElementFn;
		bdo: ElementFn;
		span: ElementFn;
		br: ElementFn;

		wbr: ElementFn;
		ins: ElementFn;
		del: ElementFn;
		img: ElementFn;
		iframe: ElementFn;
		embed: ElementFn;
		object: ElementFn;
		param: ElementFn;
		video: ElementFn;

		audio: ElementFn;
		source: ElementFn;
		track: ElementFn;
		canvas: ElementFn;
		map: ElementFn;
		area: ElementFn;
		svg: ElementFn;
		math: ElementFn;

		table: ElementFn;
		caption: ElementFn;
		colgroup: ElementFn;
		col: ElementFn;
		tbody: ElementFn;
		thead: ElementFn;
		tfoot: ElementFn;
		tr: ElementFn;

		td: ElementFn;
		th: ElementFn;
		form: ElementFn;
		fieldset: ElementFn;
		legend: ElementFn;
		label: ElementFn;
		input: ElementFn;
		button: ElementFn;

		select: ElementFn;
		datalist: ElementFn;
		optgroup: ElementFn;
		option: ElementFn;
		textarea: ElementFn;
		keygen: ElementFn;

		output: ElementFn;
		progress: ElementFn;
		meter: ElementFn;
		details: ElementFn;
		summary: ElementFn;
		command: ElementFn;
		menu: ElementFn;

		escape(str: string): string;
	}
}

declare var pithy: pithy.IPithyStatic;
