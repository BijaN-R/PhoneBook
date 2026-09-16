// FILE: src/PhoneBook.Core/Layout/LayoutModels.cs
using PhoneBook.Domain.Entities;

namespace PhoneBook.Core.Layout;

public sealed record PlacedGroup(
    PhoneBookGroup Group,
    double HeightMm,
    int ColumnIndex,
    int RowIndex);

public sealed record PageLayout(
    IReadOnlyList<IReadOnlyList<PlacedGroup>> Columns,
    double[] ColumnHeightsMm);

public sealed record LayoutResult(
    IReadOnlyList<PageLayout> Pages,
    AppSettings EffectiveSettings,
    bool LayoutFailed,
    string? FailureReason);
