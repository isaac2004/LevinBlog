using System.Linq;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LevinBlog.Database;
using LevinBlog.Model;
using LevinBlog.Repository;
using LevinBlog.Service;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Pioneer.Blog.Service;
using Robotify.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights.SnapshotCollector;

namespace LevinBlog.Web
{
  public class Startup
  {
    public static void Main()
    {
      var host = new WebHostBuilder()
          .UseKestrel()
            .ConfigureLogging(factory =>
            {
              factory.AddConsole();
              factory.AddDebug();
            })
          .UseContentRoot(Directory.GetCurrentDirectory())
          .UseIISIntegration()
          .UseStartup<Startup>()
          .Build();

      host.Run();
    }
    private class SnapshotCollectorTelemetryProcessorFactory : ITelemetryProcessorFactory
    {
      private readonly IServiceProvider _serviceProvider;

      public SnapshotCollectorTelemetryProcessorFactory(IServiceProvider serviceProvider) =>
          _serviceProvider = serviceProvider;

      public ITelemetryProcessor Create(ITelemetryProcessor next)
      {
        var snapshotConfigurationOptions = _serviceProvider.GetService<IOptions<SnapshotCollectorConfiguration>>();
        return new SnapshotCollectorTelemetryProcessor(next, configuration: snapshotConfigurationOptions.Value);
      }
    }

    public Startup(IConfiguration config, IHostingEnvironment env)
    {

      var configRoot = (IConfigurationRoot)config;
      configRoot.Providers.ToList().Clear();

      var builder = new ConfigurationBuilder()
           .SetBasePath(env.ContentRootPath)
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
           .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
           .AddEnvironmentVariables();
      Configuration = builder.Build();
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      // Add framework services.
      services.AddCors();
      services.AddMvc();
      services.Configure<SnapshotCollectorConfiguration>(Configuration.GetSection(nameof(SnapshotCollectorConfiguration)));

      // Add SnapshotCollector telemetry processor.
      services.AddSingleton<ITelemetryProcessorFactory>(sp => new SnapshotCollectorTelemetryProcessorFactory(sp));

      services.AddNodeServices();
      services.AddMemoryCache();
      services.AddRobotify(Configuration);
      services.AddDbContext<BlogContext>(options =>
               options.UseSqlServer(Configuration.GetConnectionString("BlogDatabase")));

      services.Configure<AppConfiguration>(Configuration.GetSection("AppConfiguration"));

      // configure DI for application services
      // Repositories
      services.AddTransient<IContactRepository, ContactRepository>();
      services.AddTransient<ICategoryRepository, CategoryRepository>();
      services.AddTransient<ITagRepository, TagRepository>();
      services.AddTransient<IPostRepository, PostRepository>();
      services.AddTransient<IPostTagRepository, PostTagRepository>();
      services.AddTransient<IUserRepository, UserRepository>();
      // Services
      services.AddTransient<ICategoryService, CategoryService>();
      services.AddTransient<IPostService, PostService>();
      services.AddTransient<IPostTagService, PostTagService>();
      services.AddTransient<ISearchService, SearchService>();
      services.AddTransient<ICommunicationService, CommunicationService>();
      services.AddTransient<ISiteMapService, SiteMapService>();
      services.AddTransient<IRSSFeedService, RSSFeedService>();
      services.AddTransient<ITagService, TagService>();
      services.AddTransient<IUserService, UserService>();

      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(o =>
      {
        o.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          ValidateIssuer = false,
          ValidateAudience = false,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["AppConfiguration:Key"])),
        };
        o.Events = new JwtBearerEvents()
        {
          OnAuthenticationFailed = c =>
          {
            c.NoResult();

            c.Response.StatusCode = 500;
            c.Response.ContentType = "text/plain";
            return c.Response.WriteAsync("An error occurred processing your authentication.");
          }
        };
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      // global cors policy
      app.UseCors(x => x
          .AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());

      app.UseStaticFiles(new StaticFileOptions
      {
        OnPrepareResponse = context =>
        {
          var headers = context.Context.Response.GetTypedHeaders();
          headers.CacheControl = new CacheControlHeaderValue
          {
            MaxAge = TimeSpan.FromSeconds(31536000)
          };
        }
      });

      ServiceMapperConfig.Config();
      app.UseAuthentication();
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
        {
          HotModuleReplacement = true,
          HotModuleReplacementEndpoint = "/dist/__webpack_hmr"
        });
        TelemetryConfiguration.Active.DisableTelemetry = true;
      }
      app.UseRobotify();
      app.UseMvc(routes =>
      {
        routes.MapRoute(
         name: "default",
         template: "{controller=Home}/{action=Index}/{id?}");

        routes.MapRoute(
         "Sitemap",
         "sitemap.xml",
         new { controller = "Home", action = "Sitemap" });

        routes.MapRoute(
        "feed",
        "rssfeed.xml",
        new { controller = "Home", action = "RSSFeed" });

        routes.MapSpaFallbackRoute(
          name: "spa-fallback",
          defaults: new { controller = "Home", action = "Index" });

      });
      app.UseExceptionHandler("/Home/Error");
    }
  }
}
