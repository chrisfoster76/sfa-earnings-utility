using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.UI.Approvals;

public static class ApprovalEventBuilder
{
    public static ApprenticeshipCreatedEvent Build(ApprovalRequest request)
    {
        return new ApprenticeshipCreatedEvent
        {
            LearningType = request.LearningType,
            ApprenticeshipId = request.ApprenticeshipId,
            ApprenticeshipEmployerTypeOnApproval = request.EmployerType,
            TransferSenderId = request.TransferSenderId,
            ApprenticeshipHashedId = "XYZ123",
            Uln = request.Uln,
            ProviderId = request.Ukprn,
            DateOfBirth = DateTime.Parse("2005-01-14"),
            FirstName = "John",
            LastName = "Smith",
            IsOnFlexiPaymentPilot = true,
            ActualStartDate = DateTime.Parse("2025-08-01"),
            StartDate = DateTime.Parse("2025-08-01"),
            EndDate = DateTime.Parse("2026-07-31"),
            TrainingCode = request.TrainingCode,
            TrainingCourseVersion = "",
            TrainingCourseOption = "",
            TrainingType = ProgrammeType.Standard,
            LegalEntityName = "Mega Corp",
            AccountLegalEntityId = 456,
            AccountId = request.EmployerAccountId,
            PriceEpisodes = new PriceEpisode[]
            {
                new PriceEpisode
                {
                    Cost = 30000,
                    EndPointAssessmentPrice = 5000,
                    TrainingPrice = 25000,
                    FromDate = DateTime.Parse("2025-08-01"),
                    ToDate = DateTime.Parse("2026-07-31"),
                }
            }
        };
    }
}
