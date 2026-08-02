namespace iPhoneRingsMaker.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    Microsoft.UI.Xaml.Controls.Page CreatePage(string key);
}
