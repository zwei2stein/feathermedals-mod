using UnityEngine;

namespace FeatherMedals;

public static class TrophyUIStyle
{
    // ===== Colors =====

    public static readonly Color GreenColor = new(0.5f, 0.8f, 0.5f);
    public static readonly Color GoldColor = new(0.9f, 0.85f, 0.4f);
    public static readonly Color MutedColor = new(0.7f, 0.7f, 0.7f);
    public static readonly Color SeparatorColor = new(1f, 1f, 1f, 0.15f);

    // ===== Tabs =====

    public static readonly Vector2 TabSize = new(400f, 480f);
    public const float TabPadding = 10f;
    public const float HeaderHeight = 30f;
    public const float ScrollbarWidth = 16f;
    public const float RowGap = 10f; // between rows, the separator runs through its middle

    // ===== Rows =====

    public const float SidePadding = 10f;
    public const float RowPadding = 4f; // above and below row content
    public const float IconColumnWidth = 80f; // shared by every row, so texts line up
    public const float ColumnGap = 10f;
    public const float LockButtonSize = 24f;

    public const float FeatherIconSize = 80f;
    public const float ScalpIconSize = 70f;
    public const float SkullIconSize = 70f;
    public const float FeatherRowMinHeight = 90f;

    // ===== Detail view (one trophy) =====

    public const float DetailIconSize = 128f;
    public const float DetailIconGap = 10f;
    public const float DetailSectionGap = 6f; // after the name, before the tale

    // ===== Dialogs =====

    public const float ButtonHeight = 35f;
    public const float ButtonWidth = 120f;
    public static readonly Vector2 AdornedDialogSize = new(480f, 360f);
    public const float TaleDialogWidth = 760f; // height same as tabs
    public const float TaleDialogFeatherColumnWidth = 320f;

    // ===== Text =====

    public const float RowLineGap = 2f; // compact rows
    public const float DetailLineGap = 4f; // detail view
    public const float BlockGap = 8f; // between groups of lines
    public const float FixedLineHeight = 24f; // stat and trait lines
    public const float DescriptionLineHeight = 20f; // one truncated description line

    public const float TaleSideMargin = 20f;
    public const float TaleGap = 12f;
    
}