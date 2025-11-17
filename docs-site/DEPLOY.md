# Deploying Mars Vista Docs to Railway

This guide explains how to deploy the documentation site to `docs.marsvista.dev` using Railway.

## Prerequisites

- Railway CLI installed (`npm install -g @railway/cli`)
- Railway account logged in (`railway login`)
- Access to the mars-vista-api Railway project

## Deployment Steps

### Option 1: Railway Dashboard (Recommended for First Deploy)

1. **Go to Railway Dashboard**:
   - Open https://railway.app
   - Navigate to your `mars-vista-api` project

2. **Create New Service**:
   - Click "New" → "Empty Service"
   - Name it: `mars-vista-docs`

3. **Connect GitHub Repository**:
   - Settings → Service → Connect to GitHub repo
   - Select: `mars-vista-api` repository
   - Root Directory: `docs-site`
   - Branch: `main`

4. **Configure Build**:
   - Build Command: (leave empty - uses npm install automatically)
   - Start Command: `npx serve . -l $PORT`
   - Or create `nixpacks.toml` in `docs-site/`:
     ```toml
     [phases.setup]
     nixPkgs = ["nodejs"]

     [phases.install]
     cmds = ["npm install"]

     [start]
     cmd = "npx serve . -l $PORT"
     ```

5. **Add Custom Domain**:
   - Settings → Domains → Custom Domain
   - Enter: `docs.marsvista.dev`
   - Update your DNS (Cloudflare) with the CNAME provided by Railway

6. **Deploy**:
   - Railway will auto-deploy on push to `main`

### Option 2: Railway CLI (For Updates)

```bash
# From project root
cd docs-site

# Link to Railway service (first time only)
railway link

# Deploy
railway up
```

## DNS Configuration (Cloudflare)

Add a CNAME record in your DNS provider:

```
Type: CNAME
Name: docs
Target: [your-railway-domain].up.railway.app
Proxy: Enabled (if using Cloudflare)
```

## Environment Variables

No environment variables needed for the docs site.

## Verification

After deployment:

1. **Check deployment status**: Visit Railway dashboard
2. **Test the URL**: https://docs.marsvista.dev
3. **Verify OpenAPI spec loads**: Check that the API reference renders correctly
4. **Test "Try It" feature**: Make a test request to ensure CORS is configured

## Troubleshooting

### Docs site not loading

- Check Railway logs: `railway logs`
- Verify build succeeded in Railway dashboard
- Ensure `serve` package is in `dependencies` (not `devDependencies`)

### OpenAPI spec not found (404)

- Verify `openapi.yaml` is in the `docs-site/` directory
- Check file paths in `index.html` match the directory structure

### "Try It" feature fails with CORS error

- Ensure API CORS configuration allows `docs.marsvista.dev`
- Check API deployment is running: https://api.marsvista.dev/health

### Custom domain not working

- Verify DNS CNAME record is correct
- Wait 5-10 minutes for DNS propagation
- Check Railway domain settings show the custom domain as active

## Updating Documentation

1. **Edit files** in `docs-site/`
2. **Test locally**: `npm run dev`
3. **Commit and push** to `main` branch
4. **Railway auto-deploys** within 1-2 minutes

## Manual Deployment

If auto-deploy isn't working:

```bash
cd docs-site
railway up --service mars-vista-docs
```

## Cost

Railway static sites are very cheap:
- Estimated: $0.50-2/month depending on traffic
- First $5/month is free on Hobby plan

## Future Improvements

- [ ] Enable automatic OpenAPI spec generation from API
- [ ] Add analytics (optional)
- [ ] Create CI/CD workflow for validation
- [ ] Add more markdown guides
