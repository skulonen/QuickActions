using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Internal.Mapping.TOC;
using ArcGIS.Desktop.Mapping;

namespace QuickActions;

internal class QuickActionsModule : Module
{
	protected override bool Initialize()
	{
		EventManager.RegisterClassHandler(
			typeof(TreeViewItem),
			ContextMenuService.ContextMenuOpeningEvent,
			new ContextMenuEventHandler((object sender, ContextMenuEventArgs args) =>
			{
				var header = ((TreeViewItem)sender).Header;

				if (header is TOCLayerViewModel layerViewModel)
				{
					args.Handled = true;
					QuickActionsPopup.Open(layerViewModel.Layer, layerViewModel.ContextMenu);
				}
				else if (header is TOCStandaloneTableViewModel tableViewModel)
				{
					args.Handled = true;
					QuickActionsPopup.Open(tableViewModel.StandaloneTable, tableViewModel.ContextMenu);
				}
			})
		);

		return base.Initialize();
	}
}
