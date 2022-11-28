using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using ArcGIS.Desktop.Mapping;

namespace QuickActions;

public partial class QuickActionsPopup : Popup
{
	public QuickActionsPopup()
	{
		InitializeComponent();
	}

	public static void Open(MapMember mapMember, ContextMenu originalMenu)
	{
		var popup = new QuickActionsPopup();
		popup.QuickActionsControl.MapMember = mapMember;
		popup.QuickActionsControl.OriginalMenu = originalMenu;
		popup.IsOpen = true;
	}
}
