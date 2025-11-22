# Story 013: Remove Pricing Tiers - Free API for All Users

## Context

The Mars Vista API currently has a tiered pricing model (Free/Pro/Enterprise) with different rate limits. To encourage adoption and make the API more accessible, we're removing all pricing and making the API completely free for all users with generous "Pro" tier rate limits (10,000 requests/hour, 100,000 requests/day).

**Strategy:** Keep the database schema and backend models as-is (Tier field remains for future flexibility), but default all new API keys to "pro" tier and remove all pricing-related UI and documentation. This approach maintains technical flexibility while simplifying the user experience.

## Requirements

### 1. Backend Changes (Minimal)

**src/MarsVista.Api/Controllers/V1/ApiKeyInternalController.cs:**
- Change default tier from `"free"` to `"pro"` when creating API keys (line 118)

**src/MarsVista.Api/Services/RateLimitService.cs:**
- Update tier limits documentation to reflect new reality
- Consider removing "free" tier from TierLimits dictionary (optional - can keep for backwards compatibility)

**Keep as-is (no changes needed):**
- `ApiKey` entity - Tier field stays for future flexibility
- Database migrations - No schema changes needed
- Rate limiting middleware - Already works with "pro" tier
- All existing endpoints - No API changes

### 2. Frontend Changes (Major)

**Delete:**
- ❌ `web/app/app/pricing/page.tsx` - Entire pricing page

**Update:**

**web/app/components/Header.tsx:**
- Remove "Pricing" link from navigation (line 19-21)

**web/app/app/dashboard/page.tsx:**
- Remove tier display showing "Free" (line 37)
- Remove upgrade message and "View Pricing" button (lines 39-46)
- Simplify to just show current rate limits (without tier context)

**web/app/app/docs/page.tsx:**
- Remove pricing tier comparison section (lines 570-603)
- Replace with single unified rate limit section
- Update text to say "Free for all users" instead of "varies by tier"
- Remove all `/pricing` links (lines 28, 602, 690)
- Update rate limit section to show single set of limits (currently "Pro" limits)
- Update "Additional Resources" section to remove pricing link

**web/app/app/api/keys/*.ts:**
- Keep tier field in API responses (backwards compatibility)
- Just ensure it always returns "pro"

### 3. Documentation Updates

**README.md:**
- Update "Rate Limits" section (lines 42-56)
  - Remove tier comparison
  - Show single rate limit (10,000/hour, 100,000/day)
  - Remove pricing link
- Update "Features" list (line 64)
  - Change "tier-based rate limiting (free and pro tiers)" to "generous rate limiting"

**docs/AUTHENTICATION_GUIDE.md:**
- Remove "Free Tier" section (lines 124-139)
- Remove "Pro Tier" section (lines 142-155)
- Replace with single "Rate Limits" section showing 10,000/hour, 100,000/day
- Remove pricing links (lines 144, 450, 458)
- Update rate limit headers documentation (lines 170-181)
  - Remove or update `X-RateLimit-Tier` (could just always show "pro")
  - Remove `X-RateLimit-Upgrade-Url` header
- Update error examples (lines 242-257)
  - Remove tier references from error messages
  - Remove upgradeUrl from error responses

**docs/API_ENDPOINTS.md (if exists):**
- Update any rate limit documentation

**CLAUDE.md:**
- Update "System Capabilities" section to reflect free API with generous limits
- Remove any pricing tier references

### 4. Response Headers & Error Messages

**Rate Limit Headers:**
Current headers include:
```
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

**Options:**
1. **Remove tier-specific headers** (recommended)
   - Keep: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
   - Remove: `X-RateLimit-Tier`, `X-RateLimit-Upgrade-Url`

2. **Keep headers but always show "pro"**
   - Change `X-RateLimit-Tier` to always return "pro"
   - Remove `X-RateLimit-Upgrade-Url`

**Recommendation:** Option 1 (remove tier-specific headers) for cleaner API

**Error Messages:**
Update rate limit error responses to remove:
- Tier references ("free tier")
- Upgrade prompts and URLs
- Just show: "Rate limit exceeded. Try again in X minutes."

## Implementation Plan

### Phase 1: Backend Cleanup (15 minutes)

1. **Update default tier:**
   - `ApiKeyInternalController.cs` - Change `Tier = "free"` to `Tier = "pro"`

2. **Optional - Remove tier headers:**
   - Review where `X-RateLimit-Tier` and `X-RateLimit-Upgrade-Url` are set
   - Remove or update to not reference tiers

3. **Test:** Generate a new API key and verify it gets "pro" tier and correct limits

### Phase 2: Frontend Cleanup (30 minutes)

1. **Delete pricing page:**
   ```bash
   rm web/app/app/pricing/page.tsx
   ```

2. **Update Header:**
   - Remove pricing link

3. **Update Dashboard:**
   - Remove tier display
   - Remove upgrade messaging
   - Show simple rate limits

4. **Update Docs page:**
   - Remove tier comparison section
   - Add single "Rate Limits" section
   - Remove all pricing links

5. **Test:**
   - Build Next.js app (`npm run build`)
   - Verify all pages load
   - Check navigation
   - Verify dashboard shows correct info

### Phase 3: Documentation Updates (30 minutes)

1. **Update README.md:**
   - Simplify rate limits section
   - Remove pricing references

2. **Update AUTHENTICATION_GUIDE.md:**
   - Remove tier sections
   - Simplify to single rate limit documentation
   - Update error examples

3. **Update CLAUDE.md:**
   - Update system capabilities

### Phase 4: Testing & Verification (15 minutes)

1. **Backend testing:**
   - Generate new API key → should get "pro" tier
   - Make requests → should have 10,000/hour limit
   - Check rate limit headers → should not reference tiers/pricing

2. **Frontend testing:**
   - All pages load correctly
   - No broken links to /pricing
   - Dashboard shows correct info
   - Docs page is clear and helpful

3. **Documentation verification:**
   - All docs updated
   - No broken links
   - Consistent messaging

## Success Criteria

### Functional Requirements
- ✅ All new API keys default to "pro" tier (10,000/hour, 100,000/day)
- ✅ No pricing page or pricing links anywhere
- ✅ Dashboard shows rate limits without tier context
- ✅ Documentation clearly states "free for all users"
- ✅ All frontend pages build and load correctly

### Technical Requirements
- ✅ Database schema unchanged (Tier field kept)
- ✅ Existing API keys continue to work
- ✅ Rate limiting service works correctly
- ✅ No breaking API changes

### User Experience
- ✅ Clear messaging: "Free API with generous rate limits"
- ✅ Simple onboarding: Sign in → Get API key → Start using
- ✅ No confusion about tiers or pricing
- ✅ Documentation is comprehensive and helpful

## Files to Modify

### Backend
- `src/MarsVista.Api/Controllers/V1/ApiKeyInternalController.cs` - Change default tier
- (Optional) Rate limit header generation - Remove tier-specific headers

### Frontend
- **DELETE:** `web/app/app/pricing/page.tsx`
- `web/app/components/Header.tsx` - Remove pricing link
- `web/app/app/dashboard/page.tsx` - Simplify rate limit display
- `web/app/app/docs/page.tsx` - Remove tier sections, update rate limits

### Documentation
- `README.md` - Update rate limits section
- `docs/AUTHENTICATION_GUIDE.md` - Remove tier sections
- `CLAUDE.md` - Update system capabilities
- (Optional) `docs/API_ENDPOINTS.md` - Update if exists

## Migration Notes

### For Existing Users
- Existing "free" tier users automatically benefit from "pro" limits
- No action required
- Existing API keys continue to work

### Database Considerations
- Tier field remains in database
- Can run migration to update existing "free" keys to "pro" (optional)
- Or just let rate limiting service treat all tiers equally

```sql
-- Optional: Update all free tier users to pro
UPDATE api_keys SET tier = 'pro' WHERE tier = 'free';
```

### Backwards Compatibility
- API responses still include tier field (always "pro")
- Existing integrations continue to work
- No breaking changes

## Future Flexibility

Keeping the Tier field allows for:
- Future paid features (if needed)
- Enterprise custom limits
- A/B testing different rate limits
- Geographic or use-case specific tiers

But for now: **One simple tier for everyone.**

## Why This Approach?

**Benefits:**
1. **Simplicity** - No confusing pricing tiers
2. **Generous** - 10,000/hour is more than enough for most users
3. **Adoption** - Lower barrier to entry
4. **Flexibility** - Can add tiers later if needed
5. **Clean** - Minimal code changes, just UI/docs

**Trade-offs:**
- No revenue from API (but was probably minimal anyway)
- Can't offer "premium" features
- All users get same limits (but that's the goal!)

## Testing Checklist

### Backend
- [ ] New API key gets "pro" tier
- [ ] Rate limits are 10,000/hour, 100,000/day
- [ ] Rate limit headers work correctly
- [ ] Error messages don't reference tiers/pricing

### Frontend
- [ ] `/pricing` returns 404
- [ ] Header doesn't have pricing link
- [ ] Dashboard shows rate limits clearly
- [ ] `/docs` page shows single rate limit
- [ ] No broken links anywhere
- [ ] All pages build successfully

### Documentation
- [ ] README.md updated
- [ ] AUTHENTICATION_GUIDE.md updated
- [ ] CLAUDE.md updated
- [ ] No references to pricing/tiers
- [ ] Clear "free for all" messaging

## Estimated Effort

- Backend changes: 15 minutes
- Frontend changes: 30 minutes
- Documentation: 30 minutes
- Testing: 15 minutes
- **Total: ~90 minutes**

## Notes

- This is a one-way change - can't easily go back to pricing
- Consider announcement/communication to existing users
- Update any external documentation (blog posts, etc.)
- May want to update meta descriptions for SEO
