using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Data;
using OrgTechRepair.Models;

namespace OrgTechRepair.Services;

/// <summary>Создаёт карточку клиента в БД для учётной записи с ролью Client (без участия администратора).</summary>
public static class ClientProfileProvisioner
{
    public static async Task<Client> GetOrCreateForUserAsync(
        ApplicationDbContext context,
        string userId,
        string? displayName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.Clients
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (existing != null) return existing;

        var name = string.IsNullOrWhiteSpace(displayName)
            ? (string.IsNullOrWhiteSpace(email) ? "Клиент" : email.Trim())
            : displayName.Trim();

        var client = new Client
        {
            UserId = userId,
            FullName = name,
            Email = email
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync(cancellationToken);
        return client;
    }
}
