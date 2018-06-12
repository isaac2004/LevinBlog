using ApplicationInsightsTelemetryExtensions;
using LevinBlog.Database;
using LevinBlog.Model;
using LevinBlog.Repository;
using LevinBlog.Service;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using Robotify.AspNetCore;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace LevinBlog.Web
{
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
      // Add framework services.
      services.AddCors();

      services.Configure<CookiePolicyOptions>(options =>
      {
        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
        options.CheckConsentNeeded = context => true;
        options.MinimumSameSitePolicy =  Microsoft.AspNetCore.Http.SameSiteMode.None;
      });


      services.AddMvc()
          .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);




      services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
      services.AddSingleton<ITelemetryInitializer, RequestBodyInitializer>();



      services.AddNodeServices();
      services.AddMemoryCache();
      services.AddRobotify();
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

      //  var telemetryConfiguration =
      //services.BuildServiceProvider().GetService<TelemetryConfiguration>();

      var telemetryConfiguration = TelemetryConfiguration.Active;
      var builder = telemetryConfiguration.TelemetryProcessorChainBuilder;
      builder.Use((next) => new NotFoundProcessor(next));
      builder.Use((next) => new BotRequestTracking(next));


      // Register the Swagger generator, defining one or more Swagger documents
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new Info { Title = "LevinBlog", Version = "v1" });
      });

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

      app.UseStaticFiles(new StaticFileOptions()
      {
        OnPrepareResponse = c =>
        {
          //Do not add cache to json files. We need to have new versions when we add new translations.

          if (!c.Context.Request.Path.Value.Contains(".json"))
          {
            c.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
              MaxAge = TimeSpan.FromDays(30) // Cache everything except json for 30 days
            };
          }
          else
          {
            c.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
              MaxAge = TimeSpan.FromMinutes(15) // Cache json for 15 minutes
            };
          }
        }
      });


      app.UseDeveloperExceptionPage();





      ServiceMapperConfig.Config();
      app.UseAuthentication();
      if (env.IsDevelopment())
      {

        app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
        {
          HotModuleReplacement = true,
          HotModuleReplacementEndpoint = "/dist/__webpack_hmr"
        });
        TelemetryConfiguration.Active.DisableTelemetry = true;
      }
      else
      {
        app.UseHsts();
      }


      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
      });

      app.UseRobotify();
      app.UseHttpsRedirection();
      app.UseCookiePolicy();
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
