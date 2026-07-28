using Shared.Wrappers;
using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace AILA.Api.Middlewares
{
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await HandleValidationException(context, ex);
            }
            catch (ArgumentException ex)
            {
                await WriteError(
                    context,
                    HttpStatusCode.BadRequest,
                    "BAD_REQUEST",
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteError(
                    context,
                    HttpStatusCode.BadRequest,
                    "INVALID_OPERATION",
                    ex.Message);
            }
            catch (Exception ex)
            {
                await WriteError(
                    context,
                    HttpStatusCode.InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    ex.Message);
            }
        }

        private static async Task HandleValidationException(
            HttpContext context,
            ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = ResponseDto<object>.FailResult(
                "VALIDATION_ERROR",
                string.Join("; ", ex.Errors.Select(x => x.ErrorMessage)));

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }

        private static async Task WriteError(
            HttpContext context,
            HttpStatusCode statusCode,
            string code,
            string message)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var response = ResponseDto<object>.FailResult(code, message);

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
