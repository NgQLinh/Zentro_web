using Zentro.Models;

namespace Zentro.Services
{
    public sealed class PlcCommunicationMonitor : BackgroundService
    {
        private readonly BangTaiModel plcState;
        private readonly ProductionDatabaseService database;
        private bool? previousConnectionState;

        public PlcCommunicationMonitor(BangTaiModel plcState, ProductionDatabaseService database)
        {
            this.plcState = plcState;
            this.database = database;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CheckCommunication();
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        private void CheckCommunication()
        {
            var isConnected = plcState.PlcOnline;
            if (isConnected)
            {
                plcState.PlcLastCommunication = DateTime.Now;
            }

            if (previousConnectionState.HasValue && previousConnectionState.Value != isConnected)
            {
                database.SaveAlarm(new AlarmRecord
                {
                    MucDo = isConnected ? "INFO" : "ERROR",
                    ThietBi = "PLC",
                    NoiDung = isConnected ? "PLC Communication Restored" : "PLC Communication Lost",
                    NgayGio = DateTime.Now,
                    TrangThai = "CHƯA XỬ LÝ"
                });
            }

            previousConnectionState = isConnected;
        }
    }
}