using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IAiDiagnosticTraceStore
{
    void AddTrace(AiDiagnosticTraceDto trace);
    IReadOnlyList<AiDiagnosticTraceSummaryDto> GetRecentTraces();
    AiDiagnosticTraceDto? GetTrace(string traceId);
    void Clear();
}

public class AiDiagnosticTraceStore : IAiDiagnosticTraceStore
{
    private const int MaxCapacity = 20;
    private readonly object _lock = new();
    private readonly List<AiDiagnosticTraceDto> _traces = new();

    public void AddTrace(AiDiagnosticTraceDto trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        lock (_lock)
        {
            _traces.Insert(0, trace);
            if (_traces.Count > MaxCapacity)
            {
                _traces.RemoveAt(_traces.Count - 1);
            }
        }
    }

    public IReadOnlyList<AiDiagnosticTraceSummaryDto> GetRecentTraces()
    {
        lock (_lock)
        {
            return _traces.Select(t => new AiDiagnosticTraceSummaryDto(
                t.TraceId,
                t.Timestamp,
                t.Question.QuestionId,
                t.Question.SkillId,
                t.Runtime.ActiveEvaluator,
                t.FinalResult.Level,
                t.Timing.DurationMs
            )).ToList();
        }
    }

    public AiDiagnosticTraceDto? GetTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId)) return null;

        lock (_lock)
        {
            return _traces.FirstOrDefault(t => string.Equals(t.TraceId, traceId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _traces.Clear();
        }
    }
}
