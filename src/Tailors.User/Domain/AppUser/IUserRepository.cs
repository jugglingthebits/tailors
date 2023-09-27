namespace Tailors.User.Domain.AppUser;

public interface IUserRepository
{
    Task<List<AppUser>> Search(string term);
}