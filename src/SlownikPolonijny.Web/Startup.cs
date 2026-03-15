using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using SlownikPolonijny.Dal;
using SlownikPolonijny.Web.Services;

namespace SlownikPolonijny.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        var dalProvider = Configuration.GetValue<string>("Dal:Provider") ?? "Mongo";

        if (dalProvider.Equals("Json", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<JsonRepositorySettings>(Configuration.GetSection("Dal:Json"));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<JsonRepositorySettings>>().Value);
            services.AddSingleton<IRepository, JsonRepository>();
            services.AddSingleton<IEntryAuditor>(resolver =>
                new EntryAuditor(resolver.GetRequiredService<IRepository>()));
        }
        else
        {
            services.Configure<MongoRepositorySettings>(Configuration.GetSection("Mongo"));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<MongoRepositorySettings>>().Value);
            services.AddSingleton<IRepository, MongoRepository>();
            services.AddSingleton<IEntryAuditor>(resolver =>
                new EntryAuditor(resolver.GetRequiredService<IRepository>()));
        }

        services.AddRouting(options =>
        {
            options.ConstraintMap["dashed"] = typeof(DashedParameterTransformer);
        });

        services.AddMemoryCache();
        services.AddCors();

        var usersFile = Configuration.GetValue<string>("Auth:UsersFile") ?? "data/users.json";
        services.AddSingleton(new FileUserService(usersFile));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("@admin"));
        });

        services.AddControllersWithViews();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");
        app.UseStaticFiles();

        app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true) // allow any origin
        );
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}
