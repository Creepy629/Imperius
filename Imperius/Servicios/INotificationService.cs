namespace Imperius.Servicios
{
    public interface INotificationService
    {
        void ScheduleNotification(int id, string titulo, string cuerpo, DateTime notificarEn);
        void CancelNotification(int id);
    }
}