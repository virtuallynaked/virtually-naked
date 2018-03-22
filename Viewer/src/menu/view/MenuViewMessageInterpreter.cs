using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

public class MenuViewMessageInterpreter {
	public const float Width = 1024;
	public const float Height = 1024;

	// Colors convert to RGB at http://colorizer.org/

	private readonly SolidColorBrush DefaultBorderStroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
	private readonly SolidColorBrush ActiveBorderStroke = new SolidColorBrush(Color.FromRgb(0xff, 0x14, 0x93)); // hsl(327.6, 100%, 53.9%)
	private readonly SolidColorBrush EditingBorderStroke = new SolidColorBrush(Color.FromRgb(0xff, 0x8b, 0x59)); // hsl(18, 100%, 67.5%)

	private readonly SolidColorBrush DefaultBackground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)); // hsl(*, 0, 26.7%)
	private readonly SolidColorBrush UpLevelButtonBackground = new SolidColorBrush(Color.FromRgb(0x88, 0x00, 0x4a)); // hsl(327.6, 100%, 26.7%)
	private readonly SolidColorBrush SubLevelButtonBackground = new SolidColorBrush(Color.FromRgb(0x66, 0x22, 0x47)); // hsl(327.6, 50%, 26.7%)
	private readonly SolidColorBrush RangeBackground = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // hsl(*, 0, 0%)
	private readonly SolidColorBrush RangeEditingBackground = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x23)); // hsl(*, 0, 13.6%)
	private readonly SolidColorBrush ToggleSetBackground = new SolidColorBrush(Color.FromRgb(0x33, 0x6b, 0x9a)); // hsl(207.6, 50%, 40.3%)

	private readonly SolidColorBrush RangeForeground = new SolidColorBrush(Color.FromRgb(0x00, 0x4a, 0x88)); // hsl(207.6, 100%, 26.7%)
	private readonly SolidColorBrush RangeEditingForeground = new SolidColorBrush(Color.FromRgb(0x00, 0x6f, 0xce)); // hsl(207.6, 100%, 40.3%)

	private readonly double itemHeight;

	public MenuViewMessageInterpreter() {
		var itemForMeasuring = CreateItem(MenuItemViewMessage.MakeSubLevelButton("foo"));
		itemForMeasuring.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
		itemHeight = itemForMeasuring.DesiredSize.Height;
	}

	private FrameworkElement CreateItem(MenuItemViewMessage message) {
		SolidColorBrush backgroundBrush;
		if (message.Type == MenuItemViewType.UpLevelButton) {
			backgroundBrush = UpLevelButtonBackground;
		} else if (message.Type == MenuItemViewType.SubLevelButton) {
			backgroundBrush = SubLevelButtonBackground;
		} else if (message.Type == MenuItemViewType.Range) {
			if (message.IsEditing) {
				backgroundBrush = RangeEditingBackground;
			} else {
				backgroundBrush = RangeBackground;
			}
		} else if (message.Type == MenuItemViewType.Toggle) {
			if (message.IsSet) {
				backgroundBrush = ToggleSetBackground;
			} else {
				backgroundBrush = DefaultBackground;
			}
		} else {
			backgroundBrush = DefaultBackground;
		}

		var textOverlayGrid = new Grid {
			Background = backgroundBrush
		};
		
		if (message.Type == MenuItemViewType.Range) {
			double min = message.Min;
			double max = message.Max;
			double value = MathExtensions.Clamp(message.Value, min, max);

			double centerLeft, centerRight;
			if (value < 0) {
				centerLeft = value;
				centerRight = Math.Min(0, max);
			} else {
				centerLeft = Math.Max(0, min);
				centerRight = value;
			}
		
			var leftRect = new Rectangle {
			};
			var middleRect = new Rectangle {
				Fill = message.IsEditing ? RangeEditingForeground : RangeForeground
			};
			var rightRect = new Rectangle {
			};

			var rectGrid = new Grid {
				Children = { leftRect, middleRect, rightRect },
			};
			rectGrid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(centerLeft - min, GridUnitType.Star)
			});
			rectGrid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(centerRight - centerLeft + (max - min) * 0.002, GridUnitType.Star)
			});
			rectGrid.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(max - centerRight, GridUnitType.Star)
			});
			Grid.SetColumn(leftRect, 0);
			Grid.SetColumn(middleRect, 1);
			Grid.SetColumn(rightRect, 2);

			textOverlayGrid.Children.Add(rectGrid);
		}
		
		var labelTextBlock = new TextBlock {
			Foreground = Brushes.White,
			Text = message.Label,
			FontSize = 48,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		textOverlayGrid.Children.Add(labelTextBlock);
		
		var border = new Border {
			Child = textOverlayGrid,
			BorderThickness = new Thickness(5),
			CornerRadius = new CornerRadius(5),
			BorderBrush = message.Active ? (message.IsEditing ? EditingBorderStroke : ActiveBorderStroke) : DefaultBorderStroke,
			Margin = new Thickness(2)
		};

		return border;
	}

	public UIElement Interpret(MenuViewMessage state) {
		var canvas = new Canvas {
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		
		if (state == null) {
			return canvas;
		}
		
		double position = state.Position;
		double topOffset = Height / 2 - itemHeight * position;
		
		int firstVisibleChildIdx = IntegerUtils.Clamp(
			(int) Math.Floor(-(itemHeight + topOffset) / itemHeight),
			0, state.Items.Count);
		int lastVisibleChildIdx = IntegerUtils.Clamp(
			(int) Math.Ceiling((Height - topOffset) / itemHeight),
			0, state.Items.Count - 1);
		
		for (int childIdx = firstVisibleChildIdx; childIdx <= lastVisibleChildIdx; ++childIdx) {
			var item = state.Items[childIdx];
			var childElem = CreateItem(item);
			
			canvas.Children.Add(childElem);

			Canvas.SetTop(childElem, topOffset + itemHeight * childIdx);
			childElem.Width = Width;
		}
		
		return canvas;
	}
}
