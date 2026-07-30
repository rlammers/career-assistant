using System.Reflection;
using System.Text.Json;
using CareerAssistant.Api.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CareerAssistant.Api.Tests;

public class AzureSqlUnavailableExceptionHandlerTests
{
    [Fact]
    public async Task DatabaseUnavailableErrorReturnsSanitizedServiceUnavailableResponse()
    {
        var handler = new AzureSqlUnavailableExceptionHandler(
            NullLogger<AzureSqlUnavailableExceptionHandler>.Instance);
        var context = CreateHttpContext();
        var exception = CreateSqlException(
            AzureSqlUnavailableExceptionHandler.DatabaseUnavailableErrorNumber,
            "private-server",
            "private-database");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(
            AzureSqlUnavailableExceptionHandler.RetryAfterSeconds.ToString(),
            context.Response.Headers.RetryAfter);

        var problem = await ReadProblemDetailsAsync(context);
        Assert.Equal("The database is temporarily unavailable.", problem.Title);
        Assert.Equal("The database is starting. Retry the request shortly.", problem.Detail);
        Assert.DoesNotContain("private-server", problem.Title + problem.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("private-database", problem.Title + problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WrappedDatabaseUnavailableErrorIsHandled()
    {
        var handler = new AzureSqlUnavailableExceptionHandler(
            NullLogger<AzureSqlUnavailableExceptionHandler>.Instance);
        var context = CreateHttpContext();
        var exception = new InvalidOperationException(
            "Retry limit exceeded.",
            CreateSqlException(
                AzureSqlUnavailableExceptionHandler.DatabaseUnavailableErrorNumber,
                "private-server",
                "private-database"));

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnrelatedSqlErrorIsNotHandled()
    {
        var handler = new AzureSqlUnavailableExceptionHandler(
            NullLogger<AzureSqlUnavailableExceptionHandler>.Instance);
        var context = CreateHttpContext();
        var exception = CreateSqlException(2627, "private-server", "private-database");

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Assert.IsType<ProblemDetails>(problem);
    }

    private static SqlException CreateSqlException(int number, string server, string database)
    {
        var errorCollection = Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)
            ?? throw new InvalidOperationException("Could not create SqlErrorCollection.");
        var errorConstructor = typeof(SqlError)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .OrderByDescending(constructor => constructor.GetParameters().Length)
            .First();
        var errorArguments = errorConstructor.GetParameters()
            .Select(parameter => CreateArgument(parameter, number, server, database))
            .ToArray();
        var error = errorConstructor.Invoke(errorArguments);
        var addMethod = typeof(SqlErrorCollection).GetMethod(
            "Add",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find SqlErrorCollection.Add.");
        addMethod.Invoke(errorCollection, [error]);

        var createMethod = typeof(SqlException)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.Name == "CreateException")
            .OrderBy(method => method.GetParameters().Length)
            .First();
        var exceptionArguments = createMethod.GetParameters()
            .Select(parameter =>
            {
                if (parameter.ParameterType == typeof(SqlErrorCollection))
                {
                    return errorCollection;
                }

                if (parameter.ParameterType == typeof(string))
                {
                    return "test-version";
                }

                return parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
            })
            .ToArray();

        return Assert.IsType<SqlException>(createMethod.Invoke(null, exceptionArguments));
    }

    private static object? CreateArgument(
        ParameterInfo parameter,
        int number,
        string server,
        string database)
    {
        if (parameter.ParameterType == typeof(int))
        {
            return parameter.Name?.Contains("infoNumber", StringComparison.OrdinalIgnoreCase) == true
                ? number
                : 0;
        }

        if (parameter.ParameterType == typeof(byte))
        {
            return (byte)0;
        }

        if (parameter.ParameterType == typeof(uint))
        {
            return 0u;
        }

        if (parameter.ParameterType == typeof(string))
        {
            return parameter.Name?.Contains("server", StringComparison.OrdinalIgnoreCase) == true
                ? server
                : $"Database '{database}' is unavailable.";
        }

        return parameter.HasDefaultValue
            ? parameter.DefaultValue
            : parameter.ParameterType.IsValueType
                ? Activator.CreateInstance(parameter.ParameterType)
                : null;
    }
}
