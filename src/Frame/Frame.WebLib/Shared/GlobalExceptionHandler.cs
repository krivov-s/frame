using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Frame.WebLib.Shared
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger = logger;

        //public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        //{
        //    _logger = logger;
        //}

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception, "Exception: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error",
                Detail = exception.Message
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
