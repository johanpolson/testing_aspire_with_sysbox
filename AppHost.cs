using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
    {
        Args = args,
        EnableResourceLogging = true,
    });

var tag = builder.Configuration.GetValue("tag", "2.2.0");
Console.WriteLine("Using tag: " + tag);
var port = builder.Configuration.GetValue("port", 8200);
Console.WriteLine("Using port: " + port);

var openBao = builder.AddContainer("OpenBao", "quay.io/openbao/openbao", tag)
.WithContainerName("openbao_" + tag)
.WithEnvironment("BAO_DEV_ROOT_TOKEN_ID", "TestRootTokenId")
.WithHttpEndpoint(port, 8200, isProxied: false)
//.WithContainerRuntimeArgs("--runtime=sysbox-runc")
;

var app = builder.Build();
await app.StartAsync();

Console.WriteLine("Testing Server Is initialized");

var status = await TestServerIsinitialized();
Console.WriteLine("status: " + status);

await app.StopAsync();

return status ? 0 : 1;

async Task<bool> TestServerIsinitialized()
{
    using var httpClient = new HttpClient();
    for (int i = 0; i < 30; i++)
    {
        try
        {
            var response = await httpClient.GetAsync("http://127.0.0.1:" + port + "/v1/sys/init");
            var content = await response.Content.ReadAsStringAsync();

            if (content == "{\"initialized\":true}\n")
                return true;
            Console.WriteLine("Server not initialized yet, response: " + content);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred while testing server initialization: " + ex.Message);
        }

        await Task.Delay(5000);
    }

    return false;
}