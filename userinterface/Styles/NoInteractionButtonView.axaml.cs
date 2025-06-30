using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;

namespace Styles
{
    public class NoInteractionButtonView : Button
    {
        static NoInteractionButtonView()
        {
            TemplateProperty.OverrideDefaultValue<NoInteractionButtonView>(CreateTemplate());
        }

        public NoInteractionButtonView()
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        }

        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<NoInteractionButtonView>((button, scope) =>
            {
                var border = new Border
                {
                    Name = "PART_Border",
                    [!Border.BackgroundProperty] = button[!BackgroundProperty],
                    [!Border.BorderBrushProperty] = button[!BorderBrushProperty],
                    [!Border.BorderThicknessProperty] = button[!BorderThicknessProperty],
                    [!Border.CornerRadiusProperty] = button[!CornerRadiusProperty],
                    [!Decorator.PaddingProperty] = button[!PaddingProperty]
                };

                var contentPresenter = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = button[!ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = button[!ContentTemplateProperty],
                    [!ContentPresenter.HorizontalContentAlignmentProperty] = button[!HorizontalContentAlignmentProperty],
                    [!ContentPresenter.VerticalContentAlignmentProperty] = button[!VerticalContentAlignmentProperty],
                };

                contentPresenter.Bind(TextBlock.ForegroundProperty, button.GetObservable(ForegroundProperty));

                border.Child = contentPresenter;
                return border;
            });
        }
    }
}
