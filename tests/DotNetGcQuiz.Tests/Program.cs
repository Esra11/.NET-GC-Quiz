using DotNetGcQuiz;

int checks = 0;
void Check(bool value, string message)
{
    checks++;
    if (!value) throw new Exception(message);
}
void Reject(Action action, string message)
{
    try { action(); }
    catch (InvalidOperationException) { checks++; return; }
    throw new Exception(message);
}

var bank = Content.LoadQuestions();
Check(bank.Count >= 120, "Question coverage unexpectedly reduced");
Check(bank.Take(16).All(q => q.Topic == "Overview"), "General questions must come first");
Check(bank.Skip(16).All(q => q.Topic != "Overview"), "Deep dives must follow overview");
Check(bank.Select(q => q.Prompt.Trim().ToLowerInvariant()).Distinct().Count() == bank.Count, "Duplicate question prompts");
Check(bank.All(q => q.Explanation.Contains("\n\n") && q.Explanation.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length >= 60), "A question lacks a detailed follow-up explanation");
Check(bank.Select(q => q.Topic).Distinct().Count() >= 12, "Missing deep-dive topics");
Check(Content.Introduction().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length is > 450 and < 800, "Introduction length is not about three minutes");
foreach (var group in bank.GroupBy(q => q.Topic))
{
    int start = bank.ToList().FindIndex(q => q.Topic == group.Key);
    Check(bank.Skip(start).Take(group.Count()).All(q => q.Topic == group.Key), "Topic sections are not contiguous");
}

var learning = new ExamSession(bank, ExamMode.Learning);
Reject(() => learning.Next(), "Learning advanced before an answer");
Reject(() => learning.ToHtmlReport(), "Report available during exam");
learning.Select(bank[0].CorrectIndex);
Check(learning.Index == 0 && learning.Correct == 1, "Learning must wait after selecting");
Reject(() => learning.Select(1), "Learning allowed answer replacement");
learning.Next();
learning.Select((bank[1].CorrectIndex + 1) % 4);
learning.Finish();
Check(learning.Answered == 2 && learning.Correct == 1 && learning.Incorrect == 1 && learning.Unanswered == bank.Count - 2, "Early-exam scoring failed");
Check(Math.Abs(learning.Percent - 100.0 / bank.Count) < 0.0001, "Wrong score denominator");
string report = learning.ToHtmlReport();
Check(report.Contains("YOUR ANSWER") && report.Contains("CORRECT ANSWER") && report.Contains("Not answered"), "Report labels missing");
Check(report.Split("<article>").Length - 1 == bank.Count, "Report did not include the entire exam");
Reject(() => learning.Select(0), "Finished session accepted an answer");

foreach (var mode in new[] { ExamMode.Learning, ExamMode.Live })
{
    var mixed = new ExamSession(bank.Take(3).ToArray(), mode);
    mixed.Select((bank[0].CorrectIndex + 1) % 4);
    if (mode == ExamMode.Learning) mixed.Next();
    mixed.Select(bank[1].CorrectIndex);
    mixed.Finish();
    string filtered = mixed.ToHtmlReport(onlyIncorrect: true);
    Check(filtered.Split("<article>").Length - 1 == 1, $"{mode}: filter included correct or unanswered questions");
    Check(filtered.Contains(System.Net.WebUtility.HtmlEncode(bank[0].Prompt)) && !filtered.Contains(System.Net.WebUtility.HtmlEncode(bank[1].Prompt)), $"{mode}: incorrect filter contents wrong");
    Check(filtered.Contains(">✕ ") && filtered.Contains(">✓ "), $"{mode}: leading answer symbols missing");
    Check(mixed.Correct == 1 && mixed.Incorrect == 1 && mixed.Unanswered == 1, "Filter changed scores");
}

var live = new ExamSession(bank, ExamMode.Live);
foreach (var q in bank) live.Select(q.CorrectIndex);
Check(live.Finished && live.Correct == bank.Count && live.Percent == 100 && live.Unanswered == 0, "Full live quiz failed");
var empty = new ExamSession(bank, ExamMode.Live);
empty.Finish();
Check(empty.Correct == 0 && empty.Unanswered == bank.Count && empty.Percent == 0, "Empty exam failed");
Check(empty.ToHtmlReport(true).Contains("No incorrect answers.") && !empty.ToHtmlReport(true).Contains("<article>"), "Empty incorrect-only export failed");
Check(live.ToHtmlReport(true).Contains("No incorrect answers."), "All-correct incorrect-only export failed");

var injection = new Question("escape", "Round", 1, "Topic", "<script>&", ["<b>", "B", "C", "D"], 0, "<img>&");
var escape = new ExamSession([injection], ExamMode.Live);
escape.Select(0);
string html = escape.ToHtmlReport();
Check(!html.Contains("<script>") && html.Contains("&lt;script&gt;&amp;") && html.Contains("&lt;img&gt;&amp;"), "HTML was not escaped");
Console.WriteLine($"PASS: {checks} checks; {bank.Count} questions, {bank.Select(q => q.Topic).Distinct().Count()} sections; scores, progression and report verified.");
