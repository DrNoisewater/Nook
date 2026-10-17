using System.Drawing;
using System.Windows.Forms;
namespace Nook {
internal sealed class TrayColors:ProfessionalColorTable {
    public override Color ToolStripDropDownBackground {get{return Palette.Drawing("#202D27");}}
    public override Color MenuBorder {get{return Palette.Drawing("#536B5D");}}
    public override Color MenuItemBorder {get{return Palette.Drawing("#365044");}}
    public override Color MenuItemSelected {get{return Palette.Drawing("#365044");}}
    public override Color SeparatorDark {get{return Palette.Drawing("#3A4E44");}}
    public override Color SeparatorLight {get{return Palette.Drawing("#3A4E44");}}
}
internal sealed class TrayRenderer:ToolStripProfessionalRenderer {
    public TrayRenderer():base(new TrayColors()){RoundedEdges=false;}
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
        e.TextColor=e.Item.Selected?Palette.Drawing("#CFFFE4"):Palette.Drawing("#EEF5F1");base.OnRenderItemText(e);
    }
}
}
