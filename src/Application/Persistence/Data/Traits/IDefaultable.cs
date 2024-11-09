namespace Application.Persistence.Data.Traits;

public interface IDefaultable<out T>
{
    static abstract T GetDefault();
}
