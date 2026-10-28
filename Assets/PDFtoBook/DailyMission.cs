using System;
using System.Globalization;

namespace PDFtoBook
{
    [Serializable]
    public sealed class DailyMission
    {
        public string day;
        public int target;
        public int pages;
        public bool celebrationShown;
        public bool Complete => target > 0 && pages >= target;

        // Device-local midnight: a closed app catches up when it next opens.
        public bool Refresh(DateTime localNow)
        {
            string today = localNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (day == today) return false;
            day = today;
            pages = 0;
            celebrationShown = false;
            return true;
        }

        public bool TryCelebrate()
        {
            if (!Complete || celebrationShown) return false;
            celebrationShown = true;
            return true;
        }

        public bool RecordAdvance(int previousPage, int displayedPage, DateTime localNow)
        {
            bool changed = Refresh(localNow);
            if (displayedPage <= previousPage) return changed;
            if (pages < int.MaxValue) pages++;
            return true;
        }
    }
}