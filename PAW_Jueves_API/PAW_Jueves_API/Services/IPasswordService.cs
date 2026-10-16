namespace PAW_Jueves_API.Services
{
    public interface IPasswordService
    {
        string Cifrar(string userId, TimeSpan vigencia);
        string Descifrar(string token);
    }
}
