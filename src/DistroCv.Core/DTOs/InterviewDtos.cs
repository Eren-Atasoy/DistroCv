namespace DistroCv.Core.DTOs;

public record InterviewPreparationDto(
    Guid Id,
    Guid ApplicationId,
    List<string> Questions,
    List<AnswerWithFeedback>? Answers,
    DateTime CreatedAt
);

public record AnswerWithFeedback(
    string Question,
    string Answer,
    string Feedback
);

public record SubmitAnswerDto(
    string Question,
    string Answer
);

public record AnswerFeedbackDto(
    string Question,
    string Answer,
    string Feedback,
    List<string> ImprovementSuggestions
);
