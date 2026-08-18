public class WarHackUserModel
{
    private string Id {get;init;}
    private string Username {get;init;}
    private string Email {get;init;}
    public WarHackUserModel(
        string id,
        string userNm,
        string email
    )
    {
        Id = id;
        Username = userNm;
        Email = email;
    }
}