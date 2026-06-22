using AILA.Api.Configurations;
using AILA.Application;
using AILA.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký DI của Tầng Application (MediatR, Business Services)
builder.Services.AddApplication();

// Đăng ký DI của Tầng Infrastructure (DB Context, UoW, Hash, Token)
builder.Services.AddInfrastructure(builder.Configuration);

// Đăng ký Cơ chế Xác thực JWT từ file cấu hình cũ của bạn
builder.Services.AddCustomAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
