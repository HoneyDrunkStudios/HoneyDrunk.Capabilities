using HoneyDrunk.Audit.Abstractions;

namespace HoneyDrunk.Capabilities.Canary;

/// <summary>Records every audit entry appended.</summary>
internal sealed class RecordingAuditLog : IAuditLog
{
    private readonly List<AuditEntry> entries = [];

    public IReadOnlyList<AuditEntry> Entries => this.entries;

    public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        this.entries.Add(entry);
        return Task.CompletedTask;
    }
}
