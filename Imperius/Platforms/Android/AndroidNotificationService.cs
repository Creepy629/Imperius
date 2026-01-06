using Android.App;
using Android.Content;
using Android.OS;
using Imperius.Servicios;
using System;
using Microsoft.Maui.ApplicationModel;

[assembly: Dependency(typeof(Imperius.Platforms.Android.AndroidNotificationService))]
namespace Imperius.Platforms.Android
{
    public class AndroidNotificationService : INotificationService
    {
        public const string ChannelId = "imperius_channel_01";
        private const string ChannelName = "Recordatorios de Tareas";

        public AndroidNotificationService()
        {
            CreateNotificationChannel();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High);
                var notificationManager = Platform.CurrentActivity.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        public void ScheduleNotification(int id, string titulo, string cuerpo, DateTime notificarEn)
        {
            var alarmManager = Platform.CurrentActivity.GetSystemService(Context.AlarmService) as AlarmManager;
            if (alarmManager == null) return;

            var intent = new Intent(Platform.CurrentActivity, typeof(NotificationPublisher));
            intent.PutExtra("id", id);
            intent.PutExtra("titulo", titulo);
            intent.PutExtra("cuerpo", cuerpo);

            var pendingIntent = PendingIntent.GetBroadcast(
                Platform.CurrentActivity,
                id,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            long triggerTimeMs = (long)(notificarEn - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    if (alarmManager.CanScheduleExactAlarms())
                    {
                        alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                    }
                    else
                    {
                        alarmManager.Set(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                        System.Console.WriteLine("Advertencia: Permiso SCHEDULE_EXACT_ALARM no concedido. Usando alarma inexacta.");
                    }
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                }
                else
                {
                    alarmManager.SetExact(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                }
            }
            catch (Java.Lang.SecurityException ex)
            {
                System.Console.WriteLine($"Error de seguridad al programar alarma: {ex.Message}. Usando fallback.");
                alarmManager.Set(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            }
        }

        public void CancelNotification(int id)
        {
            var alarmManager = Platform.CurrentActivity.GetSystemService(Context.AlarmService) as AlarmManager;
            var intent = new Intent(Platform.CurrentActivity, typeof(NotificationPublisher));

            var pendingIntent = PendingIntent.GetBroadcast(
                Platform.CurrentActivity,
                id,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            alarmManager?.Cancel(pendingIntent);
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class NotificationPublisher : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var id = intent.GetIntExtra("id", 0);
            var titulo = intent.GetStringExtra("titulo");
            var cuerpo = intent.GetStringExtra("cuerpo");

            var builder = new Notification.Builder(context, AndroidNotificationService.ChannelId)
                .SetContentTitle(titulo)
                .SetContentText(cuerpo)
                .SetSmallIcon(Resource.Drawable.notification_icon_background) // Reemplaza esto con un ícono real
                .SetAutoCancel(true);

            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.Notify(id, builder.Build());
        }
    }
}