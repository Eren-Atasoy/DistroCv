using System.Text.Json;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DistroCv.Infrastructure.Services;

/// <summary>
/// Service implementation for interview coaching and preparation
/// </summary>
public class InterviewCoachService : IInterviewCoachService
{
    private readonly IInterviewPreparationRepository _repository;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<InterviewCoachService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InterviewCoachService(
        IInterviewPreparationRepository repository,
        IGeminiService geminiService,
        ILogger<InterviewCoachService> logger)
    {
        _repository = repository;
        _geminiService = geminiService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    /// <inheritdoc/>
    public async Task<List<string>> GenerateQuestionsAsync(JobPosting job, VerifiedCompany? company = null)
    {
        try
        {
            _logger.LogInformation("Generating interview questions for job: {JobTitle}", job.Title);

            var prompt = BuildQuestionsPrompt(job, company);
            var response = await _geminiService.GenerateContentAsync(prompt);
            
            // Extract JSON array from response
            var questions = ParseJsonList(response);

            return questions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating interview questions");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> AnalyzeAnswerAsync(string question, string answer)
    {
        try
        {
            _logger.LogInformation("Analyzing answer for question: {Question}", question);

            var prompt = BuildAnalysisPrompt(question, answer);
            var response = await _geminiService.GenerateContentAsync(prompt);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing answer");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GenerateImprovementSuggestionsAsync(List<string> answers)
    {
        try
        {
            _logger.LogInformation("Generating improvement suggestions for {Count} answers", answers.Count);

            var prompt = BuildSuggestionsPrompt(answers);
            var response = await _geminiService.GenerateContentAsync(prompt);
            
            var suggestions = ParseJsonList(response);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating improvement suggestions");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<InterviewPreparation?> GetPreparationAsync(Guid applicationId)
    {
        return await _repository.GetByApplicationIdAsync(applicationId);
    }

    /// <inheritdoc/>
    public async Task<InterviewPreparation> CreatePreparationAsync(Guid applicationId)
    {
        var preparation = new InterviewPreparation
        {
            ApplicationId = applicationId,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.CreateAsync(preparation);
    }

    #region Helper Methods

    private string BuildQuestionsPrompt(JobPosting job, VerifiedCompany? company)
    {
        var companyInfo = company != null ? $" at {company.Name} ({company.CompanyCulture ?? "Standard Culture"})" : "";
        
        return $@"You are an expert technical interviewer. Generate 10 interview questions for the position of {job.Title}{companyInfo}.
        
Job Description:
{job.Description}

Requirements:
{job.Requirements ?? "Standard requirements"}

Please provide exactly 10 questions covering:
1. Technical skills relevant to the job
2. System design or architecture (if applicable)
3. Behavioral/Cultural fit
4. Problem-solving scenarios

Return the result strictly as a JSON array of strings:
[""Question 1"", ""Question 2"", ...]";
    }

    private string BuildAnalysisPrompt(string question, string answer)
    {
        return $@"You are an interview coach. Analyze the following answer using the STAR (Situation, Task, Action, Result) technique.

Question: {question}

Candidate's Answer: {answer}

Provide constructive feedback:
1. Identify if the STAR method was used effectively.
2. Highlight strengths in the answer.
3. specific areas for improvement.
4. Suggest a refined version of the answer.

Keep the tone professional and encouraging.";
    }

    private string BuildSuggestionsPrompt(List<string> answers)
    {
        var answersText = string.Join("\n\n---\n\n", answers);
        
        return $@"Based on the following set of interview answers, provide a list of general improvement suggestions. Focus on communication style, technical depth, and clarity.

Answers:
{answersText}

Return the result strictly as a JSON array of strings:
[""Suggestion 1"", ""Suggestion 2"", ...]";
    }

    private List<string> ParseJsonList(string response)
    {
        try 
        {
            // Clean markdown code blocks
            var cleaned = response.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(7);
            if (cleaned.StartsWith("```")) cleaned = cleaned.Substring(3);
            if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);
            cleaned = cleaned.Trim();

            var list = JsonSerializer.Deserialize<List<string>>(cleaned, _jsonOptions);
            return list ?? new List<string>();
        }
        catch 
        {
            // Fallback: try to split by newlines if JSON parsing fails
            return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().TrimStart('-', '*', '1', '.', ' '))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }
    }

    #endregion
}
