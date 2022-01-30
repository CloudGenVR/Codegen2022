using FluentValidation;
using PhotoGallery.Models;

namespace PhotoGallery.Validations;

public class NewCommentValidator : AbstractValidator<NewComment>
{
    public NewCommentValidator()
    {
        RuleFor(c => c.Text).NotEmpty().WithMessage("You must specifiy a text for your comment");
    }
}
