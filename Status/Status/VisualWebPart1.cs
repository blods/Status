using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Linq;
using System.Globalization;
using System.Text;


namespace Status.VisualWebPart1
{
    [ToolboxItemAttribute(false)]
    public class VisualWebPart1 : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/Status/Status/VisualWebPart1UserControl.ascx";
        public DolbySystem[] dolbysystems;
        public string siteURL;

        // Strings to hold the image URLs
        public string tickImgURL;
        public string stopImgURL;
        public string coneImgURL;
        public string warnImgURL;
        
        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            Controls.Add(control);

           // Communicate with SPMetal class
            using (StatusDataContext context = new StatusDataContext(SPContext.Current.Web.Url))
            {
                var result = context.Systems.OrderBy(x => x.SortOrder); // Returns the systems sorted by sortorder
                
                siteURL = context.Web;                                  // Get the URL of the website (for adding references)
                int systemcount = result.Count();                       // Number of systems in list

                // Create the URLs to the images
                tickImgURL = siteURL + "/icons/tick.png";
                stopImgURL = siteURL + "/icons/stop.png";
                coneImgURL = siteURL + "/icons/cone.png";
                warnImgURL = siteURL + "/icons/warn.png";

                // Create an array of systems to store the info
                dolbysystems = new DolbySystem[systemcount];

                int currentsystem = 0;  // start at 0 and loop through each
                
                // Populate the array with the list of systems
                foreach (SystemsItem system in result)
                {
                    dolbysystems[currentsystem] = new DolbySystem();

                    dolbysystems[currentsystem].description = "";
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
                            dolbysystems[currentsystem].daystatus[x].daytext = "Now";
                        }
                        else
                        {   // Looks complicated but this is just working out a clean start and end fo each day so 12AM to 11:59:59PM 
                            dolbysystems[currentsystem].daystatus[x].periodStart = DateTime.Today.Date.AddDays(-(x-1));
                            dolbysystems[currentsystem].daystatus[x].periodEnd = DateTime.Today.Date.AddDays(-(x-1)).AddSeconds(86399);
                            dolbysystems[currentsystem].daystatus[x].daytext = DateTime.Today.Date.AddDays(-(x - 1)).ToString("ddd", CultureInfo.CreateSpecificCulture("en-US")) + "<br />" + DateTime.Today.Date.AddDays(-(x - 1)).ToString("dd", CultureInfo.CreateSpecificCulture("en-US"));
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

                // Loop around each of the outages in the last 10 days and map to dolbysystems
                foreach (var outages in query)
                {
                    // For each outage we need to first match the system 

                    foreach (DolbySystem s in dolbysystems)
                    {
                        if (s.name == outages.System.Title)
                        {
                            
                            // Check each of the last 7 days 
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
                                else if (outages.Start > s.daystatus[x].periodEnd)
                                {
                                    // This didnt start until after this date

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

                int y = 1;

            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            // Do any necessary prerender stuff here

            //Add qtip2 CSS
            writer.WriteBeginTag("link");
            writer.WriteAttribute("type", "text/css");
            writer.WriteAttribute("rel", "stylesheet");
            writer.WriteAttribute("href", siteURL + "/scripts/jquery.qtip.min.css");
            writer.Write(HtmlTextWriter.SlashChar);
            writer.Write(HtmlTextWriter.TagRightChar);

            // Add jquery 1.7.2 from google
            writer.WriteBeginTag("script");
            writer.WriteAttribute("type", "text/javascript");
            writer.WriteAttribute("src", "//ajax.googleapis.com/ajax/libs/jquery/1.7.2/jquery.min.js");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.WriteEndTag("script");


            // Add qtip javascript
            writer.WriteBeginTag("script");
            writer.WriteAttribute("type", "text/javascript");
            writer.WriteAttribute("src", siteURL + "/scripts/jquery.qtip.min.js");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.WriteEndTag("script");

            // Add CSS to hide tooltips and add padding
            writer.Write("<style>  .hidden {display:none;} </style>");

            // Start the table
            Table infoTable = new Table();
            infoTable.Attributes.Add("border", "1px");
            

            
  
            

            //Render the table
            infoTable.RenderControl(writer);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<table align=""center"" style=""border: 1px solid #D4D0C8; width: 100%"">");
            sb.AppendLine("<tbody>");

            // Start a row
            sb.AppendLine("<tr>");

            // Heading Row 
            sb.AppendLine(@"<td style=""text-align: center; background-color: #909090""></td>"); // FIrst column for the check boxes
            sb.AppendLine(@"<td style=""font-size: 12px; text-align: center; background-color: #909090; color: #FFFFFF""><strong>System</strong></td>");

            for (int x = 0; x < 8; x++)
            {
                sb.AppendLine(@"<td style=""font-size: 12px; text-align: center; background-color: #909090; color: #FFFFFF""><strong>" + dolbysystems[0].daystatus[x].daytext + "</strong></td>");                    
            }

            // End a row
            sb.AppendLine("</tr>");

            int alternaterows = 0;              // Used to keep track and flip colors 
            
            string alternateshade = "#DBE8EA";  // This is for the shading of the first 3 columns
            string alternateshade2 = "#F5FAFA"; // This is for the shading for the columns 4 and up - but is either on or off

            // Now do the other rows
            foreach (DolbySystem s in dolbysystems)
            {
                sb.AppendLine("<tr>");

                // Flip between the two colors for the first 3 columns
                if (alternaterows == 0) {
                    alternateshade = "#DBE8EA";
                    alternaterows = 1;
                } else {
                    alternateshade = "#F5FAFA";
                    alternaterows = 0;
                }


                // Do the System title
                sb.AppendLine(@"<td valign=""middle"" style=""text-align: center; background-color: " + alternateshade + @";""><input name=""Checkbox1"" type=""checkbox"" /></td><td style=""background-color: " + alternateshade + @"""><strong>" + s.name + @"</strong></td>");

                for (int x = 0; x < 8; x++ )
                {
                    sb.Append(@"<td style=""text-align: center;");

                    //If this is 0 then we need to pic from the alternaterows colors
                    if (x == 0)
                    {
                        sb.Append(@" background-color: " + alternateshade + @";"">");
                    }
                    else
                    {
                        // Only add the shading for column 4 and up every other cycle - otherwise its clear
                        if (alternaterows == 1) {
                            sb.Append(@" background-color: " + alternateshade2 + @";"">");
                        }
                        else {  // No shading here
                            sb.Append(@""">");
                        }
                    }
                    
                    // Now we can add the symbol
                    if (s.daystatus[x].status == 0)
                    {
                        sb.Append(@"<img src=""" + tickImgURL + @"""/></td>");
                    }
                    else
                    {
                        // Needs a tooltip
                        sb.Append(@"<div class=""hasTooltip""><a href=""#tip"">");

                        // Now add the image URL
                        if (s.daystatus[x].status == 1)
                        {
                            sb.Append(@"<img src=""" + stopImgURL + @""" border=""0""> ");
                        }
                        if (s.daystatus[x].status == 2)
                        {
                            sb.Append(@"<img src=""" + warnImgURL + @""" border=""0""> ");
                        }
                        if (s.daystatus[x].status == 4)
                        {
                            sb.Append(@"<img src=""" + coneImgURL + @""" border=""0""> ");
                        }

                        
                        sb.Append(@"</a></div>");   // Closes out the anchor and the div tag for the tooltip

                        // Now add the tooltip text
                        sb.Append(@"<div class=""hidden"">"); // Which should initially be hidden
                        
                        // Build the tooptip here
                        sb.Append(@"<b>" + s.daystatus[x].title + @"</b><BR>");
                        sb.Append(@"Start (PST): " + String.Format("{0:HH:mm}",s.daystatus[x].start) + @"<BR>");
                      

                        // Only show the end time if its not an ongoing outage
                        if (s.daystatus[x].end < DateTime.Now)
                        {
                            sb.Append(@"End   (PST): " + String.Format("{0:HH:mm}", s.daystatus[x].end) + @"<BR>");
                        }
                        else { sb.Append(@"ONGOING<BR>");
                        }

                        sb.Append(@"TZ: " + s.daystatus[x].start.ToString("zzz") + @"<BR>");
                        sb.Append(@"<BR>"); // Put a extrabreak before the next section
                        if (s.daystatus[x].impacted != null) sb.Append(@"Impacted: " + s.daystatus[x].impacted + @"<BR>");
                        if (s.daystatus[x].offices != null) sb.Append(@"Office:" + s.daystatus[x].offices + @"<BR>");
                        if (s.daystatus[x].region != null) sb.Append(@"Region:" + s.daystatus[x].region + @"<BR><BR>");

                        
                        sb.Append(s.daystatus[x].details);
                        

                        sb.Append(@"</div>");


                        // Now close out the cell
                        sb.Append(@"</td>");

                    }

                    
                }


                sb.AppendLine("</tr>");
            }



            // Close the table off here
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            writer.Write(sb);

            // Wrote out the little bit of javascript
            
            // Add jquery 1.7.2 from google
            writer.WriteBeginTag("script");
            writer.WriteAttribute("language", "javascript");
            writer.WriteAttribute("type", "text/javascript");
            writer.Write(HtmlTextWriter.TagRightChar);

            // Put the qtip javascript code in the page
            // Thinks we want to be tips have to reside in a .hasTooltip class
            // The next class to it needs to be called hidden and contains the text
            writer.Write(@"$('.hasTooltip').each(function() 
                { 
                    $(this).qtip({
                        content: {
                            text: $(this).next('div'), 
			                title: 'Status'
                        }
                    });
                });
            ");
            writer.WriteEndTag("script");
            
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
        public string daytext;      // day text

    }
}


