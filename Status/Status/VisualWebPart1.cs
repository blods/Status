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

                    dolbysystems[currentsystem].daystatus = new DayStatus[8];
                    for (int x = 0; x < 8; x++)
                    {
                        dolbysystems[currentsystem].daystatus[x] = new DayStatus();
                        dolbysystems[currentsystem].daystatus[x].status = 0;
                    }

                    currentsystem++;    // Move onto the next
                }

            }
        }
    }

    // This class holds the information for one system
    public class DolbySystem
    {
        public string name;             // name of the system
        public string description;      // description
        public double id;               // List ID
        public int sortorder;           // sort order
        public int currentstatus;       // Status now

        public DayStatus[] daystatus;   // Statuses for particular days

        
        
    }

    // Represents the info for one specific day
    public class DayStatus
    {
        public int status;          // Status for this particular time (defcom)
        public string title;        // title for this outage
        public string impacted;     // impacted systems
        public string region;       // region impacted
        public string offices;      // offices impacted
        public DateTime start;      // start time
        public DateTime end;        // end time
        public string details;      // details
        public string trackingref;  // tracking reference

    }
}
