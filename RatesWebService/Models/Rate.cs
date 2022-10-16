namespace RatesWebService.Models;

public class Rate
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Code { get; set; }
    public float Value { get; set; }
    public DateTime ADate { get; set; }
}
