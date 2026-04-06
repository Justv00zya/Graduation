namespace OrgTechRepair.Models.DTOs;

public class ClientDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}

public class CreateClientDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}

public class UpdateClientDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
