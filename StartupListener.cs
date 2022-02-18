using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TDXLambda;

namespace Bookindotcom
{
    public class StartupListener : TDXLambdaStartupListener
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {

        }

        public void Configure(IApplicationBuilder builder, IServiceProvider serviceProvider)
        {

        }

        void TDXLambdaStartupListener.ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {

        }

        void TDXLambdaStartupListener.Configure(IApplicationBuilder builder, IServiceProvider serviceProvider)
        {

        }
    }
}
