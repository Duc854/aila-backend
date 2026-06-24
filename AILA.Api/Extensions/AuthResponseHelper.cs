using Shared.Wrappers;
using System.Text.Json;

namespace AILA.Api.Extensions
{
    public static class AuthResponseHelper
    {
        public static async Task WriteCustomResponseAsync(HttpContext context, int statusCode, string errorCode, string errorMessage)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            // Dùng đúng cấu trúc ResponseDto của hệ thống
            var responseDto = ResponseDto<object>.FailResult(errorCode, errorMessage);

            // Giữ chuẩn CamelCase cho JSON trả về Frontend
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonResult = JsonSerializer.Serialize(responseDto, jsonOptions);

            await context.Response.WriteAsync(jsonResult);
        }
    }
}
