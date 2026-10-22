namespace DotNetGcQuiz;

public sealed class MainForm : Form
{
    private bool darkMode = true;
    private Color Ink => darkMode ? Color.FromArgb(232, 239, 245) : Color.FromArgb(25, 43, 57);
    private Color Teal => darkMode ? Color.FromArgb(89, 219, 196) : Color.FromArgb(0, 108, 103);
    private Color Paper => darkMode ? Color.FromArgb(17, 24, 32) : Color.FromArgb(246, 247, 243);
    private Color Surface => darkMode ? Color.FromArgb(28, 39, 50) : Color.White;
    private Color Muted => darkMode ? Color.FromArgb(171, 190, 204) : Color.FromArgb(84, 103, 112);
    private Color Success => darkMode ? Color.FromArgb(126, 235, 180) : Color.FromArgb(0, 100, 68);
    private Color Error => darkMode ? Color.FromArgb(255, 162, 166) : Color.FromArgb(163, 37, 43);
    private Color SuccessBackground => darkMode ? Color.FromArgb(26, 65, 49) : Color.FromArgb(232, 246, 239);
    private Color ErrorBackground => darkMode ? Color.FromArgb(67, 35, 43) : Color.FromArgb(255, 240, 239);
    private Action currentPage = () => { };
    private readonly Button themeToggle;
    private readonly IReadOnlyList<Question> bank;
    private readonly Panel host = new() { Dock = DockStyle.Fill, Padding = new Padding(36, 24, 36, 24) };
    private ExamSession? exam;
    private bool changingAnswer;
    private bool onlyIncorrectReport;
    internal ExamSession? Session => exam;

    public MainForm(IReadOnlyList<Question>? questions = null)
    {
        bank = questions ?? Content.LoadQuestions();
        Text = ".NET GC Quiz";
        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("DotNetGcQuiz.Assets.quiz.ico")
            ?? throw new InvalidDataException("The app icon is missing."))
        using (var appIcon = new Icon(iconStream))
            Icon = (Icon)appIcon.Clone();
        Font = new Font("Segoe UI", 11);
        ForeColor = Ink;
        BackColor = Paper;
        Size = new Size(1160, 880);
        MinimumSize = new Size(920, 740);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        var brand = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(25, 43, 57), Padding = new Padding(36, 8, 24, 8) };
        brand.Controls.Add(new Label { Text = ".NET  /  GC QUIZ", ForeColor = Color.White, Font = new Font("Segoe UI", 17, FontStyle.Bold), AutoSize = true });
        themeToggle = Button("ThemeToggle", "Light Mode", ToggleTheme, false);
        themeToggle.Dock = DockStyle.Right;
        themeToggle.Width = 215;
        themeToggle.AutoSize = false;
        themeToggle.Margin = Padding.Empty;
        themeToggle.BackColor = Color.FromArgb(43, 61, 76);
        themeToggle.ForeColor = Color.White;
        themeToggle.AccessibleDescription = "Switch to light mode. Dark mode is enabled by default.";
        brand.Controls.Add(themeToggle);
        Controls.Add(host);
        Controls.Add(brand);
        FormClosing += (_, e) =>
        {
            if (exam is { Finished: false } && MessageBox.Show(this, "Close this exam? Your current answers will be lost.", "Exam in progress", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                e.Cancel = true;
        };
        Home();
    }

    private void ToggleTheme()
    {
        darkMode = !darkMode;
        themeToggle.Text = darkMode ? "Light Mode" : "Dark Mode";
        themeToggle.AccessibleDescription = darkMode ? "Switch to light mode." : "Switch to dark mode.";
        BackColor = host.BackColor = Paper;
        ForeColor = Ink;
        // Rebuild only the current screen; keep the exam and submitted answers intact.
        currentPage();
    }

    private TableLayoutPanel Page(string eyebrow, string title, string subtitle)
    {
        host.SuspendLayout();
        while (host.Controls.Count > 0) host.Controls[0].Dispose();
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Paper };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(TextLabel(eyebrow.ToUpperInvariant(), 10, Teal, true), 0, 0);
        page.Controls.Add(TextLabel(title, 24, Ink, true), 0, 1);
        page.Controls.Add(TextLabel(subtitle, 11, Muted), 0, 2);
        host.Controls.Add(page);
        host.ResumeLayout(true);
        return page;
    }

    private Label TextLabel(string text, float size, Color? color = null, bool bold = false) => new()
    {
        Text = text, Dock = DockStyle.Fill, AutoSize = true, AutoEllipsis = false, UseMnemonic = false,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? Ink, Margin = new Padding(0, 0, 0, 8)
    };

    private Button Button(string name, string text, Action click, bool primary = true)
    {
        var b = new Button { Name = name, Text = text, AutoSize = true, MinimumSize = new Size(150, 44), Padding = new Padding(16, 8, 16, 8), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Teal : Surface, ForeColor = primary ? darkMode ? Paper : Color.White : Ink, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(0, 6, 12, 6) };
        b.FlatAppearance.BorderColor = primary ? Teal : Color.FromArgb(205, 216, 218);
        b.Click += (_, _) => click();
        return b;
    }

    private TableLayoutPanel Rows(params float[] heights)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = heights.Length, BackColor = Paper };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var h in heights) p.RowStyles.Add(new RowStyle(h == 0 ? SizeType.AutoSize : h < 0 ? SizeType.Percent : SizeType.Absolute, Math.Abs(h)));
        return p;
    }

    private void Home()
    {
        currentPage = Home;
        exam = null;
        var page = Page("Learn • practice • understand", "Understand what keeps objects alive.", $"{bank.Count} interview questions • Four choices each • No timer");
        var rows = Rows(0, 96, 96, -100);
        var note = TextLabel("Start with the big picture. Then follow a guided path from GC fundamentals into the details of marking, planning, relocation, sweeping, compaction and finalization.", 14);
        note.Padding = new Padding(24);
        note.BackColor = Surface;
        rows.Controls.Add(note, 0, 0);
        var learn = Button("Learn", "Learn Introduction to GC     →", Introduction);
        learn.Dock = DockStyle.Fill;
        rows.Controls.Add(learn, 0, 1);
        var quiz = Button("TakeQuiz", "Take Quiz     →", Modes);
        quiz.Dock = DockStyle.Fill;
        rows.Controls.Add(quiz, 0, 2);
        var topics = TextLabel("YOUR LEARNING PATH\n\nOverview → generations & allocation → roots & metadata → marking → planning → sweeping → relocation & compaction → pinning → finalization → GC modes & diagnostics", 12, Muted);
        topics.Padding = new Padding(0, 26, 0, 0);
        rows.Controls.Add(topics, 0, 3);
        page.Controls.Add(rows, 0, 3);
    }

    private void Introduction()
    {
        currentPage = Introduction;
        var page = Page("The essentials • about 3 minutes", "Learn Introduction to GC", "A short guide to the ideas used throughout the quiz.");
        var rows = Rows(-100, 80);
        var reading = new RichTextBox { Name = "IntroductionText", Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Surface, ForeColor = Ink, Font = new Font("Segoe UI", 12), DetectUrls = false };
        reading.Text = Content.Introduction();
        foreach (var line in reading.Lines.Where(l => l.StartsWith("• ")))
        {
            int start = reading.Text.IndexOf(line, StringComparison.Ordinal);
            reading.Select(start, line.Length);
            reading.SelectionFont = new Font("Segoe UI", 13, FontStyle.Bold);
            reading.SelectionColor = Teal;
        }
        reading.Select(0, 0);
        rows.Controls.Add(reading, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        actions.Controls.Add(Button("Home", "Back to Home", Home, false));
        actions.Controls.Add(Button("StartFromIntro", "Take Quiz →", Modes));
        rows.Controls.Add(actions, 0, 1);
        page.Controls.Add(rows, 0, 3);
    }

    private void Modes()
    {
        currentPage = Modes;
        var page = Page("Choose your exam mode", "Practice your way.", "Both modes follow the same path: general questions first, then deeper questions grouped by concept.");
        var rows = Rows(90, 0, 80, 0, -100, 80);
        var learn = Button("LearningMode", "Show Answer and explanation when selecting answer", () => Start(ExamMode.Learning));
        learn.Dock = DockStyle.Fill;
        rows.Controls.Add(learn, 0, 0);
        rows.Controls.Add(TextLabel("See whether you are correct immediately. Read the explanation, then select Next Question.", 11), 0, 1);
        var live = Button("LiveMode", "Live Quiz", () => Start(ExamMode.Live));
        live.Dock = DockStyle.Fill;
        rows.Controls.Add(live, 0, 2);
        rows.Controls.Add(TextLabel("answers report will be shared at the end of the exam", 11), 0, 3);
        var footer = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var note = TextLabel("End Exam is available on every question. Unanswered questions count as zero. You can display and save a full report after either mode.", 11);
        note.Dock = DockStyle.Top;
        note.MaximumSize = new Size(Math.Max(600, host.ClientSize.Width - 100), 0);
        note.Padding = new Padding(0, 16, 0, 0);
        footer.Controls.Add(note);
        rows.Controls.Add(footer, 0, 4);
        var back = new FlowLayoutPanel { Dock = DockStyle.Fill };
        back.Controls.Add(Button("Home", "Back to Home", Home, false));
        rows.Controls.Add(back, 0, 5);
        page.Controls.Add(rows, 0, 3);
    }

    private void Start(ExamMode mode)
    {
        onlyIncorrectReport = false;
        exam = new ExamSession(bank, mode);
        QuestionPage();
    }

    private void QuestionPage()
    {
        currentPage = QuestionPage;
        if (exam is null) return;
        if (exam.Finished) { Summary(); return; }
        var q = exam.Current;
        bool answered = exam.Answers.TryGetValue(q.Id, out int selected);
        var page = Page($"{(exam.Mode == ExamMode.Live ? "Live Quiz" : "Learning mode")} • {q.Round}", $"Question {exam.Index + 1} of {bank.Count}", q.Topic);
        var rows = Rows(54, -100, 80);
        var progress = new ProgressBar { Name = "Progress", Dock = DockStyle.Top, Height = 8, Maximum = bank.Count, Value = exam.Answered, Margin = new Padding(0, 8, 0, 0) };
        rows.Controls.Add(progress, 0, 0);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var body = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(0, 0, 18, 12) };
        Label? feedbackToShow = null;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var prompt = TextLabel(q.Prompt, 17, Ink, true);
        prompt.AutoSize = true;
        prompt.MaximumSize = new Size(Math.Max(600, host.ClientSize.Width - 100), 0);
        prompt.Margin = new Padding(0, 0, 0, 20);
        body.Controls.Add(prompt);
        for (int i = 0; i < 4; i++)
        {
            int choice = i;
            var radio = new RadioButton { Name = $"Option{i}", Text = $"{(char)('A' + i)}.   {q.Options[i]}", Dock = DockStyle.Top,
                AutoSize = true, MinimumSize = new Size(0, 62), MaximumSize = new Size(Math.Max(600, host.ClientSize.Width - 100), 0), Padding = new Padding(16, 12, 16, 12), Margin = new Padding(0, 0, 0, 10),
                BackColor = Surface, ForeColor = Ink, AutoCheck = false, Checked = answered && selected == i, Cursor = Cursors.Hand, AccessibleName = $"Option {(char)('A' + i)}: {q.Options[i]}" };
            if (answered && i == q.CorrectIndex) radio.BackColor = SuccessBackground;
            if (answered && selected == i && selected != q.CorrectIndex) radio.BackColor = ErrorBackground;
            radio.Click += (_, _) => Choose(choice);
            body.Controls.Add(radio);
        }
        if (answered)
        {
            bool correct = selected == q.CorrectIndex;
            var feedback = TextLabel($"{(correct ? "✓ Correct" : "✕ Incorrect")}\n\nCorrect answer: {(char)('A' + q.CorrectIndex)}. {q.Options[q.CorrectIndex]}\n\n{q.Explanation}", 12, correct ? Success : Error);
            feedback.Name = "Feedback";
            feedback.AutoSize = true;
            feedback.MaximumSize = new Size(Math.Max(600, host.ClientSize.Width - 100), 0);
            feedback.Padding = new Padding(18);
            feedback.BackColor = correct ? SuccessBackground : ErrorBackground;
            body.Controls.Add(feedback);
            feedbackToShow = feedback;
        }
        scroll.Controls.Add(body);
        rows.Controls.Add(scroll, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        if (answered) actions.Controls.Add(Button("Next", exam.Index == bank.Count - 1 ? "Finish Exam →" : "Next Question →", () => { exam.Next(); QuestionPage(); }));
        actions.Controls.Add(Button("EndExam", "End Exam", () => { exam.Finish(); Summary(); }, false));
        rows.Controls.Add(actions, 0, 2);
        page.Controls.Add(rows, 0, 3);
        if (feedbackToShow is not null)
        {
            var feedback = feedbackToShow;
            BeginInvoke(() =>
            {
                if (!scroll.IsDisposed && !feedback.IsDisposed)
                {
                    // Start at the verdict so long, detailed explanations read top to bottom.
                    scroll.AutoScrollPosition = new Point(0, feedback.Top);
                }
            });
        }
    }

    private void Choose(int choice)
    {
        if (changingAnswer || exam is null || exam.Finished || exam.Answers.ContainsKey(exam.Current.Id)) return;
        changingAnswer = true;
        try { exam.Select(choice); QuestionPage(); }
        finally { changingAnswer = false; }
    }

    private void Summary()
    {
        currentPage = Summary;
        if (exam is null) return;
        var page = Page("Exam complete", "Your results", "Review the explanations to see what to practice next.");
        var rows = Rows(136, 100, 80, -100);
        var score = TextLabel($"{exam.Correct} / {bank.Count}     ·     {exam.Percent:0.#}%", 38, Teal, true);
        score.Name = "Score";
        rows.Controls.Add(score, 0, 0);
        rows.Controls.Add(TextLabel($"{exam.Answered} answered      {exam.Correct} correct      {exam.Incorrect} incorrect      {exam.Unanswered} unanswered\n\nScore uses the full exam. Unanswered questions count as zero.", 12), 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        actions.Controls.Add(Button("DisplayReport", "Display Report", Report));
        actions.Controls.Add(Button("NewExam", "Try Again", Modes, false));
        actions.Controls.Add(Button("Home", "Home", Home, false));
        rows.Controls.Add(actions, 0, 2);
        page.Controls.Add(rows, 0, 3);
    }

    private void Report()
    {
        currentPage = Report;
        if (exam is null) return;
        var page = Page("Your answers • correct answers • explanations", "Answer report", $"{exam.Correct}/{bank.Count} correct ({exam.Percent:0.#}%) • {exam.Unanswered} unanswered");
        var rows = Rows(80, -100, 80);
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill };
        filters.Controls.Add(Button("FilterIncorrect", onlyIncorrectReport ? "Display All Answers" : "Display Only Incorrect Answers", () =>
        {
            onlyIncorrectReport = !onlyIncorrectReport;
            Report();
        }, false));
        rows.Controls.Add(filters, 0, 0);
        var report = new ReportView(Surface) { Name = "ReportText", Dock = DockStyle.Fill, ForeColor = Ink };
        if (onlyIncorrectReport)
            report.Append($"Incorrect answers only · {exam.Incorrect} question(s). Unanswered questions are excluded.\n{(exam.Incorrect == 0 ? "\nNo incorrect answers.\n" : "")}", Muted);
        for (int n = 0; n < bank.Count; n++)
        {
            var q = bank[n];
            if (onlyIncorrectReport && !exam.IsIncorrect(q)) continue;
            bool answered = exam.Answers.TryGetValue(q.Id, out int selected);
            report.Append($"\n{n + 1}. {q.Prompt}\n", Ink, true);
            report.Append($"{q.Topic} • {(answered ? selected == q.CorrectIndex ? "Correct" : "Incorrect" : "Not answered")}\n", Teal);
            for (int i = 0; i < 4; i++)
            {
                bool correct = i == q.CorrectIndex;
                bool wrong = answered && selected == i && !correct;
                report.Append($"{(correct ? "✓ " : wrong ? "✕ " : "   ")}{(char)('A' + i)}. {q.Options[i]}{(answered && selected == i ? "  ← YOUR ANSWER" : "")}{(correct ? "  CORRECT ANSWER" : "")}\n",
                    correct ? Success : wrong ? Error : Ink, correct || wrong,
                    correct ? SuccessBackground : wrong ? ErrorBackground : Surface, answer: true);
            }
            report.Append($"Explanation: {q.Explanation}\n\n", Ink);
        }
        report.Complete();
        rows.Controls.Add(report, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        actions.Controls.Add(Button("SaveReport", "Save HTML Report", () =>
        {
            using var dialog = new SaveFileDialog { Filter = "HTML report (*.html)|*.html", FileName = "GC-Quiz-Report.html", DefaultExt = "html" };
            if (dialog.ShowDialog(this) == DialogResult.OK) File.WriteAllText(dialog.FileName, exam.ToHtmlReport(onlyIncorrectReport));
        }));
        actions.Controls.Add(Button("Results", "Back to Results", Summary, false));
        actions.Controls.Add(Button("Home", "Home", Home, false));
        rows.Controls.Add(actions, 0, 2);
        page.Controls.Add(rows, 0, 3);
    }
}
