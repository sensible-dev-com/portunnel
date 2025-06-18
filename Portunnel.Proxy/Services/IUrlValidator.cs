namespace Portunnel.Proxy.Services;

public interface IUrlValidator
{
  bool IsValid(string url, out string errorMessage);
}