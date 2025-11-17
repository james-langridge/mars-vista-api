# Story 019: Public API Documentation Site

**Priority:** HIGH (Phase 1: Pre-Launch Critical)
**Status:** Not Started
**Estimate:** 4-6 hours

## Problem Statement

The Mars Vista API currently lacks public-facing documentation. While an OpenAPI/Swagger spec exists and works in development, it's disabled in production. Users have no way to:

- Discover available endpoints
- Learn how to authenticate and use the API
- Understand rate limits and error responses
- See code examples in multiple languages
- Follow a "Getting Started" tutorial

**Impact:** Without documentation, potential users cannot effectively use the API, limiting adoption and creating support burden.

## Goals

1. Deploy interactive API documentation to `docs.marsvista.dev`
2. Provide comprehensive endpoint reference with examples
3. Include authentication, rate limiting, and error handling guides
4. Enable users to make test requests directly from the docs
5. Create "Getting Started" tutorial for new users
6. Document all NASA-compatible endpoints with comparison to original API

## Solution Options Analysis

### Option 1: Stoplight Elements (Recommended)

**Pros:**
- Beautiful, modern UI built specifically for OpenAPI specs
- Interactive "Try It" feature for testing endpoints
- Excellent developer experience
- Free for public documentation
- Easy to deploy as static site (Vercel, Netlify, Railway static)
- Supports markdown content alongside API reference

**Cons:**
- Requires building/hosting static site
- Less customization than fully custom solution

**Deployment:** Static HTML/JS site hosted on Vercel or Railway

### Option 2: Redoc

**Pros:**
- Clean, responsive three-panel layout
- Excellent for read-heavy documentation
- Fast, lightweight
- Easy integration with OpenAPI spec
- Free and open-source

**Cons:**
- No interactive "Try It" feature (read-only)
- Less modern UI compared to Stoplight

**Deployment:** Single HTML file or static site

### Option 3: Swagger UI in Production

**Pros:**
- Already integrated in the API
- Zero additional infrastructure
- Interactive API explorer built-in

**Cons:**
- Less polished UI for public consumption
- Harder to add tutorials and guides
- Couples documentation with API deployment
- Limited customization options

**Deployment:** Enable in production environment

### Option 4: Custom Documentation Site (Docusaurus/VitePress)

**Pros:**
- Complete control over content and design
- Can integrate blog, guides, tutorials
- Version documentation alongside code
- Great for comprehensive developer portals

**Cons:**
- Most time-intensive to build
- Requires ongoing maintenance
- Overkill for current needs

**Deployment:** Static site generator + hosting

## Recommended Approach: Stoplight Elements

**Why:**
1. Best balance of features, ease of use, and time investment
2. Interactive "Try It" feature is crucial for user adoption
3. Professional appearance increases credibility
4. Can add markdown guides alongside API reference
5. Static deployment is simple and free

**Deployment Strategy:**
- Build static site with Stoplight Elements
- Deploy to Railway static hosting (simplest) or Vercel
- Configure custom domain: `docs.marsvista.dev`
- Auto-deploy on OpenAPI spec changes (CI/CD)

## Implementation Plan

### Step 1: Prepare OpenAPI Specification

**Tasks:**
1. Review and enhance existing OpenAPI spec in `src/MarsVista.Api/`
2. Add comprehensive descriptions for all endpoints
3. Add request/response examples for common use cases
4. Document authentication (API key header)
5. Document rate limiting behavior (100 req/min per IP)
6. Add error response schemas (400, 404, 429, 500)
7. Include NASA API compatibility notes

**Endpoint documentation checklist:**
- `GET /api/v1/rovers` - List all rovers
- `GET /api/v1/rovers/{name}` - Get single rover
- `GET /api/v1/rovers/{name}/photos` - Query photos (with all filters)
- `GET /api/v1/manifests/{name}` - Get rover manifest
- `GET /health` - Health check

**Example enhancements needed:**
```yaml
paths:
  /api/v1/rovers/{name}/photos:
    get:
      summary: Query photos for a specific rover
      description: |
        Query photos taken by a Mars rover with flexible filtering options.
        This endpoint is fully compatible with NASA's Mars Photo API.

        **Filters:**
        - `sol`: Martian sol (mission day)
        - `earth_date`: Earth date in YYYY-MM-DD format
        - `camera`: Camera name (e.g., FHAZ, RHAZ, NAVCAM)

        **Performance:** Typical response time < 260ms for most queries.

        **Rate Limit:** 100 requests per minute per IP address.
      parameters:
        - name: sol
          in: query
          description: Martian sol (mission day) to query
          schema:
            type: integer
          example: 1000
        - name: earth_date
          in: query
          description: Earth date in YYYY-MM-DD format
          schema:
            type: string
            format: date
          example: "2021-04-19"
        - name: camera
          in: query
          description: Camera name filter
          schema:
            type: string
          example: "NAVCAM"
      responses:
        '200':
          description: Successful query
          content:
            application/json:
              schema:
                type: object
                properties:
                  photos:
                    type: array
                    items:
                      $ref: '#/components/schemas/Photo'
              examples:
                navcam_example:
                  summary: Navigation camera photos from sol 1000
                  value:
                    photos:
                      - id: 123456
                        sol: 1000
                        camera: { name: "NAVCAM", full_name: "Navigation Camera" }
                        img_src: "https://mars.nasa.gov/..."
                        earth_date: "2021-04-19"
                        rover: { name: "Perseverance" }
        '429':
          description: Rate limit exceeded
          content:
            application/json:
              schema:
                type: object
                properties:
                  error:
                    type: string
                    example: "Rate limit exceeded. Please try again later."
```

### Step 2: Create Stoplight Elements Site

**Tasks:**
1. Create new directory: `docs-site/`
2. Initialize npm project with Stoplight Elements
3. Create `index.html` with Stoplight configuration
4. Add custom branding (Mars Vista logo, colors)
5. Add markdown content:
   - Getting Started guide
   - Authentication guide
   - Rate limiting documentation
   - Error handling guide
   - NASA API compatibility notes
   - Code examples (curl, JavaScript, Python)
6. Configure API base URL (production)

**Directory structure:**
```
docs-site/
├── index.html          # Main Stoplight page
├── guides/
│   ├── getting-started.md
│   ├── authentication.md
│   ├── rate-limits.md
│   └── examples.md
├── openapi.yaml        # Copy of API spec (or fetch dynamically)
└── assets/
    ├── logo.png
    └── favicon.ico
```

**Example index.html:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Mars Vista API Documentation</title>
  <meta name="description" content="Complete API documentation for Mars Vista - NASA Mars Rover Photo API">
  <link rel="stylesheet" href="https://unpkg.com/@stoplight/elements/styles.min.css">
</head>
<body>
  <elements-api
    apiDescriptionUrl="./openapi.yaml"
    router="hash"
    layout="sidebar"
    logo="./assets/logo.png"
  />
  <script src="https://unpkg.com/@stoplight/elements/web-components.min.js"></script>
</body>
</html>
```

### Step 3: Create Guides and Examples

**Getting Started Guide (`guides/getting-started.md`):**
- Introduction to Mars Vista API
- Basic usage example (curl)
- Authentication setup
- First query walkthrough
- Common use cases

**Code Examples (`guides/examples.md`):**
- **curl** examples for all endpoints
- **JavaScript/Node.js** with fetch/axios
- **Python** with requests library
- **C#** with HttpClient

**Example curl commands:**
```bash
# Get all rovers
curl https://api.marsvista.dev/api/v1/rovers

# Get Perseverance photos from sol 100
curl "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=100"

# Get Curiosity NAVCAM photos from Earth date
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2023-01-15&camera=NAVCAM"

# With API key (for higher rate limits)
curl -H "X-API-Key: YOUR_KEY_HERE" \
  "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=500"
```

**Example JavaScript:**
```javascript
// Fetch photos from Perseverance rover
async function getPhotos(sol) {
  const response = await fetch(
    `https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=${sol}`
  );
  const data = await response.json();
  return data.photos;
}

// Usage
const photos = await getPhotos(100);
console.log(`Found ${photos.length} photos`);
```

**Example Python:**
```python
import requests

# Query Curiosity photos by Earth date
def get_curiosity_photos(earth_date, camera=None):
    url = "https://api.marsvista.dev/api/v1/rovers/curiosity/photos"
    params = {"earth_date": earth_date}
    if camera:
        params["camera"] = camera

    response = requests.get(url, params=params)
    response.raise_for_status()
    return response.json()["photos"]

# Usage
photos = get_curiosity_photos("2023-06-15", camera="NAVCAM")
print(f"Found {len(photos)} NAVCAM photos")
```

### Step 4: Deploy to docs.marsvista.dev

**Option A: Railway Static Hosting (Recommended for simplicity)**

1. Add `docs-site/` as Railway static site service
2. Configure custom domain: `docs.marsvista.dev`
3. Set up automatic deployments on push to `main`

**Railway configuration (`railway.toml` or dashboard):**
```toml
[build]
builder = "NIXPACKS"
buildCommand = "cd docs-site && npm install"

[deploy]
startCommand = "npx serve docs-site -l 3000"
```

**Option B: Vercel (Alternative)**

1. Create Vercel project linked to `docs-site/` directory
2. Configure custom domain
3. Auto-deploy on git push

**Option C: GitHub Pages + Custom Domain**

1. Create `gh-pages` branch with docs-site content
2. Enable GitHub Pages
3. Configure CNAME for `docs.marsvista.dev`

**DNS Configuration (Cloudflare/DNS provider):**
```
Type: CNAME
Name: docs
Target: [railway-domain].up.railway.app (or Vercel)
Proxy: Enabled (if using Cloudflare)
```

### Step 5: Add Documentation Links to Main API

**Tasks:**
1. Update API root endpoint (`GET /`) to redirect to docs
2. Add `Link` header to all API responses pointing to docs
3. Update `README.md` with prominent docs link
4. Add docs link to error responses (especially 404, 429)

**Example API root endpoint:**
```csharp
app.MapGet("/", () => Results.Redirect("https://docs.marsvista.dev"));
```

**Example error response with docs link:**
```json
{
  "error": "Rate limit exceeded (100 requests per minute)",
  "retry_after": 60,
  "documentation": "https://docs.marsvista.dev/guides/rate-limits"
}
```

### Step 6: CI/CD for Documentation Updates

**Tasks:**
1. Add GitHub Action to validate OpenAPI spec on changes
2. Auto-deploy docs site when spec changes
3. Add preview deployments for pull requests (Vercel/Railway)

**Example GitHub Action (`.github/workflows/docs.yml`):**
```yaml
name: Deploy Documentation

on:
  push:
    branches: [main]
    paths:
      - 'docs-site/**'
      - 'src/MarsVista.Api/openapi.yaml'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy to Railway
        run: railway up --service docs
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
```

## NASA API Compatibility Documentation

**Important:** Document how Mars Vista API compares to NASA's official API.

**Compatibility notes to include:**
- ✅ All NASA endpoints supported
- ✅ 100% compatible request/response format
- ✅ Additional features: JSONB raw_data preservation
- ⚡ Faster response times (< 260ms vs NASA's variable latency)
- 🔒 Rate limiting (100 req/min) vs NASA's limits

**Migration guide for existing NASA API users:**
```markdown
## Migrating from NASA Mars Photo API

Mars Vista is a drop-in replacement for NASA's Mars Photo API.

### Change your base URL:
- **NASA:** `https://api.nasa.gov/mars-photos/api/v1/`
- **Mars Vista:** `https://api.marsvista.dev/api/v1/`

### Authentication:
- **NASA:** Requires `?api_key=YOUR_NASA_KEY`
- **Mars Vista:** Optional `X-API-Key` header for higher rate limits

### Example:
```bash
# NASA API
curl "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&api_key=DEMO_KEY"

# Mars Vista API (same data, faster)
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"
```

### Advantages:
- ⚡ 50-70% faster response times
- 🔓 No API key required for public endpoints
- 📦 Complete NASA metadata preserved in `raw_data` field
- 🆙 Regular data updates from NASA sources
```

## Success Criteria

**Documentation must include:**
- ✅ Interactive API reference (Stoplight Elements)
- ✅ "Try It" feature for testing endpoints
- ✅ Getting Started tutorial
- ✅ Authentication guide
- ✅ Rate limiting documentation
- ✅ Error handling guide
- ✅ Code examples (curl, JavaScript, Python, C#)
- ✅ NASA API compatibility notes
- ✅ Migration guide for NASA API users

**Deployment requirements:**
- ✅ Accessible at `https://docs.marsvista.dev`
- ✅ Fast load time (< 2 seconds)
- ✅ Mobile-responsive design
- ✅ HTTPS enabled
- ✅ Auto-deploys on spec changes

**User validation:**
- Can a new user make their first successful API request within 5 minutes?
- Is authentication clearly explained?
- Are rate limits and error responses well-documented?
- Do code examples work copy-paste?

## Testing Plan

**Before deployment:**
1. Verify all OpenAPI spec examples are accurate
2. Test "Try It" feature for each endpoint
3. Validate code examples (run curl/JS/Python samples)
4. Check mobile responsiveness
5. Test custom domain setup

**After deployment:**
1. Share docs link with 2-3 beta users for feedback
2. Monitor analytics (if added) for most-viewed pages
3. Track support questions to identify documentation gaps

## Follow-up Work

**Future enhancements (not in this story):**
- Add search functionality to docs
- Create video tutorial/walkthrough
- Add FAQ section based on user questions
- Create Postman collection for easy testing
- Add analytics to track popular endpoints
- Version documentation when API v2 is released

## Related Stories

- **Story 017:** Production monitoring (shows API performance in docs)
- **Story 023:** Legal/attribution (add NASA attribution to docs footer)

## Technical Decisions

**Decision:** Use Stoplight Elements over custom documentation site

**Rationale:**
- Time-to-value: Can deploy professional docs in hours, not days
- Interactive features: "Try It" is crucial for developer experience
- Maintenance: Static site requires minimal upkeep
- Cost: Free for public documentation
- Flexibility: Can migrate to custom solution later if needed

**Trade-offs:**
- Less customization than fully custom site
- Dependency on Stoplight's design patterns
- Acceptable trade-off for faster launch

---

**Decision:** Deploy docs as separate static site vs enabling Swagger in production

**Rationale:**
- Separation of concerns: Docs site can evolve independently
- Better UX: Stoplight provides superior experience vs Swagger UI
- Performance: Static site doesn't load API server
- Flexibility: Can add guides, tutorials, blog content

**Trade-offs:**
- Additional infrastructure to manage
- Need to keep OpenAPI spec in sync
- Worth it for professional presentation

---

## Implementation Notes

**For the implementer:**
1. Start with OpenAPI spec enhancements - this is the foundation
2. Use Stoplight's examples and templates as starting point
3. Test "Try It" feature against production API to ensure CORS is configured
4. Write code examples that you actually run and verify work
5. Get feedback early - share with 1-2 users before full launch
6. Keep it simple for v1 - can always add more content later

**Gotchas to avoid:**
- Don't forget CORS configuration for "Try It" to work from docs domain
- Ensure OpenAPI spec `servers` array points to production URL
- Test rate limiting behavior when users try examples
- Remember to add NASA attribution footer (Story 023 requirement)

**Time estimates:**
- OpenAPI spec enhancements: 1-2 hours
- Stoplight site setup: 1 hour
- Guides and examples: 2-3 hours
- Deployment and DNS setup: 30 minutes
- Testing and refinement: 1 hour

**Total: 4-6 hours**

---

## Deployment Architecture Diagram

```
┌─────────────────────────────────────────────┐
│  docs.marsvista.dev (Stoplight Elements)    │
│  ┌────────────────────────────────────────┐ │
│  │ Interactive API Reference              │ │
│  │ - OpenAPI Spec Display                 │ │
│  │ - "Try It" Feature (CORS to API)       │ │
│  │ - Markdown Guides                      │ │
│  └────────────────────────────────────────┘ │
│  Railway Static / Vercel / GitHub Pages     │
└─────────────────────────────────────────────┘
                     │
                     │ API Requests from "Try It"
                     ▼
┌─────────────────────────────────────────────┐
│  api.marsvista.dev (Mars Vista API)         │
│  ┌────────────────────────────────────────┐ │
│  │ API Endpoints                          │ │
│  │ - GET /api/v1/rovers                   │ │
│  │ - GET /api/v1/rovers/{name}/photos     │ │
│  │ - Rate Limiting Middleware             │ │
│  │ - CORS: Allow docs.marsvista.dev       │ │
│  └────────────────────────────────────────┘ │
│  Railway API Service                        │
└─────────────────────────────────────────────┘
```

## Resources

**Stoplight Elements:**
- Docs: https://stoplight.io/open-source/elements
- Examples: https://github.com/stoplightio/elements/tree/main/examples
- CDN: https://unpkg.com/@stoplight/elements

**OpenAPI Best Practices:**
- https://swagger.io/docs/specification/about/
- https://oai.github.io/Documentation/

**Inspiration (well-documented APIs):**
- Stripe API docs: https://stripe.com/docs/api
- Twilio API docs: https://www.twilio.com/docs
- NASA API docs: https://api.nasa.gov/

---

**Ready to implement!** 🚀
