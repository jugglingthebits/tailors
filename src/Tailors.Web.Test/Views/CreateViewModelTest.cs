using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tailors.Web.Features.Tweed;
using Tailors.Web.Test.TestHelper;
using Xunit;

namespace Tailors.Web.Test.Views;

public class CreateViewModelTest
{
    [Fact]
    public void ValidatesTextRequired()
    {
        CreateTweedViewModel viewModel = new();
        var result = viewModel.Validate();

        Assert.Equal(ModelValidationState.Invalid, result["text"]!.ValidationState);
    }

    [Fact]
    public void ValidatesTextTooLong()
    {
        CreateTweedViewModel viewModel = new()
        {
            Text = new string('a', 281)
        };

        var result = viewModel.Validate();

        Assert.Equal(ModelValidationState.Invalid, result["text"]!.ValidationState);
    }
}