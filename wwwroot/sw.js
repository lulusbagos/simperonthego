const STATIC_CACHE = "simper-static-v1";
const PAGE_CACHE = "simper-pages-v1";

const STATIC_ASSETS = [
	"/",
	"/css/site.css",
	"/css/admin-premium.css",
	"/js/site.js",
	"/js/admin-theme.js",
	"/logo/favicon.ico",
	"/logo/logo.png"
];

self.addEventListener("install", (event) => {
	event.waitUntil(
		caches.open(STATIC_CACHE).then((cache) => cache.addAll(STATIC_ASSETS)).then(() => self.skipWaiting())
	);
});

self.addEventListener("activate", (event) => {
	event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", (event) => {
	const request = event.request;
	if (request.method !== "GET") {
		return;
	}

	const requestUrl = new URL(request.url);
	if (requestUrl.origin !== self.location.origin) {
		return;
	}

	if (request.mode === "navigate") {
		event.respondWith(networkFirstPage(request));
		return;
	}

	event.respondWith(staleWhileRevalidate(request));
});

async function networkFirstPage(request) {
	const cache = await caches.open(PAGE_CACHE);

	try {
		const response = await fetch(request);
		cache.put(request, response.clone());
		return response;
	} catch (_error) {
		const cachedResponse = await cache.match(request);
		if (cachedResponse) {
			return cachedResponse;
		}

		const fallback = await cache.match("/");
		if (fallback) {
			return fallback;
		}

		return new Response("Halaman tidak tersedia saat offline.", {
			status: 503,
			headers: { "Content-Type": "text/plain; charset=utf-8" }
		});
	}
}

async function staleWhileRevalidate(request) {
	const cache = await caches.open(STATIC_CACHE);
	const cachedResponse = await cache.match(request);
	const networkFetch = fetch(request)
		.then((response) => {
			cache.put(request, response.clone());
			return response;
		})
		.catch(() => cachedResponse);

	return cachedResponse || networkFetch;
}
