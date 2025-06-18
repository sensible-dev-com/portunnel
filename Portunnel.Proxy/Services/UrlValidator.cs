namespace Portunnel.Proxy.Services;

public class UrlValidator : IUrlValidator
{
  public bool IsValid(string url, out string errorMessage)
  {
    var context = new ValidationContext();
    
    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) is false)
    {
      errorMessage = "Invalid URL";
      return false;
    }
    
    ValidateScheme(context, uri);
    ValidateHost(context, uri);

    errorMessage = string.Join('\n', context.Errors);
    return context.IsValid;
  }

  private static void ValidateHost(ValidationContext context, Uri uri)
  {
    var isHostEmpty = string.IsNullOrEmpty(uri.Host);
    context.IsValid = isHostEmpty is false;
    
    if(isHostEmpty is false)
      return;

    context.Errors.Add($"Uri host is empty: '{uri.Host}'");
  }


  private static void ValidateScheme(ValidationContext context, Uri uri)
  {
    var isValidScheme = uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
    context.IsValid = isValidScheme;
    
    if (isValidScheme)
      return;
    
    context.Errors.Add($"Uri scheme is not valid: '{uri.Scheme}'");
  }

  private class ValidationContext
  {
    private bool _isValid = true;

    public bool IsValid
    {
      get => _isValid;
      set
      {
        if (_isValid == false)
          return;
        
        _isValid = value;
      }
    }

    public List<string> Errors { get; set; } = [];
  }
}
