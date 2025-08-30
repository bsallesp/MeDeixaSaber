namespace MDS.Api.Security.Pow;

public interface IPowValidator
{
    bool IsValid(string token, int difficulty, long nowUnix, int allowedSkewSeconds);
}