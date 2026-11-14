using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Windows.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Playroom.Windows;

public partial class MainWindow : Window
{
    private static readonly string[] GameIds =
    [
        "connect-four", "rock-paper-scissors", "memory-match", "2048", "whack-a-mole"
    ];

    private static readonly string[] GameNames =
    [
        "Connect Four", "Rock Paper Scissors", "Memory Match", "2048", "Whack-a-Mole"
    ];

    private static readonly string[] GameDescriptions =
    [
        "Take turns and line up four.",
        "Make your move and see what happens.",
        "Find the matching pairs.",
        "Slide tiles together to make 2048.",
        "Catch as many little moles as you can."
    ];

    private static readonly string[] MemoryFaces =
    [
        "🍓", "🌿", "🍋", "🌼", "🍇", "🌙", "🐚", "🍑"
    ];

    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedStatKeys =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["connect-four"] = ["wins", "gamesPlayed"],
            ["rock-paper-scissors"] = ["wins", "ties", "losses", "currentStreak", "bestStreak", "gamesPlayed"],
            ["memory-match"] = ["bestMoves", "gamesPlayed"],
            ["2048"] = ["best", "bestTile", "gamesPlayed"],
            ["whack-a-mole"] = ["best", "gamesPlayed"],
            ["playroom"] = ["dailyDate", "dailyProgress", "dailyStreak", "dailyCompletedDate",
                "dailyChallengesCompleted"]
        };

    private readonly Random _random = new();
    private readonly Dictionary<string, Dictionary<string, int>> _stats = new();
    private readonly string _statsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Playroom", "scores.json");

    private string? _currentGame;
    private bool _persistenceAvailable = true;
    private bool _saveWarningShown;
    private TextBlock? _dailyTitle;
    private TextBlock? _dailyDescription;
    private TextBlock? _dailyProgressLabel;
    private ProgressBar? _dailyProgressBar;
    private int[,] _connectBoard = new int[6, 7];
    private Button[,] _connectButtons = new Button[6, 7];
    private TextBlock? _connectTurn;
    private bool _connectEnded;
    private int _connectPlayer = 1;

    private bool _rpsBusy;
    private int _rpsRoundId;
    private int _rpsWins;
    private int _rpsTies;
    private int _rpsStreak;
    private TextBlock? _rpsStatus;
    private TextBlock? _rpsWinsLabel;
    private TextBlock? _rpsTiesLabel;
    private TextBlock? _playerHand;
    private TextBlock? _computerHand;

    private int[] _memoryDeck = [];
    private bool[] _memoryMatched = [];
    private Button[] _memoryButtons = [];
    private int? _memoryFirst;
    private int _memoryMoves;
    private int _memoryPairs;
    private DispatcherTimer? _memoryMismatchTimer;
    private TextBlock? _memoryMovesLabel;
    private TextBlock? _memoryPairsLabel;
    private TextBlock? _memoryStatus;

    private int[,] _numberBoard = new int[4, 4];
    private int[,]? _numberUndoBoard;
    private Button[,] _numberButtons = new Button[4, 4];
    private int _numberScore;
    private int _numberUndoScore;
    private bool _numberGameOver;
    private bool _numberUndoAvailable;
    private Button? _numberUndoButton;
    private TextBlock? _numberScoreLabel;
    private TextBlock? _numberBestLabel;
    private TextBlock? _numberStatus;

    private Button[] _moleButtons = [];
    private int _moleScore;
    private int _moleSeconds;
    private int _activeMole = -1;
    private int _moleCombo;
    private DispatcherTimer? _moleClock;
    private DispatcherTimer? _moleSpawner;
    private TextBlock? _moleScoreLabel;
    private TextBlock? _moleBestLabel;
    private TextBlock? _moleStatus;
    private TextBlock? _moleTimerLabel;
    private ProgressBar? _moleProgress;
    private Button? _moleStartButton;

    public MainWindow()
    {
        InitializeComponent();
        LoadStats();
        EnsureDailyChallenge();
        ShowHome();
    }

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));

    private static TextBlock Text(string value, double size, Brush? color = null, bool bold = false) =>
        new()
        {
            Text = value,
            FontSize = size,
            Foreground = color ?? Brush("#383A34"),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

    private Button MakeButton(string label, Action onClick, bool primary = false, double minWidth = 0)
    {
        var button = new Button
        {
            Content = label,
            Style = (Style)FindResource(primary ? "PrimaryButton" : "QuietButton"),
            MinWidth = minWidth,
            Height = 42,
            Margin = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private Border Panel(UIElement child, Thickness? padding = null) =>
        new()
        {
            Background = Brush("#FFFEFA"),
            BorderBrush = Brush("#ECE9E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = padding ?? new Thickness(22),
            Child = child
        };

    private StackPanel Vertical(double spacing = 0) =>
        new() { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, spacing) };

    private StackPanel Horizontal(VerticalAlignment alignment = VerticalAlignment.Center) =>
        new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = alignment
        };

    private void ShowHome()
    {
        StopGameTimers();
        _currentGame = null;
        BackButton.Visibility = Visibility.Collapsed;
        UpdateHeader("Your playroom", "Five games. No rush.");
        SetNavigationSelection(HomeNavButton);

        var content = Vertical();
        content.Margin = new Thickness(8, 10, 8, 28);
        var welcome = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        welcome.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        welcome.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        welcome.Children.Add(new TextBlock
        {
            Text = "Your desk, ready for play.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 31,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#383A34")
        });
        var dateText = Text(DateTime.Today.ToString("dddd, MMMM d", CultureInfo.CurrentCulture), 12, Brush("#6D6B63"));
        dateText.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(dateText, 1);
        welcome.Children.Add(dateText);
        content.Children.Add(welcome);
        content.Children.Add(Text("Pick up a favorite, try today's small challenge, or find something new.",
            14, Brush("#6D6B63")));

        var featureRow = new Grid { Margin = new Thickness(0, 17, 0, 23), Height = 208 };
        featureRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
        featureRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var featuredGrid = new Grid();
        featuredGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        featuredGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(144) });
        var featuredCopy = Vertical(8);
        featuredCopy.VerticalAlignment = VerticalAlignment.Center;
        featuredCopy.Children.Add(Text("Start with a favorite", 13, Brush("#D8E0D0"), true));
        featuredCopy.Children.Add(new TextBlock
        {
            Text = "2048",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 36,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });
        featuredCopy.Children.Add(Text($"Best score  {Stat("2048", "best"):N0}", 13, Brush("#E0E7D9")));
        featuredCopy.Children.Add(Text("Open 2048   ›", 12, Brushes.White, true));
        Grid.SetColumn(featuredCopy, 0);
        featuredGrid.Children.Add(featuredCopy);
        var tilePreview = CreateTilePreview();
        Grid.SetColumn(tilePreview, 1);
        featuredGrid.Children.Add(tilePreview);

        var feature = new Border
        {
            Background = Brush("#536448"),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(22),
            Margin = new Thickness(0, 0, 8, 0),
            Child = featuredGrid,
            Cursor = Cursors.Hand
        };
        var featureButton = new Button
        {
            Content = feature,
            Style = (Style)FindResource("FlatButton"),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = "Continue with 2048"
        };
        AutomationProperties.SetName(featureButton,
            $"Play 2048. Personal best score: {Stat("2048", "best"):N0}.");
        featureButton.Click += (_, _) => OpenGame("2048");
        Grid.SetColumn(featureButton, 0);
        featureRow.Children.Add(featureButton);

        var challenge = BuildDailyChallengeCard();
        Grid.SetColumn(challenge, 1);
        featureRow.Children.Add(challenge);
        content.Children.Add(featureRow);

        var libraryHeading = new Grid { Margin = new Thickness(0, 2, 0, 3) };
        libraryHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        libraryHeading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        libraryHeading.Children.Add(Text("The collection", 21, Brush("#383A34"), true));
        var scoreLink = MakeButton("View your records", ShowRecords);
        Grid.SetColumn(scoreLink, 1);
        libraryHeading.Children.Add(scoreLink);
        content.Children.Add(libraryHeading);

        var cards = new UniformGrid { Columns = 2, Margin = new Thickness(-5, 5, -5, 0) };
        for (var i = 0; i < GameIds.Length; i++)
        {
            cards.Children.Add(CreateGameCard(i));
        }
        content.Children.Add(cards);

        MainContent.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        RefreshDailyChallengeUi();
    }

    private void UpdateHeader(string title, string subtitle)
    {
        HeaderTitle.Text = title;
        HeaderSubtitle.Text = subtitle;
    }

    private void SetNavigationSelection(Button selected)
    {
        foreach (var button in new[] { HomeNavButton, RecordsNavButton, PrivacyNavButton })
        {
            button.Background = button == selected ? Brush("#E1E5DA") : Brushes.Transparent;
            button.FontWeight = button == selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private Border BuildDailyChallengeCard()
    {
        var body = Vertical(7);
        body.Children.Add(Text("A small goal for today", 12, Brush("#536448"), true));
        _dailyTitle = Text("", 18, Brush("#383A34"), true);
        body.Children.Add(_dailyTitle);
        _dailyDescription = Text("", 12, Brush("#6D6B63"));
        body.Children.Add(_dailyDescription);

        var progressRow = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _dailyProgressBar = new ProgressBar
        {
            Minimum = 0,
            Height = 7,
            Foreground = Brush("#536448"),
            Background = Brush("#D7DBD0"),
            VerticalAlignment = VerticalAlignment.Center
        };
        progressRow.Children.Add(_dailyProgressBar);
        _dailyProgressLabel = Text("", 11, Brush("#536448"), true);
        _dailyProgressLabel.Margin = new Thickness(9, 0, 0, 0);
        Grid.SetColumn(_dailyProgressLabel, 1);
        progressRow.Children.Add(_dailyProgressLabel);
        body.Children.Add(progressRow);

        var play = MakeButton("Play today's game", PlayDailyChallenge, true);
        play.HorizontalAlignment = HorizontalAlignment.Left;
        body.Children.Add(play);

        var card = new Border
        {
            Background = Brush("#E9EDE5"),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(20, 17, 20, 14),
            Margin = new Thickness(8, 0, 0, 0),
            Child = body
        };
        RefreshDailyChallengeUi();
        return card;
    }

    private int DailyGameIndex => (DateTime.Today.DayOfYear + DateTime.Today.Year) % GameIds.Length;

    private int DailyGoal => DailyGameIndex switch
    {
        0 => 1,
        1 => 3,
        2 => 1,
        3 => 256,
        _ => 15
    };

    private string DailyGoalDescription => DailyGameIndex switch
    {
        0 => "Win a local Connect Four match.",
        1 => "Win three rounds in a row.",
        2 => "Find every pair in 30 moves or fewer.",
        3 => "Make a tile worth 256.",
        _ => "Score 15 catches in one round."
    };

    private int TodayKey => int.Parse(DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
        CultureInfo.InvariantCulture);

    private bool DailyChallengeComplete => Stat("playroom", "dailyCompletedDate") == TodayKey;

    private void EnsureDailyChallenge()
    {
        var today = TodayKey;
        if (Stat("playroom", "dailyDate") == today) return;
        SetStat("playroom", "dailyDate", today);
        SetStat("playroom", "dailyProgress", 0);
    }

    private void RefreshDailyChallengeUi()
    {
        if (!IsInitialized) return;
        EnsureDailyChallenge();
        var index = DailyGameIndex;
        var progress = Math.Clamp(Stat("playroom", "dailyProgress"), 0, DailyGoal);
        var complete = DailyChallengeComplete;
        var title = complete ? "Today's goal is yours" : GameNames[index];
        UpdateText(_dailyTitle, title);
        UpdateText(_dailyDescription, complete ? "Come back tomorrow for a fresh challenge." : DailyGoalDescription);
        if (_dailyProgressBar is not null)
        {
            _dailyProgressBar.Maximum = DailyGoal;
            _dailyProgressBar.Value = complete ? DailyGoal : progress;
        }
        UpdateText(_dailyProgressLabel, complete ? "Done" : DailyProgressText(progress, index));
        UpdateText(SidebarDailyTitle, complete ? "Goal complete" : GameNames[index]);
        UpdateText(SidebarDailyDescription, complete ? "A new one arrives tomorrow." : DailyGoalDescription);
        if (SidebarDailyProgress is not null)
        {
            SidebarDailyProgress.Maximum = DailyGoal;
            SidebarDailyProgress.Value = complete ? DailyGoal : progress;
        }
        var streak = ActiveDailyStreak;
        UpdateText(SidebarStreak, streak > 0
            ? $"🔥  {streak}-day play streak"
            : "Complete a daily goal to start a streak.");
        if (DailyPlayButton is not null)
            DailyPlayButton.Content = complete ? "Play again" : "Play today's challenge";
    }

    private string DailyProgressText(int progress, int gameIndex) =>
        gameIndex == 3 ? $"{progress} / 256" : $"{progress} / {DailyGoal}";

    private int ActiveDailyStreak
    {
        get
        {
            var completed = Stat("playroom", "dailyCompletedDate");
            if (completed == TodayKey) return Stat("playroom", "dailyStreak");
            var yesterday = int.Parse(DateTime.Today.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
            return completed == yesterday ? Stat("playroom", "dailyStreak") : 0;
        }
    }

    private void PlayDailyChallenge() => OpenGame(GameIds[DailyGameIndex]);

    private void AdvanceDailyChallenge(int progress)
    {
        EnsureDailyChallenge();
        if (DailyChallengeComplete) return;
        var current = Math.Clamp(progress, 0, DailyGoal);
        SetStat("playroom", "dailyProgress", current);
        if (current >= DailyGoal)
        {
            var yesterday = int.Parse(DateTime.Today.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
            var streak = Stat("playroom", "dailyCompletedDate") == yesterday
                ? Stat("playroom", "dailyStreak") + 1
                : 1;
            SetStat("playroom", "dailyCompletedDate", TodayKey);
            SetStat("playroom", "dailyStreak", streak);
            SetStat("playroom", "dailyChallengesCompleted",
                Stat("playroom", "dailyChallengesCompleted") + 1);
        }
        RefreshDailyChallengeUi();
    }

    private void HomeNav_Click(object sender, RoutedEventArgs e) => ShowHome();

    private void RecordsNav_Click(object sender, RoutedEventArgs e) => ShowRecords();

    private void PrivacyNav_Click(object sender, RoutedEventArgs e) => ShowPrivacy();

    private void DailyPlay_Click(object sender, RoutedEventArgs e) => PlayDailyChallenge();

    private void ShowRecords()
    {
        StopGameTimers();
        _currentGame = null;
        BackButton.Visibility = Visibility.Collapsed;
        SetNavigationSelection(RecordsNavButton);
        UpdateHeader("Hall of Play", "Personal records, saved on this PC and shared with nobody.");

        var page = Vertical();
        page.Margin = new Thickness(8, 8, 8, 26);
        page.Children.Add(new TextBlock
        {
            Text = "Your best moments.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 31,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#383A34"),
            Margin = new Thickness(0, 0, 0, 5)
        });
        page.Children.Add(Text("Every game keeps its own record. There are no online leaderboards or accounts.",
            13, Brush("#6D6B63")));

        var totals = new UniformGrid { Columns = 3, Margin = new Thickness(-5, 14, -5, 14) };
        totals.Children.Add(SummaryCard("Games played", TotalGamesPlayed.ToString("N0"), "Across all five games"));
        totals.Children.Add(SummaryCard("Play streak", $"{ActiveDailyStreak} days", "Complete a daily goal"));
        totals.Children.Add(SummaryCard("Daily goals", Stat("playroom", "dailyChallengesCompleted").ToString("N0"),
            "Challenges completed"));
        page.Children.Add(totals);

        var records = Vertical(8);
        foreach (var index in Enumerable.Range(0, GameIds.Length))
            records.Children.Add(CreateRecordRow(index));
        page.Children.Add(Panel(records, new Thickness(15, 15, 15, 15)));

        var milestoneHeading = Text("Little milestones", 19, Brush("#383A34"), true);
        milestoneHeading.Margin = new Thickness(0, 18, 0, 8);
        page.Children.Add(milestoneHeading);
        var badges = new UniformGrid { Columns = 2, Margin = new Thickness(-4, 0, -4, 0) };
        badges.Children.Add(AchievementCard("A little of everything",
            GameIds.All(id => Stat(id, "gamesPlayed") > 0),
            $"{GameIds.Count(id => Stat(id, "gamesPlayed") > 0)} of {GameIds.Length} games tried"));
        badges.Children.Add(AchievementCard("Three-day rhythm", ActiveDailyStreak >= 3,
            "Complete a daily goal three days running"));
        badges.Children.Add(AchievementCard("Card sharp", Stat("memory-match", "bestMoves") is > 0 and <= 24,
            Stat("memory-match", "bestMoves") is > 0
                ? $"Best clear: {Stat("memory-match", "bestMoves")} moves"
                : "Clear all pairs in 24 moves or fewer"));
        badges.Children.Add(AchievementCard("The big merge", Stat("2048", "bestTile") >= 2048,
            "Build a 2,048 tile in 2048"));
        page.Children.Add(badges);

        MainContent.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = page
        };
        RefreshDailyChallengeUi();
    }

    private int TotalGamesPlayed => GameIds.Sum(id => Stat(id, "gamesPlayed"));

    private Border SummaryCard(string label, string value, string detail)
    {
        var body = Vertical(2);
        body.Children.Add(Text(label, 12, Brush("#6D6B63")));
        body.Children.Add(Text(value, 23, Brush("#383A34"), true));
        body.Children.Add(Text(detail, 11, Brush("#6D6B63")));
        return new Border
        {
            Background = Brush("#EFEDE5"),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Margin = new Thickness(4),
            Child = body
        };
    }

    private Border CreateRecordRow(int index)
    {
        var (value, caption, hasRecord) = index switch
        {
            0 => (Stat("connect-four", "wins").ToString("N0"), "local wins", Stat("connect-four", "wins") > 0),
            1 => (Stat("rock-paper-scissors", "bestStreak").ToString("N0"), "wins in a row",
                Stat("rock-paper-scissors", "bestStreak") > 0),
            2 => (Stat("memory-match", "bestMoves") == 0 ? "—" : Stat("memory-match", "bestMoves").ToString(),
                "fewest moves", Stat("memory-match", "bestMoves") > 0),
            3 => (Stat("2048", "best").ToString("N0"), "best points", Stat("2048", "best") > 0),
            _ => (Stat("whack-a-mole", "best").ToString("N0"), "best catches",
                Stat("whack-a-mole", "best") > 0)
        };

        var grid = new Grid { MinHeight = 62 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var art = CreateGameArt(index);
        Grid.SetColumn(art, 0);
        grid.Children.Add(art);
        var title = Vertical(2);
        title.VerticalAlignment = VerticalAlignment.Center;
        title.Children.Add(Text(GameNames[index], 14, Brush("#383A34"), true));
        title.Children.Add(Text(caption, 11, Brush("#6D6B63")));
        Grid.SetColumn(title, 1);
        grid.Children.Add(title);

        var record = Text(hasRecord ? value : "—", 17, Brush("#536448"), true);
        record.HorizontalAlignment = HorizontalAlignment.Right;
        record.Margin = new Thickness(8, 0, 5, 0);
        Grid.SetColumn(record, 2);
        grid.Children.Add(record);
        var play = MakeButton("Play", () => OpenGame(GameIds[index]), false, 62);
        Grid.SetColumn(play, 3);
        grid.Children.Add(play);

        return new Border
        {
            Background = index % 2 == 0 ? Brush("#F8F6EF") : Brushes.Transparent,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 3, 5, 3),
            Child = grid
        };
    }

    private Border AchievementCard(string title, bool earned, string detail)
    {
        var copy = Vertical(3);
        copy.Children.Add(Text(earned ? $"✦  {title}" : $"◇  {title}", 13,
            earned ? Brush("#536448") : Brush("#6D6B63"), true));
        copy.Children.Add(Text(detail, 11, Brush("#6D6B63")));
        return new Border
        {
            Background = earned ? Brush("#E9EDE5") : Brush("#F0EEE7"),
            CornerRadius = new CornerRadius(13),
            Padding = new Thickness(13, 11, 13, 11),
            Margin = new Thickness(4),
            Opacity = earned ? 1 : 0.78,
            Child = copy
        };
    }

    private void ShowPrivacy()
    {
        StopGameTimers();
        _currentGame = null;
        BackButton.Visibility = Visibility.Collapsed;
        SetNavigationSelection(PrivacyNavButton);
        UpdateHeader("Privacy & data", "You decide what stays on this PC.");

        var page = Vertical();
        page.Margin = new Thickness(8, 8, 8, 24);
        page.Children.Add(new TextBlock
        {
            Text = "A quiet game should stay private.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#383A34"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 7)
        });
        page.Children.Add(Text("Playroom works without an account, internet connection, or administrator access.",
            14, Brush("#6D6B63")));

        var details = Vertical(12);
        details.Children.Add(PrivacyFact("No network access",
            "The Windows app does not connect to servers, show ads, use analytics, or send your activity anywhere."));
        details.Children.Add(PrivacyFact("Only on this PC",
            $"Scores and challenge streaks are stored in {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Playroom."));
        details.Children.Add(PrivacyFact("No special access",
            "The app runs as your Windows user. It does not request administrator privileges or install background services."));
        details.Children.Add(PrivacyFact("You can clear everything",
            "Removing your local scores also resets records, challenge progress, and streaks."));
        page.Children.Add(Panel(details));

        var clear = MakeButton("Clear all local game data", ClearLocalData, false);
        clear.HorizontalAlignment = HorizontalAlignment.Left;
        clear.Margin = new Thickness(0, 16, 0, 0);
        page.Children.Add(clear);
        var signingNote = Text("The portable download is not signed with a trusted publisher certificate, so Windows may still show a SmartScreen warning. This does not change the app's offline behavior.",
            12, Brush("#6D6B63"));
        signingNote.Margin = new Thickness(0, 17, 0, 0);
        signingNote.MaxWidth = 680;
        page.Children.Add(signingNote);

        MainContent.Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = page
        };
    }

    private Border PrivacyFact(string title, string detail)
    {
        var body = Vertical(3);
        body.Children.Add(Text(title, 14, Brush("#383A34"), true));
        body.Children.Add(Text(detail, 12, Brush("#6D6B63")));
        return new Border
        {
            Background = Brush("#F8F6EF"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Child = body
        };
    }

    private void ClearLocalData()
    {
        var answer = MessageBox.Show(this,
            "This clears every saved score, personal record, achievement, and daily streak from this PC. This cannot be undone.",
            "Clear local game data?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            if (File.Exists(_statsPath)) File.Delete(_statsPath);
            _stats.Clear();
            _rpsWins = 0;
            _rpsTies = 0;
            _rpsStreak = 0;
            _persistenceAvailable = true;
            _saveWarningShown = false;
            EnsureDailyChallenge();
            RefreshDailyChallengeUi();
            MessageBox.Show(this, "Your local game data has been cleared.",
                "Data cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException exception)
        {
            MessageBox.Show(this, $"Your local game data could not be cleared:\n{exception.Message}",
                "Could not clear data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (UnauthorizedAccessException exception)
        {
            MessageBox.Show(this, $"Windows did not allow Playroom to clear its score file:\n{exception.Message}",
                "Could not clear data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private UIElement CreateTilePreview()
    {
        var grid = new UniformGrid
        {
            Rows = 2,
            Columns = 2,
            Width = 180,
            Height = 126,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var (value, color) in new[]
        {
            ("2", "#F8F2DF"), ("4", "#F0E7CD"), ("8", "#E9BD7D"), ("", "#708365")
        })
        {
            grid.Children.Add(new Border
            {
                Background = Brush(color),
                CornerRadius = new CornerRadius(13),
                Margin = new Thickness(5),
                Child = Text(value, 24, Brush("#536448"), true)
            });
        }
        return grid;
    }

    private UIElement CreateGameCard(int index)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var art = CreateGameArt(index);
        Grid.SetColumn(art, 0);
        grid.Children.Add(art);
        var copy = Vertical(3);
        copy.VerticalAlignment = VerticalAlignment.Center;
        copy.Children.Add(Text(GameNames[index], 15, Brush("#383A34"), true));
        copy.Children.Add(Text(GameDescriptions[index], 12, Brush("#6D6B63")));
        var (record, label, hasRecord) = index switch
        {
            0 => (Stat("connect-four", "wins"), "wins", true),
            1 => (Stat("rock-paper-scissors", "bestStreak"), "best streak", true),
            2 => (Stat("memory-match", "bestMoves"), "fewest moves", false),
            3 => (Stat("2048", "best"), "best score", true),
            _ => (Stat("whack-a-mole", "best"), "best catches", true)
        };
        var recordText = index == 2
            ? record == 0 ? "No clear yet" : $"Best clear · {record} moves"
            : hasRecord ? $"{label} · {record:N0}" : "";
        if (recordText.Length > 0)
            copy.Children.Add(Text(recordText, 10, Brush("#536448"), true));
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        var arrow = Text("›", 28, Brush("#879779"));
        arrow.HorizontalAlignment = HorizontalAlignment.Center;
        arrow.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(arrow, 2);
        grid.Children.Add(arrow);

        var button = new Button
        {
            Content = grid,
            Style = (Style)FindResource("CellButton"),
            Background = Brushes.Transparent,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Height = 98,
            Margin = new Thickness(5),
            Padding = new Thickness(8),
            Cursor = Cursors.Hand,
            ToolTip = GameDescriptions[index]
        };
        AutomationProperties.SetName(button, $"{GameNames[index]}. {GameDescriptions[index]}");
        button.Click += (_, _) => OpenGame(GameIds[index]);
        return button;
    }

    private UIElement CreateGameArt(int index)
    {
        var art = new Border
        {
            Width = 54,
            Height = 50,
            CornerRadius = new CornerRadius(14),
            Background = Brush(index switch
            {
                0 => "#EFF0E8",
                1 => "#F8ECE7",
                2 => "#F1EDF5",
                _ => "#F6F0DF"
            }),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var label = Text(index switch
        {
            0 => "● ●",
            1 => "✊",
            2 => "✿",
            _ => "●"
        }, index == 1 ? 25 : 19, Brush(index switch
        {
            0 => "#879779",
            1 => "#DF7868",
            2 => "#877AA2",
            _ => "#E8BC69"
        }), true);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        art.Child = label;
        return art;
    }

    private void OpenGame(string id)
    {
        StopGameTimers();
        _currentGame = id;
        BackButton.Visibility = Visibility.Visible;
        var index = Array.IndexOf(GameIds, id);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown game");
        UpdateHeader(GameNames[index], id == "2048"
            ? "Use the arrow keys to slide and merge tiles."
            : "A quick game. Your saved record stays yours.");
        SetNavigationSelection(HomeNavButton);
        MainContent.Content = id switch
        {
            "connect-four" => CreateConnectFour(),
            "rock-paper-scissors" => CreateRockPaperScissors(),
            "memory-match" => CreateMemoryMatch(),
            "2048" => Create2048(),
            "whack-a-mole" => CreateWhackAMole(),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown game")
        };
        MainContent.Focus();
    }

    private UIElement GamePage(string title, string subtitle, UIElement gamePanel)
    {
        var page = Vertical();
        page.Margin = new Thickness(8, 8, 8, 24);
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        heading.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 29,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brush("#383A34")
        });
        var subtitleBlock = Text(subtitle, 13, Brush("#6D6B63"));
        subtitleBlock.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleBlock.TextAlignment = TextAlignment.Center;
        subtitleBlock.Margin = new Thickness(0, 5, 0, 0);
        heading.Children.Add(subtitleBlock);
        page.Children.Add(heading);
        page.Children.Add(Panel(gamePanel));
        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = page
        };
    }

    private StackPanel NewGamePanel() => Vertical();

    private void AddToolbar(StackPanel panel, params UIElement[] items)
    {
        var bar = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        for (var i = 0; i < items.Length; i++)
            bar.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = i == items.Length - 1
                    ? GridLength.Auto
                    : new GridLength(1, GridUnitType.Star)
            });
        for (var i = 0; i < items.Length; i++)
        {
            Grid.SetColumn(items[i], i);
            bar.Children.Add(items[i]);
        }
        panel.Children.Add(bar);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => ShowHome();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _currentGame is not null)
        {
            ShowHome();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.R)
        {
            switch (_currentGame)
            {
                case "connect-four": ResetConnectFour(); e.Handled = true; return;
                case "memory-match": ResetMemoryMatch(); e.Handled = true; return;
                case "2048": Reset2048(); e.Handled = true; return;
            }
        }
        var digit = KeyDigit(e.Key);
        if (_currentGame == "connect-four" && digit is >= 1 and <= 7)
        {
            DropConnectDisc(digit - 1);
            e.Handled = true;
            return;
        }
        if (_currentGame == "rock-paper-scissors")
        {
            if (digit == 1) _ = PlayRpsAsync("rock");
            else if (digit == 2) _ = PlayRpsAsync("paper");
            else if (digit == 3) _ = PlayRpsAsync("scissors");
            else return;
            e.Handled = true;
            return;
        }
        if (_currentGame == "whack-a-mole")
        {
            if (e.Key == Key.Space && _moleClock?.IsEnabled != true)
            {
                StartMoleRound();
                e.Handled = true;
                return;
            }
            if (digit is >= 1 and <= 9)
            {
                HitMole(digit - 1);
                e.Handled = true;
                return;
            }
        }
        if (_currentGame != "2048") return;
        switch (e.Key)
        {
            case Key.Left:
            case Key.A:
                Move2048(0, -1);
                e.Handled = true;
                break;
            case Key.Right:
            case Key.D:
                Move2048(0, 1);
                e.Handled = true;
                break;
            case Key.Up:
            case Key.W:
                Move2048(-1, 0);
                e.Handled = true;
                break;
            case Key.Down:
            case Key.S:
                Move2048(1, 0);
                e.Handled = true;
                break;
        }
    }

    private static int KeyDigit(Key key) => key switch
    {
        Key.D1 or Key.NumPad1 => 1,
        Key.D2 or Key.NumPad2 => 2,
        Key.D3 or Key.NumPad3 => 3,
        Key.D4 or Key.NumPad4 => 4,
        Key.D5 or Key.NumPad5 => 5,
        Key.D6 or Key.NumPad6 => 6,
        Key.D7 or Key.NumPad7 => 7,
        Key.D8 or Key.NumPad8 => 8,
        Key.D9 or Key.NumPad9 => 9,
        _ => 0
    };

    private void StopGameTimers()
    {
        _memoryMismatchTimer?.Stop();
        _memoryMismatchTimer = null;
        _moleClock?.Stop();
        _moleClock = null;
        _moleSpawner?.Stop();
        _moleSpawner = null;
        _activeMole = -1;
        _rpsRoundId++;
        _rpsBusy = false;
    }

    private void LoadStats()
    {
        if (!File.Exists(_statsPath)) return;
        try
        {
            var file = new FileInfo(_statsPath);
            if (file.Length > 64 * 1024)
            {
                MarkPersistenceUnavailable("The local score file is larger than expected. Playroom left it untouched and will not save over it.");
                return;
            }
            var loaded = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(
                File.ReadAllText(_statsPath), new JsonSerializerOptions { MaxDepth = 8 });
            if (loaded is null || loaded.Count > AllowedStatKeys.Count)
            {
                MarkPersistenceUnavailable("The local score file has an unsupported format. Playroom left it untouched.");
                return;
            }
            foreach (var entry in loaded)
            {
                if (!AllowedStatKeys.TryGetValue(entry.Key, out var allowedKeys)
                    || entry.Value is null
                    || entry.Value.Count > allowedKeys.Count
                    || entry.Value.Any(stat => !allowedKeys.Contains(stat.Key)
                        || stat.Value < 0 || stat.Value > 1_000_000_000))
                {
                    MarkPersistenceUnavailable("The local score file contains invalid or unsupported data. Playroom left it untouched.");
                    return;
                }
                _stats[entry.Key] = new Dictionary<string, int>(entry.Value, StringComparer.Ordinal);
            }
        }
        catch (JsonException exception)
        {
            MarkPersistenceUnavailable($"The local score file could not be read. It was left untouched.\n{exception.Message}");
        }
        catch (IOException exception)
        {
            MarkPersistenceUnavailable($"The local score file could not be opened. It was left untouched.\n{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            MarkPersistenceUnavailable($"Windows did not allow Playroom to read its local score file.\n{exception.Message}");
        }
    }

    private int Stat(string game, string key) =>
        _stats.TryGetValue(game, out var values) && values.TryGetValue(key, out var value)
            ? value
            : 0;

    private void SetStat(string game, string key, int value)
    {
        if (!AllowedStatKeys.TryGetValue(game, out var allowedKeys) || !allowedKeys.Contains(key))
            throw new ArgumentOutOfRangeException(nameof(key), "This score is not part of Playroom's local save format.");
        if (value is < 0 or > 1_000_000_000)
            throw new ArgumentOutOfRangeException(nameof(value), "Saved game values must be non-negative and in range.");
        if (!_stats.TryGetValue(game, out var values))
            _stats[game] = values = new Dictionary<string, int>(StringComparer.Ordinal);
        values[key] = value;
        SaveStats();
    }

    private void RecordGameCompleted(string game)
    {
        SetStat(game, "gamesPlayed", Stat(game, "gamesPlayed") + 1);
    }

    private void SaveStats()
    {
        if (!_persistenceAvailable) return;
        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_statsPath)!);
            var data = JsonSerializer.SerializeToUtf8Bytes(_stats, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            if (data.Length > 64 * 1024)
            {
                MarkPersistenceUnavailable("The local score file reached its size limit. Existing scores were left untouched.");
                return;
            }
            temporaryPath = $"{_statsPath}.{Guid.NewGuid():N}.tmp";
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(data);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, _statsPath, overwrite: true);
        }
        catch (IOException exception)
        {
            MarkPersistenceUnavailable($"Your latest score could not be saved. Existing scores were left untouched.\n{exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            MarkPersistenceUnavailable($"Windows did not allow Playroom to save scores. Existing scores were left untouched.\n{exception.Message}");
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
                catch (IOException exception)
                {
                    MarkPersistenceUnavailable($"A temporary score file could not be cleaned up.\n{exception.Message}");
                }
                catch (UnauthorizedAccessException exception)
                {
                    MarkPersistenceUnavailable($"Windows did not allow cleanup of a temporary score file.\n{exception.Message}");
                }
            }
        }
    }

    private void MarkPersistenceUnavailable(string message)
    {
        _persistenceAvailable = false;
        if (_saveWarningShown) return;
        _saveWarningShown = true;
        MessageBox.Show(this, message, "Playroom local scores", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void Pulse(FrameworkElement element)
    {
        if (!SystemParameters.ClientAreaAnimation) return;
        var transform = new ScaleTransform(0.94, 0.94);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = transform;
        var animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(190))
        {
            EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
        };
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static void AnimateTile(Button tile, double offsetX, double offsetY, bool merged)
    {
        if (Math.Abs(offsetX) < 0.5 && Math.Abs(offsetY) < 0.5 && !merged) return;
        if (!SystemParameters.ClientAreaAnimation) return;
        var translate = new TranslateTransform(offsetX, offsetY);
        var scale = new ScaleTransform(merged ? 0.82 : 0.94, merged ? 0.82 : 0.94);
        var transform = new TransformGroup();
        transform.Children.Add(translate);
        transform.Children.Add(scale);
        tile.RenderTransformOrigin = new Point(0.5, 0.5);
        tile.RenderTransform = transform;
        var duration = TimeSpan.FromMilliseconds(135);
        translate.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        var pop = new DoubleAnimation(1, TimeSpan.FromMilliseconds(175))
        {
            EasingFunction = new BackEase { Amplitude = 0.24, EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private void UpdateText(TextBlock? target, string value)
    {
        if (target is not null) target.Text = value;
    }

    private UIElement CreateConnectFour()
    {
        var panel = NewGamePanel();
        _connectTurn = Text("Coral's turn", 15, Brush("#DF7868"), true);
        var newGame = MakeButton("New game", ResetConnectFour);
        var toolbar = Horizontal();
        toolbar.Children.Add(_connectTurn);
        toolbar.Children.Add(newGame);
        panel.Children.Add(toolbar);

        var board = new Grid
        {
            Background = Brush("#839474"),
            Margin = new Thickness(0, 10, 0, 0),
            Height = 410,
            MaxWidth = 680,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        for (var column = 0; column < 7; column++)
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var row = 0; row < 6; row++)
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _connectButtons = new Button[6, 7];
        for (var row = 0; row < 6; row++)
        for (var column = 0; column < 7; column++)
        {
            var hole = new Ellipse
            {
                Fill = Brush("#F7F5EF"),
                Width = 42,
                Height = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var button = new Button
            {
                Content = hole,
                Style = (Style)FindResource("CellButton"),
                Background = Brushes.Transparent,
                Padding = new Thickness(0)
            };
            AutomationProperties.SetName(button, $"Column {column + 1}, row {row + 1}, empty");
            var targetColumn = column;
            button.Click += (_, _) => DropConnectDisc(targetColumn);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, column);
            board.Children.Add(button);
            _connectButtons[row, column] = button;
        }
        panel.Children.Add(board);
        var instructions = Text("Two players · click a column or press 1–7.", 13, Brush("#6D6B63"));
        instructions.HorizontalAlignment = HorizontalAlignment.Center;
        instructions.Margin = new Thickness(0, 11, 0, 0);
        panel.Children.Add(instructions);
        ResetConnectFour();
        return GamePage("Connect Four", "Take turns. Connect four. Have a lovely little victory.", panel);
    }

    private void ResetConnectFour()
    {
        _connectBoard = new int[6, 7];
        _connectPlayer = 1;
        _connectEnded = false;
        UpdateText(_connectTurn, "Coral's turn");
        if (_connectTurn is not null) _connectTurn.Foreground = Brush("#DF7868");
        if (_connectButtons.Length == 0) return;
        for (var row = 0; row < 6; row++)
        for (var column = 0; column < 7; column++)
        {
            _connectButtons[row, column].Content = new Ellipse
            {
                Fill = Brush("#F7F5EF"),
                Width = 42,
                Height = 42,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            AutomationProperties.SetName(_connectButtons[row, column], $"Column {column + 1}, row {row + 1}, empty");
        }
    }

    private void DropConnectDisc(int column)
    {
        if (_connectEnded) return;
        var row = 5;
        while (row >= 0 && _connectBoard[row, column] != 0) row--;
        if (row < 0) return;

        var player = _connectPlayer;
        _connectBoard[row, column] = player;
        var color = player == 1 ? "#DF7868" : "#E8BC69";
        var disc = new Ellipse
        {
            Fill = Brush(color),
            Width = 42,
            Height = 42,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _connectButtons[row, column].Content = disc;
        AutomationProperties.SetName(_connectButtons[row, column],
            $"Column {column + 1}, row {row + 1}, {(player == 1 ? "Coral" : "Gold")} disc");
        AnimateConnectFourDrop(disc, _connectButtons[row, column], row);

        var line = WinningLine(row, column, player);
        if (line.Count >= 4)
        {
            _connectEnded = true;
            UpdateText(_connectTurn, $"{(player == 1 ? "Coral" : "Gold")} wins!");
            if (_connectTurn is not null) _connectTurn.Foreground = Brush("#536448");
            foreach (var (winningRow, winningColumn) in line)
            {
                if (_connectButtons[winningRow, winningColumn].Content is Ellipse winningDisc)
                    Pulse(winningDisc);
            }
            SetStat("connect-four", "wins", Stat("connect-four", "wins") + 1);
            RecordGameCompleted("connect-four");
            if (DailyGameIndex == 0) AdvanceDailyChallenge(1);
        }
        else if (Enumerable.Range(0, 7).All(c => _connectBoard[0, c] != 0))
        {
            _connectEnded = true;
            UpdateText(_connectTurn, "It's a lovely draw.");
            if (_connectTurn is not null) _connectTurn.Foreground = Brush("#6D6B63");
            RecordGameCompleted("connect-four");
        }
        else
        {
            _connectPlayer = player == 1 ? 2 : 1;
            var coralTurn = _connectPlayer == 1;
            UpdateText(_connectTurn, coralTurn ? "Coral's turn" : "Gold's turn");
            if (_connectTurn is not null)
                _connectTurn.Foreground = Brush(coralTurn ? "#DF7868" : "#B88E44");
        }
    }

    private List<(int Row, int Column)> WinningLine(int row, int column, int player)
    {
        foreach (var (rowStep, columnStep) in new[] { (0, 1), (1, 0), (1, 1), (1, -1) })
        {
            var line = new List<(int Row, int Column)> { (row, column) };
            foreach (var sign in new[] { -1, 1 })
            {
                var r = row + rowStep * sign;
                var c = column + columnStep * sign;
                while (r is >= 0 and < 6 && c is >= 0 and < 7 && _connectBoard[r, c] == player)
                {
                    line.Add((r, c));
                    r += rowStep * sign;
                    c += columnStep * sign;
                }
            }
            if (line.Count >= 4) return line;
        }
        return [];
    }

    private static void AnimateConnectFourDrop(Ellipse disc, Button cell, int row)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            Pulse(disc);
            return;
        }
        var transform = new TranslateTransform(0, -Math.Max(cell.ActualHeight, 48) * (row + 1));
        disc.RenderTransform = transform;
        transform.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(270))
            {
                EasingFunction = new BounceEase
                {
                    Bounces = 2,
                    Bounciness = 1.35,
                    EasingMode = EasingMode.EaseOut
                }
            });
    }

    private UIElement CreateRockPaperScissors()
    {
        var panel = NewGamePanel();
        _rpsWins = Stat("rock-paper-scissors", "wins");
        _rpsTies = Stat("rock-paper-scissors", "ties");
        _rpsStreak = 0;
        SetStat("rock-paper-scissors", "currentStreak", 0);
        if (DailyGameIndex == 1) AdvanceDailyChallenge(0);
        _rpsWinsLabel = Text($"You  {_rpsWins}", 15, Brush("#383A34"), true);
        _rpsTiesLabel = Text($"Ties  {_rpsTies}", 15, Brush("#383A34"), true);
        var toolbar = Horizontal();
        toolbar.Children.Add(_rpsWinsLabel);
        toolbar.Children.Add(MakeButton("Reset score", ResetRpsScore));
        toolbar.Children.Add(_rpsTiesLabel);
        panel.Children.Add(toolbar);

        var duel = new Grid { Height = 190, Margin = new Thickness(0, 7, 0, 0) };
        duel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        duel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        duel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = HandPanel("YOU", false);
        var right = HandPanel("PLAYROOM", true);
        _playerHand = (TextBlock)left.Tag;
        _computerHand = (TextBlock)right.Tag;
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 2);
        duel.Children.Add(left);
        duel.Children.Add(right);
        var versus = Text("VS", 12, Brush("#6D6B63"), true);
        versus.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetColumn(versus, 1);
        duel.Children.Add(versus);
        panel.Children.Add(duel);

        _rpsStatus = Text("Choose your move · or press 1, 2, or 3.", 14, Brush("#6D6B63"));
        _rpsStatus.HorizontalAlignment = HorizontalAlignment.Center;
        _rpsStatus.Margin = new Thickness(0, 2, 0, 8);
        panel.Children.Add(_rpsStatus);
        var choices = Horizontal();
        foreach (var (name, label) in new[]
        {
            ("rock", "✊  Rock"), ("paper", "✋  Paper"), ("scissors", "✌️  Scissors")
        })
        {
            var button = MakeButton(label, () => _ = PlayRpsAsync(name), false, 150);
            button.Height = 62;
            button.Margin = new Thickness(5);
            choices.Children.Add(button);
        }
        choices.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(choices);
        return GamePage("Rock Paper Scissors", "One little countdown. One very important choice.", panel);
    }

    private Border HandPanel(string label, bool mirror)
    {
        var stack = Vertical(8);
        var caption = Text(label, 11, Brush("#6D6B63"), true);
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        var hand = Text("✊", 52, Brush("#383A34"), true);
        hand.HorizontalAlignment = HorizontalAlignment.Center;
        hand.TextAlignment = TextAlignment.Center;
        hand.Width = 108;
        hand.Height = 108;
        hand.Background = Brush("#F8F6EF");
        hand.RenderTransformOrigin = new Point(0.5, 0.5);
        hand.RenderTransform = new ScaleTransform(mirror ? -1 : 1, 1);
        stack.Children.Add(caption);
        stack.Children.Add(hand);
        var border = new Border
        {
            Child = stack,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        border.Tag = hand;
        return border;
    }

    private async Task PlayRpsAsync(string move)
    {
        if (_rpsBusy) return;
        _rpsBusy = true;
        var roundId = ++_rpsRoundId;
        var currentGame = _currentGame;
        UpdateText(_rpsStatus, "Rock… paper… scissors…");
        if (_rpsStatus is not null) _rpsStatus.Foreground = Brush("#6D6B63");
        if (_playerHand is not null) Pulse(_playerHand);
        if (_computerHand is not null) Pulse(_computerHand);
        await Task.Delay(260);
        if (roundId != _rpsRoundId || currentGame != _currentGame || currentGame != "rock-paper-scissors") return;

        var theirs = new[] { "rock", "paper", "scissors" }[_random.Next(3)];
        var hands = new Dictionary<string, string>
        {
            ["rock"] = "✊", ["paper"] = "✋", ["scissors"] = "✌️"
        };
        UpdateText(_playerHand, hands[move]);
        UpdateText(_computerHand, hands[theirs]);
        if (_computerHand?.RenderTransform is ScaleTransform mirror) mirror.ScaleX = -1;

        if (move == theirs)
        {
            _rpsTies++;
            SetStat("rock-paper-scissors", "ties", _rpsTies);
            UpdateText(_rpsTiesLabel, $"Ties  {_rpsTies}");
            UpdateText(_rpsStatus, "A perfect tie. Great minds!");
            if (_rpsStatus is not null) _rpsStatus.Foreground = Brush("#6D6B63");
            _rpsStreak = 0;
            SetStat("rock-paper-scissors", "currentStreak", 0);
            if (DailyGameIndex == 1) AdvanceDailyChallenge(0);
        }
        else if ((move == "rock" && theirs == "scissors")
              || (move == "paper" && theirs == "rock")
              || (move == "scissors" && theirs == "paper"))
        {
            _rpsWins++;
            SetStat("rock-paper-scissors", "wins", _rpsWins);
            _rpsStreak++;
            SetStat("rock-paper-scissors", "currentStreak", _rpsStreak);
            if (_rpsStreak > Stat("rock-paper-scissors", "bestStreak"))
                SetStat("rock-paper-scissors", "bestStreak", _rpsStreak);
            UpdateText(_rpsWinsLabel, $"You  {_rpsWins}");
            UpdateText(_rpsStatus, "You win! Lovely choice.");
            if (DailyGameIndex == 1)
                AdvanceDailyChallenge(Stat("playroom", "dailyProgress") + 1);
            if (_rpsStatus is not null)
            {
                _rpsStatus.Foreground = Brush("#536448");
                Pulse(_rpsStatus);
            }
        }
        else
        {
            SetStat("rock-paper-scissors", "losses", Stat("rock-paper-scissors", "losses") + 1);
            UpdateText(_rpsStatus, "Playroom wins this one. Again?");
            if (_rpsStatus is not null) _rpsStatus.Foreground = Brush("#DF7868");
            _rpsStreak = 0;
            SetStat("rock-paper-scissors", "currentStreak", 0);
            if (DailyGameIndex == 1) AdvanceDailyChallenge(0);
        }
        RecordGameCompleted("rock-paper-scissors");
        _rpsBusy = false;
    }

    private void ResetRpsScore()
    {
        _rpsWins = 0;
        _rpsTies = 0;
        _rpsStreak = 0;
        SetStat("rock-paper-scissors", "wins", 0);
        SetStat("rock-paper-scissors", "ties", 0);
        SetStat("rock-paper-scissors", "losses", 0);
        SetStat("rock-paper-scissors", "currentStreak", 0);
        UpdateText(_rpsWinsLabel, "You  0");
        UpdateText(_rpsTiesLabel, "Ties  0");
        UpdateText(_rpsStatus, "Choose your move.");
    }

    private UIElement CreateMemoryMatch()
    {
        var panel = NewGamePanel();
        _memoryMovesLabel = Text("Moves  0", 15, Brush("#383A34"), true);
        _memoryPairsLabel = Text("Pairs  0 / 8", 15, Brush("#383A34"), true);
        var toolbar = Horizontal();
        toolbar.Children.Add(_memoryMovesLabel);
        toolbar.Children.Add(MakeButton("Shuffle cards", ResetMemoryMatch));
        toolbar.Children.Add(_memoryPairsLabel);
        panel.Children.Add(toolbar);

        _memoryStatus = Text("Turn over two cards · use Tab and Enter to play by keyboard.", 13, Brush("#6D6B63"));
        _memoryStatus.HorizontalAlignment = HorizontalAlignment.Center;
        _memoryStatus.Margin = new Thickness(0, 0, 0, 8);
        panel.Children.Add(_memoryStatus);

        var grid = new UniformGrid { Rows = 4, Columns = 4, MaxWidth = 610, Height = 430 };
        _memoryButtons = new Button[16];
        _memoryMatched = new bool[16];
        _memoryDeck = Enumerable.Range(0, 8).SelectMany(value => new[] { value, value }).ToArray();
        Shuffle(_memoryDeck);
        for (var index = 0; index < _memoryDeck.Length; index++)
        {
            var cardIndex = index;
            var button = new Button
            {
                Content = "✳",
                Style = (Style)FindResource("CellButton"),
                FontSize = 30,
                Background = Brush("#F4F1E9"),
                Margin = new Thickness(5),
                MinHeight = 76
            };
            AutomationProperties.SetName(button, $"Card {index + 1}, face down");
            button.Click += (_, _) => FlipMemoryCard(cardIndex);
            grid.Children.Add(button);
            _memoryButtons[index] = button;
        }
        grid.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(grid);
        ResetMemoryMatch();
        return GamePage("Memory Match", "A few lovely cards. Can you find all eight pairs?", panel);
    }

    private void ResetMemoryMatch()
    {
        _memoryMismatchTimer?.Stop();
        _memoryFirst = null;
        _memoryMoves = 0;
        _memoryPairs = 0;
        _memoryDeck = Enumerable.Range(0, 8).SelectMany(value => new[] { value, value }).ToArray();
        Shuffle(_memoryDeck);
        _memoryMatched = new bool[16];
        UpdateText(_memoryMovesLabel, "Moves  0");
        UpdateText(_memoryPairsLabel, "Pairs  0 / 8");
        UpdateText(_memoryStatus, "Turn over two cards to find a pair.");
        if (_memoryButtons.Length == 0) return;
        for (var index = 0; index < _memoryButtons.Length; index++)
        {
            var button = _memoryButtons[index];
            button.Content = "✳";
            button.Background = Brush("#F4F1E9");
            button.IsEnabled = true;
            AutomationProperties.SetName(button, $"Card {index + 1}, face down");
            Pulse(button);
        }
    }

    private void FlipMemoryCard(int index)
    {
        if (index >= _memoryButtons.Length || _memoryMatched[index]
            || _memoryFirst == index || _memoryMismatchTimer?.IsEnabled == true)
            return;

        var button = _memoryButtons[index];
        button.Content = MemoryFaces[_memoryDeck[index]];
        button.Background = Brush("#F8F6EF");
        AutomationProperties.SetName(button, $"Card {index + 1}, {MemoryFaces[_memoryDeck[index]]}");
        Pulse(button);
        if (_memoryFirst is null)
        {
            _memoryFirst = index;
            return;
        }

        _memoryMoves++;
        UpdateText(_memoryMovesLabel, $"Moves  {_memoryMoves}");
        var first = _memoryFirst.Value;
        _memoryFirst = null;
        if (_memoryDeck[first] == _memoryDeck[index])
        {
            _memoryMatched[first] = true;
            _memoryMatched[index] = true;
            AutomationProperties.SetName(_memoryButtons[first],
                $"Card {first + 1}, {MemoryFaces[_memoryDeck[first]]}, matched");
            AutomationProperties.SetName(button, $"Card {index + 1}, {MemoryFaces[_memoryDeck[index]]}, matched");
            _memoryButtons[first].Background = Brush("#E8EEE2");
            button.Background = Brush("#E8EEE2");
            _memoryPairs++;
            UpdateText(_memoryPairsLabel, $"Pairs  {_memoryPairs} / 8");
            UpdateText(_memoryStatus, _memoryPairs == 8 ? "Lovely work. Every pair found!" : "A lovely match!");
            if (_memoryPairs == 8)
            {
                RecordGameCompleted("memory-match");
                var previous = Stat("memory-match", "bestMoves");
                if (previous == 0 || _memoryMoves < previous)
                    SetStat("memory-match", "bestMoves", _memoryMoves);
                if (DailyGameIndex == 2 && _memoryMoves <= 30)
                    AdvanceDailyChallenge(1);
            }
            return;
        }

        UpdateText(_memoryStatus, "Not quite. Try to remember those cards.");
        _memoryMismatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(720) };
        _memoryMismatchTimer.Tick += (_, _) =>
        {
            _memoryMismatchTimer.Stop();
            if (_memoryButtons.Length <= index || _memoryButtons.Length <= first) return;
            _memoryButtons[first].Content = "✳";
            _memoryButtons[index].Content = "✳";
            AutomationProperties.SetName(_memoryButtons[first], $"Card {first + 1}, face down");
            AutomationProperties.SetName(_memoryButtons[index], $"Card {index + 1}, face down");
            _memoryButtons[first].Background = Brush("#F4F1E9");
            _memoryButtons[index].Background = Brush("#F4F1E9");
        };
        _memoryMismatchTimer.Start();
    }

    private void Shuffle<T>(IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var other = _random.Next(i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }

    private UIElement Create2048()
    {
        var panel = NewGamePanel();
        _numberScoreLabel = Text("Score  0", 15, Brush("#383A34"), true);
        _numberBestLabel = Text($"Best  {Stat("2048", "best")}", 15, Brush("#383A34"), true);
        var toolbar = Horizontal();
        toolbar.Children.Add(_numberScoreLabel);
        toolbar.Children.Add(_numberBestLabel);
        _numberUndoButton = MakeButton("Undo", Undo2048, false, 72);
        _numberUndoButton.IsEnabled = false;
        toolbar.Children.Add(_numberUndoButton);
        toolbar.Children.Add(MakeButton("New game", Reset2048));
        panel.Children.Add(toolbar);

        _numberStatus = Text("Use the arrow keys or these buttons to move tiles.", 13, Brush("#6D6B63"));
        _numberStatus.HorizontalAlignment = HorizontalAlignment.Center;
        _numberStatus.Margin = new Thickness(0, 0, 0, 7);
        panel.Children.Add(_numberStatus);

        var board = new UniformGrid
        {
            Rows = 4,
            Columns = 4,
            MaxWidth = 540,
            Height = 430,
            Background = Brush("#B7B09F"),
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _numberButtons = new Button[4, 4];
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
        {
            var button = new Button
            {
                Style = (Style)FindResource("CellButton"),
                FontSize = 24,
                Margin = new Thickness(4),
                MinHeight = 75,
                Focusable = false,
                IsHitTestVisible = false
            };
            AutomationProperties.SetName(button, $"Row {row + 1}, column {column + 1}, empty");
            board.Children.Add(button);
            _numberButtons[row, column] = button;
        }
        panel.Children.Add(board);

        var directions = new Grid { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        directions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        directions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        directions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var left = MakeButton("←", () => Move2048(0, -1), false, 58);
        var up = MakeButton("↑", () => Move2048(-1, 0), false, 58);
        var down = MakeButton("↓", () => Move2048(1, 0), false, 58);
        var right = MakeButton("→", () => Move2048(0, 1), false, 58);
        AutomationProperties.SetName(left, "Move left");
        AutomationProperties.SetName(up, "Move up");
        AutomationProperties.SetName(down, "Move down");
        AutomationProperties.SetName(right, "Move right");
        var middle = new StackPanel();
        middle.Children.Add(up);
        middle.Children.Add(down);
        Grid.SetColumn(left, 0);
        Grid.SetColumn(middle, 1);
        Grid.SetColumn(right, 2);
        directions.Children.Add(left);
        directions.Children.Add(middle);
        directions.Children.Add(right);
        panel.Children.Add(directions);

        Reset2048();
        return GamePage("2048", "Slide the tiles. Join the numbers. Take it one move at a time.", panel);
    }

    private void Reset2048()
    {
        _numberBoard = new int[4, 4];
        _numberScore = 0;
        _numberGameOver = false;
        _numberUndoBoard = null;
        _numberUndoAvailable = false;
        if (_numberUndoButton is not null) _numberUndoButton.IsEnabled = false;
        if (DailyGameIndex == 3 && !DailyChallengeComplete) AdvanceDailyChallenge(0);
        UpdateText(_numberStatus, "Use the arrow keys or these buttons to move tiles.");
        if (_numberStatus is not null) _numberStatus.Foreground = Brush("#6D6B63");
        Spawn2048();
        Spawn2048();
        Render2048();
    }

    private void Spawn2048()
    {
        var empty = new List<(int Row, int Column)>();
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
            if (_numberBoard[row, column] == 0) empty.Add((row, column));
        if (empty.Count == 0) return;
        var (targetRow, targetColumn) = empty[_random.Next(empty.Count)];
        _numberBoard[targetRow, targetColumn] = _random.NextDouble() < 0.9 ? 2 : 4;
    }

    private void Render2048(
        (int Row, int Column)?[,]? origins = null,
        bool[,]? merges = null)
    {
        if (_numberButtons.Length == 0) return;
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
        {
            var value = _numberBoard[row, column];
            var button = _numberButtons[row, column];
            button.Content = value == 0 ? "" : value.ToString();
            AutomationProperties.SetName(button, value == 0
                ? $"Row {row + 1}, column {column + 1}, empty"
                : $"Row {row + 1}, column {column + 1}, {value}");
            button.Background = Brush(value switch
            {
                0 => "#D6D1C4",
                2 => "#F8F2DF",
                4 => "#F0E7CD",
                8 => "#E9BD7D",
                16 => "#DFA16E",
                32 => "#D98168",
                64 => "#CB6859",
                _ => "#9E9A72"
            });
            button.Foreground = value <= 4 ? Brush("#777165") : Brushes.White;
            button.FontSize = value >= 128 ? 21 : 27;
            button.FontWeight = FontWeights.Bold;
            if (value == 0 || origins is null) continue;
            if (origins[row, column] is { } origin)
            {
                var offsetX = (origin.Column - column) * button.ActualWidth;
                var offsetY = (origin.Row - row) * button.ActualHeight;
                AnimateTile(button, offsetX, offsetY, merges?[row, column] == true);
            }
            else
            {
                Pulse(button);
            }
        }
        UpdateText(_numberScoreLabel, $"Score  {_numberScore}");
        var best = Math.Max(_numberScore, Stat("2048", "best"));
        if (best != Stat("2048", "best")) SetStat("2048", "best", best);
        UpdateText(_numberBestLabel, $"Best  {best}");
        var bestTile = _numberBoard.Cast<int>().Max();
        if (bestTile > Stat("2048", "bestTile")) SetStat("2048", "bestTile", bestTile);
        if (DailyGameIndex == 3)
            AdvanceDailyChallenge(Math.Max(Stat("playroom", "dailyProgress"), bestTile));
    }

    private void Move2048(int rowStep, int columnStep)
    {
        if (_numberGameOver || _numberButtons.Length == 0) return;
        var before = (int[,])_numberBoard.Clone();
        var origins = new (int Row, int Column)?[4, 4];
        var merges = new bool[4, 4];
        var scoreBeforeMove = _numberScore;
        for (var line = 0; line < 4; line++)
        {
            var positions = Enumerable.Range(0, 4)
                .Select(offset => rowStep != 0
                    ? (Row: rowStep > 0 ? 3 - offset : offset, Column: line)
                    : (Row: line, Column: columnStep > 0 ? 3 - offset : offset))
                .ToArray();
            var tiles = positions
                .Select(position => (
                    Value: _numberBoard[position.Row, position.Column],
                    Row: position.Row,
                    Column: position.Column))
                .Where(tile => tile.Value != 0)
                .ToList();
            var merged = new List<(int Value, int SourceRow, int SourceColumn, bool IsMerge)>();
            for (var i = 0; i < tiles.Count; i++)
            {
                if (i + 1 < tiles.Count && tiles[i].Value == tiles[i + 1].Value)
                {
                    var value = tiles[i].Value * 2;
                    merged.Add((value, tiles[i].Row, tiles[i].Column, true));
                    _numberScore += value;
                    i++;
                }
                else
                {
                    merged.Add((tiles[i].Value, tiles[i].Row, tiles[i].Column, false));
                }
            }
            for (var offset = 0; offset < positions.Length; offset++)
            {
                var target = positions[offset];
                _numberBoard[positions[offset].Row, positions[offset].Column] =
                    offset < merged.Count ? merged[offset].Value : 0;
                if (offset < merged.Count)
                {
                    var tile = merged[offset];
                    origins[target.Row, target.Column] = (tile.SourceRow, tile.SourceColumn);
                    merges[target.Row, target.Column] = tile.IsMerge;
                }
            }
        }

        if (BoardsEqual(before, _numberBoard)) return;
        _numberUndoBoard = before;
        _numberUndoScore = scoreBeforeMove;
        _numberUndoAvailable = true;
        if (_numberUndoButton is not null) _numberUndoButton.IsEnabled = true;
        Spawn2048();
        Render2048(origins, merges);
        if (!CanMove2048())
        {
            _numberGameOver = true;
            UpdateText(_numberStatus, "No more moves. Start a new game whenever you like.");
            if (_numberStatus is not null) _numberStatus.Foreground = Brush("#DF7868");
            RecordGameCompleted("2048");
        }
    }

    private void Undo2048()
    {
        if (!_numberUndoAvailable || _numberUndoBoard is null) return;
        _numberBoard = _numberUndoBoard;
        _numberScore = _numberUndoScore;
        _numberUndoBoard = null;
        _numberUndoAvailable = false;
        _numberGameOver = false;
        if (_numberUndoButton is not null) _numberUndoButton.IsEnabled = false;
        UpdateText(_numberStatus, "Move restored. Undo is ready after your next move.");
        if (_numberStatus is not null) _numberStatus.Foreground = Brush("#6D6B63");
        Render2048();
    }

    private static bool BoardsEqual(int[,] first, int[,] second)
    {
        for (var row = 0; row < first.GetLength(0); row++)
        for (var column = 0; column < first.GetLength(1); column++)
            if (first[row, column] != second[row, column]) return false;
        return true;
    }

    private bool CanMove2048()
    {
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 4; column++)
        {
            if (_numberBoard[row, column] == 0) return true;
            if (column < 3 && _numberBoard[row, column] == _numberBoard[row, column + 1]) return true;
            if (row < 3 && _numberBoard[row, column] == _numberBoard[row + 1, column]) return true;
        }
        return false;
    }

    private UIElement CreateWhackAMole()
    {
        var panel = NewGamePanel();
        _moleScoreLabel = Text("Score  0", 15, Brush("#383A34"), true);
        _moleBestLabel = Text($"Best  {Stat("whack-a-mole", "best")}", 15, Brush("#383A34"), true);
        _moleTimerLabel = Text("30s", 19, Brush("#536448"), true);
        var toolbar = Horizontal();
        toolbar.Children.Add(_moleScoreLabel);
        toolbar.Children.Add(_moleTimerLabel);
        toolbar.Children.Add(_moleBestLabel);
        panel.Children.Add(toolbar);

        _moleProgress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 30,
            Value = 30,
            Height = 8,
            Foreground = Brush("#879779"),
            Background = Brush("#EAE7DE"),
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(_moleProgress);

        var grid = new UniformGrid
        {
            Rows = 3,
            Columns = 3,
            MaxWidth = 510,
            Height = 390,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _moleButtons = new Button[9];
        for (var index = 0; index < 9; index++)
        {
            var hole = index;
            var button = new Button
            {
                Content = "·",
                Style = (Style)FindResource("CellButton"),
                FontSize = 36,
                Background = Brush("#EEF0E8"),
                Margin = new Thickness(8),
                Cursor = Cursors.Hand
            };
            AutomationProperties.SetName(button, $"Hole {index + 1}, empty");
            button.Click += (_, _) => HitMole(hole);
            grid.Children.Add(button);
            _moleButtons[index] = button;
        }
        panel.Children.Add(grid);

        _moleStatus = Text("Click a mole or press 1–9 for its hole · Space starts.", 13, Brush("#6D6B63"));
        _moleStatus.HorizontalAlignment = HorizontalAlignment.Center;
        _moleStatus.Margin = new Thickness(0, 3, 0, 4);
        panel.Children.Add(_moleStatus);
        _moleStartButton = MakeButton("Start 30-second round", StartMoleRound, true, 210);
        _moleStartButton.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(_moleStartButton);
        ResetMoleBoard();
        return GamePage("Whack-a-Mole", "A tiny burst of speed. Catch a mole whenever it pops up.", panel);
    }

    private void ResetMoleBoard()
    {
        _moleSeconds = 30;
        _moleScore = 0;
        _moleCombo = 0;
        _activeMole = -1;
        UpdateText(_moleScoreLabel, "Score  0");
        UpdateText(_moleTimerLabel, "30s");
        UpdateText(_moleStatus, "Ready for a quick round?");
        if (_moleProgress is not null) _moleProgress.Value = 30;
        for (var index = 0; index < _moleButtons.Length; index++)
        {
            var button = _moleButtons[index];
            button.Content = "·";
            button.Background = Brush("#EEF0E8");
            AutomationProperties.SetName(button, $"Hole {index + 1}, empty");
        }
        if (_moleStartButton is not null)
        {
            _moleStartButton.Content = "Start 30-second round";
            _moleStartButton.IsEnabled = true;
        }
    }

    private void StartMoleRound()
    {
        _moleClock?.Stop();
        _moleSpawner?.Stop();
        ResetMoleBoard();
        if (DailyGameIndex == 4) AdvanceDailyChallenge(0);
        if (_moleStartButton is not null) _moleStartButton.IsEnabled = false;
        UpdateText(_moleStatus, "Go! Catch as many as you can.");
        ShowMole();

        _moleClock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _moleClock.Tick += (_, _) =>
        {
            _moleSeconds--;
            UpdateText(_moleTimerLabel, $"{_moleSeconds}s");
            if (_moleProgress is not null) _moleProgress.Value = _moleSeconds;
            if (_moleSeconds <= 0) FinishMoleRound();
        };
        _moleClock.Start();

        _moleSpawner = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(790) };
        _moleSpawner.Tick += (_, _) => ShowMole();
        _moleSpawner.Start();
    }

    private void ShowMole()
    {
        if (_activeMole >= 0 && _activeMole < _moleButtons.Length)
        {
            _moleButtons[_activeMole].Content = "·";
            _moleButtons[_activeMole].Background = Brush("#EEF0E8");
            AutomationProperties.SetName(_moleButtons[_activeMole], $"Hole {_activeMole + 1}, empty");
        }
        if (_moleButtons.Length == 0) return;
        var next = _random.Next(_moleButtons.Length);
        if (_activeMole >= 0 && next >= _activeMole) next = (next + 1) % _moleButtons.Length;
        _activeMole = next;
        var button = _moleButtons[next];
        button.Content = "🐹";
        button.Background = Brush("#F6E8C9");
        AutomationProperties.SetName(button, $"Hole {next + 1}, mole");
        Pulse(button);
    }

    private void HitMole(int hole)
    {
        if (_moleClock?.IsEnabled != true) return;
        if (hole != _activeMole)
        {
            _moleCombo = 0;
            UpdateText(_moleStatus, "A little early. Keep watching!");
            return;
        }
        _activeMole = -1;
        _moleButtons[hole].Content = "+1";
        _moleButtons[hole].Background = Brush("#E8EEE2");
        AutomationProperties.SetName(_moleButtons[hole], $"Hole {hole + 1}, caught");
        Pulse(_moleButtons[hole]);
        _moleScore++;
        _moleCombo++;
        UpdateText(_moleScoreLabel, $"Score  {_moleScore}");
        UpdateText(_moleStatus, _moleCombo >= 3 ? $"Lovely streak · ×{_moleCombo}!" : "Nice catch!");
        if (_moleScore > Stat("whack-a-mole", "best"))
        {
            SetStat("whack-a-mole", "best", _moleScore);
            UpdateText(_moleBestLabel, $"Best  {_moleScore}");
        }
        if (DailyGameIndex == 4) AdvanceDailyChallenge(_moleScore);
    }

    private void FinishMoleRound()
    {
        _moleClock?.Stop();
        _moleSpawner?.Stop();
        _activeMole = -1;
        RecordGameCompleted("whack-a-mole");
        for (var index = 0; index < _moleButtons.Length; index++)
        {
            var button = _moleButtons[index];
            button.Content = "·";
            button.Background = Brush("#EEF0E8");
            AutomationProperties.SetName(button, $"Hole {index + 1}, empty");
        }
        if (_moleStartButton is not null)
        {
            _moleStartButton.Content = "Play again";
            _moleStartButton.IsEnabled = true;
        }
        UpdateText(_moleTimerLabel, "0s");
        UpdateText(_moleStatus, $"Round over · you caught {_moleScore}.");
    }
}
