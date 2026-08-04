using System.Collections.Concurrent;

namespace CleanArchitecture.Web.TodoArchive;

/// <summary>
/// Holds the manual approval decisions that the archive purge waits on.
/// <para>
/// A purge request is registered by <see cref="Register"/> and starts out unapproved. Nothing in
/// this process advances it: the flag flips only when a human calls the approval endpoint, which
/// invokes <see cref="Approve"/>. There is no timer, no retry and no workflow engine, so an
/// unapproved request stays pending indefinitely.
/// </para>
/// </summary>
public class TodoArchiveApprovalGate
{
    private readonly ConcurrentDictionary<int, bool> _approvals = new();

    /// <summary>Registers a purge request for <paramref name="todoItemId"/> as pending approval.</summary>
    public void Register(int todoItemId) => _approvals.TryAdd(todoItemId, false);

    /// <summary>
    /// Records the human decision for <paramref name="todoItemId"/>. This is the only way an
    /// approval flag is ever set, and it is reachable only from the approval endpoint.
    /// </summary>
    public void Approve(int todoItemId, bool approved) => _approvals[todoItemId] = approved;

    /// <summary>
    /// Whether a human has approved purging <paramref name="todoItemId"/>. False while the request
    /// is pending and false when no request was ever registered.
    /// </summary>
    public bool IsApproved(int todoItemId) => _approvals.TryGetValue(todoItemId, out var approved) && approved;

    /// <summary>Every purge request seen so far with its current decision.</summary>
    public IReadOnlyList<TodoArchivePurgeState> Pending() =>
        [.. _approvals.Select(entry => new TodoArchivePurgeState(entry.Key, entry.Value))];
}

/// <summary>The manual-approval state of a single purge request.</summary>
/// <param name="TodoItemId">The item the request targets.</param>
/// <param name="Approved">True once a human has approved the purge.</param>
public sealed record TodoArchivePurgeState(int TodoItemId, bool Approved);

/// <summary>Body of a request to purge an archived todo item.</summary>
/// <param name="TodoItemId">The item to purge.</param>
public sealed record TodoArchivePurgeRequest(int TodoItemId);

/// <summary>Body of a human's decision on a pending purge request.</summary>
/// <param name="TodoItemId">The item the decision applies to.</param>
/// <param name="Approved">True to allow the purge to proceed.</param>
public sealed record TodoArchivePurgeApproval(int TodoItemId, bool Approved);
