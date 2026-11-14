package com.gamemania.playroom;

import android.app.AlertDialog;
import android.content.Context;
import android.content.SharedPreferences;
import android.content.res.ColorStateList;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.OvershootInterpolator;
import android.widget.FrameLayout;
import android.widget.GridLayout;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.graphics.Insets;
import androidx.core.view.ViewCompat;
import androidx.core.view.WindowCompat;
import androidx.core.view.WindowInsetsCompat;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

public class MainActivity extends AppCompatActivity {
    private static final int PAPER = 0xFFF7F5EF;
    private static final int WHITE = 0xFFFFFEFA;
    private static final int INK = 0xFF383A34;
    private static final int MUTED = 0xFF6D6B63;
    private static final int SAGE = 0xFF879779;
    private static final int SAGE_DARK = 0xFF536448;
    private static final int CORAL = 0xFFDF7868;
    private static final int GOLD = 0xFFE8BC69;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Random random = new Random();
    private LinearLayout root;
    private LinearLayout page;
    private String currentGame;

    private int[][] connectBoard;
    private int connectPlayer;
    private boolean connectBusy;
    private boolean connectEnded;
    private TextView connectTurn;
    private final TextView[][] connectCells = new TextView[6][7];

    private boolean rpsBusy;
    private TextView rpsScore;
    private TextView rpsTiesView;
    private TextView rpsStatus;
    private TextView playerHand;
    private TextView computerHand;

    private List<MemoryCard> memoryCards;
    private MemoryCard firstCard;
    private boolean memoryLocked;
    private int memoryMoves;
    private int memoryPairs;
    private int memoryRound;
    private TextView memoryMovesView;
    private TextView memoryPairsView;
    private TextView memoryStatus;

    private final List<NumberTile> numberTiles = new ArrayList<>();
    private Native2048Board numberBoard;
    private int nextTileId;
    private int numberScore;
    private boolean numberGameOver;
    private TextView numberScoreView;
    private TextView numberBestView;
    private TextView numberStatus;

    private final TextView[] moleHoles = new TextView[9];
    private int moleScore;
    private int moleSeconds;
    private int activeMole = -1;
    private int moleCombo;
    private boolean moleRunning;
    private TextView moleScoreView;
    private TextView moleBestView;
    private TextView moleTimerView;
    private TextView moleStatus;
    private TextView moleStart;
    private Runnable moleHideTask;

    private final Runnable moleClock = new Runnable() {
        @Override public void run() {
            if (!moleRunning) return;
            moleSeconds--;
            moleTimerView.setText(moleSeconds + "s");
            if (moleSeconds <= 0) {
                finishMoleRound();
            } else {
                handler.postDelayed(this, 1000);
            }
        }
    };

    @Override public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        WindowCompat.setDecorFitsSystemWindows(getWindow(), false);
        getWindow().setStatusBarColor(PAPER);
        getWindow().setNavigationBarColor(PAPER);
        getWindow().getDecorView().setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR | View.SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR
        );
        showHome();
    }

    @Override public void onBackPressed() {
        if (currentGame != null) {
            if ("whack-a-mole".equals(currentGame)) stopMoleRound();
            showHome();
        } else {
            super.onBackPressed();
        }
    }

    @Override protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        super.onDestroy();
    }

    private int dp(float value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }

    private GradientDrawable shape(int color, int radius, int strokeColor) {
        GradientDrawable drawable = new GradientDrawable();
        drawable.setColor(color);
        drawable.setCornerRadius(dp(radius));
        if (strokeColor != 0) drawable.setStroke(dp(1), strokeColor);
        return drawable;
    }

    private TextView text(String value, int size, int color, boolean bold) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(size);
        view.setTextColor(color);
        view.setGravity(Gravity.CENTER_VERTICAL);
        if (bold) view.setTypeface(Typeface.create("sans-serif", Typeface.BOLD));
        return view;
    }

    private LinearLayout column() {
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        return layout;
    }

    private LinearLayout row() {
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.HORIZONTAL);
        layout.setGravity(Gravity.CENTER_VERTICAL);
        return layout;
    }

    private LinearLayout.LayoutParams linearParams(int width, int height) {
        return new LinearLayout.LayoutParams(width < 0 ? width : dp(width), height < 0 ? height : dp(height));
    }

    private void startPage(String game) {
        handler.removeCallbacksAndMessages(null);
        currentGame = game;
        root = column();
        root.setBackgroundColor(PAPER);
        ViewCompat.setOnApplyWindowInsetsListener(root, (view, insets) -> {
            Insets safeArea = insets.getInsets(
                WindowInsetsCompat.Type.systemBars() | WindowInsetsCompat.Type.displayCutout()
            );
            view.setPadding(safeArea.left, safeArea.top, safeArea.right, safeArea.bottom);
            return insets;
        });
        setContentView(root);
        ViewCompat.requestApplyInsets(root);
        page = column();
        page.setPadding(dp(20), dp(12), dp(20), dp(28));
        root.addView(page, new LinearLayout.LayoutParams(-1, -1));
        addHeader(game == null);
    }

    private void addHeader(boolean home) {
        LinearLayout header = row();
        View mark = new BrandMark(this);
        header.addView(mark, linearParams(32, 32));
        TextView brand = text("  playroom", 16, INK, true);
        LinearLayout.LayoutParams brandParams = linearParams(-2, 42);
        header.addView(brand, brandParams);
        if (home) {
            TextView subtitle = text("A little room for play", 11, MUTED, false);
            subtitle.setGravity(Gravity.RIGHT | Gravity.CENTER_VERTICAL);
            header.addView(subtitle, new LinearLayout.LayoutParams(0, dp(42), 1));
        } else {
            TextView back = text("←  All games", 12, MUTED, false);
            back.setGravity(Gravity.RIGHT | Gravity.CENTER_VERTICAL);
            back.setOnClickListener(v -> onBackPressed());
            header.addView(back, new LinearLayout.LayoutParams(0, dp(42), 1));
        }
        page.addView(header, linearParams(-1, 42));
    }

    private class BrandMark extends View {
        private final Paint brush = new Paint(Paint.ANTI_ALIAS_FLAG);

        BrandMark(Context context) {
            super(context);
            setContentDescription("Playroom");
        }

        @Override protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            float scale = Math.min(getWidth(), getHeight()) / 32f;
            canvas.save();
            canvas.scale(scale, scale);
            brush.setColor(SAGE);
            canvas.drawRoundRect(0, 0, 32, 32, 9, 9, brush);
            int[] colors = { WHITE, 0xFFEACD94, 0xFFE9A99A, WHITE };
            for (int i = 0; i < 4; i++) {
                float x = 7 + (i % 2) * 10;
                float y = 7 + (i / 2) * 10;
                brush.setColor(colors[i]);
                canvas.drawRoundRect(x, y, x + 7, y + 7, 2.3f, 2.3f, brush);
            }
            canvas.restore();
        }
    }

    private ScrollView addScrollContent() {
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(false);
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, 0, 1);
        params.topMargin = dp(12);
        page.addView(scroll, params);
        return scroll;
    }

    private class GameArt extends View {
        private final int game;
        private final Paint brush = new Paint(Paint.ANTI_ALIAS_FLAG);

        GameArt(Context context, int game) {
            super(context);
            this.game = game;
            setLayerType(View.LAYER_TYPE_SOFTWARE, null);
            setContentDescription(new String[] {
                "Connect Four board preview",
                "Rock Paper Scissors move preview",
                "Memory Match cards preview",
                "2048 number tiles preview",
                "Whack-a-Mole game preview"
            }[game]);
        }

        private void rounded(Canvas canvas, float left, float top, float right, float bottom, int color, float radius) {
            brush.setColor(color);
            brush.setStyle(Paint.Style.FILL);
            canvas.drawRoundRect(left, top, right, bottom, radius, radius, brush);
        }

        private void circle(Canvas canvas, float x, float y, float radius, int color) {
            brush.setColor(color);
            brush.setStyle(Paint.Style.FILL);
            canvas.drawCircle(x, y, radius, brush);
        }

        private void word(Canvas canvas, String label, float x, float y, float size, int color) {
            brush.setTypeface(Typeface.create("sans-serif-medium", Typeface.BOLD));
            brush.setTextSize(size);
            brush.setTextAlign(Paint.Align.CENTER);
            brush.setColor(color);
            canvas.drawText(label, x, y, brush);
        }

        @Override protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            float scale = Math.min(getWidth() / 210f, getHeight() / 114f);
            if (scale <= 0) return;
            float dx = (getWidth() - 210f * scale) / 2f;
            float dy = (getHeight() - 114f * scale) / 2f;
            int[] backgrounds = { 0xFFEFE6CF, 0xFFF2E5DF, 0xFFE8ECDF, 0xFFEDE7D8, 0xFFF1E5DA };
            canvas.save();
            brush.setColor(backgrounds[game]);
            brush.setStyle(Paint.Style.FILL);
            canvas.drawRoundRect(0, 0, getWidth(), getHeight(), dp(16), dp(16), brush);
            canvas.translate(dx, dy);
            canvas.scale(scale, scale);

            switch (game) {
                case 0:
                    rounded(canvas, 48, 12, 162, 102, 0xFF849574, 12);
                    for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++) {
                        int color = (r == 1 && c == 1) || (r == 1 && c == 2) || (r == 2 && c == 1)
                            ? CORAL : (r == 0 && c == 2) || (r == 1 && c == 3) ? GOLD : 0xFFF8F5EC;
                        circle(canvas, 67 + c * 25, 30 + r * 25, 7.5f, color);
                    }
                    break;
                case 1:
                    rounded(canvas, 39, 20, 91, 83, 0xFFFFFEFA, 14);
                    rounded(canvas, 79, 34, 131, 97, 0xFFFFFEFA, 14);
                    rounded(canvas, 119, 20, 171, 83, 0xFFFFFEFA, 14);
                    circle(canvas, 65, 50, 14, CORAL);
                    rounded(canvas, 91, 45, 119, 71, SAGE, 7);
                    brush.setColor(0xFFD3A358);
                    brush.setStrokeWidth(5);
                    brush.setStrokeCap(Paint.Cap.ROUND);
                    brush.setStyle(Paint.Style.STROKE);
                    canvas.drawLine(140, 42, 155, 67, brush);
                    canvas.drawLine(155, 42, 140, 67, brush);
                    brush.setStyle(Paint.Style.FILL);
                    circle(canvas, 140, 42, 4, 0xFFD3A358);
                    circle(canvas, 155, 67, 4, 0xFFD3A358);
                    break;
                case 2:
                    float[][] cardPos = { {55, 13}, {112, 13}, {55, 60}, {112, 60} };
                    int[] symbols = { CORAL, 0xFFC9A35C, SAGE, 0xFFB08471 };
                    for (int i = 0; i < cardPos.length; i++) {
                        float x = cardPos[i][0], y = cardPos[i][1];
                        rounded(canvas, x, y, x + 43, y + 39, WHITE, 8);
                        if (i == 0) circle(canvas, x + 21.5f, y + 19.5f, 9, symbols[i]);
                        else if (i == 1) {
                            Path diamond = new Path();
                            diamond.moveTo(x + 21.5f, y + 8);
                            diamond.lineTo(x + 33, y + 19.5f);
                            diamond.lineTo(x + 21.5f, y + 31);
                            diamond.lineTo(x + 10, y + 19.5f);
                            diamond.close();
                            brush.setColor(symbols[i]);
                            canvas.drawPath(diamond, brush);
                        } else word(canvas, i == 2 ? "✦" : "♥", x + 21.5f, y + 27, 21, symbols[i]);
                    }
                    break;
                case 3:
                    drawNumberTile(canvas, 48, 20, "2", 0xFFF8F2DF, INK);
                    drawNumberTile(canvas, 86, 20, "4", 0xFFF0E7CD, INK);
                    drawNumberTile(canvas, 124, 20, "8", 0xFFE9BD7D, WHITE);
                    drawNumberTile(canvas, 67, 59, "16", 0xFFDFA16E, WHITE);
                    drawNumberTile(canvas, 105, 59, "32", 0xFFD98168, WHITE);
                    break;
                case 4:
                    circle(canvas, 105, 73, 35, 0xFFD9C8AF);
                    circle(canvas, 105, 68, 29, 0xFFB89572);
                    circle(canvas, 95, 65, 3.2f, 0xFF483A31);
                    circle(canvas, 115, 65, 3.2f, 0xFF483A31);
                    rounded(canvas, 101, 75, 109, 80, 0xFF775347, 3);
                    circle(canvas, 63, 24, 5, 0xFFD3A358);
                    circle(canvas, 153, 28, 4, CORAL);
                    word(canvas, "✦", 148, 88, 17, SAGE);
                    break;
            }
            canvas.restore();
        }

        private void drawNumberTile(Canvas canvas, float x, float y, String value, int background, int foreground) {
            rounded(canvas, x, y, x + 35, y + 35, background, 8);
            word(canvas, value, x + 17.5f, y + 24, value.length() > 1 ? 12 : 17, foreground);
        }
    }

    private void showHome() {
        startPage(null);
        ScrollView scroll = addScrollContent();
        LinearLayout content = column();
        content.setPadding(0, dp(15), 0, dp(12));
        scroll.addView(content);

        TextView title = text("Make room\nfor a little play.", 34, INK, true);
        title.setTypeface(Typeface.create("serif", Typeface.BOLD));
        title.setLineSpacing(0, 1.02f);
        LinearLayout.LayoutParams titleParams = linearParams(-1, -2);
        titleParams.topMargin = dp(7);
        content.addView(title, titleParams);
        TextView intro = text("Pick a game. Leave the day outside for a minute.", 14, MUTED, false);
        LinearLayout.LayoutParams introParams = linearParams(-1, -2);
        introParams.topMargin = dp(6);
        content.addView(intro, introParams);

        LinearLayout featured = row();
        featured.setPadding(dp(16), dp(12), dp(8), dp(12));
        featured.setBackground(shape(SAGE_DARK, 22, 0));
        featured.setForeground(new RippleDrawable(
            ColorStateList.valueOf(0x33FFFFFF), null, shape(SAGE_DARK, 22, 0)
        ));
        featured.setElevation(dp(3));
        featured.setClickable(true);
        featured.setFocusable(true);
        featured.setContentDescription("Play 2048. Slide a few tiles and merge numbers.");
        LinearLayout.LayoutParams featuredParams = linearParams(-1, 150);
        featuredParams.topMargin = dp(16);
        content.addView(featured, featuredParams);
        LinearLayout featuredCopy = column();
        featuredCopy.setGravity(Gravity.CENTER_VERTICAL);
        featured.addView(featuredCopy, new LinearLayout.LayoutParams(0, -1, 1));
        TextView featureLabel = text("A quick puzzle", 11, WHITE, true);
        featuredCopy.addView(featureLabel, linearParams(-1, 20));
        TextView featureTitle = text("2048", 28, WHITE, true);
        featureTitle.setTypeface(Typeface.create("serif", Typeface.BOLD));
        featuredCopy.addView(featureTitle, linearParams(-1, 37));
        TextView featureDescription = text("One more move?", 12, WHITE, false);
        featuredCopy.addView(featureDescription, linearParams(-1, 22));
        TextView playButton = text("Play 2048", 12, INK, true);
        playButton.setGravity(Gravity.CENTER);
        playButton.setPadding(dp(12), 0, dp(12), 0);
        playButton.setBackground(shape(0xFFFFFDF7, 18, 0));
        LinearLayout.LayoutParams playButtonParams = linearParams(-2, 33);
        playButtonParams.topMargin = dp(5);
        featuredCopy.addView(playButton, playButtonParams);
        GameArt featuredArt = new GameArt(this, 3);
        LinearLayout.LayoutParams featuredArtParams = linearParams(134, -1);
        featuredArtParams.leftMargin = dp(5);
        featured.addView(featuredArt, featuredArtParams);
        featured.setOnClickListener(v -> {
            v.animate().scaleX(.985f).scaleY(.985f).setDuration(75).withEndAction(() -> {
                v.animate().scaleX(1f).scaleY(1f).setDuration(100).start();
                openGame("2048");
            }).start();
        });

        LinearLayout section = row();
        LinearLayout.LayoutParams sectionParams = linearParams(-1, 43);
        sectionParams.topMargin = dp(12);
        content.addView(section, sectionParams);
        TextView sectionTitle = text("Pick another game", 17, INK, true);
        section.addView(sectionTitle, new LinearLayout.LayoutParams(0, -2, 1));

        String[] ids = { "connect-four", "rock-paper-scissors", "memory-match", "2048", "whack-a-mole" };
        String[] names = { "Connect Four", "Rock Paper Scissors", "Memory Match", "2048", "Whack-a-Mole" };
        String[] subtitles = {
            "Line up four to win.",
            "Make your move.",
            "Find the matching pairs.",
            "Merge tiles, make 2048.",
            "Catch a quick little mole."
        };
        int[] listGames = { 0, 1, 2, 4 };
        for (int index = 0; index < listGames.length; index++) {
            int i = listGames[index];
            final String game = ids[i];
            LinearLayout gameRow = row();
            gameRow.setPadding(dp(7), dp(5), dp(8), dp(5));
            gameRow.setBackground(new RippleDrawable(
                ColorStateList.valueOf(0x20879779), null, shape(PAPER, 14, 0)
            ));
            gameRow.setClickable(true);
            gameRow.setFocusable(true);
            gameRow.setContentDescription("Open " + names[i] + ". " + subtitles[i]);
            gameRow.setOnClickListener(v -> {
                v.animate().scaleX(.985f).scaleY(.985f).setDuration(75).withEndAction(() -> {
                    v.animate().scaleX(1f).scaleY(1f).setDuration(100).start();
                    openGame(game);
                }).start();
            });
            GameArt artwork = new GameArt(this, i);
            LinearLayout.LayoutParams artParams = linearParams(76, 57);
            artParams.rightMargin = dp(12);
            gameRow.addView(artwork, artParams);
            LinearLayout copy = column();
            copy.setGravity(Gravity.CENTER_VERTICAL);
            gameRow.addView(copy, new LinearLayout.LayoutParams(0, -1, 1));
            TextView name = text(names[i], 14, INK, true);
            copy.addView(name, linearParams(-1, 24));
            TextView description = text(subtitles[i], 12, MUTED, false);
            copy.addView(description, linearParams(-1, 22));
            content.addView(gameRow, linearParams(-1, 72));
            if (index < listGames.length - 1) {
                View divider = new View(this);
                divider.setBackgroundColor(0xFFE7E4DA);
                LinearLayout.LayoutParams dividerParams = linearParams(-1, 1);
                dividerParams.leftMargin = dp(91);
                content.addView(divider, dividerParams);
            }
        }

        TextView privacy = text("Offline play · Your scores stay on this device.", 11, MUTED, false);
        privacy.setPadding(dp(2), dp(14), dp(2), dp(8));
        privacy.setOnClickListener(v -> new AlertDialog.Builder(this)
            .setTitle("Your privacy")
            .setMessage("Playroom works offline. It has no ads, analytics, accounts, or tracking. Game records are saved only on this device.")
            .setPositiveButton("Got it", null)
            .show());
        content.addView(privacy, linearParams(-1, -2));
    }

    private void addGameHeading(String title, String subtitle) {
        TextView heading = text(title, 25, INK, true);
        heading.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams titleParams = linearParams(-1, 43);
        titleParams.topMargin = dp(14);
        page.addView(heading, titleParams);
        TextView copy = text(subtitle, 12, MUTED, false);
        copy.setGravity(Gravity.CENTER);
        page.addView(copy, linearParams(-1, 27));
    }

    private LinearLayout gameShell() {
        ScrollView scroll = addScrollContent();
        LinearLayout shell = column();
        shell.setPadding(dp(16), dp(17), dp(16), dp(17));
        shell.setBackground(shape(WHITE, 20, 0xFFECE9E1));
        shell.setElevation(dp(2));
        scroll.addView(shell);
        return shell;
    }

    private TextView nativeButton(String label, boolean primary) {
        TextView button = text(label, 12, primary ? WHITE : 0xFF67675F, true);
        button.setGravity(Gravity.CENTER);
        button.setPadding(dp(12), 0, dp(12), 0);
        button.setBackground(shape(primary ? SAGE_DARK : WHITE, 11, primary ? 0 : 0xFFE8E5DB));
        button.setForeground(new RippleDrawable(
            ColorStateList.valueOf(0x25879779), null,
            shape(primary ? SAGE_DARK : WHITE, 11, 0)
        ));
        button.setClickable(true);
        button.setFocusable(true);
        return button;
    }

    private TextView statValue(String value, int color) {
        TextView view = text(value, 16, color, true);
        view.setGravity(Gravity.CENTER);
        return view;
    }

    private SharedPreferences stats(String game) {
        return getSharedPreferences("game-" + game, MODE_PRIVATE);
    }

    private void openGame(String game) {
        if ("whack-a-mole".equals(currentGame)) stopMoleRound();
        switch (game) {
            case "connect-four": showConnectFour(); break;
            case "rock-paper-scissors": showRockPaperScissors(); break;
            case "memory-match": showMemoryMatch(); break;
            case "2048": show2048(); break;
            case "whack-a-mole": showWhackAMole(); break;
            default: showHome();
        }
    }

    private void showConnectFour() {
        startPage("connect-four");
        addGameHeading("Connect Four", "Take turns. Connect four. Have a lovely little victory.");
        LinearLayout shell = gameShell();
        LinearLayout toolbar = row();
        shell.addView(toolbar, linearParams(-1, 42));
        connectTurn = text("Coral goes first", 12, CORAL, true);
        toolbar.addView(connectTurn, new LinearLayout.LayoutParams(0, -1, 1));
        TextView reset = nativeButton("New game", false);
        toolbar.addView(reset, linearParams(-2, 38));

        GridLayout grid = new GridLayout(this);
        grid.setColumnCount(7);
        grid.setRowCount(6);
        grid.setPadding(dp(7), dp(7), dp(7), dp(7));
        grid.setBackground(shape(0xFF839474, 16, 0));
        LinearLayout.LayoutParams gridParams = linearParams(-1, 300);
        gridParams.topMargin = dp(8);
        shell.addView(grid, gridParams);
        for (int r = 0; r < 6; r++) {
            for (int c = 0; c < 7; c++) {
                TextView cell = text("", 23, PAPER, true);
                cell.setGravity(Gravity.CENTER);
                cell.setBackground(shape(PAPER, 50, 0));
                cell.setElevation(dp(1));
                GridLayout.LayoutParams cellParams = new GridLayout.LayoutParams(
                    GridLayout.spec(r, 1, 1f), GridLayout.spec(c, 1, 1f)
                );
                cellParams.width = 0;
                cellParams.height = 0;
                cellParams.setMargins(dp(3), dp(3), dp(3), dp(3));
                final int column = c;
                cell.setOnClickListener(v -> dropConnectDisc(column));
                grid.addView(cell, cellParams);
                connectCells[r][c] = cell;
            }
        }
        TextView status = text("Two players · take turns on the same screen", 11, MUTED, false);
        status.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams statusParams = linearParams(-1, 36);
        statusParams.topMargin = dp(8);
        shell.addView(status, statusParams);
        reset.setOnClickListener(v -> resetConnectFour());
        resetConnectFour();
    }

    private void resetConnectFour() {
        connectBoard = new int[6][7];
        connectPlayer = 1;
        connectBusy = false;
        connectEnded = false;
        connectTurn.setText("Coral's turn");
        connectTurn.setTextColor(CORAL);
        for (int r = 0; r < 6; r++) for (int c = 0; c < 7; c++) {
            connectCells[r][c].setText("");
            connectCells[r][c].setBackground(shape(PAPER, 50, 0));
            connectCells[r][c].setAlpha(1f);
        }
    }

    private void dropConnectDisc(int column) {
        if (connectBusy || connectEnded) return;
        int target = 5;
        while (target >= 0 && connectBoard[target][column] != 0) target--;
        if (target < 0) return;
        int player = connectPlayer;
        final int targetRow = target;
        connectBoard[targetRow][column] = player;
        TextView disc = connectCells[targetRow][column];
        int color = player == 1 ? CORAL : GOLD;
        disc.setText(player == 1 ? "●" : "●");
        disc.setTextColor(color);
        disc.setAlpha(0f);
        disc.setTranslationY(-dp(44 * (target + 1)));
        connectBusy = true;
        disc.animate().alpha(1f).translationY(0).setDuration(210)
            .setInterpolator(new OvershootInterpolator(0.65f))
            .withEndAction(() -> {
                connectBusy = false;
                if (connectWinAt(targetRow, column, player)) {
                    connectEnded = true;
                    connectTurn.setText((player == 1 ? "Coral" : "Gold") + " wins!");
                    connectTurn.setTextColor(SAGE);
                    for (int[] point : connectWinningLine(targetRow, column, player)) {
                        connectCells[point[0]][point[1]].animate().scaleX(1.12f).scaleY(1.12f).setDuration(180).start();
                    }
                    SharedPreferences prefs = stats("connect-four");
                    prefs.edit().putInt("wins", prefs.getInt("wins", 0) + 1).apply();
                } else if (connectBoard[0][0] != 0 && isBoardFull()) {
                    connectEnded = true;
                    connectTurn.setText("It's a lovely draw.");
                } else {
                    connectPlayer = player == 1 ? 2 : 1;
                    connectTurn.setText(connectPlayer == 1 ? "Coral's turn" : "Gold's turn");
                    connectTurn.setTextColor(connectPlayer == 1 ? CORAL : 0xFFB88E44);
                }
            }).start();
    }

    private boolean isBoardFull() {
        for (int c = 0; c < 7; c++) if (connectBoard[0][c] == 0) return false;
        return true;
    }

    private boolean connectWinAt(int row, int col, int player) {
        return connectWinningLine(row, col, player).size() >= 4;
    }

    private List<int[]> connectWinningLine(int row, int col, int player) {
        int[][] directions = { {0, 1}, {1, 0}, {1, 1}, {1, -1} };
        for (int[] direction : directions) {
            List<int[]> line = new ArrayList<>();
            line.add(new int[] { row, col });
            for (int sign : new int[] { -1, 1 }) {
                int r = row + direction[0] * sign;
                int c = col + direction[1] * sign;
                while (r >= 0 && r < 6 && c >= 0 && c < 7 && connectBoard[r][c] == player) {
                    line.add(new int[] { r, c });
                    r += direction[0] * sign;
                    c += direction[1] * sign;
                }
            }
            if (line.size() >= 4) return line;
        }
        return new ArrayList<>();
    }

    private void showRockPaperScissors() {
        startPage("rock-paper-scissors");
        rpsBusy = false;
        addGameHeading("Rock Paper Scissors", "One little countdown. One very important choice.");
        LinearLayout shell = gameShell();
        LinearLayout toolbar = row();
        shell.addView(toolbar, linearParams(-1, 42));
        rpsScore = statValue("You  " + stats("rock-paper-scissors").getInt("wins", 0), INK);
        toolbar.addView(rpsScore, new LinearLayout.LayoutParams(0, -1, 1));
        TextView reset = nativeButton("Reset score", false);
        toolbar.addView(reset, linearParams(-2, 38));
        rpsTiesView = statValue("Ties  " + stats("rock-paper-scissors").getInt("ties", 0), INK);
        toolbar.addView(rpsTiesView, new LinearLayout.LayoutParams(0, -1, 1));

        LinearLayout duel = row();
        duel.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams duelParams = linearParams(-1, 158);
        duelParams.topMargin = dp(9);
        shell.addView(duel, duelParams);
        LinearLayout left = handPanel("YOU", false);
        LinearLayout right = handPanel("PLAYROOM", true);
        duel.addView(left, new LinearLayout.LayoutParams(0, -1, 1));
        TextView versus = text("VS", 11, MUTED, true);
        versus.setGravity(Gravity.CENTER);
        duel.addView(versus, linearParams(42, -1));
        duel.addView(right, new LinearLayout.LayoutParams(0, -1, 1));
        playerHand = (TextView) left.getChildAt(1);
        computerHand = (TextView) right.getChildAt(1);

        rpsStatus = text("Choose your move.", 12, MUTED, false);
        rpsStatus.setGravity(Gravity.CENTER);
        shell.addView(rpsStatus, linearParams(-1, 36));
        LinearLayout choices = row();
        choices.setGravity(Gravity.CENTER);
        LinearLayout.LayoutParams choicesParams = linearParams(-1, 75);
        choicesParams.topMargin = dp(2);
        shell.addView(choices, choicesParams);
        String[] labels = { "✊\nRock", "✋\nPaper", "✌️\nScissors" };
        String[] moves = { "rock", "paper", "scissors" };
        for (int i = 0; i < moves.length; i++) {
            final String move = moves[i];
            TextView button = nativeButton(labels[i], false);
            button.setTextSize(13);
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, dp(59), 1);
            params.setMargins(dp(4), 0, dp(4), 0);
            choices.addView(button, params);
            button.setOnClickListener(v -> playRps(move));
        }
        reset.setOnClickListener(v -> {
            stats("rock-paper-scissors").edit().putInt("wins", 0).putInt("ties", 0).putInt("losses", 0).apply();
            rpsScore.setText("You  0");
            rpsTiesView.setText("Ties  0");
            rpsStatus.setText("Choose your move.");
        });
    }

    private LinearLayout handPanel(String label, boolean mirror) {
        LinearLayout panel = column();
        panel.setGravity(Gravity.CENTER);
        TextView caption = text(label, 9, MUTED, true);
        caption.setGravity(Gravity.CENTER);
        panel.addView(caption, linearParams(-1, 24));
        TextView hand = text("✊", 42, INK, true);
        hand.setGravity(Gravity.CENTER);
        hand.setBackground(shape(0xFFF8F6EF, 23, 0xFFEEECE4));
        if (mirror) hand.setScaleX(-1);
        panel.addView(hand, linearParams(88, 88));
        return panel;
    }

    private void playRps(String mine) {
        if (rpsBusy) return;
        rpsBusy = true;
        rpsStatus.setText("Rock... paper... scissors...");
        playerHand.animate().translationY(dp(7)).rotation(-12).setDuration(160).withEndAction(() ->
            playerHand.animate().translationY(0).rotation(0).setDuration(170).start()
        ).start();
        computerHand.animate().translationY(dp(7)).rotation(12).setDuration(160).withEndAction(() ->
            computerHand.animate().translationY(0).rotation(0).setDuration(170).start()
        ).start();
        handler.postDelayed(() -> {
            String[] options = { "rock", "paper", "scissors" };
            String theirs = options[random.nextInt(options.length)];
            String[] emoji = { "✊", "✋", "✌️" };
            playerHand.setText(emoji[moveIndex(mine)]);
            computerHand.setText(emoji[moveIndex(theirs)]);
            computerHand.setScaleX(-1);
            SharedPreferences prefs = stats("rock-paper-scissors");
            if (mine.equals(theirs)) {
                rpsStatus.setText("A perfect tie. Great minds!");
                int ties = prefs.getInt("ties", 0) + 1;
                prefs.edit().putInt("ties", ties).apply();
                rpsTiesView.setText("Ties  " + ties);
            } else if (beats(mine, theirs)) {
                rpsStatus.setText("You win! Lovely choice.");
                rpsStatus.setTextColor(SAGE);
                int wins = prefs.getInt("wins", 0) + 1;
                prefs.edit().putInt("wins", wins).apply();
                rpsScore.setText("You  " + wins);
                rpsStatus.animate().scaleX(1.05f).scaleY(1.05f).setDuration(160).withEndAction(() ->
                    rpsStatus.animate().scaleX(1).scaleY(1).setDuration(150).start()
                ).start();
            } else {
                rpsStatus.setText("Playroom wins this one. Again?");
                rpsStatus.setTextColor(CORAL);
                prefs.edit().putInt("losses", prefs.getInt("losses", 0) + 1).apply();
            }
            rpsBusy = false;
        }, 520);
    }

    private int moveIndex(String move) {
        if ("paper".equals(move)) return 1;
        if ("scissors".equals(move)) return 2;
        return 0;
    }

    private boolean beats(String mine, String theirs) {
        return ("rock".equals(mine) && "scissors".equals(theirs))
            || ("paper".equals(mine) && "rock".equals(theirs))
            || ("scissors".equals(mine) && "paper".equals(theirs));
    }

    private static class MemoryCard {
        final int symbol;
        final TextView view;
        boolean matched;
        MemoryCard(int symbol, TextView view) { this.symbol = symbol; this.view = view; }
    }

    private void showMemoryMatch() {
        startPage("memory-match");
        addGameHeading("Memory Match", "A few lovely cards. Can you find all eight pairs?");
        LinearLayout shell = gameShell();
        LinearLayout toolbar = row();
        shell.addView(toolbar, linearParams(-1, 44));
        memoryMovesView = statValue("Moves  0", INK);
        toolbar.addView(memoryMovesView, new LinearLayout.LayoutParams(0, -1, 1));
        TextView reset = nativeButton("Shuffle cards", false);
        toolbar.addView(reset, linearParams(-2, 38));
        memoryPairsView = statValue("Pairs  0 / 8", INK);
        toolbar.addView(memoryPairsView, new LinearLayout.LayoutParams(0, -1, 1));

        GridLayout grid = new GridLayout(this);
        grid.setColumnCount(4);
        grid.setRowCount(4);
        LinearLayout.LayoutParams gridParams = linearParams(-1, 346);
        gridParams.topMargin = dp(10);
        shell.addView(grid, gridParams);
        memoryStatus = text("Turn over two cards to find a pair.", 11, MUTED, false);
        memoryStatus.setGravity(Gravity.CENTER);
        shell.addView(memoryStatus, linearParams(-1, 38));
        reset.setOnClickListener(v -> resetMemoryMatch(grid));
        resetMemoryMatch(grid);
    }

    private void resetMemoryMatch(GridLayout grid) {
        memoryRound++;
        memoryLocked = false;
        firstCard = null;
        memoryMoves = 0;
        memoryPairs = 0;
        memoryMovesView.setText("Moves  0");
        memoryPairsView.setText("Pairs  0 / 8");
        memoryStatus.setText("Turn over two cards to find a pair.");
        grid.removeAllViews();
        List<Integer> symbols = new ArrayList<>();
        for (int i = 0; i < 8; i++) { symbols.add(i); symbols.add(i); }
        Collections.shuffle(symbols, random);
        memoryCards = new ArrayList<>();
        String[] faces = { "✦", "●", "◆", "♥", "☀", "☘", "☾", "✿" };
        for (int i = 0; i < symbols.size(); i++) {
            final int symbol = symbols.get(i);
            TextView card = text("✳", 22, SAGE, true);
            card.setGravity(Gravity.CENTER);
            card.setBackground(shape(0xFFE9ECDF, 12, 0));
            card.setClickable(true);
            GridLayout.LayoutParams params = new GridLayout.LayoutParams(
                GridLayout.spec(i / 4, 1, 1f), GridLayout.spec(i % 4, 1, 1f)
            );
            params.width = 0; params.height = 0;
            params.setMargins(dp(4), dp(4), dp(4), dp(4));
            grid.addView(card, params);
            MemoryCard model = new MemoryCard(symbol, card);
            memoryCards.add(model);
            card.setOnClickListener(v -> flipMemoryCard(model, faces));
            card.setAlpha(0f);
            card.setTranslationY(dp(8));
            card.animate().alpha(1f).translationY(0).setStartDelay(i * 18L).setDuration(180).start();
        }
    }

    private void flipMemoryCard(MemoryCard card, String[] faces) {
        if (memoryLocked || card.matched || card == firstCard) return;
        revealMemoryCard(card, faces);
        card.view.animate().rotationY(0).scaleX(1.05f).scaleY(1.05f).setDuration(150).withEndAction(() ->
            card.view.animate().scaleX(1).scaleY(1).setDuration(110).start()
        ).start();
        if (firstCard == null) {
            firstCard = card;
            return;
        }
        memoryLocked = true;
        memoryMoves++;
        memoryMovesView.setText("Moves  " + memoryMoves);
        MemoryCard previous = firstCard;
        firstCard = null;
        int round = memoryRound;
        if (previous.symbol == card.symbol) {
            previous.matched = true;
            card.matched = true;
            previous.view.setBackground(shape(0xFFEFF5E9, 12, 0xFFCDDDC3));
            card.view.setBackground(shape(0xFFEFF5E9, 12, 0xFFCDDDC3));
            memoryPairs++;
            memoryPairsView.setText("Pairs  " + memoryPairs + " / 8");
            memoryStatus.setText(memoryPairs == 8 ? "All matched in " + memoryMoves + " moves. Beautifully done!" : "A match! Keep going.");
            SharedPreferences prefs = stats("memory-match");
            if (memoryPairs == 8) {
                prefs.edit().putInt("games", prefs.getInt("games", 0) + 1).apply();
                if (prefs.getInt("best", 0) == 0 || memoryMoves < prefs.getInt("best", 0))
                    prefs.edit().putInt("best", memoryMoves).apply();
            }
            memoryLocked = false;
        } else {
            memoryStatus.setText("Not quite. Have another look.");
            handler.postDelayed(() -> {
                if (round != memoryRound) return;
                concealMemoryCard(previous);
                concealMemoryCard(card);
                memoryLocked = false;
                memoryStatus.setText("Turn over two cards to find a pair.");
            }, 620);
        }
    }

    private void revealMemoryCard(MemoryCard card, String[] faces) {
        card.view.setText(faces[card.symbol]);
        card.view.setTextColor(new int[] { SAGE, SAGE, 0xFFAA8853, CORAL, 0xFFD4A65D, SAGE, 0xFFAA8853, CORAL }[card.symbol]);
        card.view.setBackground(shape(WHITE, 12, 0xFFE8E5DB));
        card.view.setRotationY(-80);
        card.view.animate().rotationY(0).setDuration(180).start();
    }

    private void concealMemoryCard(MemoryCard card) {
        if (card.matched) return;
        card.view.animate().rotationY(80).setDuration(130).withEndAction(() -> {
            card.view.setText("✳");
            card.view.setTextColor(SAGE);
            card.view.setBackground(shape(0xFFE9ECDF, 12, 0));
            card.view.setRotationY(-80);
            card.view.animate().rotationY(0).setDuration(150).start();
        }).start();
    }

    private static class NumberTile {
        final int id;
        int value;
        int row;
        int col;
        final TextView view;
        NumberTile(int id, int value, int row, int col, TextView view) {
            this.id = id; this.value = value; this.row = row; this.col = col; this.view = view;
        }
    }

    private class Native2048Board extends ViewGroup {
        private final Paint paint = new Paint(Paint.ANTI_ALIAS_FLAG);
        private float cell;
        private float gap;
        private float inset;

        Native2048Board(Context context) {
            super(context);
            setWillNotDraw(false);
            setBackground(shape(0xFFE8E2D4, 16, 0));
        }

        @Override protected void onMeasure(int widthSpec, int heightSpec) {
            int width = MeasureSpec.getSize(widthSpec);
            int size = Math.min(width, dp(430));
            setMeasuredDimension(size, size);
            inset = dp(10);
            gap = dp(9);
            cell = (size - inset * 2 - gap * 3) / 4f;
            int exact = MeasureSpec.makeMeasureSpec((int) cell, MeasureSpec.EXACTLY);
            for (int i = 0; i < getChildCount(); i++) getChildAt(i).measure(exact, exact);
        }

        @Override protected void onDraw(Canvas canvas) {
            super.onDraw(canvas);
            paint.setColor(0xFFF4F0E7);
            float radius = dp(9);
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) {
                float left = inset + c * (cell + gap);
                float top = inset + r * (cell + gap);
                canvas.drawRoundRect(left, top, left + cell, top + cell, radius, radius, paint);
            }
        }

        @Override protected void onLayout(boolean changed, int left, int top, int right, int bottom) {
            for (int i = 0; i < getChildCount(); i++) {
                NumberTile tile = (NumberTile) getChildAt(i).getTag();
                if (tile == null) continue;
                int x = Math.round(inset + tile.col * (cell + gap));
                int y = Math.round(inset + tile.row * (cell + gap));
                tile.view.layout(0, 0, Math.round(cell), Math.round(cell));
                tile.view.setX(x);
                tile.view.setY(y);
            }
        }

        float step() { return cell + gap; }
        float inset() { return inset; }

        @Override public boolean onTouchEvent(MotionEvent event) {
            if (event.getAction() == MotionEvent.ACTION_DOWN) {
                swipeStart = new float[] { event.getX(), event.getY() };
                return true;
            }
            if (event.getAction() == MotionEvent.ACTION_UP && swipeStart != null) {
                float dx = event.getX() - swipeStart[0];
                float dy = event.getY() - swipeStart[1];
                swipeStart = null;
                if (Math.max(Math.abs(dx), Math.abs(dy)) > dp(22)) {
                    move2048(Math.abs(dx) > Math.abs(dy)
                        ? (dx > 0 ? "right" : "left")
                        : (dy > 0 ? "down" : "up"));
                }
                return true;
            }
            return true;
        }
    }

    private float[] swipeStart;

    private void show2048() {
        startPage("2048");
        addGameHeading("2048", "Slide the tiles. Join the numbers. Take it one move at a time.");
        LinearLayout shell = gameShell();
        LinearLayout toolbar = row();
        shell.addView(toolbar, linearParams(-1, 43));
        numberScoreView = statValue("Score  0", INK);
        toolbar.addView(numberScoreView, new LinearLayout.LayoutParams(0, -1, 1));
        numberBestView = statValue("Best  " + stats("2048").getInt("best", 0), INK);
        toolbar.addView(numberBestView, new LinearLayout.LayoutParams(0, -1, 1));
        TextView reset = nativeButton("New game", false);
        toolbar.addView(reset, linearParams(-2, 37));
        numberStatus = text("Swipe or use the arrow keys to move tiles.", 11, MUTED, false);
        numberStatus.setGravity(Gravity.CENTER);
        numberStatus.setPadding(dp(2), 0, dp(2), 0);
        numberBoard = new Native2048Board(this);
        numberBoard.setFocusable(true);
        LinearLayout.LayoutParams boardParams = linearParams(-1, 350);
        boardParams.topMargin = dp(8);
        shell.addView(numberBoard, boardParams);
        numberBoard.setOnKeyListener((v, keyCode, event) -> {
            if (event.getAction() != android.view.KeyEvent.ACTION_DOWN) return false;
            switch (keyCode) {
                case android.view.KeyEvent.KEYCODE_DPAD_LEFT: move2048("left"); return true;
                case android.view.KeyEvent.KEYCODE_DPAD_RIGHT: move2048("right"); return true;
                case android.view.KeyEvent.KEYCODE_DPAD_UP: move2048("up"); return true;
                case android.view.KeyEvent.KEYCODE_DPAD_DOWN: move2048("down"); return true;
                default: return false;
            }
        });
        shell.addView(numberStatus, linearParams(-1, 36));
        reset.setOnClickListener(v -> reset2048());
        reset2048();
        numberBoard.requestFocus();
    }

    private void reset2048() {
        numberTiles.clear();
        nextTileId = 1;
        numberScore = 0;
        numberGameOver = false;
        swipeStart = null;
        numberStatus.setText("Swipe or use the arrow keys to move tiles.");
        numberStatus.setTextColor(MUTED);
        render2048(null);
        spawn2048();
        spawn2048();
        render2048(null);
    }

    private void spawn2048() {
        List<Integer> empty = new ArrayList<>();
        for (int i = 0; i < 16; i++) {
            final int row = i / 4, col = i % 4;
            boolean occupied = false;
            for (NumberTile tile : numberTiles) if (tile.row == row && tile.col == col) occupied = true;
            if (!occupied) empty.add(i);
        }
        if (empty.isEmpty()) return;
        int position = empty.get(random.nextInt(empty.size()));
        TextView view = text("", 24, 0xFF777165, true);
        view.setGravity(Gravity.CENTER);
        NumberTile tile = new NumberTile(nextTileId++, random.nextFloat() < .9f ? 2 : 4, position / 4, position % 4, view);
        tile.view.setTag(tile);
        numberTiles.add(tile);
    }

    private void render2048(NumberTile spawned) {
        numberBoard.removeAllViews();
        for (NumberTile tile : numberTiles) {
            tile.view.setTag(tile);
            tile.view.setText(String.valueOf(tile.value));
            tile.view.setTextColor(tile.value <= 4 ? 0xFF777165 : WHITE);
            int color = tile.value == 2 ? 0xFFF8F2DF
                : tile.value == 4 ? 0xFFF0E7CD
                : tile.value == 8 ? 0xFFE9BD7D
                : tile.value == 16 ? 0xFFDFA16E
                : tile.value == 32 ? 0xFFD98168
                : tile.value == 64 ? 0xFFCB6859 : 0xFF9E9A72;
            tile.view.setTextSize(tile.value >= 128 ? 17 : 23);
            tile.view.setBackground(shape(color, 9, 0));
            FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(-2, -2);
            numberBoard.addView(tile.view, params);
            if (tile == spawned) {
                tile.view.setScaleX(.55f);
                tile.view.setScaleY(.55f);
                tile.view.animate().scaleX(1).scaleY(1).setDuration(180).start();
            }
        }
        numberBoard.requestLayout();
        numberScoreView.setText("Score  " + numberScore);
        int best = Math.max(numberScore, stats("2048").getInt("best", 0));
        stats("2048").edit().putInt("best", best).apply();
        numberBestView.setText("Best  " + best);
    }

    private void move2048(String direction) {
        if (numberGameOver) return;
        String before = numberState();
        float step = numberBoard.step();
        Map<NumberTile, int[]> oldPositions = new HashMap<>();
        for (NumberTile tile : numberTiles) oldPositions.put(tile, new int[] { tile.row, tile.col });
        NumberTile[][] original = new NumberTile[4][4];
        for (NumberTile tile : numberTiles) original[tile.row][tile.col] = tile;
        List<NumberTile> merged = new ArrayList<>();
        List<NumberTile> resultTiles = new ArrayList<>();
        for (int line = 0; line < 4; line++) {
            List<NumberTile> ordered = new ArrayList<>();
            for (int offset = 0; offset < 4; offset++) {
                int row = direction.equals("up") ? offset : direction.equals("down") ? 3 - offset : line;
                int col = direction.equals("left") ? offset : direction.equals("right") ? 3 - offset : line;
                if (original[row][col] != null) ordered.add(original[row][col]);
            }
            List<NumberTile> result = new ArrayList<>();
            for (int i = 0; i < ordered.size(); i++) {
                NumberTile tile = ordered.get(i);
                if (i + 1 < ordered.size() && tile.value == ordered.get(i + 1).value) {
                    tile.value *= 2;
                    numberScore += tile.value;
                    merged.add(ordered.get(++i));
                }
                result.add(tile);
            }
            for (int offset = 0; offset < result.size(); offset++) {
                NumberTile tile = result.get(offset);
                tile.row = direction.equals("up") ? offset : direction.equals("down") ? 3 - offset : line;
                tile.col = direction.equals("left") ? offset : direction.equals("right") ? 3 - offset : line;
                resultTiles.add(tile);
            }
        }
        numberTiles.clear();
        numberTiles.addAll(resultTiles);
        for (NumberTile tile : merged) tile.view.animate().alpha(0).setDuration(100).start();
        if (before.equals(numberState())) return;
        NumberTile spawned = createSpawnedTile();
        render2048(spawned);
        numberBoard.post(() -> {
            for (NumberTile tile : numberTiles) {
                int[] old = oldPositions.get(tile);
                if (old == null) continue;
                float targetX = numberBoard.inset() + tile.col * step;
                float targetY = numberBoard.inset() + tile.row * step;
                float startX = numberBoard.inset() + old[1] * step;
                float startY = numberBoard.inset() + old[0] * step;
                if (startX != targetX || startY != targetY) {
                    tile.view.setX(startX);
                    tile.view.setY(startY);
                    tile.view.animate().x(targetX).y(targetY).setDuration(135).start();
                }
            }
        });
        if (!canMove2048()) {
            numberGameOver = true;
            numberStatus.setText("No more moves. Start a new game whenever you like.");
            numberStatus.setTextColor(CORAL);
        }
    }

    private NumberTile createSpawnedTile() {
        int oldSize = numberTiles.size();
        spawn2048();
        return numberTiles.size() > oldSize ? numberTiles.get(numberTiles.size() - 1) : null;
    }

    private NumberTile tileAt(int row, int col) {
        if (row < 0 || row >= 4 || col < 0 || col >= 4) return null;
        for (NumberTile tile : numberTiles) if (tile.row == row && tile.col == col) return tile;
        return null;
    }

    private String numberState() {
        List<String> values = new ArrayList<>();
        for (NumberTile tile : numberTiles) values.add(tile.row + ":" + tile.col + ":" + tile.value);
        Collections.sort(values);
        return values.toString();
    }

    private boolean canMove2048() {
        if (numberTiles.size() < 16) return true;
        for (NumberTile tile : numberTiles) {
            NumberTile right = tileAt(tile.row, tile.col + 1);
            NumberTile down = tileAt(tile.row + 1, tile.col);
            if (right != null && right.value == tile.value) return true;
            if (down != null && down.value == tile.value) return true;
        }
        return false;
    }

    private void showWhackAMole() {
        startPage("whack-a-mole");
        addGameHeading("Whack-a-Mole", "Little moles, quick visits. See how many you can catch.");
        LinearLayout shell = gameShell();
        LinearLayout toolbar = row();
        shell.addView(toolbar, linearParams(-1, 46));
        moleScoreView = statValue("Score  0", INK);
        toolbar.addView(moleScoreView, new LinearLayout.LayoutParams(0, -1, 1));
        moleTimerView = statValue("20s", SAGE);
        toolbar.addView(moleTimerView, linearParams(58, -1));
        moleBestView = statValue("Best  " + stats("whack-a-mole").getInt("best", 0), INK);
        toolbar.addView(moleBestView, new LinearLayout.LayoutParams(0, -1, 1));

        GridLayout grid = new GridLayout(this);
        grid.setColumnCount(3);
        grid.setRowCount(3);
        LinearLayout.LayoutParams gridParams = linearParams(-1, 315);
        gridParams.topMargin = dp(8);
        shell.addView(grid, gridParams);
        for (int i = 0; i < 9; i++) {
            final int hole = i;
            TextView spot = text("·", 26, 0xFFD0C9B9, true);
            spot.setGravity(Gravity.CENTER);
            spot.setBackground(shape(0xFFF1EEE5, 18, 0));
            GridLayout.LayoutParams params = new GridLayout.LayoutParams(
                GridLayout.spec(i / 3, 1, 1f), GridLayout.spec(i % 3, 1, 1f)
            );
            params.width = 0; params.height = 0;
            params.setMargins(dp(4), dp(4), dp(4), dp(4));
            grid.addView(spot, params);
            moleHoles[i] = spot;
            spot.setOnClickListener(v -> hitMole(hole));
        }
        moleStatus = text("Ready when you are. Tap start!", 11, MUTED, false);
        moleStatus.setGravity(Gravity.CENTER);
        shell.addView(moleStatus, linearParams(-1, 37));
        moleStart = nativeButton("Start a round", true);
        LinearLayout.LayoutParams startParams = linearParams(-1, 44);
        startParams.topMargin = dp(2);
        shell.addView(moleStart, startParams);
        TextView hint = text("A tiny 20-second burst of fun", 10, MUTED, false);
        hint.setGravity(Gravity.CENTER);
        shell.addView(hint, linearParams(-1, 30));
        moleStart.setOnClickListener(v -> startMoleRound());
        resetMoleBoard();
    }

    private void resetMoleBoard() {
        for (TextView hole : moleHoles) {
            hole.setText("·");
            hole.setTextColor(0xFFD0C9B9);
            hole.setBackground(shape(0xFFF1EEE5, 18, 0));
            hole.setScaleX(1);
            hole.setScaleY(1);
            hole.setTranslationY(0);
        }
        activeMole = -1;
    }

    private void startMoleRound() {
        if (moleRunning) return;
        moleRunning = true;
        moleScore = 0;
        moleCombo = 0;
        moleSeconds = 20;
        moleScoreView.setText("Score  0");
        moleTimerView.setText("20s");
        moleTimerView.setTextColor(SAGE);
        moleStatus.setText("Go, go, go!");
        moleStart.setText("Playing…");
        moleStart.setAlpha(.65f);
        moleStart.setClickable(false);
        showMole();
        handler.removeCallbacks(moleClock);
        handler.postDelayed(moleClock, 1000);
    }

    private void showMole() {
        if (!moleRunning) return;
        if (activeMole >= 0) {
            TextView previous = moleHoles[activeMole];
            previous.setText("·");
            previous.setTextColor(0xFFD0C9B9);
            previous.setBackground(shape(0xFFF1EEE5, 18, 0));
            previous.animate().scaleY(.75f).setDuration(90).start();
        }
        int next = random.nextInt(moleHoles.length);
        if (next == activeMole) next = (next + 1 + random.nextInt(8)) % 9;
        activeMole = next;
        TextView hole = moleHoles[next];
        hole.setText("●");
        hole.setTextColor(0xFF795C46);
        hole.setBackground(shape(0xFFE9DEC9, 18, 0));
        hole.setScaleY(.5f);
        hole.setScaleX(.8f);
        hole.animate().scaleX(1).scaleY(1).setDuration(190).setInterpolator(new OvershootInterpolator()).start();
        if (moleHideTask != null) handler.removeCallbacks(moleHideTask);
        moleHideTask = () -> {
            if (activeMole >= 0) {
                TextView old = moleHoles[activeMole];
                old.setText("·");
                old.setTextColor(0xFFD0C9B9);
                old.setBackground(shape(0xFFF1EEE5, 18, 0));
                activeMole = -1;
                moleCombo = 0;
            }
            handler.postDelayed(this::showMole, 180);
        };
        handler.postDelayed(moleHideTask, 850);
    }

    private void hitMole(int hole) {
        if (!moleRunning) return;
        if (hole != activeMole) {
            moleCombo = 0;
            moleStatus.setText("Whoops, missed! Keep your eyes peeled.");
            moleStatus.setTextColor(CORAL);
            moleHoles[hole].animate().translationX(dp(4)).setDuration(65).withEndAction(() ->
                moleHoles[hole].animate().translationX(0).setDuration(65).start()
            ).start();
            return;
        }
        if (moleHideTask != null) handler.removeCallbacks(moleHideTask);
        TextView hit = moleHoles[hole];
        hit.animate().scaleX(1.15f).scaleY(.76f).setDuration(100).withEndAction(() -> {
            hit.setText("+1");
            hit.setTextColor(SAGE);
            hit.animate().scaleX(1).scaleY(1).setDuration(90).start();
        }).start();
        moleScore++;
        moleCombo++;
        moleScoreView.setText("Score  " + moleScore);
        moleStatus.setText(moleCombo > 1 ? "Lovely! x" + moleCombo + " combo!" : "Nice catch! Keep going.");
        moleStatus.setTextColor(SAGE);
        activeMole = -1;
        handler.postDelayed(() -> {
            hit.setText("·");
            hit.setTextColor(0xFFD0C9B9);
            hit.setBackground(shape(0xFFF1EEE5, 18, 0));
        }, 330);
        handler.postDelayed(this::showMole, 180);
    }

    private void finishMoleRound() {
        moleRunning = false;
        handler.removeCallbacks(moleClock);
        if (moleHideTask != null) handler.removeCallbacks(moleHideTask);
        resetMoleBoard();
        int best = Math.max(moleScore, stats("whack-a-mole").getInt("best", 0));
        stats("whack-a-mole").edit()
            .putInt("best", best)
            .putInt("games", stats("whack-a-mole").getInt("games", 0) + 1)
            .apply();
        moleBestView.setText("Best  " + best);
        moleTimerView.setText("0s");
        moleStatus.setText("Time! You caught " + moleScore + " moles. Lovely work!");
        moleStart.setText("Play again");
        moleStart.setAlpha(1f);
        moleStart.setClickable(true);
    }

    private void stopMoleRound() {
        if (!moleRunning) return;
        moleRunning = false;
        handler.removeCallbacks(moleClock);
        if (moleHideTask != null) handler.removeCallbacks(moleHideTask);
        resetMoleBoard();
    }
}
