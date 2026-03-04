using AuraWellness.Application.Interfaces.External;
using AuraWellness.Domain.Interfaces;
using AuraWellness.Infrastructure.Grpc;
using AuraWellness.Infrastructure.Identity;
using AuraWellness.Infrastructure.Persistence;
using AuraWellness.Infrastructure.Persistence.Repositories;
using AuraWellness.Infrastructure.Redis;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Proto = AuraWellness.ChatService.V1;

namespace AuraWellness.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis
        var redisConn = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(redisConn);
            opts.AllowAdmin = true;
            return ConnectionMultiplexer.Connect(opts);
        });
        services.AddScoped<IRedisResetter, RedisResetter>();

        // Repositories
        services.AddScoped<IDatabaseResetter, DatabaseResetter>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IBuStaffProfileRepository, BuStaffProfileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Identity services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // gRPC client for chat service (lazily resolved so tests can replace IChatServiceClient)
        services.AddSingleton(_ =>
        {
            var grpcUrl = configuration["ChatService:GrpcUrl"]
                ?? throw new InvalidOperationException("ChatService:GrpcUrl not configured");
            var apiKey = configuration["ChatService:InternalApiKey"]
                ?? throw new InvalidOperationException("ChatService:InternalApiKey not configured");
            var handler = new ApiKeyHandler(apiKey)
            {
                InnerHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true }
            };
            var httpClient = new HttpClient(handler);
            var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions { HttpClient = httpClient });
            return new Proto.ChatService.ChatServiceClient(channel);
        });

        services.AddScoped<IChatServiceClient, ChatServiceClient>();

        return services;
    }
}
