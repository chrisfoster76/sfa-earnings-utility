using EarningsUtility.UI.Approvals;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.Tests.Approvals;

public class ApprovalEventBuilderTests
{
    private static readonly ApprovalRequest Request = new(
        Uln: "1234567890",
        EmployerAccountId: 12345,
        EmployerType: ApprenticeshipEmployerType.Levy,
        ApprenticeshipId: 99,
        LearningType: LearningType.Apprenticeship,
        Ukprn: 10005077,
        TrainingCode: "21",
        TransferSenderId: 555);

    [Fact]
    public void Build_carries_user_supplied_fields_through_unchanged()
    {
        var result = ApprovalEventBuilder.Build(Request);

        Assert.Equal("1234567890", result.Uln);
        Assert.Equal(12345, result.AccountId);
        Assert.Equal(ApprenticeshipEmployerType.Levy, result.ApprenticeshipEmployerTypeOnApproval);
        Assert.Equal(99, result.ApprenticeshipId);
        Assert.Equal(LearningType.Apprenticeship, result.LearningType);
        Assert.Equal(10005077, result.ProviderId);
        Assert.Equal("21", result.TrainingCode);
        Assert.Equal(555, result.TransferSenderId);
    }

    [Fact]
    public void Build_leaves_TransferSenderId_null_when_not_supplied()
    {
        var request = Request with { TransferSenderId = null };

        var result = ApprovalEventBuilder.Build(request);

        Assert.Null(result.TransferSenderId);
    }

    [Fact]
    public void Build_sets_hardcoded_fields_unrelated_to_user_input()
    {
        var result = ApprovalEventBuilder.Build(Request);

        Assert.Equal("XYZ123", result.ApprenticeshipHashedId);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Smith", result.LastName);
        Assert.Equal("Mega Corp", result.LegalEntityName);
        Assert.Equal(456, result.AccountLegalEntityId);
        Assert.Equal(ProgrammeType.Standard, result.TrainingType);
        Assert.True(result.IsOnFlexiPaymentPilot);
        Assert.Single(result.PriceEpisodes);
    }

    [Fact]
    public void Build_maps_LearningType_through_without_translation()
    {
        var request = Request with { LearningType = LearningType.ApprenticeshipUnit };

        var result = ApprovalEventBuilder.Build(request);

        Assert.Equal(LearningType.ApprenticeshipUnit, result.LearningType);
    }
}
