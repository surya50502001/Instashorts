using FluentValidation;
using ScrollGuardian.Application.Common.Interfaces;

namespace ScrollGuardian.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class OnboardingRequestValidator : AbstractValidator<OnboardingRequestDto>
{
    public OnboardingRequestValidator()
    {
        RuleFor(x => x.SelectedCategories)
            .NotEmpty().WithMessage("Please select at least one focus category.");

        RuleFor(x => x.PrimaryGoalText)
            .NotEmpty().WithMessage("Please provide a goal description.")
            .MinimumLength(5).WithMessage("Goal description should be at least 5 characters.")
            .MaximumLength(500).WithMessage("Goal description cannot exceed 500 characters.");

        RuleFor(x => x.EstimatedDailyMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated minutes must be positive.")
            .LessThanOrEqualTo(1440).WithMessage("Estimated minutes cannot exceed 24 hours.");
    }
}

public class CreateUserGoalValidator : AbstractValidator<CreateUserGoalDto>
{
    public CreateUserGoalValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

        RuleFor(x => x.TargetWeeklyHours)
            .GreaterThan(0).WithMessage("Target weekly hours must be greater than 0.")
            .LessThanOrEqualTo(168).WithMessage("Target weekly hours cannot exceed 168.");
    }
}

public class IngestContentEventValidator : AbstractValidator<IngestContentEventDto>
{
    public IngestContentEventValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Content URL is required.")
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out _)).WithMessage("A valid absolute URL is required.");

        RuleFor(x => x.TimeSpentSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("Time spent must be non-negative.");
    }
}

public class SubmitKnowledgeAnswerValidator : AbstractValidator<SubmitKnowledgeAnswerDto>
{
    public SubmitKnowledgeAnswerValidator()
    {
        RuleFor(x => x.CheckId)
            .NotEmpty().WithMessage("Knowledge Check ID is required.");

        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required.");

        RuleFor(x => x.SelectedOptionIndex)
            .GreaterThanOrEqualTo(0).WithMessage("Valid option index must be selected.");
    }
}
