// FILE: src/PhoneBook.Core/Layout/LayoutEngine.cs
using System.Diagnostics;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Enums;

namespace PhoneBook.Core.Layout;

public sealed class LayoutEngine
{
    private const int ColumnCount = 3;
    private const int BeamWidth = 128;
    private static readonly TimeSpan PackerTimeLimit = TimeSpan.FromSeconds(2);
    private readonly HeightEstimator _heightEstimator;

    public LayoutEngine()
        : this(new HeightEstimator())
    {
    }

    public LayoutEngine(HeightEstimator heightEstimator)
    {
        _heightEstimator = heightEstimator ?? throw new ArgumentNullException(nameof(heightEstimator));
    }

    public LayoutResult CreateLayout(
        IReadOnlyList<PhoneBookGroup> groups,
        AppSettings settings,
        ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(measurer);

        ValidateSettings(settings);
        ValidatePriorities(groups, settings.PriorityTopLimit);

        AppSettings originalSettings = CloneSettings(settings);

        foreach (AppSettings candidateSettings in CreateCompressionCandidates(originalSettings))
        {
            if (TryCreateSinglePage(groups, candidateSettings, measurer, out PageLayout? page))
            {
                return new LayoutResult(
                    [page!],
                    candidateSettings,
                    LayoutFailed: false,
                    FailureReason: null);
            }
        }

        return CreateMultiPageLayout(groups, originalSettings, measurer);
    }

    private bool TryCreateSinglePage(
        IReadOnlyList<PhoneBookGroup> groups,
        AppSettings settings,
        ITextMeasurer measurer,
        out PageLayout? page)
    {
        double printableHeight = GetPrintableHeight(settings);
        List<GroupMeasurement> measuredGroups = MeasureGroups(groups, settings, measurer);

        if (measuredGroups.Any(group => group.HeightMm > printableHeight))
        {
            page = null;
            return false;
        }

        double minimumTotalGap = Math.Max(0, measuredGroups.Count - ColumnCount) * settings.GroupGapMm;
        if (measuredGroups.Sum(group => group.HeightMm) + minimumTotalGap
            > printableHeight * ColumnCount)
        {
            page = null;
            return false;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        BeamState baseState = new();
        HashSet<PhoneBookGroup> constrainedGroups = new(ReferenceEqualityComparer.Instance);

        for (int priority = 1; priority <= settings.PriorityTopLimit; priority++)
        {
            GroupMeasurement? priorityGroup = measuredGroups.SingleOrDefault(
                item => item.Group.Priority == priority);
            if (priorityGroup is null)
            {
                continue;
            }

            int columnIndex = priority - 1;
            if (columnIndex >= ColumnCount)
            {
                break;
            }

            baseState.Add(priorityGroup, columnIndex, settings.GroupGapMm);
            constrainedGroups.Add(priorityGroup.Group);
        }

        GroupMeasurement? fourthPriority = measuredGroups.SingleOrDefault(
            item => item.Group.Priority == settings.PriorityTopLimit + 1);
        List<BeamState> beam = [];

        if (fourthPriority is null)
        {
            beam.Add(baseState);
        }
        else
        {
            constrainedGroups.Add(fourthPriority.Group);
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                BeamState candidate = baseState.Clone();
                candidate.Add(fourthPriority, columnIndex, settings.GroupGapMm);
                beam.Add(candidate);
            }
        }

        List<GroupMeasurement> remainingGroups = measuredGroups
            .Where(item => !constrainedGroups.Contains(item.Group))
            .OrderByDescending(item => item.HeightMm)
            .ThenBy(item => item.Group.DisplayOrder)
            .ThenBy(item => item.SourceIndex)
            .ToList();

        int nextGroupIndex = 0;
        for (; nextGroupIndex < remainingGroups.Count; nextGroupIndex++)
        {
            if (stopwatch.Elapsed >= PackerTimeLimit)
            {
                break;
            }

            GroupMeasurement group = remainingGroups[nextGroupIndex];
            List<BeamState> expanded = new(beam.Count * ColumnCount);

            foreach (BeamState state in beam)
            {
                for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
                {
                    BeamState candidate = state.Clone();
                    candidate.Add(group, columnIndex, settings.GroupGapMm);
                    expanded.Add(candidate);
                }
            }

            beam = OrderStates(expanded, printableHeight)
                .Take(BeamWidth)
                .ToList();
        }

        BeamState bestState = OrderStates(beam, printableHeight).First();

        // Complete a timed-out search greedily from the best partial state.
        for (; nextGroupIndex < remainingGroups.Count; nextGroupIndex++)
        {
            GroupMeasurement group = remainingGroups[nextGroupIndex];
            bestState = OrderStates(
                    Enumerable.Range(0, ColumnCount).Select(columnIndex =>
                    {
                        BeamState candidate = bestState.Clone();
                        candidate.Add(group, columnIndex, settings.GroupGapMm);
                        return candidate;
                    }),
                    printableHeight)
                .First();
        }

        if (bestState.Heights.Any(height => height > printableHeight + 0.0001))
        {
            page = null;
            return false;
        }

        page = bestState.ToPageLayout();
        return true;
    }

    private LayoutResult CreateMultiPageLayout(
        IReadOnlyList<PhoneBookGroup> groups,
        AppSettings originalSettings,
        ITextMeasurer measurer)
    {
        double printableHeight = GetPrintableHeight(originalSettings);
        List<GroupMeasurement> measuredGroups = MeasureGroups(groups, originalSettings, measurer);
        List<MutablePage> pages = [new MutablePage()];
        MutablePage firstPage = pages[0];
        HashSet<PhoneBookGroup> constrainedGroups = new(ReferenceEqualityComparer.Instance);
        string? failureReason = null;

        foreach (GroupMeasurement measuredGroup in measuredGroups)
        {
            if (failureReason is null
                && measuredGroup.Group.KeepTogether
                && measuredGroup.HeightMm > printableHeight)
            {
                failureReason = $"Group '{measuredGroup.Group.Title}' exceeds one full page and cannot be split (KeepTogether = true).";
            }
        }

        for (int priority = 1; priority <= originalSettings.PriorityTopLimit; priority++)
        {
            GroupMeasurement? priorityGroup = measuredGroups.SingleOrDefault(
                item => item.Group.Priority == priority);
            if (priorityGroup is null)
            {
                continue;
            }

            int columnIndex = priority - 1;
            if (columnIndex >= ColumnCount)
            {
                break;
            }

            firstPage.Add(priorityGroup, columnIndex, originalSettings.GroupGapMm);
            constrainedGroups.Add(priorityGroup.Group);
        }

        GroupMeasurement? fourthPriority = measuredGroups.SingleOrDefault(
            item => item.Group.Priority == originalSettings.PriorityTopLimit + 1);
        if (fourthPriority is not null)
        {
            int shortestColumn = Enumerable.Range(0, ColumnCount)
                .OrderBy(columnIndex => firstPage.Heights[columnIndex])
                .ThenBy(columnIndex => columnIndex)
                .First();
            firstPage.Add(fourthPriority, shortestColumn, originalSettings.GroupGapMm);
            constrainedGroups.Add(fourthPriority.Group);
        }

        IEnumerable<GroupMeasurement> remainingGroups = measuredGroups
            .Where(item => !constrainedGroups.Contains(item.Group))
            .OrderBy(item => item.Group.DisplayOrder)
            .ThenBy(item => item.SourceIndex);

        MutablePage currentPage = firstPage;
        int currentColumnIndex = 0;
        foreach (GroupMeasurement group in remainingGroups)
        {
            while (!CanFit(
                       currentPage,
                       currentColumnIndex,
                       group.HeightMm,
                       originalSettings.GroupGapMm,
                       printableHeight))
            {
                currentColumnIndex++;
                if (currentColumnIndex < ColumnCount)
                {
                    continue;
                }

                currentPage = new MutablePage();
                pages.Add(currentPage);
                currentColumnIndex = 0;
                break;
            }

            currentPage.Add(group, currentColumnIndex, originalSettings.GroupGapMm);
        }

        return new LayoutResult(
            pages.Select(page => page.ToPageLayout()).ToList(),
            originalSettings,
            LayoutFailed: failureReason is not null,
            FailureReason: failureReason);
    }

    private List<GroupMeasurement> MeasureGroups(
        IReadOnlyList<PhoneBookGroup> groups,
        AppSettings settings,
        ITextMeasurer measurer)
    {
        List<GroupMeasurement> result = new(groups.Count);
        for (int index = 0; index < groups.Count; index++)
        {
            PhoneBookGroup group = groups[index]
                ?? throw new ArgumentException("The group collection cannot contain null values.", nameof(groups));
            double height = _heightEstimator.EstimateGroupHeightMm(group, settings, measurer);
            result.Add(new GroupMeasurement(group, height, index));
        }

        return result;
    }

    private static bool CanFit(
        MutablePage page,
        int columnIndex,
        double groupHeight,
        double groupGap,
        double printableHeight)
    {
        double requiredGap = page.Columns[columnIndex].Count == 0 ? 0 : groupGap;
        return page.Heights[columnIndex] + requiredGap + groupHeight <= printableHeight + 0.0001;
    }

    private static IOrderedEnumerable<BeamState> OrderStates(
        IEnumerable<BeamState> states,
        double printableHeight)
    {
        return states
            .OrderBy(state => state.GetOverflow(printableHeight))
            .ThenBy(state => state.GetVariance())
            .ThenBy(state => state.PreferredColumnViolations)
            .ThenBy(state => state.GetImbalance())
            .ThenBy(state => state.GetAssignmentKey(), StringComparer.Ordinal);
    }

    private static IEnumerable<AppSettings> CreateCompressionCandidates(AppSettings original)
    {
        AppSettings candidate = CloneSettings(original);
        yield return candidate;

        foreach (double factor in new[] { 0.75, 0.50, 0.25, 0.0 })
        {
            candidate = CloneSettings(candidate);
            candidate.GroupGapMm = original.GroupGapMm * factor;
            yield return candidate;
        }

        double minimumPadding = Math.Min(original.CellPaddingMm, 0.4);
        foreach (double factor in new[] { 0.85, 0.70, 0.50 })
        {
            candidate = CloneSettings(candidate);
            candidate.CellPaddingMm = Math.Max(minimumPadding, original.CellPaddingMm * factor);
            yield return candidate;
        }

        foreach (double factor in new[] { 0.85, 0.70, 0.50 })
        {
            candidate = CloneSettings(candidate);
            candidate.MarginTopMm = Math.Max(Math.Min(original.MarginTopMm, 5), original.MarginTopMm * factor);
            candidate.MarginBottomMm = Math.Max(Math.Min(original.MarginBottomMm, 5), original.MarginBottomMm * factor);
            candidate.MarginLeftMm = Math.Max(Math.Min(original.MarginLeftMm, 4), original.MarginLeftMm * factor);
            candidate.MarginRightMm = Math.Max(Math.Min(original.MarginRightMm, 4), original.MarginRightMm * factor);
            yield return candidate;
        }

        while (candidate.DefaultFontSizePt > original.MinFontSizePt
               || candidate.GroupHeaderFontSizePt > original.MinFontSizePt
               || candidate.HeaderFontSizePt > original.MinFontSizePt)
        {
            candidate = CloneSettings(candidate);
            candidate.DefaultFontSizePt = Math.Max(
                original.MinFontSizePt,
                candidate.DefaultFontSizePt - 0.5);
            candidate.GroupHeaderFontSizePt = Math.Max(
                original.MinFontSizePt,
                candidate.GroupHeaderFontSizePt - 0.5);
            candidate.HeaderFontSizePt = Math.Max(
                original.MinFontSizePt,
                candidate.HeaderFontSizePt - 0.5);
            yield return candidate;
        }
    }

    private static void ValidatePriorities(IReadOnlyList<PhoneBookGroup> groups, int priorityTopLimit)
    {
        int constrainedCount = groups.Count(
            group => group.Priority >= 1 && group.Priority <= priorityTopLimit);
        if (constrainedCount > priorityTopLimit)
        {
            throw new InvalidOperationException(
                $"Found {constrainedCount} groups with priorities 1..{priorityTopLimit}; "
                + $"at most {priorityTopLimit} top-priority groups can be placed in {ColumnCount} columns.");
        }

        IGrouping<int, PhoneBookGroup>? duplicateTopPriority = groups
            .Where(group => group.Priority >= 1 && group.Priority <= priorityTopLimit + 1)
            .GroupBy(group => group.Priority)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTopPriority is not null)
        {
            throw new InvalidOperationException(
                $"Priority {duplicateTopPriority.Key} is assigned to more than one group; priorities 1..{priorityTopLimit + 1} must be unique.");
        }
    }

    private static void ValidateSettings(AppSettings settings)
    {
        if (settings.PriorityTopLimit != ColumnCount)
        {
            throw new InvalidOperationException(
                $"PriorityTopLimit must be {ColumnCount} for the fixed three-column layout.");
        }

        ValidatePositive(settings.PageWidthMm, nameof(settings.PageWidthMm));
        ValidatePositive(settings.PageHeightMm, nameof(settings.PageHeightMm));
        ValidateNonNegative(settings.MarginTopMm, nameof(settings.MarginTopMm));
        ValidateNonNegative(settings.MarginBottomMm, nameof(settings.MarginBottomMm));
        ValidateNonNegative(settings.MarginLeftMm, nameof(settings.MarginLeftMm));
        ValidateNonNegative(settings.MarginRightMm, nameof(settings.MarginRightMm));
        ValidateNonNegative(settings.GroupGapMm, nameof(settings.GroupGapMm));
        ValidateNonNegative(settings.CellPaddingMm, nameof(settings.CellPaddingMm));
        ValidatePositive(settings.MinFontSizePt, nameof(settings.MinFontSizePt));
        ValidatePositive(settings.DefaultFontSizePt, nameof(settings.DefaultFontSizePt));
        ValidatePositive(settings.HeaderFontSizePt, nameof(settings.HeaderFontSizePt));
        ValidatePositive(settings.GroupHeaderFontSizePt, nameof(settings.GroupHeaderFontSizePt));

        if (settings.DefaultFontSizePt < settings.MinFontSizePt
            || settings.HeaderFontSizePt < settings.MinFontSizePt
            || settings.GroupHeaderFontSizePt < settings.MinFontSizePt)
        {
            throw new ArgumentException("Configured font sizes cannot be smaller than MinFontSizePt.", nameof(settings));
        }

        if (GetPrintableHeight(settings) <= 0)
        {
            throw new ArgumentException("Top and bottom margins leave no printable page height.", nameof(settings));
        }
    }

    private static void ValidatePositive(double value, string propertyName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, "The value must be finite and greater than zero.");
        }
    }

    private static void ValidateNonNegative(double value, string propertyName)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(propertyName, "The value must be finite and non-negative.");
        }
    }

    private static double GetPrintableHeight(AppSettings settings)
    {
        return settings.PageHeightMm - settings.MarginTopMm - settings.MarginBottomMm;
    }

    private static AppSettings CloneSettings(AppSettings settings)
    {
        return new AppSettings
        {
            Id = settings.Id,
            PageWidthMm = settings.PageWidthMm,
            PageHeightMm = settings.PageHeightMm,
            MarginTopMm = settings.MarginTopMm,
            MarginBottomMm = settings.MarginBottomMm,
            MarginLeftMm = settings.MarginLeftMm,
            MarginRightMm = settings.MarginRightMm,
            GroupGapMm = settings.GroupGapMm,
            CellPaddingMm = settings.CellPaddingMm,
            PrimaryFontFamily = settings.PrimaryFontFamily,
            UsePersianDigits = settings.UsePersianDigits,
            MinFontSizePt = settings.MinFontSizePt,
            DefaultFontSizePt = settings.DefaultFontSizePt,
            HeaderFontSizePt = settings.HeaderFontSizePt,
            GroupHeaderFontSizePt = settings.GroupHeaderFontSizePt,
            PriorityTopLimit = settings.PriorityTopLimit
        };
    }

    private sealed record GroupMeasurement(PhoneBookGroup Group, double HeightMm, int SourceIndex);

    private sealed class BeamState
    {
        public List<GroupMeasurement>[] Columns { get; } =
            Enumerable.Range(0, ColumnCount).Select(_ => new List<GroupMeasurement>()).ToArray();

        public double[] Heights { get; } = new double[ColumnCount];

        public int PreferredColumnViolations { get; private set; }

        public void Add(GroupMeasurement group, int columnIndex, double groupGap)
        {
            if (Columns[columnIndex].Count > 0)
            {
                Heights[columnIndex] += groupGap;
            }

            Columns[columnIndex].Add(group);
            Heights[columnIndex] += group.HeightMm;

            ColumnPosition? preferredColumn = group.Group.PreferredColumn;
            if (preferredColumn is not null
                && preferredColumn != ColumnPosition.Auto
                && (int)preferredColumn.Value != columnIndex)
            {
                PreferredColumnViolations++;
            }
        }

        public BeamState Clone()
        {
            BeamState clone = new()
            {
                PreferredColumnViolations = PreferredColumnViolations
            };

            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                clone.Columns[columnIndex].AddRange(Columns[columnIndex]);
                clone.Heights[columnIndex] = Heights[columnIndex];
            }

            return clone;
        }

        public double GetOverflow(double printableHeight)
        {
            return Heights.Sum(height => Math.Max(0, height - printableHeight));
        }

        public double GetVariance()
        {
            double mean = Heights.Average();
            return Heights.Sum(height => (height - mean) * (height - mean)) / ColumnCount;
        }

        public double GetImbalance()
        {
            return Heights.Max() - Heights.Min();
        }

        public string GetAssignmentKey()
        {
            return string.Join(
                '|',
                Columns.Select(column => string.Join(',', column.Select(group => group.SourceIndex))));
        }

        public PageLayout ToPageLayout()
        {
            IReadOnlyList<IReadOnlyList<PlacedGroup>> columns = Columns
                .Select((column, columnIndex) =>
                    (IReadOnlyList<PlacedGroup>)column
                        .Select((group, rowIndex) => new PlacedGroup(
                            group.Group,
                            group.HeightMm,
                            columnIndex,
                            rowIndex))
                        .ToList())
                .ToList();

            return new PageLayout(columns, (double[])Heights.Clone());
        }
    }

    private sealed class MutablePage
    {
        public List<GroupMeasurement>[] Columns { get; } =
            Enumerable.Range(0, ColumnCount).Select(_ => new List<GroupMeasurement>()).ToArray();

        public double[] Heights { get; } = new double[ColumnCount];

        public void Add(GroupMeasurement group, int columnIndex, double groupGap)
        {
            if (Columns[columnIndex].Count > 0)
            {
                Heights[columnIndex] += groupGap;
            }

            Columns[columnIndex].Add(group);
            Heights[columnIndex] += group.HeightMm;
        }

        public PageLayout ToPageLayout()
        {
            IReadOnlyList<IReadOnlyList<PlacedGroup>> columns = Columns
                .Select((column, columnIndex) =>
                    (IReadOnlyList<PlacedGroup>)column
                        .Select((group, rowIndex) => new PlacedGroup(
                            group.Group,
                            group.HeightMm,
                            columnIndex,
                            rowIndex))
                        .ToList())
                .ToList();

            return new PageLayout(columns, (double[])Heights.Clone());
        }
    }
}
