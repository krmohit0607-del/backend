namespace MultiTenantSaaS.Application.DTOs.EmailTemplates;

public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string DefaultRecipients { get; set; }
    public string Tags { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEmailTemplateRequestDto
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string DefaultRecipients { get; set; }
    public string Tags { get; set; }
}

public class UpdateEmailTemplateRequestDto
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string DefaultRecipients { get; set; }
    public string Tags { get; set; }
    public bool? IsActive { get; set; }
}
