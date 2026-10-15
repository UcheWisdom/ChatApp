namespace ChatApp.Domain.Interfaces;

public interface IUsernameGenerator
{
    Task<string> GenerateAsync(string firstName, string otherName);
}