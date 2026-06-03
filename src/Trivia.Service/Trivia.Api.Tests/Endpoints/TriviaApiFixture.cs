using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Infrastructure;
using Xunit;

namespace Trivia.Api.Tests.Endpoints;

public class TriviaApiFixture
{
    public WebApplicationFactory<Program> Factory { get; }

    public TriviaApiFixture()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TriviaDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<TriviaDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });
    }
}

[CollectionDefinition("TriviaApi")]
public class TriviaApiCollection : ICollectionFixture<TriviaApiFixture>
{
}
