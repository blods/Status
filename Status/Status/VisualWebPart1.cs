using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Linq;


namespace Status.VisualWebPart1
{
    [ToolboxItemAttribute(false)]
    public class VisualWebPart1 : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/Status/Status/VisualWebPart1UserControl.ascx";

        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            Controls.Add(control);

           // Testing SPMetal
            using (StatusDataContext context = new StatusDataContext(SPContext.Current.Web.Url))
            {
                var result = context.Systems;

                int i = result.Count();

               
                foreach (SystemsItem system in result)
                {
                    var test="";
                    test = system.Title;
                    
                }



            }
        }
    }

    public class 
}
