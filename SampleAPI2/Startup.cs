using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SampleAPI2
{
    public class Startup
    {
        private static readonly Regex Regexp = new Regex(@"\{secrets.(\w*?)\}}", RegexOptions.Compiled);
        private const string API_NAME = "SAMLE API";

        public IHostingEnvironment currentEnvironment { get; }

        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            currentEnvironment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName.ToLower()}.json", true, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            IConfiguration secrets = new ConfigurationBuilder()
                 .AddJsonFile(Path.Combine(env.ContentRootPath, "appsecrets.json"), optional: false, reloadOnChange: false)
                 .Build();

            ApplySecret(secrets, Configuration);
        }
       

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddOptions();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = API_NAME, Version = "v1" });
                c.CustomSchemaIds(x => x.FullName);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseHsts();
            //}

            app.UseHttpsRedirection();
            app.UseMvc();

            if(!env.IsProduction())
            {
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("../swagger/v1/swagger.json", API_NAME);
                });
            }
        }

        private void ApplySecret(IConfiguration config, IConfiguration Secrets)
        {
            string responseContent = "";

            foreach (var item in config. AsEnumerable())
            {
                string value = item.Value;

                if(!string.IsNullOrEmpty(value))
                {
                    string replaceValue = Regexp.Replace(value, (match) =>
                    {
                        string key = match.Groups[1].Value;

                        if (string.IsNullOrEmpty(key))
                        {
                            throw new ArgumentException("Empty secret key not supported");
                        }

                        string secretValue = Secrets[key];

                        if (string.IsNullOrEmpty(secretValue))
                        {
                            throw new ArgumentException("unable to load value for secret key", key);
                        }

                        return secretValue;

                    });

                    responseContent = responseContent + "config[item.key]" + config[item.Key] + "=" + replaceValue;
                    responseContent = responseContent + Environment.NewLine;

                    config[item.Key] = replaceValue;
                }
            }
        }

        
    }
}
