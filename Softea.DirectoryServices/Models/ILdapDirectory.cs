namespace Softea.DirectoryServices.Models
{
    public interface ILdapDirectory
    {
        int Id { get; }
        bool Enabled { get; }
        string Name { get; }
        LdapServer Server { get; }
        string ServiceAccountUserName { get; }
        string ServiceAccountPassword { get; }
        string BaseDn { get; }
        string UserObjectClass { get; }
        string UserFilter { get; }
        string UserNameAttribute { get; }
        string UserPasswordAttribute { get; }
        string UserEmailAttribute { get; }

        string UserMemberOf { get; }
        string UserObjectCategory { get; }
        int UpdatePeriod { get; }
    }
}