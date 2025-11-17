# Mars Vista API Documentation Site

Interactive API documentation powered by Stoplight Elements.

## Development

```bash
cd docs-site
npm install
npm run dev
```

Visit http://localhost:3000

## Deployment

This site is deployed to Railway as a static site service.

**Custom domain:** https://docs.marsvista.dev

## Structure

```
docs-site/
├── index.html          # Main Stoplight Elements page
├── openapi.yaml        # OpenAPI specification
├── guides/             # Markdown guides
│   ├── getting-started.md
│   ├── examples.md
│   └── rate-limits.md
├── assets/             # Images, favicon, etc.
└── package.json        # Dependencies
```

## Updating Documentation

1. **Edit OpenAPI spec:** Modify `openapi.yaml`
2. **Add guides:** Create markdown files in `guides/`
3. **Test locally:** Run `npm run dev`
4. **Deploy:** Push to `main` branch (auto-deploys via Railway)

## OpenAPI Spec

The OpenAPI spec is manually maintained to provide rich descriptions and examples.

**Future improvement:** Generate base spec from API and enhance with annotations.
