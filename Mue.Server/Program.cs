using Mue.Server.Core;
using Mue.Server.Core.Utils;
using Mue.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Services.ConfigureMueServices();

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(config =>
{
    Json.UpdateJsonConfig(config.SerializerSettings);
});
builder.Services.AddSignalR().AddNewtonsoftJsonProtocol(config =>
{
    config.PayloadSerializerSettings = Json.JsonConfig;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MueConnectionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseWebSockets();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MueClientHub>("/mueclient");
    endpoints.MapControllers();
});

app.Run();
