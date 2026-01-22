using System.Text.Json;
using DistroCv.Core.DTOs;
using DistroCv.Core.Entities;
using DistroCv.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DistroCv.Api.Controllers;

/// <summary>
/// Interview preparation and coaching controller
/// </summary>
public class InterviewController : BaseApiController
{
    private readonly IInterviewCoachService _coachService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IInterviewPreparationRepository _preparationRepository;
    private readonly ILogger<InterviewController> _logger;

    public InterviewController(
        IInterviewCoachService coachService,
        IApplicationRepository applicationRepository,
        IInterviewPreparationRepository preparationRepository,
        ILogger<InterviewController> logger)
    {
        _coachService = coachService;
        _applicationRepository = applicationRepository;
        _preparationRepository = preparationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get interview questions for an application (or generate if first time)
    /// </summary>
    [HttpGet("{applicationId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid applicationId)
    {
        _logger.LogInformation("Getting interview questions for application: {ApplicationId}", applicationId);

        var preparation = await _preparationRepository.GetByApplicationIdAsync(applicationId);
        
        // If already exists and has questions, return them
        if (preparation != null && !string.IsNullOrEmpty(preparation.Questions))
        {
            var questions = JsonSerializer.Deserialize<List<string>>(preparation.Questions) ?? new List<string>();
            return Ok(new { applicationId, questions });
        }

        // Generate new questions
        var application = await _applicationRepository.GetByIdAsync(applicationId);
        if (application == null)
            return NotFound("Application not found");

        // Ensure job details are loaded (Repository GetByIdAsync usually includes them or we need a new method)
        // Assuming GetByIdAsync includes JobPosting and VerifiedCompany or lazy loading is off
        // We might need to fetch job posting if not included. Let's assume repo handles includes for now or user Include in query
        if (application.JobPosting == null)
        {
             // Fallback if not loaded
             // For now assume application.JobPosting is populated (standard aggregation root pattern)
             // If null, we can't proceed.
             return BadRequest("Job posting data is missing");
        }

        var generatedQuestions = await _coachService.GenerateQuestionsAsync(application.JobPosting, application.JobPosting.VerifiedCompany);

        // Save to DB
        if (preparation == null)
        {
            preparation = new InterviewPreparation
            {
                ApplicationId = applicationId,
                Questions = JsonSerializer.Serialize(generatedQuestions),
                UserAnswers = "[]", 
                Feedback = "[]",
                CreatedAt = DateTime.UtcNow
            };
            await _preparationRepository.CreateAsync(preparation);
        }
        else
        {
            preparation.Questions = JsonSerializer.Serialize(generatedQuestions);
            await _preparationRepository.UpdateAsync(preparation);
        }

        return Ok(new { applicationId, questions = generatedQuestions });
    }

    /// <summary>
    /// Start interview simulation
    /// </summary>
    [HttpPost("{applicationId:guid}/simulate")]
    public async Task<IActionResult> StartSimulation(Guid applicationId)
    {
        _logger.LogInformation("Starting interview simulation for application: {ApplicationId}", applicationId);

        // Ensure preparation exists
        var preparation = await _preparationRepository.GetByApplicationIdAsync(applicationId);
        if (preparation == null)
        {
            // Trigger generation
            await GetQuestions(applicationId);
            preparation = await _preparationRepository.GetByApplicationIdAsync(applicationId);
        }

        return Ok(new { message = "Simulation started", sessionId = Guid.NewGuid(), preparationId = preparation?.Id });
    }

    /// <summary>
    /// Submit answer for analysis
    /// </summary>
    [HttpPost("{applicationId:guid}/answer")]
    public async Task<IActionResult> SubmitAnswer(Guid applicationId, [FromBody] SubmitAnswerDto dto)
    {
        if (string.IsNullOrEmpty(dto.Question) || string.IsNullOrEmpty(dto.Answer))
        {
            return BadRequest(new { message = "Question and answer are required" });
        }

        _logger.LogInformation("Analyzing answer for application: {ApplicationId}", applicationId);

        // Analyze
        var analysis = await _coachService.AnalyzeAnswerAsync(dto.Question, dto.Answer);
        
        // Save result
        var preparation = await _preparationRepository.GetByApplicationIdAsync(applicationId);
        if (preparation != null)
        {
            // Load existing answers
            var answers = string.IsNullOrEmpty(preparation.UserAnswers) 
                ? new List<AnswerWithFeedback>() 
                : JsonSerializer.Deserialize<List<AnswerWithFeedback>>(preparation.UserAnswers) ?? new List<AnswerWithFeedback>();
            
            var newEntry = new AnswerWithFeedback(dto.Question, dto.Answer, analysis);
            answers.Add(newEntry);
            
            preparation.UserAnswers = JsonSerializer.Serialize(answers);
            
            // Should we update Feedback field too? Maybe accumulate overall improvement suggestions?
            // For now, let's keep it simple.
            
            await _preparationRepository.UpdateAsync(preparation);
        }

        var feedback = new AnswerFeedbackDto(
            Question: dto.Question,
            Answer: dto.Answer,
            Feedback: analysis,
            ImprovementSuggestions: new List<string>() // Can extract from analysis text if structured, otherwise empty for now
        );

        return Ok(feedback);
    }

    /// <summary>
    /// Get overall feedback for interview preparation
    /// </summary>
    [HttpGet("{applicationId:guid}/feedback")]
    public async Task<IActionResult> GetFeedback(Guid applicationId)
    {
        _logger.LogInformation("Getting interview feedback for application: {ApplicationId}", applicationId);

        var preparation = await _preparationRepository.GetByApplicationIdAsync(applicationId);
        if (preparation == null)
            return NotFound("Preparation not found");

        var answers = string.IsNullOrEmpty(preparation.UserAnswers) 
            ? new List<AnswerWithFeedback>() 
            : JsonSerializer.Deserialize<List<AnswerWithFeedback>>(preparation.UserAnswers) ?? new List<AnswerWithFeedback>();

        // Generate overall improvement suggestions if we have enough answers (e.g., > 3)
        List<string> suggestions = new();
        if (answers.Count >= 3)
        {
             var answerTexts = answers.Select(a => $"Q: {a.Question}\nA: {a.Answer}").ToList();
             suggestions = await _coachService.GenerateImprovementSuggestionsAsync(answerTexts);
             
             // Save suggestions
             preparation.Feedback = JsonSerializer.Serialize(suggestions);
             await _preparationRepository.UpdateAsync(preparation);
        }
        else if (!string.IsNullOrEmpty(preparation.Feedback))
        {
             suggestions = JsonSerializer.Deserialize<List<string>>(preparation.Feedback) ?? new List<string>();
        }

        return Ok(new 
        { 
            preparation.Id,
            answers,
            overallSuggestions = suggestions 
        });
    }
}
