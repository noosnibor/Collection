using Collection.Library.Model;

namespace Collection.Web.Components.Custom.Toast;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;
    public event Action? OnClear;

    public void ShowSuccess(string message)
        => Show(ToastTypeModel.Success, message);

    public void ShowWarning(string message)
        => Show(ToastTypeModel.Warning, message);

    public void ShowError(string message)
        => Show(ToastTypeModel.Danger, message);

    public void ShowInfo(string message)
        => Show(ToastTypeModel.Primary, message);

    public void Show(ToastTypeModel type, string message)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = type,
            Message = message
        });
    }

    public void Clear() => OnClear?.Invoke();
}
