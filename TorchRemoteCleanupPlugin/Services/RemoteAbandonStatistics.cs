using System;
using System.Threading;
using Torch;

namespace RemoteAbandon.Services
{
    public class RemoteAbandonStatistics : ViewModel
    {
        private long _totalGridsAbandoned;
        private long _totalSubgridsProcessed;
        private long _totalBeaconsDestroyed;
        private long _totalCombatBlocked;
        private long _totalPcuRefunded;
        private string _lastAbandonedGridName = "None";
        private ulong _lastAbandonedPlayerSteamId = 0;
        private string _lastAbandonedTimestamp = "Never";

        public long TotalGridsAbandoned => Interlocked.Read(ref _totalGridsAbandoned);
        public long TotalSubgridsProcessed => Interlocked.Read(ref _totalSubgridsProcessed);
        public long TotalBeaconsDestroyed => Interlocked.Read(ref _totalBeaconsDestroyed);
        public long TotalCombatBlocked => Interlocked.Read(ref _totalCombatBlocked);
        public long TotalPcuRefunded => Interlocked.Read(ref _totalPcuRefunded);

        public string LastAbandonedGridName
        {
            get => _lastAbandonedGridName;
            private set => SetValue(ref _lastAbandonedGridName, value);
        }

        public ulong LastAbandonedPlayerSteamId
        {
            get => _lastAbandonedPlayerSteamId;
            private set => SetValue(ref _lastAbandonedPlayerSteamId, value);
        }

        public string LastAbandonedTimestamp
        {
            get => _lastAbandonedTimestamp;
            private set => SetValue(ref _lastAbandonedTimestamp, value);
        }

        public void RecordGridAbandoned(int subgridCount, int beaconsDestroyed, int pcuRefunded, string gridName, ulong steamId)
        {
            Interlocked.Increment(ref _totalGridsAbandoned);
            Interlocked.Add(ref _totalSubgridsProcessed, subgridCount);
            Interlocked.Add(ref _totalBeaconsDestroyed, beaconsDestroyed);
            Interlocked.Add(ref _totalPcuRefunded, pcuRefunded);

            LastAbandonedGridName = gridName ?? "Unknown";
            LastAbandonedPlayerSteamId = steamId;
            LastAbandonedTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

            OnPropertyChanged(nameof(TotalGridsAbandoned));
            OnPropertyChanged(nameof(TotalSubgridsProcessed));
            OnPropertyChanged(nameof(TotalBeaconsDestroyed));
            OnPropertyChanged(nameof(TotalPcuRefunded));
        }

        public void RecordCombatBlocked()
        {
            Interlocked.Increment(ref _totalCombatBlocked);
            OnPropertyChanged(nameof(TotalCombatBlocked));
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _totalGridsAbandoned, 0);
            Interlocked.Exchange(ref _totalSubgridsProcessed, 0);
            Interlocked.Exchange(ref _totalBeaconsDestroyed, 0);
            Interlocked.Exchange(ref _totalCombatBlocked, 0);
            Interlocked.Exchange(ref _totalPcuRefunded, 0);

            LastAbandonedGridName = "None";
            LastAbandonedPlayerSteamId = 0;
            LastAbandonedTimestamp = "Never";

            OnPropertyChanged(nameof(TotalGridsAbandoned));
            OnPropertyChanged(nameof(TotalSubgridsProcessed));
            OnPropertyChanged(nameof(TotalBeaconsDestroyed));
            OnPropertyChanged(nameof(TotalCombatBlocked));
            OnPropertyChanged(nameof(TotalPcuRefunded));
        }
    }
}

