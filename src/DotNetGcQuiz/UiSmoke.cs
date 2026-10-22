namespace DotNetGcQuiz;

internal static class UiSmoke
{
    private static void WaitForReport(Form form)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (form.Controls.Find("ReportText", true).FirstOrDefault() is ReportView { IsLayoutReady: false })
        {
            if (watch.Elapsed.TotalSeconds > 10) throw new InvalidOperationException("Report layout did not complete.");
            Application.DoEvents();
            Thread.Sleep(1);
        }
    }

    internal static void BenchmarkReport(string directory)
    {
        Directory.CreateDirectory(directory);
        var bank = Content.LoadQuestions();
        using var form = new MainForm(bank) { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000) };
        form.Show();
        void Click(string name)
        {
            ((Button)form.Controls.Find(name, true).Single()).PerformClick();
            Application.DoEvents();
        }
        Click("TakeQuiz");
        Click("LiveMode");
        foreach (var q in bank.Take(18)) form.Session!.Select((q.CorrectIndex + (q.Number % 3 == 0 ? 0 : 1)) % 4);
        Click("EndExam");
        var times = new Dictionary<string, double>();
        int heartbeatCount = 0;
        using var heartbeat = new System.Windows.Forms.Timer { Interval = 15 };
        heartbeat.Tick += (_, _) => heartbeatCount++;
        heartbeat.Start();
        void Time(string label, string button)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Click(button);
            times[label] = watch.Elapsed.TotalMilliseconds;
            WaitForReport(form);
            times[label.Replace("Ms", "ReadyMs")] = watch.Elapsed.TotalMilliseconds;
            File.WriteAllText(Path.Combine(directory, "timings.json"), System.Text.Json.JsonSerializer.Serialize(times));
        }
        Time("FullReportMs", "DisplayReport");
        Time("IncorrectFilterMs", "FilterIncorrect");
        Time("RestoreFullMs", "FilterIncorrect");
        Time("ThemeSwitchMs", "ThemeToggle");
        var report = (ReportView)form.Controls.Find("ReportText", true).Single();
        if (report.AnswerRows.Count != bank.Count * 4 || !report.Text.Contains(bank[^1].Prompt))
            throw new InvalidOperationException($"Full report omitted questions: {report.AnswerRows.Count}/{bank.Count * 4} answers, {report.Text.Length} characters, last question present: {report.Text.Contains(bank[^1].Prompt)}.");
        if (report.Controls.Count != 0 || heartbeatCount == 0)
            throw new InvalidOperationException("Report must avoid child controls and keep the UI message loop responsive.");
        report.AutoScrollPosition = new Point(0, report.AutoScrollMinSize.Height);
        using var viewport = new Bitmap(report.Width, report.Height);
        report.DrawToBitmap(viewport, new Rectangle(Point.Empty, report.Size));
        if (report.LastPaintedRowCount > 30 || report.LastPaintedRowCount == 0)
            throw new InvalidOperationException($"Report must paint visible rows only: {report.LastPaintedRowCount} painted at {report.AutoScrollPosition.Y}, extent {report.AutoScrollMinSize.Height}.");
        report.Size = new Size(report.Width - 50, report.Height);
        WaitForReport(form);
        report.AutoScrollPosition = Point.Empty;
        // Navigating or toggling the theme while layout is pending must cancel
        // stale work without posting updates to a disposed report.
        Click("ThemeToggle");
        Click("ThemeToggle");
        Click("Results");
        Click("DisplayReport");
        WaitForReport(form);
        using var picture = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
        picture.Save(Path.Combine(directory, "full-report.png"));
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(times));
        form.Close();
    }

    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var all = Content.LoadQuestions();
        Content.Validate(all);
        using var form = new MainForm(all.Take(3).ToArray()) { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-30000, -30000) };
        form.Show();
        if (form.Icon is null) throw new InvalidOperationException("The app window icon is missing.");
        void Click(string name)
        {
            var control = form.Controls.Find(name, true).Single();
            if (control is Button b) b.PerformClick();
            else if (control is RadioButton r) typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(r, [EventArgs.Empty]);
            Application.DoEvents();
            WaitForReport(form);
        }
        void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); }
        void Capture(string name)
        {
            Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
        void VerifyReportFilter(string screenshot)
        {
            void VerifyColors()
            {
                var view = (ReportView)form.Controls.Find("ReportText", true).Single();
                var wrongRow = view.AnswerRows.First(row => row.Text.StartsWith("✕ "));
                var rightRow = view.AnswerRows.First(row => row.Text.StartsWith("✓ "));
                Check(wrongRow.ForeColor.R > wrongRow.ForeColor.G && wrongRow.BackColor.R > wrongRow.BackColor.G, "Incorrect row must have red text and red background");
                Check(rightRow.ForeColor.G > rightRow.ForeColor.R && rightRow.BackColor.G > rightRow.BackColor.R, "Correct row must have green text and green background");
                Check(wrongRow.Width == rightRow.Width && wrongRow.Width >= view.ClientSize.Width - 60, "Answer highlights must fill the row width");
            }
            VerifyColors();
            Click("FilterIncorrect");
            var report = (ReportView)form.Controls.Find("ReportText", true).Single();
            Check(report.Text.Contains(all[0].Prompt) && !report.Text.Contains(all[1].Prompt) && !report.Text.Contains(all[2].Prompt), "Incorrect filter included correct or unanswered questions");
            Click("ThemeToggle");
            VerifyColors();
            Capture(screenshot + "-light");
            Check(form.Controls.Find("FilterIncorrect", true).Single().Text == "Display All Answers", "Theme toggle lost report filter");
            Click("ThemeToggle");
            VerifyColors();
            Capture(screenshot);
            Click("FilterIncorrect");
            Check(form.Controls.Find("ReportText", true).Single().Text.Contains(all[2].Prompt), "Display All did not restore unanswered questions");
        }
        Capture("home");
        Check(form.BackColor.GetBrightness() < 0.2f, "Dark mode is not the default");
        Check(form.Controls.Find("ThemeToggle", true).Single().Text == "Light Mode", "Dark theme must offer Light Mode");
        Click("ThemeToggle");
        Check(form.BackColor.GetBrightness() > 0.8f, "Light mode toggle failed");
        Check(form.Controls.Find("ThemeToggle", true).Single().Text == "Dark Mode", "Light theme must offer Dark Mode");
        Capture("home-light");
        Click("ThemeToggle");
        form.Size = form.MinimumSize;
        Capture("home-small");
        Click("TakeQuiz");
        Capture("modes-small");
        Check(form.Controls.Find("LiveMode", true).Single().Bottom > 0, "Small-window mode navigation missing");
        Click("Home");
        form.Size = new Size(1160, 880);
        Click("Learn");
        Check(form.Controls.Find("IntroductionText", true).Length == 1, "Introduction missing");
        Capture("introduction");
        Click("StartFromIntro");
        Capture("modes");
        Click("LearningMode");
        Click($"Option{(all[0].CorrectIndex + 1) % 4}");
        Check(form.Session!.Index == 0 && form.Session.Incorrect == 1, "Learning mode advanced unexpectedly");
        Check(form.Controls.Find("Feedback", true).Single().Text.Contains("Incorrect"), "Incorrect feedback missing");
        Capture("feedback");
        var activeSession = form.Session;
        Click("ThemeToggle");
        Check(ReferenceEquals(activeSession, form.Session) && form.Session!.Answered == 1 && form.Session.Index == 0, "Theme toggle changed exam state");
        Check(form.Controls.Find("Feedback", true).Single().Text.Contains("Incorrect"), "Theme toggle lost feedback");
        Click("ThemeToggle");
        Click("Next");
        Click($"Option{all[1].CorrectIndex}");
        var green = form.Controls.Find("Feedback", true).Single();
        Check(green.Text.StartsWith("✓ Correct") && green.ForeColor.G > green.ForeColor.R, "Green correct feedback missing");
        Capture("correct-feedback");
        Click("EndExam");
        Check(form.Session!.Unanswered == 1 && form.Session.Finished && form.Session.Correct == 1 && form.Session.Incorrect == 1, "Early end failed");
        Click("DisplayReport");
        Check(form.Controls.Find("ReportText", true).Single().Text.Contains("YOUR ANSWER"), "Report selection missing");
        Capture("report");
        VerifyReportFilter("incorrect-report-learning");
        Click("ThemeToggle");
        Check(form.Controls.Find("ReportText", true).Single().BackColor.GetBrightness() > 0.8f, "Report did not switch to light mode");
        Click("ThemeToggle");
        Click("Home");
        Click("TakeQuiz");
        Click("LiveMode");
        for (int i = 0; i < 3; i++)
        {
            Check(form.Controls.Find("Feedback", true).Length == 0, "Live mode exposed feedback");
            Click($"Option{all[i].CorrectIndex}");
        }
        Check(form.Session!.Finished && form.Session.Correct == 3, "Live completion failed");
        Capture("results");
        File.WriteAllText(Path.Combine(directory, "smoke-report.html"), form.Session.ToHtmlReport());
        Click("NewExam");
        Click("LiveMode");
        Click($"Option{(all[0].CorrectIndex + 1) % 4}");
        Click($"Option{all[1].CorrectIndex}");
        Click("EndExam");
        Click("DisplayReport");
        VerifyReportFilter("incorrect-report-live");
        Click("Results");
        Click("NewExam");
        Click("LiveMode");
        Click("EndExam");
        Check(form.Session!.Finished && form.Session.Unanswered == 3 && form.Session.Correct == 0, "Live early end failed");
        Click("DisplayReport");
        Check(form.Controls.Find("ReportText", true).Single().Text.Contains("Not answered"), "Live unanswered report missing");
        Click("FilterIncorrect");
        Check(form.Controls.Find("ReportText", true).Single().Text.Contains("No incorrect answers."), "Empty filtered report missing explanation");
        File.WriteAllText(Path.Combine(directory, "passed.txt"), $"PASS: {all.Count} questions validated; introduction, modes, learning feedback, early end, report and live completion verified.");
        form.Close();
    }
}
