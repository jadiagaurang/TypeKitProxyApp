using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TypeKitProxyApp {
    public class Startup {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers();

            // To check Uptime
            services.AddHealthChecks();

            // ConfigurationManager
            services.AddSingleton<IConfiguration>(this.Configuration);

            // In Memory Local Cache
            services.AddMemoryCache();

            // Memcache
            services.AddEnyimMemcached(this.Configuration, "EnyimMemcached");
            services.AddSingleton<IMemcacheService, MemcacheManager>();

            // AWS S3
            services.AddSingleton<IS3Service, S3Service>();

            // TypeKit Service
            services.AddSingleton<ITypeKitService, TypeKitService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            app.UseHealthChecks("/health");

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/TypeKit/CatchAll");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseStaticFiles();
            //app.UseAuthorization();
            
            app.UseHttpsRedirection();
            app.Use(async (context, next) => {
                context.Response.Headers.Add("cache-control", "public, max-age=31536000, s-maxage=31536000");
                await next.Invoke();
            });

            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=TypeKit}/{action=Index}/{TypeKitCode?}"
                );
            });
        }
    }
}
