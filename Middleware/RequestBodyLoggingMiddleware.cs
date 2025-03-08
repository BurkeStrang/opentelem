using System.Text;

namespace opentelem.Middleware;

public class RequestBodyLoggingMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable buffering so that the request body can be read more than once.
        context.Request.EnableBuffering();

        // Read the request body as a string.
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Reset position for downstream middleware


        // Optionally attach the body to the current Activity (if available)
        var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("http.request.body", body);

        // Continue processing the pipeline
        await _next(context);
    }
}
