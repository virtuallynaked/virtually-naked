using System;
using System.Collections.Generic;

public class MenuViewMessageAuthor {
	public MenuViewMessage AuthorMessage(MenuModel model) {
		List<MenuItemViewMessage> itemMessages = new List<MenuItemViewMessage>();
		for (int i = 0; i < model.Items.Count; ++i) {
			var item = model.Items[i];
			bool selected = i == model.SelectedItemIdx;
			bool isEditing = selected && model.IsEditing;

			MenuItemViewMessage itemMessage;
			switch (item) {
				case UpMenuItem upMenuItem:
					itemMessage = MenuItemViewMessage.MakeUpLevelButton(upMenuItem.Label);
					break;

				case SubLevelMenuItem subLevelItem:
					itemMessage = MenuItemViewMessage.MakeSubLevelButton(subLevelItem.Label);
					break;

				case IRangeMenuItem rangeItem:
					itemMessage = MenuItemViewMessage.MakeRange(rangeItem.Label, rangeItem.Min, rangeItem.Max, rangeItem.Value, isEditing);
					break;

				case IToggleMenuItem toggleMenuItem:
					itemMessage = MenuItemViewMessage.MakeToggle(toggleMenuItem.Label, toggleMenuItem.IsSet);
					break;

				case ActionMenuItem actionMenuItem:
					itemMessage = MenuItemViewMessage.MakeActionButton(actionMenuItem.Label);
					break;

				default:
					throw new InvalidOperationException();
			}

			if (selected) {
				itemMessage.Active = true;
			}
			itemMessages.Add(itemMessage);
		}

		return new MenuViewMessage {
			Items = itemMessages,
			Position = model.ScrollPosition
		};
	}
}
