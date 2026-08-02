namespace iPhoneRingsMaker.Contracts.Services;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel,
}

public interface IUserDialogService
{
    Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync();
}
