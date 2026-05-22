using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

static void AddCandidate(List<string> candidates, string? path)
{
    if (string.IsNullOrWhiteSpace(path))
        return;

    var full = Path.GetFullPath(path);
    if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
        candidates.Add(full);
}

var jsonFiles = new List<string>();
AddCandidate(jsonFiles, Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
AddCandidate(jsonFiles, Path.Combine(AppContext.BaseDirectory, "appsettings.json"));

var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir != null)
{
    if (dir.GetFiles("*.csproj").Length > 0)
    {
        AddCandidate(jsonFiles, Path.Combine(dir.FullName, "appsettings.json"));
        AddCandidate(jsonFiles, Path.Combine(dir.FullName, "appsettings.Development.json"));
        AddCandidate(jsonFiles, Path.Combine(dir.FullName, "appsettings.Local.json"));
        break;
    }

    dir = dir.Parent;
}

var builder = new ConfigurationBuilder().AddEnvironmentVariables();
foreach (var path in jsonFiles.Where(File.Exists))
    builder.AddJsonFile(path, optional: true);

var config = builder.Build();

var host = config["Email:Smtp:Host"] ?? "smtp.gmail.com";
var port = config.GetValue<int?>("Email:Smtp:Port") ?? 587;
var user = config["Email:Smtp:Username"] ?? "";
var pass = config["Email:Smtp:Password"] ?? "";
var from = config["Email:Smtp:FromEmail"] ?? user;

Console.WriteLine($"Host={host} Port={port} User={user} From={from} PassLen={pass.Length}");

if (string.IsNullOrWhiteSpace(pass))
{
    Console.WriteLine("FAIL: Password empty — проверьте appsettings.Local.json в корне проекта OrgTechRepair.");
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
