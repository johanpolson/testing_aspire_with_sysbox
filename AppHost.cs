using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
    {
        Args = args,
        EnableResourceLogging = true,
    });

var tag = builder.Configuration.GetValue("tag", "2.2.0");
var port = builder.Configuration.GetValue("port", 8200);

var openBao = builder.AddContainer("OpenBao", "quay.io/openbao/openbao", tag)
.WithContainerName("openbao_" + tag)
.WithEnvironment("BAO_DEV_ROOT_TOKEN_ID", "TestRootTokenId")
.WithHttpEndpoint(port, 8200, isProxied: false)
.WithContainerRuntimeArgs("--runtime=sysbox-runc");

// https://learn.arm.com/install-guides/sysbox/

var app = builder.Build();
await app.StartAsync();

System.Console.WriteLine("Testing Server Is initialized");
System.Console.WriteLine(await TestServerIsinitialized());

await app.StopAsync();

async Task<bool> TestServerIsinitialized()
{
    using var httpClient = new HttpClient();
    for (int i = 0; i < 50; i++)
    {
        try
        {
            var response = await httpClient.GetAsync("http://localhost:" + port + "/v1/sys/init");
            var content = await response.Content.ReadAsStringAsync();

            if (content == "{\"initialized\":true}\n")
                return true;
        }

        catch (Exception)
        {
        }

        await Task.Delay(1000);
    }

    return false;
}