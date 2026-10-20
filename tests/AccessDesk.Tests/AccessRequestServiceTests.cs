using AccessDesk.Api.Data;
using AccessDesk.Api.Domain;
using AccessDesk.Api.Security;
using AccessDesk.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AccessDesk.Tests;

public class AccessRequestServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AccessDeskDbContext _db;
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
    private readonly AccessRequestService _service;

    private static readonly CurrentUser Alice = new("alice-id", "alice", false, false);
    private static readonly CurrentUser Bob = new("bob-id", "bob", false, false);
    private static readonly CurrentUser Manager = new("mia-id", "mia", true, false);

    public AccessRequestServiceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AccessDeskDbContext>().UseSqlite(_connection).Options;
        _db = new AccessDeskDbContext(options);
        _db.Database.EnsureCreated();
        _service = new AccessRequestService(_db, _clock);
    }

    [Fact]
    public async Task Create_StoresPendingRequest_TrimsInput_AndWritesAudit()
    {
        var created = await _service.CreateAsync(Alice, "  GitHub  ", "  Need access for the platform team  ");

        Assert.Equal(AccessRequestStatus.Pending, created.Status);
        Assert.Equal("GitHub", created.System);
        Assert.Equal("Need access for the platform team", created.Justification);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, created.CreatedAtUtc);

        var audit = await _db.AuditEntries.SingleAsync();
        Assert.Equal("request.created", audit.Action);
        Assert.Equal("alice", audit.Actor);
    }

    [Fact]
    public async Task List_EmployeeSeesOnlyOwnRequests_ManagerSeesAll()
    {
        await _service.CreateAsync(Alice, "GitHub", "Alice needs GitHub access");
        await _service.CreateAsync(Bob, "Azure", "Bob needs Azure access");

        var aliceList = await _service.ListAsync(Alice, null);
        var managerList = await _service.ListAsync(Manager, null);

        Assert.Single(aliceList);
        Assert.Equal("alice", aliceList[0].RequesterName);
        Assert.Equal(2, managerList.Count);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var first = await _service.CreateAsync(Alice, "GitHub", "Alice needs GitHub access");
        await _service.CreateAsync(Alice, "Azure", "Alice needs Azure access");
        await _service.DecideAsync(Manager, first.Id, approve: true, comment: null);

        var pending = await _service.ListAsync(Manager, AccessRequestStatus.Pending);
        var approved = await _service.ListAsync(Manager, AccessRequestStatus.Approved);

        Assert.Single(pending);
        Assert.Single(approved);
    }

    [Fact]
    public async Task Decide_Approve_UpdatesRequest_AndWritesAudit()
    {
        var created = await _service.CreateAsync(Alice, "GitHub", "Alice needs GitHub access");
        _clock.Advance(TimeSpan.FromHours(1));

        var result = await _service.DecideAsync(Manager, created.Id, approve: true, comment: "  ok  ");

        Assert.Equal(DecisionOutcome.Ok, result.Outcome);
        Assert.Equal(AccessRequestStatus.Approved, result.Request!.Status);
        Assert.Equal("mia", result.Request.DecidedBy);
        Assert.Equal("ok", result.Request.DecisionComment);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, result.Request.DecidedAtUtc);
        Assert.Contains(await _db.AuditEntries.ToListAsync(), a => a.Action == "request.approved");
    }

    [Fact]
    public async Task Decide_Reject_SetsRejected()
    {
        var created = await _service.CreateAsync(Alice, "GitHub", "Alice needs GitHub access");

        var result = await _service.DecideAsync(Manager, created.Id, approve: false, comment: null);

        Assert.Equal(AccessRequestStatus.Rejected, result.Request!.Status);
        Assert.Null(result.Request.DecisionComment);
    }

    [Fact]
    public async Task Decide_UnknownRequest_ReturnsNotFound()
    {
        var result = await _service.DecideAsync(Manager, 999, approve: true, comment: null);

        Assert.Equal(DecisionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Decide_Twice_ReturnsAlreadyDecided_AndDoesNotChangeRequest()
    {
        var created = await _service.CreateAsync(Alice, "GitHub", "Alice needs GitHub access");
        await _service.DecideAsync(Manager, created.Id, approve: true, comment: null);

        var second = await _service.DecideAsync(Manager, created.Id, approve: false, comment: null);

        Assert.Equal(DecisionOutcome.AlreadyDecided, second.Outcome);
        Assert.Equal(AccessRequestStatus.Approved, (await _db.AccessRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task Decide_OwnRequest_IsRefused()
    {
        var created = await _service.CreateAsync(Manager, "GitHub", "Manager needs GitHub access");

        var result = await _service.DecideAsync(Manager, created.Id, approve: true, comment: null);

        Assert.Equal(DecisionOutcome.CannotDecideOwnRequest, result.Outcome);
        Assert.Equal(AccessRequestStatus.Pending, (await _db.AccessRequests.SingleAsync()).Status);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class FixedClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
