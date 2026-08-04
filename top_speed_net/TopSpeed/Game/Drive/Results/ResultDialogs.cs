using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Drive;

namespace TopSpeed.Game
{
    internal sealed class ResultDialogs
    {
        private readonly Pick _pick;
        private readonly ResultFmt _fmt;

        public ResultDialogs(Pick pick, ResultFmt fmt)
        {
            _pick = pick ?? throw new ArgumentNullException(nameof(pick));
            _fmt = fmt ?? throw new ArgumentNullException(nameof(fmt));
        }

        public ResultPlan Build(DriveResultSummary summary, Action? onClosed = null)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            if (summary.Mode == DriveResultMode.TimeTrial)
                return BuildTimeTrial(summary, onClosed);
            return BuildRace(summary, onClosed);
        }

        private ResultPlan BuildRace(DriveResultSummary summary, Action? onClosed = null)
        {
            var localWon = summary.LocalPosition == 1;
            var title = _pick.One(localWon ? ResultCatalog.WinnerTitles : ResultCatalog.NonWinnerTitles);
            var caption = _pick.One(localWon ? ResultCatalog.WinnerCaptions : ResultCatalog.NonWinnerCaptions);

            var items = new List<DialogItem>();
            var entries = summary.Entries ?? Array.Empty<DriveResultEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;
                items.Add(new DialogItem(_fmt.Line(entry)));
            }

            AppendCrashSummary(items, summary.LocalCrashCount);

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: onClosed == null ? (Action<int>?)null : _ => onClosed(),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            return new ResultPlan(dialog, playWin: localWon);
        }

        private ResultPlan BuildTimeTrial(DriveResultSummary summary, Action? onClosed = null)
        {
            var beatRecord = summary.TimeTrialBeatRecord;
            var title = _pick.One(beatRecord ? ResultCatalog.TimeTrialRecordTitles : ResultCatalog.TimeTrialNoRecordTitles);
            var caption = _pick.One(beatRecord ? ResultCatalog.TimeTrialRecordCaptions : ResultCatalog.TimeTrialNoRecordCaptions);

            var items = new List<DialogItem>();
            AppendTimeTrialRunSummary(items, summary);
            AppendTimeTrialLapSummary(items, summary);

            var dialog = new Dialog(
                title,
                caption,
                QuestionId.Close,
                items,
                onResult: onClosed == null ? (Action<int>?)null : _ => onClosed(),
                new DialogButton(QuestionId.Close, LocalizationService.Mark("Close")));

            return new ResultPlan(dialog, playWin: beatRecord);
        }

        private void AppendCrashSummary(List<DialogItem> items, int crashCount)
        {
            if (crashCount <= 0)
                return;

            string text;
            if (crashCount == 1)
            {
                text = CombineCrashText(
                    _pick.One(ResultCatalog.CrashOncePrefixes),
                    _pick.One(ResultCatalog.CrashOnceRemarks));
            }
            else if (crashCount <= 3)
            {
                text = CombineCrashText(
                    LocalizationService.Format(_pick.One(ResultCatalog.CrashPluralPrefixes), crashCount),
                    _pick.One(ResultCatalog.CrashFewRemarks));
            }
            else if (crashCount <= 7)
            {
                text = CombineCrashText(
                    LocalizationService.Format(_pick.One(ResultCatalog.CrashPluralPrefixes), crashCount),
                    _pick.One(ResultCatalog.CrashSeveralRemarks));
            }
            else if (crashCount <= 14)
            {
                text = CombineCrashText(
                    LocalizationService.Format(_pick.One(ResultCatalog.CrashPluralPrefixes), crashCount),
                    _pick.One(ResultCatalog.CrashManyRemarks));
            }
            else
            {
                text = CombineCrashText(
                    LocalizationService.Format(_pick.One(ResultCatalog.CrashPluralPrefixes), crashCount),
                    _pick.One(ResultCatalog.CrashDisasterRemarks));
            }

            items.Add(new DialogItem(text));
        }

        private static string CombineCrashText(string prefix, string remark)
        {
            return string.Concat(LocalizationService.Translate(prefix), " ", LocalizationService.Translate(remark));
        }

        private void AppendTimeTrialRunSummary(List<DialogItem> items, DriveResultSummary summary)
        {
            items.Add(new DialogItem(LocalizationService.Format(
                _pick.One(ResultCatalog.TimeTrialCurrentLineTemplates),
                _fmt.Time(summary.TimeTrialCurrentRunMs))));

            if (summary.TimeTrialBestRunMs > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialBestRunLineTemplates),
                    _fmt.Time(summary.TimeTrialBestRunMs))));
            }

            if (summary.TimeTrialAverageRunMs > 0 && summary.TimeTrialLapCount > 0)
            {
                items.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialAverageRunLineTemplates),
                    summary.TimeTrialLapCount,
                    _fmt.Time(summary.TimeTrialAverageRunMs))));
            }
        }

        private void AppendTimeTrialLapSummary(List<DialogItem> items, DriveResultSummary summary)
        {
            var lapItems = new List<DialogItem>();
            if (summary.TimeTrialBestLapThisRunMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialRunBestLapLineTemplates),
                    _fmt.Time(summary.TimeTrialBestLapThisRunMs))));
            }

            if (summary.TimeTrialBestLapMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialBestLapLineTemplates),
                    _fmt.Time(summary.TimeTrialBestLapMs))));
            }

            if (summary.TimeTrialAverageLapMs > 0)
            {
                lapItems.Add(new DialogItem(LocalizationService.Format(
                    _pick.One(ResultCatalog.TimeTrialAverageLapLineTemplates),
                    _fmt.Time(summary.TimeTrialAverageLapMs))));
            }

            if (lapItems.Count == 0)
                return;

            items.Add(new DialogItem(_pick.One(ResultCatalog.TimeTrialLapSummaryTitles)));
            items.AddRange(lapItems);
        }
    }
}



