# Decision 006A: HTTP Resilience Strategy

**Status:** Active
**Date:** 2025-11-13
**Story:** 006 - NASA API Scraper Service

## Context

When scraping photos from NASA's APIs, we will encounter transient failures:
- Network timeouts
- Server errors (500, 502, 503, 504)
- Rate limiting (429 Too Many Requests)
- Temporary service unavailability

How should we handle these failures gracefully?

## Options Considered

### Option 1: Polly with Retry + Circuit Breaker (Recommended)

Use Polly library for resilience policies:

```csharp
builder.Services.AddHttpClient("NASA")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));
}
```

**Pros:**
- Industry-standard resilience library
- Declarative policy configuration
- Exponential backoff prevents overwhelming servers
- Circuit breaker prevents cascading failures
- Integrates seamlessly with HttpClientFactory
- Extensive testing and battle-proven
- Observability (onRetry callbacks)

**Cons:**
- Additional NuGet dependency
- Learning curve for policy syntax

### Option 2: Manual Retry Logic

Implement retry with try/catch:

```csharp
for (int i = 0; i < 3; i++)
{
    try
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return response;

        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
    }
    catch (HttpRequestException)
    {
        if (i == 2) throw;
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
    }
}
```

**Pros:**
- No external dependencies
- Full control over logic
- Simple to understand

**Cons:**
- Boilerplate in every HTTP call
- Error-prone (easy to get wrong)
- No circuit breaker
- Hard to test
- No observability
- Code duplication

### Option 3: Azure Resilience Library

Use Microsoft's newer resilience library:

```csharp
builder.Services.AddHttpClient("NASA")
    .AddResilienceHandler("pipeline", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions());
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());
    });
```

**Pros:**
- First-party Microsoft library
- Modern .NET 8+ features
- Integrated with telemetry

**Cons:**
- Very new (introduced 2023)
- Less documentation than Polly
- Smaller community
- May have breaking changes
- Polly is more established

### Option 4: No Resilience (Fail Fast)

Just let failures happen:

```csharp
var response = await client.GetAsync(url);
response.EnsureSuccessStatusCode();
```

**Pros:**
- Simplest code
- Fast failure detection

**Cons:**
- Scraper fails on transient errors
- No automatic recovery
- Wastes developer time investigating transient issues
- Poor user experience
- NASA API has occasional blips

## Decision

**Use Option 1: Polly with Retry + Circuit Breaker**

## Reasoning

### Why Polly?

1. **Industry Standard:**
   - Used by Microsoft, AWS, Azure services
   - Battle-tested in production
   - 7000+ stars on GitHub
   - Active maintenance

2. **Exponential Backoff:**
   - Retry: wait 2s, 4s, 8s
   - Prevents overwhelming NASA's servers
   - Respects server recovery time
   - Industry best practice

3. **Circuit Breaker:**
   - After 5 consecutive failures, stop trying for 1 minute
   - Prevents cascading failures
   - Fail fast when service is down
   - Automatic reset after break duration
   - Saves resources (no wasted timeouts)

4. **Observability:**
   ```csharp
   onRetry: (outcome, timespan, retryCount, context) =>
   {
       _logger.LogWarning("Retry {RetryCount} after {Delay}ms",
           retryCount, timespan.TotalMilliseconds);
   }
   ```

5. **Transient Error Handling:**
   - Automatically retries: 500, 502, 503, 504, 408
   - Network failures (timeouts, DNS)
   - Custom: 429 Too Many Requests
   - Doesn't retry: 400, 401, 403, 404 (permanent failures)

### Retry Policy Configuration

```csharp
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, network failures
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Request failed. Waiting {timespan} before retry {retryCount}...");
            });
}
```

**Timeline:**
- Initial request fails
- Wait 2 seconds, retry 1
- Wait 4 seconds, retry 2
- Wait 8 seconds, retry 3
- If still failing, throw exception
- Total: ~14 seconds before giving up

### Circuit Breaker Configuration

```csharp
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1));
}
```

**How it works:**
1. Normal: All requests go through
2. After 5 consecutive failures: Circuit **opens**
   - All requests fail immediately (no waiting)
   - Saves time and resources
3. After 1 minute: Circuit goes **half-open**
   - Next request is allowed (test)
   - If succeeds: circuit closes (back to normal)
   - If fails: circuit opens again for 1 minute

**Why this is great:**
- If NASA API is down, we know in 5 requests
- Stop wasting time on timeouts
- Automatic recovery when service returns

### Combined Effect

```
Request 1: Fail → Retry (2s, 4s, 8s) → Fail (count: 1)
Request 2: Fail → Retry (2s, 4s, 8s) → Fail (count: 2)
Request 3: Fail → Retry (2s, 4s, 8s) → Fail (count: 3)
Request 4: Fail → Retry (2s, 4s, 8s) → Fail (count: 4)
Request 5: Fail → Retry (2s, 4s, 8s) → Fail (count: 5)
Circuit OPENS (1 minute break)
Requests 6-N: Fail immediately (no retries, no waiting)
After 1 minute: Circuit half-open
Request N+1: Try again...
```

## Implementation

### Install Packages

```bash
dotnet add package Microsoft.Extensions.Http.Polly
dotnet add package Polly
```

### Register HTTP Client

```csharp
// In Program.cs
builder.Services.AddHttpClient("NASA", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaAPI/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());
```

### Use in Scraper

```csharp
public class PerseveranceScraper : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<int> ScrapeAsync()
    {
        // Polly policies applied automatically
        var client = _httpClientFactory.CreateClient("NASA");
        var response = await client.GetStringAsync(ApiUrl);
        // ...
    }
}
```

## Trade-offs Accepted

### Dependency on External Library
- **Accepted:** Polly NuGet package dependency
- **Why it's OK:** Industry standard, well-maintained, small footprint
- **Risk:** Library abandonment (very low - Microsoft uses it)

### Increased Latency on Failures
- **Accepted:** Up to 14 seconds on transient failures (2s + 4s + 8s)
- **Why it's OK:** Scraping is background operation, not user-facing
- **Alternative:** Fail fast means manual re-run (worse UX)

### Circuit Breaker Blocks Valid Requests
- **Accepted:** When circuit is open, all requests fail immediately
- **Why it's OK:** NASA API truly down, retrying wastes resources
- **Mitigation:** 1-minute break is short, automatic recovery

## Alternatives Rejected

### Why Not Manual Retry? (Option 2)
- Boilerplate code in every HTTP call
- Easy to implement incorrectly
- No circuit breaker
- No observability

### Why Not Azure Resilience? (Option 3)
- Too new (2023), less battle-tested
- Smaller community
- Polly is the established choice
- Can migrate later if needed

### Why Not Fail Fast? (Option 4)
- NASA API has transient errors
- Scraper would fail unnecessarily
- Manual re-runs waste time
- Poor operational experience

## Validation

This strategy is validated by:
- ✅ Transient errors (500, timeout) recovered automatically
- ✅ Permanent errors (404) fail immediately (no retry)
- ✅ Circuit breaker prevents cascading failures
- ✅ Exponential backoff respects server load
- ✅ Observability through retry callbacks
- ✅ Used by major services (Azure, AWS)

## Real-World Scenarios

### Scenario 1: Temporary Network Blip
```
Request → Network timeout
Retry 1 (2s) → Success ✅
Total time: ~2 seconds
Result: Photo scraped successfully
```

### Scenario 2: NASA Server Overloaded
```
Request → 503 Service Unavailable
Retry 1 (2s) → 503
Retry 2 (4s) → 503
Retry 3 (8s) → 200 OK ✅
Total time: ~14 seconds
Result: Photo scraped after server recovered
```

### Scenario 3: NASA API Down
```
Request 1 → Fail (3 retries, 14s)
Request 2 → Fail (3 retries, 14s)
Request 3 → Fail (3 retries, 14s)
Request 4 → Fail (3 retries, 14s)
Request 5 → Fail (3 retries, 14s)
Circuit OPENS (after 70 seconds total)
Request 6-100 → Fail immediately (0s each) ✅
After 1 minute → Circuit half-open
Request 101 → Try again...

Saved: (100 - 5) * 14s = 1330 seconds (22 minutes!)
```

## Related Decisions

- [Decision 006: Scraper Service Pattern](006-scraper-service-pattern.md) - Scrapers using HTTP client
- [Decision 006B: Duplicate Photo Detection](006b-duplicate-detection.md) - Idempotency

## References

- [Polly Documentation](https://github.com/App-vNext/Polly)
- [Circuit Breaker Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)
- [Exponential Backoff](https://en.wikipedia.org/wiki/Exponential_backoff)
- [Transient Fault Handling](https://learn.microsoft.com/en-us/azure/architecture/best-practices/transient-faults)
- [HttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
