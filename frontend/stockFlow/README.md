# StockFlow Frontend

Arabic RTL Angular 21 client for the single-company, multi-warehouse StockFlow API.

Read **README-AR.md** first for placement and step-by-step instructions.

Requires Node.js 22.12+ (22.x) or Node.js 24.x; Node 24 LTS recommended.

```sh
npm ci
npm start
npm test
npm run build
```

Default API: `https://stock-flow.runasp.net/api/v1`. Runtime setting: `public/app-config.json` (source) or `dist/stockflow/browser/app-config.json` (built artifact).

Deploy the contents of `dist/stockflow/browser` to a separate static frontend host. Add its exact origin to the backend CORS allowlist and restart the backend. The package includes SPA fallback settings for IIS, Netlify-style static hosting and Vercel. Do not overwrite the existing API site's web.config with the frontend web.config.

Source and a production build are both included. node_modules and build cache are excluded. Credentials, database settings and synthetic QA server are not included. No default admin account is embedded.

See docs/VERIFICATION.md for executed checks and remaining manual production validation.
