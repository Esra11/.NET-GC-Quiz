using System.Reflection;
using System.Text.Json;
using System.Net;
using System.Text;

namespace DotNetGcQuiz;

public sealed record Question(string Id, string Round, int Number, string Topic,
    string Prompt, string[] Options, int CorrectIndex, string Explanation);

public static class Content
{
    public static IReadOnlyList<Question> LoadQuestions()
    {
        using var stream = Open("questions.json");
        var questions = JsonSerializer.Deserialize<List<Question>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("The question bank is empty.");
        Validate(questions);
        return questions.AsReadOnly();
    }

    public static void Validate(IReadOnlyList<Question> questions)
    {
        if (questions.Count == 0 || questions.Select(q => q.Id).Distinct().Count() != questions.Count)
            throw new InvalidDataException("Questions need unique IDs and the bank must not be empty.");
        foreach (var q in questions)
            if (string.IsNullOrWhiteSpace(q.Prompt) || string.IsNullOrWhiteSpace(q.Explanation)
                || q.Options.Length != 4 || q.Options.Any(string.IsNullOrWhiteSpace)
                || q.Options.Distinct().Count() != 4 || q.CorrectIndex is < 0 or > 3)
                throw new InvalidDataException($"Invalid question: {q.Id}");
    }

    public static string Introduction()
    {
        using var reader = new StreamReader(Open("introduction.txt"));
        return reader.ReadToEnd();
    }

    private static Stream Open(string name) => Assembly.GetExecutingAssembly()
        .GetManifestResourceStream($"DotNetGcQuiz.Content.{name}")
        ?? throw new FileNotFoundException($"Missing embedded content: {name}");
}

public enum ExamMode { Learning, Live }

public sealed class ExamSession(IReadOnlyList<Question> questions, ExamMode mode)
{
    public IReadOnlyList<Question> Questions { get; } = questions;
    public ExamMode Mode { get; } = mode;
    private readonly Dictionary<string, int> answers = [];
    public IReadOnlyDictionary<string, int> Answers => answers;
    public int Index { get; private set; }
    public bool Finished { get; private set; }
    public Question Current => Questions[Index];
    public int Answered => answers.Count;
    public int Correct => Questions.Count(q => answers.TryGetValue(q.Id, out var a) && a == q.CorrectIndex);
    public int Incorrect => Answered - Correct;
    public int Unanswered => Questions.Count - Answered;
    public double Percent => 100.0 * Correct / Questions.Count;

    public void Select(int option)
    {
        if (Finished || option is < 0 or > 3 || answers.ContainsKey(Current.Id))
            throw new InvalidOperationException("This answer cannot be submitted.");
        answers.Add(Current.Id, option);
        if (Mode == ExamMode.Live) Next();
    }

    public void Next()
    {
        if (Finished || !answers.ContainsKey(Current.Id))
            throw new InvalidOperationException("Answer the current question first.");
        if (Index == Questions.Count - 1) Finish();
        else Index++;
    }

    public void Finish() => Finished = true;

    public bool IsIncorrect(Question question) => answers.TryGetValue(question.Id, out var selected) && selected != question.CorrectIndex;

    public string ToHtmlReport(bool onlyIncorrect = false)
    {
        if (!Finished) throw new InvalidOperationException("Finish the exam before exporting answers.");
        static string H(string value) => WebUtility.HtmlEncode(value);
        var s = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>.NET GC Quiz report</title><style>body{font:16px/1.6 system-ui;max-width:900px;margin:40px auto;padding:0 24px;color:#172b3a;background:#faf9f5}article{background:white;border:1px solid #dce4e6;border-radius:12px;padding:22px;margin:18px 0}.correct{color:#087454;background:#ddf9eb;font-weight:bold}.wrong{color:#ad2531;background:#ffe4e6;font-weight:bold}.selected{font-weight:bold}h1,h2{line-height:1.3}ol{list-style:none;padding:0}li{white-space:pre-wrap;padding:10px 12px;margin:5px 0;border-radius:5px}</style>");
        s.Append($"<h1>.NET GC Quiz · Answer report</h1><p>{H(Mode == ExamMode.Live ? "Live Quiz" : "Learning mode")} · Score <b>{Correct}/{Questions.Count} ({Percent:0.#}%)</b> · Answered {Answered} · Incorrect {Incorrect} · Unanswered {Unanswered}</p>");
        if (onlyIncorrect)
            s.Append($"<p>Incorrect answers only · {Incorrect} question(s). Unanswered questions are excluded.</p>{(Incorrect == 0 ? "<p>No incorrect answers.</p>" : "")}");
        for (int i = 0; i < Questions.Count; i++)
        {
            var q = Questions[i];
            if (onlyIncorrect && !IsIncorrect(q)) continue;
            bool answered = answers.TryGetValue(q.Id, out int selected);
            s.Append($"<article><small>{H(q.Round)} · {H(q.Topic)}</small><h2>{i + 1}. {H(q.Prompt)}</h2><p>{(answered ? (selected == q.CorrectIndex ? "Correct" : "Incorrect") : "Not answered")}</p><ol type=\"A\">");
            for (int j = 0; j < 4; j++)
                s.Append($"<li class=\"{(j == q.CorrectIndex ? "correct" : answered && selected == j ? "wrong" : "")} {(answered && selected == j ? "selected" : "")}\">{(j == q.CorrectIndex ? "✓ " : answered && selected == j ? "✕ " : "")}{(char)('A' + j)}. {H(q.Options[j])}{(answered && selected == j ? " — YOUR ANSWER" : "")}{(j == q.CorrectIndex ? " — CORRECT ANSWER" : "")}</li>");
            s.Append($"</ol><p><b>Explanation:</b> {H(q.Explanation)}</p></article>");
        }
        return s.Append("</html>").ToString();
    }
}
