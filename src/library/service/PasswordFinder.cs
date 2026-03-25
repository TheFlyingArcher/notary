using Org.BouncyCastle.OpenSsl;

namespace Notary.Service;

public class PasswordFinder : IPasswordFinder
{
    private readonly string _pwd;

    public PasswordFinder(string password)
    {
        _pwd = password;
    }

    public char[] GetPassword()
    {
        return _pwd.ToCharArray();
    }
}