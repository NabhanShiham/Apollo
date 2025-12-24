namespace Apollo.Data.Interfaces
{
    public interface IHasTimeStamps
    {
        DateTime CreatedAt {get; set;}
        DateTime UpdatedAt {get; set;}
    }
}