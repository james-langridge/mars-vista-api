# Decision 004A: Multiple Image URL Storage

**Status:** Active
**Date:** 2025-11-13
**Context:** Story 004 - Define Core Domain Entities

## Context

NASA provides Mars rover images in multiple sizes:
- **Small:** ~320px wide (thumbnails)
- **Medium:** ~800px wide (gallery view)
- **Large:** ~1200px wide (detail view)
- **Full:** Original resolution (download)

The Rails Mars Photo API stores only the full-resolution URL. We need to decide whether to store multiple URLs or generate them on demand.

## Requirements

- **Performance:** Fast image delivery for different use cases
- **Bandwidth:** Minimize data transfer (don't send 5MB images for thumbnails)
- **User Experience:** Images load quickly on all devices
- **Maintainability:** Simple implementation, no complex image processing

## Alternatives

### Alternative 1: Store Only Full URL, Resize on Server

**Implementation:**
```csharp
public class Photo
{
    public string ImgSrcFull { get; set; }
    // Generate other sizes dynamically via API:
    // GET /api/photos/{id}/image?size=small
}
```

**Server-side resize logic:**
```csharp
public async Task<IActionResult> GetImage(int id, string size)
{
    var photo = await context.Photos.FindAsync(id);
    var image = await httpClient.GetAsync(photo.ImgSrcFull);

    return size switch
    {
        "small" => ResizeImage(image, 320),
        "medium" => ResizeImage(image, 800),
        "large" => ResizeImage(image, 1200),
        _ => image
    };
}
```

**Pros:**
- Only one URL stored (saves storage)
- Single source of truth
- Can generate custom sizes (e.g., 640px)

**Cons:**
- **Requires image processing infrastructure**
  - CPU-intensive (resizing 5MB images)
  - Memory intensive (load full image to resize)
  - Need image processing libraries (ImageSharp, SkiaSharp)
- **Adds latency to every request**
  - Download full image from NASA (slow)
  - Resize on server (CPU time)
  - Send to client
  - Could be 2-5 seconds per image
- **Increases server costs**
  - More CPU/memory needed
  - Higher bandwidth (downloading from NASA)
- **Caching complexity**
  - Need to cache resized images
  - Cache invalidation logic
  - CDN configuration

**Cost estimate:**
- ImageSharp license: $1000/year (commercial use)
- Additional server resources: 2-4x current cost
- Storage for cached resizes: 500GB-1TB
- CDN costs: $100-500/month

### Alternative 2: Store Only Full URL, Generate via CDN

**Implementation:**
```csharp
public class Photo
{
    public string ImgSrcFull { get; set; }

    // Frontend uses CDN resize:
    // https://cdn.marsapi.com/resize?url={imgSrcFull}&width=320
}
```

**Use CDN image transformation** (e.g., Cloudflare Images, Imgix):

**Pros:**
- Offload processing to CDN
- Automatic caching and optimization
- Global edge distribution
- WebP/AVIF conversion

**Cons:**
- **Additional service dependency**
- **Monthly costs:**
  - Cloudflare Images: $5/1000 images + $1/1000 transformations
  - Imgix: $99/month for 20GB bandwidth
- **Vendor lock-in** to CDN provider
- **Complexity** - need to proxy NASA URLs through CDN
- **NASA URLs might not be accessible** from CDN (CORS, authentication)

**For 1M photos:**
- Cloudflare: $5,000 + $1/1000 transforms = ~$6,000/month at scale
- Imgix: $300+/month depending on traffic

### Alternative 3: Store All URLs Provided by NASA (RECOMMENDED)

**Implementation:**
```csharp
public class Photo
{
    public string ImgSrcSmall { get; set; }   // NASA's 320px
    public string ImgSrcMedium { get; set; }  // NASA's 800px
    public string ImgSrcLarge { get; set; }   // NASA's 1200px
    public string ImgSrcFull { get; set; }    // NASA's full-res
}
```

**API returns appropriate URL:**
```csharp
// Gallery endpoint returns medium URLs
GET /api/photos?size=medium
{
  "photos": [
    {
      "id": 1,
      "img_src": "https://mars.nasa.gov/.../NLB_458574869EDR_F0541800NCAM00354M_-M800.JPG"
    }
  ]
}
```

**Pros:**
- **Zero processing cost** - NASA already did the work
- **Zero latency** - direct links to NASA
- **Zero infrastructure** - no image processing needed
- **Reliable** - NASA's CDN infrastructure
- **Simple implementation** - just store URLs
- **Better UX:**
  - Mobile app loads 320px thumbnails (fast)
  - Desktop loads 1200px images (good quality)
  - Photographers download full-res (original quality)

**Cons:**
- **Storage overhead:** 4 URLs vs 1 URL
  - Each URL ~200 characters = 800 bytes
  - For 1M photos: 800MB
  - Negligible cost ($0.02/month on S3)
- **NASA might not provide all sizes for all photos**
  - Solution: Fall back to full URL if others missing
  - Can generate missing sizes later if needed

**NASA URL Examples:**

```
Small:  https://mars.nasa.gov/.../NLB_458574869EDR_F0541800NCAM00354M_-S320.JPG
Medium: https://mars.nasa.gov/.../NLB_458574869EDR_F0541800NCAM00354M_-M800.JPG
Large:  https://mars.nasa.gov/.../NLB_458574869EDR_F0541800NCAM00354M_-L1200.JPG
Full:   https://mars.nasa.gov/.../NLB_458574869EDR_F0541800NCAM00354M_.JPG
```

### Alternative 4: Store Only Small + Full

**Implementation:**
```csharp
public class Photo
{
    public string ImgSrcSmall { get; set; }  // For thumbnails
    public string ImgSrcFull { get; set; }   // For everything else
}
```

**Reasoning:**
- Small for gallery thumbnails
- Full for detail view
- Skip medium/large (users can load full)

**Pros:**
- Saves 400 bytes per photo (2 fewer URLs)
- Simpler schema

**Cons:**
- **Poor UX on mobile:**
  - Loading 5MB full image on mobile data
  - Slow load times
  - Expensive for users on metered connections
- **Wasted bandwidth:**
  - Sending 5MB when 1MB (large) would suffice
  - NASA pays for bandwidth too

**Use case analysis:**
- Thumbnails (320px): 30KB → good
- Gallery view (needs 800px): loads 5MB full → BAD
- Detail view (needs 1200px): loads 5MB full → acceptable
- Download: 5MB full → good

This approach fails for gallery view, which is the primary use case.

## Decision

**Store all URLs provided by NASA (Alternative 3)**

### Rationale

1. **NASA already provides the URLs** - they're not generated or computed
2. **Zero infrastructure cost** - no servers, CDN, or image processing
3. **Optimal user experience:**
   - Mobile gallery: loads 320px (30KB) → instant
   - Desktop gallery: loads 800px (200KB) → fast
   - Detail view: loads 1200px (800KB) → good quality
   - Download: loads full (5MB) → original quality
4. **Storage cost is negligible:** 800 bytes × 1M photos = 800MB = $0.02/month
5. **Simpler code:** No image processing, no CDN integration, no caching

### Trade-offs

**Accepted:**
- 800 bytes per photo storage (4 URLs vs 1 URL)
- Some photos might not have all sizes (handle gracefully)

**Gained:**
- Fast image loads across all device types
- Zero server-side processing
- Zero additional infrastructure
- Simple, maintainable code
- Reliable (NASA's infrastructure)

### Storage Cost Analysis

For 1 million photos:
- 4 URLs × 200 characters = 800 bytes
- 1M photos × 800 bytes = 800MB
- PostgreSQL storage: $0.025/GB/month
- **Total cost: $0.02/month**

Compared to alternatives:
- Server-side resize: $500-1000/month (servers + ImageSharp)
- CDN resize: $300-6000/month (depending on traffic)
- Store all URLs: $0.02/month ✅

**Return on complexity:**
- Saving $0.02/month storage
- Cost: 100+ hours of engineering (image processing, caching, CDN)
- Ongoing: Higher server costs, more complex deployments

Not worth it.

## Implementation

### Entity

```csharp
public class Photo
{
    public string ImgSrcSmall { get; set; } = string.Empty;   // 320px
    public string ImgSrcMedium { get; set; } = string.Empty;  // 800px
    public string ImgSrcLarge { get; set; } = string.Empty;   // 1200px
    public string ImgSrcFull { get; set; } = string.Empty;    // Full-res
}
```

### API Response

```csharp
public class PhotoDto
{
    public int Id { get; set; }
    public string ImgSrc { get; set; }  // Selected based on ?size= param

    public static PhotoDto FromEntity(Photo photo, string size = "medium")
    {
        return new PhotoDto
        {
            Id = photo.Id,
            ImgSrc = size switch
            {
                "small" => photo.ImgSrcSmall ?? photo.ImgSrcFull,
                "medium" => photo.ImgSrcMedium ?? photo.ImgSrcLarge ?? photo.ImgSrcFull,
                "large" => photo.ImgSrcLarge ?? photo.ImgSrcFull,
                "full" => photo.ImgSrcFull,
                _ => photo.ImgSrcMedium ?? photo.ImgSrcFull
            }
        };
    }
}
```

### Fallback Strategy

If NASA doesn't provide a specific size:
1. Try requested size
2. Fall back to next larger size
3. Fall back to full if no others available

```csharp
// Graceful degradation
var imgSrc = size switch
{
    "small" => photo.ImgSrcSmall ?? photo.ImgSrcMedium ?? photo.ImgSrcFull,
    "medium" => photo.ImgSrcMedium ?? photo.ImgSrcLarge ?? photo.ImgSrcFull,
    "large" => photo.ImgSrcLarge ?? photo.ImgSrcFull,
    _ => photo.ImgSrcFull
};
```

## Validation Criteria

Success metrics:
- Gallery loads quickly on mobile (320px images)
- Detail view shows high quality (1200px images)
- Download provides original files (full resolution)
- No server-side image processing needed
- No additional infrastructure costs

## References

- [NASA Mars Photos RSS](https://mars.nasa.gov/rss/api/)
- [Cloudflare Images Pricing](https://www.cloudflare.com/products/cloudflare-images/)
- [Imgix Pricing](https://www.imgix.com/pricing)
- [ImageSharp Licensing](https://sixlabors.com/pricing/)

## Related Decisions

- **Decision 004:** Entity field selection (this is a subset)
- **Future:** CDN strategy for caching NASA images (if needed)

## Notes

### Real-World Usage Patterns

From typical photo gallery applications:
- **Thumbnails (320px):** 70% of requests (gallery browsing)
- **Medium (800px):** 20% of requests (lightbox view)
- **Large (1200px):** 8% of requests (detail view)
- **Full (original):** 2% of requests (download)

Having multiple sizes optimizes for the 90% use case (thumbnails + medium).

### Bandwidth Savings

Example photo:
- Small (320px): 30KB
- Medium (800px): 200KB
- Large (1200px): 800KB
- Full (5MB): 5MB

Gallery with 50 photos:
- Using small: 50 × 30KB = 1.5MB ✅
- Using full: 50 × 5MB = 250MB ❌

**166x bandwidth savings** by using appropriate sizes.

### Future Considerations

If we ever need custom sizes:
1. Generate once during import (async job)
2. Store generated URLs in database
3. Fall back to NASA URLs if custom not available

But based on analysis, NASA's provided sizes cover all use cases.
