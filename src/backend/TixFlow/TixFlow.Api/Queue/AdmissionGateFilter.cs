namespace TixFlow.Api.Queue;

public class AdmissionGateFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var admissionHeader = httpContext.Request.Headers["X-Admission-Token"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(admissionHeader))
            return Results.Json(
                new { error = "Missing admission token. Join the queue at POST /queue/{eventId}/join and wait to be admitted." },
                statusCode: 403);

        var tokenService = httpContext.RequestServices.GetRequiredService<AdmissionTokenService>();
        var claims = tokenService.ValidateAdmissionToken(admissionHeader);

        if (claims is null)
            return Results.Json(
                new { error = "Admission token is invalid or expired. Rejoin the queue to get a new one." },
                statusCode: 403);

        var userSub = httpContext.User.FindFirst("sub")?.Value;
        if (userSub is null || !Guid.TryParse(userSub, out var authenticatedUserId)
                            || authenticatedUserId != claims.UserId)
            return Results.Json(
                new { error = "Admission token does not belong to the authenticated user." },
                statusCode: 403);

        var queueService = httpContext.RequestServices.GetRequiredService<IQueueStore>();
        var (valid, consumed) = await queueService.ValidateAdmissionTokenAsync(
            claims.TokenId, claims.UserId, claims.EventId);

        if (!valid)
            return Results.Json(
                new { error = "Admission token not found. It may have expired." },
                statusCode: 403);

        if (consumed)
            return Results.Json(
                new { error = "Admission token has already been used." },
                statusCode: 403);

        await queueService.ConsumeAdmissionTokenAsync(claims.TokenId);

        httpContext.Items["AdmissionEventId"] = claims.EventId;
        httpContext.Items["AdmissionUserId"] = claims.UserId;

        return await next(context);
    }
}
