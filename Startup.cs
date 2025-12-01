using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AspNetCoreRateLimit;
using System.Text.Json.Serialization;

namespace DispatchPublic
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add CORS for public access
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Add HTTP context accessor
            services.AddHttpContextAccessor();

            // Configure Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Register services
            services.AddScoped<DispatchPublic.Services.ContextService>();
            services.AddScoped<DispatchPublic.Services.DataServiceClient>();
            services.AddScoped<DispatchPublic.Services.InvoiceServiceClient>();

            // Configure rate limiting
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Register HttpClient for service communication
            services.AddHttpClient<DispatchPublic.Services.DataServiceClient>(client =>
            {
                var dispatchDataUrl = Configuration.GetValue<string>("Services:DispatchData")
                    ?? throw new InvalidOperationException("Services:DispatchData configuration is required");
                client.BaseAddress = new Uri(dispatchDataUrl);
            });

            services.AddHttpClient<DispatchPublic.Services.InvoiceServiceClient>(client =>
            {
                var dispatchInvoiceUrl = Configuration.GetValue<string>("Services:DispatchInvoice")
                    ?? throw new InvalidOperationException("Services:DispatchInvoice configuration is required");
                client.BaseAddress = new Uri(dispatchInvoiceUrl);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Enable Swagger in development
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Use CORS
            app.UseCors("AllowAll");

            app.UseRouting();

            // Enable rate limiting
            app.UseIpRateLimiting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
