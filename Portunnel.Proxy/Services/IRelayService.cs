namespace Portunnel.Proxy.Services;

public interface IRelayService
{
  Task Connect();

  Task<string> Register();
  Task Unregister();
  Task Disconnect();
  
  bool IsConnected();
}