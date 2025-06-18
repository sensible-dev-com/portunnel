using Portunnel.Models;

namespace Portunnel.Proxy.Services;

public interface ILogDestination
{
  Task Log(RequestLogMessage message);
}