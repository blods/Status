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
                
                // Populate the array with the list of systems
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
                        dolbysystems[currentsystem].daystatus[x] = new DayStatus(); // Create instances of each day
                        dolbysystems[currentsystem].daystatus[x].status = 0;        // Set them all to ticks

                        // Now set the periodStart and periodEnd for each day (x days will be subtracted each time)
                        if (x == 0) // If this is the first instance then start/end is right now
                        {
                            dolbysystems[currentsystem].daystatus[x].periodStart = DateTime.Now;
                            dolbysystems[currentsystem].daystatus[x].periodEnd = DateTime.Now;
                        }
                        else
                        {   // Looks complicated but this is just working out a clean start and end fo each day so 12AM to 11:59:59PM 
                            dolbysystems[currentsystem].daystatus[x].periodStart = DateTime.Today.Date.AddDays(-(x-1));
                            dolbysystems[currentsystem].daystatus[x].periodEnd = DateTime.Today.Date.AddDays(-(x-1)).AddSeconds(86399);
                        }
                    }

                    currentsystem++;    // Move onto the next system
                }


                // Systems are all populated and the 8 status days populated with defaults
                
                // Time to look for outages to reflect in the data
                // Return all outages from up to 10 days ago sorted by defcom (low to high)
                var query = from outages in context.Outages
                            where outages.Start >= DateTime.Now.AddDays(-10)
                            orderby outages.Defcom descending
                            select outages;

                // Loop around each of the outages in the last 10 days
                foreach (var outages in query)
                {
                    // For each outage we need to first match the system 

                    foreach (DolbySystem s in dolbysystems)
                    {
                        if (s.name == outages.System.Title)
                        {
                            //and then match the days
                            for (int x = 0; x < 8; x++)
                            {
                                if (outages.Start < s.daystatus[x].periodStart && outages.End < s.daystatus[x].periodStart)
                                {
                                    // this thing started and finished before this time
                                }
                                else if (outages.Start > s.daystatus[x].periodEnd && outages.End > s.daystatus[x].periodEnd)
                                {
                                    // this thing started and ended after this time
                                }
                                else
                                {
                                    // FOUND ONE
                                    s.daystatus[x].status = Convert.ToInt32(outages.Defcom.ToString().Substring(outages.Defcom.ToString().Length -1));
                                    s.daystatus[x].title = outages.Title;
                                    s.daystatus[x].details = outages.Details;

                                    // Handle an end date of null which means not set and so ongoing outage
                                    if (outages.End == null) { s.daystatus[x].end = DateTime.MaxValue;}
                                    else { s.daystatus[x].end = outages.End.Value; }
                                   
                                    s.daystatus[x].impacted = outages.ImpactedSystems;
                                    s.daystatus[x].offices = outages.Offices;
                                    s.daystatus[x].region = outages.Region.ToString();
                                    s.daystatus[x].start = outages.Start.Value;
                                    s.daystatus[x].trackingref = outages.TrackingRef;
                                }
                            }
                        }
                    }

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

    // This class represents the info for one specific day
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
        public DateTime periodStart;// represents the start of this day
        public DateTime periodEnd;  // represents the end of this day

    }
}
