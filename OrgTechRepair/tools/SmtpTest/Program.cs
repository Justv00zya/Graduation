using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var host = config["Email:Smtp:Host"] ?? "smtp.gmail.com";
var port = config.GetValue<int?>("Email:Smtp:Port") ?? 587;
var user = config["Email:Smtp:Username"] ?? "";
var pass = config["Email:Smtp:Password"] ?? "";
var from = config["Email:Smtp:FromEmail"] ?? user;

Console.WriteLine($"Host={host} Port={port} User={user} From={from} PassLen={pass.Length}");

if (string.IsNullOrWhiteSpace(pass))
{
    Console.WriteLine("FAIL: Password empty");
    return 1;
}

var message = new MimeMessage();
message.From.Add(new MailboxAddress("Test", from));
message.To.Add(MailboxAddress.Parse(user));
message.Subject = "OrgTechRepair SMTP test";
message.Body = new TextPart("plain") { Text = $"Test at {DateTime.UtcNow:O}" };

async Task Try(int p, SecureSocketOptions opt)
{
    using var client = new SmtpClient { Timeout = 60000 };
    Console.WriteLine($"Connecting {host}:{p} {opt}...");
    await client.ConnectAsync(host, p, opt);
    Console.WriteLine("Authenticating...");
    await client.AuthenticateAsync(user, pass);
    Console.WriteLine("Sending...");
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
    Console.WriteLine($"OK on port {p}");
}

try
{
    await Try(port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Port {port} failed: {ex.GetType().Name}: {ex.Message}");
    if (port != 465)
    {
        try
        {
            await Try(465, SecureSocketOptions.SslOnConnect);
            return 0;
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"Port 465 failed: {ex2.GetType().Name}: {ex2.Message}");
        }
    }
    return 1;
}
