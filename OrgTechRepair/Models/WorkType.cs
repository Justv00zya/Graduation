namespace OrgTechRepair.Models;

public class WorkType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ICollection<Work> Works { get; set; } = new List<Work>();
}
