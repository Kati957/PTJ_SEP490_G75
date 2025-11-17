public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = null!;
    public int? RelatedItemId { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}
