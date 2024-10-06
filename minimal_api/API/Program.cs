using minimal_api;

IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(WebHostBuilder =>
        {
            WebHostBuilder.UseStartup<Startup>();
        });
}

CreateHostBuilder(args).Build().Run();