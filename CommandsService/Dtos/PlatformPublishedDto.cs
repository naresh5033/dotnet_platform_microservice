namespace CommandsService.Dtos
{
    public class PlatformPublishedDto
    {
        public int Id { get; set; } // we ve to map the id to the external id ofthe platform.cs model, that we'll do it in the profile mapper
        public string Name { get; set; }
        public string Event { get; set; }
    }
}