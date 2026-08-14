namespace AILA.Application.Common.Dtos
{
    public class TagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid? CreatedById { get; set; }
        public bool IsPublished { get; set; }

        // Default constructor
        public TagDto() { }

        // Constructor for compatibility with existing code
        public TagDto(Guid id, string name)
        {
            Id = id;
            Name = name;
            Code = string.Empty;
            CreatedById = null;
            IsPublished = false;
        }
    }
}
