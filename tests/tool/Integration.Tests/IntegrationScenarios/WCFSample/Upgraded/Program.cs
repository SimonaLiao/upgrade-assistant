﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
            var builder = WebApplication.CreateBuilder();

            // Set up port (previously this was done in configuration,
            // but CoreWCF requires it be done in code)
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8733);
                
            });

            // Add CoreWCF services to the ASP.NET Core app's DI container
            builder.Services.AddServiceModelServices()
                            .AddServiceModelConfigurationManagerFile("wcf.config")
                            .AddServiceModelMetadata()
                            .AddTransient<WcfServiceLibrary1.Service1>();

            var app = builder.Build();

            // Enable getting metadata/wsdl
            var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
            serviceMetadataBehavior.HttpGetUrl = new Uri("http://localhost:8733/Service1/metadata");

            // Configure CoreWCF endpoints in the ASP.NET Core hosts
            app.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.AddService<WcfServiceLibrary1.Service1>(serviceOptions => 
                {

                });

                serviceBuilder.ConfigureServiceHostBase<WcfServiceLibrary1.Service1>(host =>
                {

                });

            });
            
            await app.StartAsync();
            Console.WriteLine("Service Hosted Sucessfully. Hit any key to exit");
            Console.ReadKey();
            await app.StopAsync();
            }
            catch(Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }
    }
}
