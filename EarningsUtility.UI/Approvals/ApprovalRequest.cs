using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.UI.Approvals;

public record ApprovalRequest(
    string Uln,
    long EmployerAccountId,
    ApprenticeshipEmployerType EmployerType,
    long ApprenticeshipId,
    LearningType LearningType,
    long Ukprn,
    string TrainingCode,
    long? TransferSenderId);
