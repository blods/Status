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

           // Communicate with SPMetal class
            using (StatusDataContext context = new StatusDataContext(SPContext.Current.Web.Url))
            {
                var result = context.Systems;

                int systemcount = result.Count(); // Number of systems in list

                // Create an array of systems to store the info
                DolbySystem[] dolbysystems = new DolbySystem[systemcount];

                int currentsystem = 0;  // start at 0 and loop through each
                // Populate the array
                foreach (SystemsItem system in result)
                {
                    dolbysystems[currentsystem] = new DolbySystem();
                    dolbysystems[currentsystem].name = system.Title;
                    dolbysystems[currentsystem].description = system.Description;
                    dolbysystems[currentsystem].id = (int)system.Id;
                    dolbysystems[currentsystem].sortorder = (int)system.SortOrder;
                    dolbysystems[currentsystem].currentstatus = 0;
              
                    
                }



            }
        }
    }

    // This class holds the information for one system
    public class DolbySystem
    {
        public string name;         // name of the system
        public string description;  // description
        public double id;           // List ID
        public int sortorder;       // sort order
        public int currentstatus;   // Status now
    }

    // Represents the info for one specific day
    public class DayStatus
    {



    }
}
