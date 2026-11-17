using System.Text.Json;
using EarningsUtility.UI.ApprovalsStub;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.Tests.ApprovalsStub;

public class CocEventBuilderTests
{
    [Fact]
    public void Build_returns_LearningChangeApprovedEvent_when_approved()
    {
        var request = new CocEventRequest(true, Guid.NewGuid(), 99, new());

        var result = CocEventBuilder.Build(request);

        Assert.IsType<LearningChangeApprovedEvent>(result);
    }

    [Fact]
    public void Build_returns_LearningChangeRejectedEvent_when_not_approved()
    {
        var request = new CocEventRequest(false, Guid.NewGuid(), 99, new());

        var result = CocEventBuilder.Build(request);

        Assert.IsType<LearningChangeRejectedEvent>(result);
    }

    [Fact]
    public void Build_carries_learning_key_apprenticeship_id_and_changes_through_unchanged()
    {
        var learningKey = Guid.NewGuid();
        var changes = new List<LearningChangeEvent.Change>
        {
            new() { ChangeType = "StartDate", Data = new() { Old = "2025-08-01", New = "2025-09-01" } }
        };
        var request = new CocEventRequest(true, learningKey, 99, changes);

        var result = CocEventBuilder.Build(request);

        Assert.Equal(learningKey, result.LearningKey);
        Assert.Equal(99, result.ApprenticeshipId);
        Assert.Same(changes, result.Changes);
        Assert.Null(result.Prices);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_carries_prices_through_unchanged(bool approved)
    {
        var prices = new List<LearningChangeEvent.Price>
        {
            new() { TrainingPrice = 7200m, AssessmentPrice = 1800m, EffectiveFrom = "2025-09-01" }
        };
        var request = new CocEventRequest(approved, Guid.NewGuid(), 99, new(), prices);

        var result = CocEventBuilder.Build(request);

        Assert.Same(prices, result.Prices);
        Assert.Empty(result.Changes);
    }

    [Fact]
    public void Built_event_serialises_changes_as_the_agreed_array_shape()
    {
        var learningKey = Guid.Parse("98ef8fee-dc63-4005-b20a-8723999f5ca8");
        var changes = new List<LearningChangeEvent.Change>
        {
            new() { ChangeType = "StartDate", Data = new() { Old = "2026-08-07", New = "2026-09-07" } }
        };
        var request = new CocEventRequest(true, learningKey, 7501659094, changes);

        var json = JsonSerializer.Serialize(CocEventBuilder.Build(request), typeof(LearningChangeApprovedEvent));

        Assert.Equal(
            """{"LearningKey":"98ef8fee-dc63-4005-b20a-8723999f5ca8","ApprenticeshipId":7501659094,"Changes":[{"ChangeType":"StartDate","ApprovalStatus":"EmployerApproved","Data":{"Old":"2026-08-07","New":"2026-09-07"}}],"Prices":null}""",
            json);
    }

    [Theory]
    [InlineData(true, "EmployerApproved")]
    [InlineData(false, "EmployerRejected")]
    public void Build_stamps_every_change_with_the_employer_decision(bool approved, string expectedStatus)
    {
        var changes = new List<LearningChangeEvent.Change>
        {
            new() { ChangeType = "StartDate", Data = new() { Old = "2025-08-01", New = "2025-09-01" } },
            new() { ChangeType = "PlannedEndDate", Data = new() { Old = "2026-08-01", New = "2026-09-01" } }
        };

        var result = CocEventBuilder.Build(new CocEventRequest(approved, Guid.NewGuid(), 99, changes));

        Assert.All(result.Changes, c => Assert.Equal(expectedStatus, c.ApprovalStatus));
    }

    [Fact]
    public void Build_gives_a_rejection_an_empty_reason()
    {
        var result = CocEventBuilder.Build(new CocEventRequest(false, Guid.NewGuid(), 99, new()));

        Assert.Equal("", Assert.IsType<LearningChangeRejectedEvent>(result).Reason);
    }

    [Fact]
    public void Approved_event_serialises_prices_in_the_approvals_master_shape()
    {
        var approved = new LearningChangeApprovedEvent
        {
            LearningKey = Guid.Parse("98ef8fee-dc63-4005-b20a-8723999f5ca8"),
            ApprenticeshipId = 7501659094,
            Changes = new()
            {
                new() { ChangeType = "StartDate", ApprovalStatus = "EmployerApproved", Data = new() { Old = "2025-08-01", New = "2025-09-01" } }
            },
            Prices = new()
            {
                new() { TrainingPrice = 7200m, AssessmentPrice = 1800m, EffectiveFrom = "2025-09-01" }
            }
        };

        var json = JsonSerializer.Serialize(approved);

        Assert.Equal(
            """{"LearningKey":"98ef8fee-dc63-4005-b20a-8723999f5ca8","ApprenticeshipId":7501659094,"Changes":[{"ChangeType":"StartDate","ApprovalStatus":"EmployerApproved","Data":{"Old":"2025-08-01","New":"2025-09-01"}}],"Prices":[{"TrainingPrice":7200,"AssessmentPrice":1800,"EffectiveFrom":"2025-09-01"}]}""",
            json);
    }

    [Fact]
    public void Rejected_event_carries_a_reason()
    {
        var rejected = new LearningChangeRejectedEvent { Reason = "" };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(rejected));

        Assert.Equal("", json.RootElement.GetProperty("Reason").GetString());
    }
}
