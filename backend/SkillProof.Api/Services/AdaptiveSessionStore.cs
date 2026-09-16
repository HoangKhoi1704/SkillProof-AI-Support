using System;
using System.Collections.Concurrent;
using System.Linq;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IAdaptiveSessionStore
{
    void CreateSession(DiagnosticSessionState state);
    bool TryGetSession(string sessionId, out DiagnosticSessionState? state);
    void UpdateSession(DiagnosticSessionState state);
    bool RemoveSession(string sessionId);
    int Count { get; }
}

public class AdaptiveSessionStore : IAdaptiveSessionStore
{
    private readonly ConcurrentDictionary<string, DiagnosticSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxCapacity;
    private readonly object _pruneLock = new();

    public AdaptiveSessionStore(int maxCapacity = 200)
    {
        _maxCapacity = maxCapacity;
    }

    public int Count => _sessions.Count;

    public void CreateSession(DiagnosticSessionState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(state.SessionId)) throw new ArgumentException("SessionId cannot be empty.", nameof(state));

        EnsureCapacity();
        state.CreatedAt = DateTimeOffset.UtcNow;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        _sessions[state.SessionId] = state;
    }

    public bool TryGetSession(string sessionId, out DiagnosticSessionState? state)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            state = null;
            return false;
        }

        return _sessions.TryGetValue(sessionId, out state);
    }

    public void UpdateSession(DiagnosticSessionState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(state.SessionId)) throw new ArgumentException("SessionId cannot be empty.", nameof(state));

        state.UpdatedAt = DateTimeOffset.UtcNow;
        _sessions[state.SessionId] = state;
    }

    public bool RemoveSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        return _sessions.TryRemove(sessionId, out _);
    }

    private void EnsureCapacity()
    {
        if (_sessions.Count < _maxCapacity) return;

        lock (_pruneLock)
        {
            if (_sessions.Count < _maxCapacity) return;

            // Remove oldest 20% of sessions by UpdatedAt
            var toRemove = _sessions.Values
                .OrderBy(s => s.UpdatedAt)
                .Take(Math.Max(1, _maxCapacity / 5))
                .Select(s => s.SessionId)
                .ToList();

            foreach (var id in toRemove)
            {
                _sessions.TryRemove(id, out _);
            }
        }
    }
}
