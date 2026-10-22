using System.Text.Json;
using Ingredients.Api.ExceptionHandling;
using Ingredients.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Ingredients.Api.Tests.ExceptionHandling;

public sealed class ValidationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithValidationException_WritesValidationProblemDetails()
    {
        await using var services = new ServiceCollection().AddOptions().AddProblemDetails().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Response.Body = new MemoryStream();
        var handler = new ValidationExceptionHandler(services.GetRequiredService<IProblemDetailsService>());
        var exception = new ValidationException(
            new Dictionary<string, string[]> { ["UnitOfMeasure"] = ["Unit of measure is invalid."] });

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<HttpValidationProblemDetails>(
            httpContext.Response.Body,
            JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        Assert.Equal("Unit of measure is invalid.", Assert.Single(problemDetails.Errors["UnitOfMeasure"]));
    }

    [Fact]
    public async Task TryHandleAsync_WithOtherException_DoesNotHandle()
    {
        var handler = new ValidationExceptionHandler(Substitute.For<IProblemDetailsService>());
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("Boom"), CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }
}
