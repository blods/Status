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
        public Classifications[] classifications;
        public DolbySystem[] dolbysystems;
        public string siteURL;

        // Strings to hold the image URLs
        public string tickImgURL;
        public string stopImgURL;
        public string coneImgURL;
        public string warnImgURL;
        public string trianglerightURL;
        public string triangledownURL;
        public string historyexpand;
        public string historycollapse;
        public string bigtriangledown;
        public string bigtriangleright;


        public string subscribedTo;         // What the current user has subscribed to

        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            Controls.Add(control);


            
            
           // Communicate with SPMetal class
            using (StatusDataContext context = new StatusDataContext(SPContext.Current.Web.Url))
            {

                // Get the current user and populate what they've subscribed to in subscribedTo
                SPUser cuser = SPControl.GetContextWeb(Context).CurrentUser; // This is the user including DOLBYNET\
                
                // Find the user in the list (after extracting the name only rs for example
                var userquery = from subscriptions in context.Subscriptions
                            where subscriptions.Title == cuser.ToString().Substring(cuser.ToString().IndexOf('\\')+1)
                            select subscriptions;
                foreach (var subscriptions in userquery)
                {
                    subscribedTo = subscriptions.SubscribedTo;
                }
                if (subscribedTo == null) subscribedTo = "";  // Make it an empty string if no value

                // Get all of the systems sorted by sort order into result
                var result = context.Systems.OrderBy(x => x.SortOrder); // Returns the systems sorted by sortorder

                // Get all of the classifications sorted by sort order into result
                var classresult = context.Classification.OrderBy(x => x.SortOrder);

                // Get the sites URL so we can safely reference the jquery libraries                
                siteURL = context.Web;
                
                // Count number of systems and classifications  
                int systemcount = result.Count();
                int classificationcount = classresult.Count();

                // Create the URLs to the images
                tickImgURL = siteURL + "/icons/tick.png";
                stopImgURL = siteURL + "/icons/stop.png";
                coneImgURL = siteURL + "/icons/cone.png";
                warnImgURL = siteURL + "/icons/warn.png";

                triangledownURL = siteURL + "/icons/greentriangledown.png";
                trianglerightURL = siteURL + "/icons/greentriangleright.png";
                historycollapse = siteURL + "/icons/historycollapse.png";
                historyexpand = siteURL + "/icons/historyexpand.png";
                bigtriangledown = siteURL + "/icons/bigtriangledown.jpg";
                bigtriangleright = siteURL + "/icons/bigtriangleright.jpg";

                // Create an array of systems & classifications
                classifications = new Classifications[classificationcount];
                dolbysystems = new DolbySystem[systemcount];

                // Populate the classifications array
                int currentclassification = 0;
                foreach (ClassificationItem classification in classresult)
                {
                    classifications[currentclassification] = new Classifications(); // Create a new classification for the array

                    classifications[currentclassification].title = classification.Title;
                    classifications[currentclassification].description = "";    // In case its empty
                    classifications[currentclassification].description = classification.Description;

                    currentclassification++;    // Go on to the next item
                }

                // Populate the array with the list of systems
                int currentsystem = 0;  // start at 0 and loop through each
                foreach (SystemsItem system in result)
                {
                    dolbysystems[currentsystem] = new DolbySystem();

                    dolbysystems[currentsystem].description = "";
                    dolbysystems[currentsystem].name = system.Title;
                    dolbysystems[currentsystem].classification = system.Classification.Title;
                    dolbysystems[currentsystem].description = system.Description;
                    dolbysystems[currentsystem].id = (int)system.Id;
                    dolbysystems[currentsystem].sortorder = (int)system.SortOrder;
                    dolbysystems[currentsystem].trackID = system.TrackID;
                    dolbysystems[currentsystem].currentstatus = 0;

                    if (subscribedTo.Contains(@";" + system.TrackID + @";"))
                    {
                        dolbysystems[currentsystem].subscribed = 1;
                    }
                    else
                    {
                        dolbysystems[currentsystem].subscribed = 0;

                    }


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
                            dolbysystems[currentsystem].daystatus[x].daytext = "Current<BR>Status";
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

           
                // Time to look for outages to reflect in the data - Return all outages from up to 10 days ago sorted by defcon (low to high)
                var query = from outages in context.Outages
                            where outages.Start >= DateTime.Now.AddDays(-10)
                            orderby outages.Defcon descending
                            select outages;

                // Loop around each of the outages in the last 10 days and map to dolbysystems
                foreach (var outages in query)
                {
                    // For each outage we need to first match the system 
                    
                    foreach (DolbySystem s in dolbysystems)
                    {
                        int match = 0;
                        foreach (var sys in outages.System)
                        {
                            if (sys.Title == s.name) match=1;

                        }

                        if (match==1)
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
                                    s.daystatus[x].status = Convert.ToInt32(outages.Defcon.ToString().Substring(outages.Defcon.ToString().Length -1));
                                    s.daystatus[x].title = outages.Title;
                                    s.daystatus[x].details = outages.Details;

                                    // Handle an end date of null which means not set and so ongoing outage
                                    if (outages.End == null) { s.daystatus[x].end = DateTime.MaxValue;}
                                    else { s.daystatus[x].end = outages.End.Value; }

                                    s.daystatus[x].scope = outages.ScopeOfImpact;
                                    s.daystatus[x].useraction = outages.UserActionRequired;
                                    s.daystatus[x].start = outages.Start.Value;
                                    s.daystatus[x].update = outages.Update;
                                    s.daystatus[x].trackingref = outages.TrackingRef;

                                    // Needs a tooltip
                                    s.daystatus[x].hover = (@"<div class=""hasTooltip""><a href=""#tip"">");

                                    // Now add the image URL
                                    if (s.daystatus[x].status == 1)
                                    {
                                        s.daystatus[x].hover += (@"<img src=""" + stopImgURL + @""" border=""0""> ");
                                    }
                                    if (s.daystatus[x].status == 2)
                                    {
                                        s.daystatus[x].hover +=  (@"<img src=""" + warnImgURL + @""" border=""0""> ");
                                    }
                                    if (s.daystatus[x].status == 3)
                                    {
                                        s.daystatus[x].hover +=  (@"<img src=""" + coneImgURL + @""" border=""0""> ");
                                    }

                                    s.daystatus[x].hover += (@"</a></div>");   // Closes out the anchor and the div tag for the tooltip

                                    // Now add the tooltip text
                                    s.daystatus[x].hover += (@"<div class=""hidden"">"); // Which should initially be hidden

                                    // Build the tooptip here
                                    s.daystatus[x].hover += (@"<b>" + s.daystatus[x].title + @"</b><BR><BR>");

                                    // Note: Even though time is correctly adjusted for users regional settings - the UTC always shows -7
                                    // So the below gets the users time offset to show the correct UTC

                                    var user = SPContext.Current.Web.CurrentUser;
                                    string userstz;
                                    if (user.RegionalSettings != null)
                                    {
                                        userstz = user.RegionalSettings.TimeZone.Description;
                                    }
                                    else
                                    {
                                        userstz = SPContext.Current.Web.RegionalSettings.TimeZone.Description;
                                    }

                                    s.daystatus[x].hover += (@"<span style=""color:cadetblue"">Start: <b>" + String.Format("{0:HH:mm}", s.daystatus[x].start) + @"</b>  ");
                                    s.daystatus[x].hover += (@"End: <b>");

                                    // Only show the end time if its not an ongoing outage
                                    if (s.daystatus[x].end < DateTime.Now)
                                    {
                                        s.daystatus[x].hover += (String.Format("{0:HH:mm}", s.daystatus[x].end) + @"</b><BR></span>");
                                    }
                                    else
                                    {
                                        s.daystatus[x].hover += (@"ONGOING</b><BR></span>");
                                    }
                                    s.daystatus[x].hover += (@"<font color=#6D6D6D>" + userstz + @"<BR>");

                                    s.daystatus[x].hover += (@"<BR>"); // Put a extrabreak before the next section



                                    if (s.daystatus[x].update != null) s.daystatus[x].hover += (@"<span style=""color:CornflowerBlue""><b>Update</b><BR>" + s.daystatus[x].update + @"<BR><BR></span>");
                                    if (s.daystatus[x].scope != null) s.daystatus[x].hover += (@"<span style=""color:DarkBlue""><b>Scope</b><BR>" + s.daystatus[x].scope + @"<BR><BR></span>");
                                    if (s.daystatus[x].useraction != null) s.daystatus[x].hover += (@"<span style=""color:DodgerBlue""><b>Action</b><BR>" + s.daystatus[x].useraction + @"<BR></span>");



                                    s.daystatus[x].hover += (@"<i>" + s.daystatus[x].details + @"</i>");
                                    if (s.daystatus[x].trackingref != null) s.daystatus[x].hover += (@"<BR>Tracked: <b>" + s.daystatus[x].trackingref + @"</b>");

                                    s.daystatus[x].hover += (@"</div>");
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

            // Add SPServices javascript
            writer.WriteBeginTag("script");
            writer.WriteAttribute("type", "text/javascript");
            writer.WriteAttribute("src", siteURL + "/scripts/jquery.SPServices-2014.01.min.js");
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

            // Add some buttons temporarily
            
            sb.Append(@"<button class=""btnshowhistory"" type=""button"" style=""font-face:'Arial';font-weight:bold;font-size:1em;color:#909090;background-color:#FFFFFF;border:1pt solid white"" value=""Show History"" id=""btnShowHistory""><img src=""" + historyexpand + @""" width=""20"" height=""20"" alt="""" align=""right""/>Show</button>");
            sb.Append(@"<button class=""btnshowhistory"" type=""button"" style=""font-face:'Arial';font-weight:bold;font-size:1em;color:#909090;background-color:#FFFFFF;border:1pt solid white"" value=""Show History"" id=""btnHideHistory""><img src=""" + historycollapse + @""" width=""20"" height=""20"" alt="""" align=""right""/>Hide</button>");

            sb.Append(@"<button class=""btnExpandAll"" type=""button"" style=""font-face:'Arial';font-weight:bold;font-size:1em;color:#909090;background-color:#FFFFFF;border:1pt solid white"" value=""Expand"" id=""btnExpandAll""><img src=""" + bigtriangledown + @""" width=""15"" height=""15"" alt="""" align=""right""/>Expand</button>");
            sb.Append(@"<button class=""btnCollapseAll"" type=""button"" style=""font-face:'Arial';font-weight:bold;font-size:1em;color:#909090;background-color:#FFFFFF;border:1pt solid white"" value=""Collapse"" id=""btnCollapseAll""><img src=""" + bigtriangleright + @""" width=""15"" height=""15"" alt="""" align=""right""/>Collapse</button>");
            
            
            sb.AppendLine(@"<BR><table class=""detail"" align=""left"" style=""border: 1px solid #D4D0C8"">");
            sb.AppendLine("<tbody>");

            // Start a row
            sb.AppendLine(@"<tr class=""parent"">");

            // Heading Row 
            sb.AppendLine(@"<td style=""text-align: center; background-color: #909090; color: #FFFFFF""><strong>System</strong></td>"); // First column 

            for (int x = 7; x >= 0; x--)
            {
                sb.AppendLine(@"<td ");
                
                if (x>0) {sb.Append(@"class=""history"" ");}    // if this isnt now then assign the class history
                sb.Append(@"style=""font-size: 12px; text-align: center; background-color: #909090; color: #FFFFFF""><strong>" + dolbysystems[0].daystatus[x].daytext + "</strong>");
                sb.Append(@"</td>");                    
            }

            // End a row
            sb.AppendLine("</tr>");



            int alternaterows = 0;              // Used to keep track and flip colors on systems
            int alternateclass = 1;             // Used to keep track and flip colors on classes

            string alternateshade = "#FFFFCC";  // This is for the shading of the first 3 columns
            string alternateshade2 = "#F2FCCA"; // This is for the shading for the columns 4 and up - but is either on or off
            string alternatecshade = "#DBE8EA";
            string alternatecshade2 = "#F5FAFA";

            // Note first columns alternate between #DBE8EA and 
            // Rest of the columns alternate between #F5FAFA and #FFFFFF (or off)

            foreach (Classifications c in classifications)
            {
                sb.AppendLine(@"<tr class=""parent"">");

                if (alternateclass == 0)
                {
                    alternatecshade = "#DBE8EA";
                    alternateclass = 1;
                }
                else
                {
                    alternatecshade = "#F5FAFA";
                    alternateclass = 0;
                }
                
                sb.AppendLine(@"<td width=""280px"" title=""" + c.description + @""" style=""cursor:context-menu; text-align: right; background-color: " + alternatecshade + @"; color: #575757""><strong>" + c.title + @" </strong><img src=" + trianglerightURL + @" title=""Toggle expand or collapse"" id=""triangle""> </img></td>");


                // Now we're doing the 8 days of classification
                for (int x = 7; x >= 0; x--)
                {
                    sb.Append(@"<td ");
                    if (x>0) sb.Append(@"class=""history"" ");

                    sb.Append(@"width=""40px"" style=""text-align: center;");

                    //If this is 0 then we need to pic from the alternaterows colors
                    if (x == 0)
                    {
                        sb.Append(@" background-color: " + alternatecshade + @";"">");
                    }
                    else
                    {
                        // Only add the shading for column 4 and up every other cycle - otherwise its clear
                        if (alternaterows == 1)
                        {
                            sb.Append(@" background-color: " + alternatecshade2 + @";"">");
                        }
                        else
                        {  // No shading here
                            sb.Append(@""">");
                        }
                     }

                    // Work out the rolled up status for this classification
                    int classstatus = 0;   // Default to checked
                    string classhover = "";
                    foreach (DolbySystem s in dolbysystems)
                    {
                        if (s.classification == c.title)   
                        {
                            if (s.daystatus[x].status > 0)  // found a matching status
                            {
                                if (classstatus != 0) { // classstatus was already set to something for this period
                                    // If the new outage is a lower number than classstatus then its a higher priority
                                    if (s.daystatus[x].status < classstatus)
                                    {
                                        classstatus = s.daystatus[x].status;
                                        classhover = s.daystatus[x].hover;
                                    }
                                    // and modify the text to say multiple systems impacted please expand 
                                    classhover = (@"<div class=""hasTooltip""><a href=""#tip"">");
                                    // Now add the image URL
                                    if (classstatus == 1)
                                    {
                                        classhover += (@"<img src=""" + stopImgURL + @""" border=""0""> ");
                                    }
                                    if (classstatus == 2)
                                    {
                                        classhover += (@"<img src=""" + warnImgURL + @""" border=""0""> ");
                                    }
                                    if (classstatus == 3)
                                    {
                                        classhover += (@"<img src=""" + coneImgURL + @""" border=""0""> ");
                                    }
                                    classhover += @"</a></div>";
                                    classhover += (@"<div class=""hidden"">"); 

                                    // Build the tooptip here
                                    classhover += (@"<b>Multiple Systems Impacted</b><BR><BR>Click to expand and view details</div>");

                                }
                                else
                                {
                                    classstatus = s.daystatus[x].status;
                                    classhover = s.daystatus[x].hover;
                                }
                                
                            }
                        }
                    }
                    
                    
                     // Now we can add the symbol
                     if (classstatus == 0) sb.Append(@"<img src=""" + tickImgURL + @""" border=""0""></td>");
                     else sb.Append(classhover + @"</td>");
             
                      
                        
                 }

                sb.AppendLine("</tr>");

                int altsyscolor = 1;
                // Now loop through the systems
                foreach (DolbySystem s in dolbysystems)
                {
                    if (s.classification == c.title)
                    {
                        sb.AppendLine(@"<tr class=""child"">");

                        // Flip between the two colors for the first 3 columns
                        if (altsyscolor == 0)
                        {
                            alternateshade = "#FFFFCC";
                            altsyscolor = 1;
                        }
                        else
                        {
                            alternateshade = "#F2FCCA";
                            altsyscolor = 0;
                        }
                        
                        // Do the Check box and System title
                       
                        sb.AppendLine(@"<td title=""" + s.description + @""" style=""cursor:default; text-align: right; background-color: " + alternateshade + @""">" + s.name + @"<input name=""Checkbox1"" class=""thecheckboxes"" title=""Select to receive E-Mail status updates for this system"" ID=""" + s.trackID + @""" ");
                        
                        if (s.subscribed == 1) { sb.AppendLine(@" checked "); }
                        sb.AppendLine(@" type=""checkbox""  onclick=""handlechange(this);"" /></td>");


                        for (int x = 7; x >= 0; x--)
                        {

 
                            sb.Append(@"<td ");
                            if (x > 0) sb.Append(@"class=""history"" ");
                            sb.Append(@"style=""text-align: center;");

                            //If this is 0 then we need to pic from the alternaterows colors
                            if (x == 0)
                            {
                                sb.Append(@" background-color: " + alternateshade + @";"">");
                            }
                            else
                            {
                                // Only add the shading for column 4 and up every other cycle - otherwise its clear
                                if (altsyscolor == 1)
                                {
                                    sb.Append(@" background-color: " + alternateshade2 + @";"">");
                                }
                                else
                                {  // No shading here
                                    sb.Append(@" background-color:#FAFEEA;"">");
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
                                sb.Append(s.daystatus[x].hover);


                                // Now close out the cell
                                sb.Append(@"</td>");

                            }
                        }
                        sb.AppendLine("</tr>");
                    }
                } // Looping systems here
            } // Looping classification here



            // Close the table off here
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            writer.Write(sb);

            // Wrote out the little bit of javascript
            
            
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

            // Handles the collapsing
            writer.WriteBeginTag("script");
            writer.WriteAttribute("language", "javascript");
            writer.WriteAttribute("type", "text/javascript");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.Write(@"var history = 0;");  // Is 0 when history is collapsed and 1 when expanded
            writer.Write(@"$(document).ready(function() {
            
            $('table.detail').each(function() {

                var $table = $(this);
                $table.find('.parent').click(function() {
                    //$childRows.hide();
                    
                    // We need some condition here to find out if we're on a collapsed or expanded view.

                    $(this).nextUntil('.parent').toggle();
                    
                    // This switches the green triangles
                    if (($('#triangle',this).attr(""src"")) == """ + trianglerightURL + @""")
                    {
                        $('#triangle', this).attr(""src"", """ + triangledownURL + @""");
                    }
                    else {
                         $('#triangle', this).attr(""src"", """ + trianglerightURL + @""");
                    }

                });

                var $childRows = $table.find('tbody tr').not ('.parent').hide();

                $('#btnCollapseAll').click(function() {
                    $childRows.hide();
                    $('img').each(function(i) {
                        if (this.src == """ + triangledownURL + @""") {
                            this.src = """ + trianglerightURL + @""";
                        }
                    });
                });

                $('#btnExpandAll').click(function() {
                    $childRows.show();
                    $('img').each(function(i) {
                        if (this.src == """ + trianglerightURL + @""") {
                            this.src = """ + triangledownURL + @""";
                        }
                    });
                  
                    

                });


                });
    

                $("".history"").hide();
                $('table.detail').attr('width','330px');

                
            });
            ");

            writer.WriteEndTag("script");


            // Called on Hide History
            writer.WriteBeginTag("script");
            writer.WriteAttribute("language", "javascript");
            writer.WriteAttribute("type", "text/javascript");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.Write(
                @"$(document).ready(function() {
                $('#btnHideHistory').click(function() {
                $("".history"").hide();
                history=0;
                $('table.detail').attr('width','330px');
                });
                });");
            writer.WriteEndTag("script");

            // Called on Show History
            writer.WriteBeginTag("script");
            writer.WriteAttribute("language", "javascript");
            writer.WriteAttribute("type", "text/javascript");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.Write(
                @"$(document).ready(function() {
                $('#btnShowHistory').click(function() {
                $("".history"").show();
                history=1;
                $('table.detail').attr('width','100%');                
                });
                });");
               
            writer.WriteEndTag("script");





            // Called when a check box is clicked
            writer.WriteBeginTag("script");
            writer.WriteAttribute("language", "javascript");
            writer.WriteAttribute("type", "text/javascript");
            writer.Write(HtmlTextWriter.TagRightChar);
            writer.Write(
                @"function handlechange(cb)
                {
                    var subscriptions = """";
                    var newsubscriptions = """"; 
                    var existingid=0;
                    

 
                    // Get current user - but remove the DOLBYNET bit (note two backslashes = 1
                    var username = $().SPServices.SPGetCurrentUser({fieldName: ""Name"", debug: false});
                    var username = username.split(""\\"").pop();
                    var userexists = false;
                    
                   
                    $().SPServices({
                        operation: ""GetListItems"", 
                        async: false, 
                        listName: ""Subscriptions"", 
                        CAMLViewFields: ""<ViewFields><FieldRef Name='Title' /><FieldRef Name='SubscribedTo' /></ViewFields>"",
                        CAMLQuery: ""<Query><Where><Eq><FieldRef Name='Title' /><Value Type='Text'>"" + username + ""</Value></Eq></Where></Query>"",
                        completefunc: function (xData, Status) {
                            // alert(xData.responseText);
                            $(xData.responseXML).SPFilterNode(""z:row"").each(function() {
                                subscriptions = ($(this).attr('ows_SubscribedTo'));
                                existingid = ($(this).attr('ows_ID'));
                            });
                        }
                    });
                    
                    // Loop around all the elements of my checkbox class and if checked add to the string
                    var myElements = $("".thecheckboxes"");
                    
                    for (var i=0;i<myElements.length;i++) {
                        if (myElements.eq(i).prop('checked') == true)
                        {
                            newsubscriptions = newsubscriptions + "";"" + myElements.eq(i).attr(""id"") + "";"";
                        }
                     
                    }               

                   // debugger;
                    // If users in the subscription database, perform an update
                    if (existingid > 0) {
                        $().SPServices({
                            operation: ""UpdateListItems"",
                            async: false,
                            batchCmd: ""Update"",
                            listName: ""Subscriptions"",
                            ID: """" + existingid.toString() + """",
                            valuepairs: [[""SubscribedTo"", """" + newsubscriptions + """"]],
                            completefunc: function (xData, Status) {
                                //alert(xData.responseText);
                            }
                         });               
                    }
                    else {
                        // User doesnt exist in subscriptions create a new entry
                        $().SPServices({
                            operation: ""UpdateListItems"",
                            async: false,
                            batchCmd: ""New"",
                            listName: ""Subscriptions"",
                            valuepairs: [[""Title"", """" + username + """"],[""SubscribedTo"", """" + newsubscriptions + """"]],
                            completefunc: function (xData, Status) {
                                //alert(xData.responseText);
                            }
                         });       
                    
                    }


                }");
            writer.WriteEndTag("script");


        }
    }

    
    // This class holds the classifications for the systems
    public class Classifications
    {
        public string title;            // Classification name
        public int sortorder;           // Sort order
        public string description;      // Description
    }
    
    // This class holds the information for one system
    public class DolbySystem
    {
        public string name;             // name of the system
        public string description;      // description
        public string classification;   // Class of system
        public double id;               // List ID
        public int sortorder;           // sort order
        public int currentstatus;       // Status now
        public string trackID;          // Tracking ID for this system (used for subscriptions)
        public int subscribed;          // 1 if user subscribed 0 if not
        public DayStatus[] daystatus;   // Statuses for particular days
        
    }

    // This class represents the info for one specific day
    public class DayStatus
    {
        public int status;          // Status for this particular time (defcon)
        public string title;        // title for this outage
        public string update;       // Any recent updates
        public string scope;        // Scope of the impact
        public string useraction;   // Steps user should take
        public DateTime start;      // start time
        public DateTime end;        // end time
        public string details;      // details
        public string trackingref;  // tracking reference
        public DateTime periodStart;// represents the start of this day
        public DateTime periodEnd;  // represents the end of this day
        public string daytext;      // day text
        public string hover;

    }
}


