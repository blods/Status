using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Net.Mail;          // Added for the email
using System.Linq;
using System.Collections.Generic;   // Added for list


namespace Status.NewOutages
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class NewOutages : SPItemEventReceiver
    {
        public string outageTitle="";
        public string outageSystem="";
        public string outageUpdate = "";
        public string outageScope = "";
        public string outageAction = "";
        public string outageStart="";
        public string outageEnd="";
        public string outageDetails="";
        public string outageDefcon="";
        public string outageTrackingRef="";

        public DateTime start;
        public DateTime end;

        /// <summary>
        /// An item is being added.
        /// </summary>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            base.ItemAdding(properties);
            BuildEmail(properties);
        }

        /// <summary>
        /// An item is being updated.
        /// </summary>
        public override void ItemUpdated(SPItemEventProperties properties)
        {
            base.ItemUpdating(properties);
            BuildEmail(properties);
            
        }

        public void BuildEmail(SPItemEventProperties prop)
        {

            List<string> recipients = new List<string>();   // To store the email recipients
            SPListItem thisItem = prop.ListItem;


            // Only do something if 'Activate Emails' is set to yes
            if (prop.ListItem["Activate Emails"].ToString() == "True")
            {
                // Copy the outage items over to variables
                if (prop.ListItem["Title"].ToString() != null) outageTitle = prop.ListItem["Title"].ToString();
                outageSystem = prop.ListItem["System"].ToString();
                if (prop.ListItem["Update"] != null) outageUpdate = prop.ListItem["Update"].ToString();
                if (prop.ListItem["Scope of Impact"] != null) outageScope = prop.ListItem["Scope of Impact"].ToString();
                if (prop.ListItem["User Action Required"] != null) outageAction = prop.ListItem["User Action Required"].ToString();
                if (prop.ListItem["Start"] != null)
                {
                    DateTime start = new DateTime();
 
                    start =  (DateTime) thisItem["Start"];
                    
                    outageStart = string.Format("{0:MMMM d, yyyy h:mm tt} (UTC {0:zz})", start);
                }
                if (prop.ListItem["End"] != null)
                {
                    DateTime end = new DateTime();
                    end = (DateTime)thisItem["End"];
                    outageEnd = string.Format("{0:MMMM d, yyyy h:mm tt} (UTC {0:zz})", end);
                }
                else
                {
                    outageEnd = "Ongoing";
                }
                if (prop.ListItem["Details"] != null) outageDetails = prop.ListItem["Details"].ToString();
                outageDefcon = prop.ListItem["Defcon"].ToString();
                if (prop.ListItem["TrackingRef"] != null) outageTrackingRef = prop.ListItem["TrackingRef"].ToString();


                // As system is a lookup field it has the format <id>;#Text the below fixes that to text
                // On moving to multi selectors this has had to change
                SPFieldLookupValueCollection multichoice = new SPFieldLookupValueCollection(outageSystem);


               

                


                
                
                // Loop through recipients here
                using (StatusDataContext context = new StatusDataContext(prop.Web.Url))
                {
                    string subscription;
                    string usersname;
                    string trackID="x";
                    int count = 0;

                    
                    // Loop around each of the systems in turn
                    foreach (SPFieldLookupValue itemValue in multichoice)
                    {
                        // Lookup the system for this outage so we can get the trackID
                        var systemquery = from systems in context.Systems where systems.Id == itemValue.LookupId select systems;
                        foreach (var systems in systemquery) { trackID = systems.TrackID; }

                        var userquery = from subscriptions in context.Subscriptions select subscriptions;
                        foreach (var subscriptions in userquery)
                        {
                            usersname = "dolbynet\\" + subscriptions.Title;
                            subscription = subscriptions.SubscribedTo;
                            if (subscription == null) subscription = ""; // guard against null entries

                            if (subscription.Contains(";" + trackID + ";")) {
                                // This person needs to be emailed - check to see if they're already a target for this email
                                if (recipients.IndexOf(subscriptions.Title)== -1) {
                                    // No existing person found
                                    recipients.Add(subscriptions.Title);    // Add this person to the recipient list as a target for an email
                                }

                            }
                        }
                    }


                    foreach (string recipient in recipients)
                    {
                        
                        // Now we start building the email
                        MailMessage mail = new MailMessage();
                        mail.From = new MailAddress("itannouncement@dolby.com");
                        mail.IsBodyHtml = true;
                        mail.BodyEncoding = System.Text.Encoding.UTF8;

                        mail.Subject = "STATUS ALERT - " + outageTitle;
                        mail.Body += @"<HTML><HEAD><STYLE TYPE=""text/css""> <!-- TD{font-family: Verdana; font-size: 10pt;} ---> </STYLE></HEAD><BODY>";
                        mail.Body += @"<table>";
                        
                        if (outageUpdate != "")
                        {
                            mail.Body += @"<tr><td style=""border-bottom=1px solid black; border-right=1px solid black;width: 200px"" valign=""top""><b>Update</b></td>";
                            mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageUpdate + @"</td></tr>";
                        }

                        if (outageScope != "")
                        {
                            mail.Body += @"<tr><td style=""border-bottom=1px solid black;border-right=1px solid black;width: 200px"" valign=""top""><b>Scope of Impact</b></td>";
                            mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageScope + @"</td></tr>";
                        }
                        
                        mail.Body += @"<tr><td style=""border-bottom=1px solid black;border-right=1px solid black;width: 200px"" valign=""top""><b>Systems Impacted</b></td>";
                        mail.Body += @"<td style=""border-bottom=1px solid black;width: 600px"">";
                        foreach (SPFieldLookupValue sysval in multichoice)
                        {
                            mail.Body += sysval.LookupValue + @"<BR>";
                        }
                        mail.Body += @"</td></tr>";

                        if (outageDetails != "")
                        {
                            mail.Body += @"<tr><td style=""border-bottom=1px solid black;border-right=1px solid black;width: 200px"" valign=""top""><b>Details</b></td>";
                            mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageDetails + @"</td></tr>";
                        }

                        if (outageAction != "")
                        {
                            mail.Body += @"<tr><td style=""border-bottom=1px solid black;border-right=1px solid black;width: 200px"" valign=""top""><b>User Action Required</b></td>";
                            mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageAction + @"</td></tr>";
                        }
                        
                        
                        mail.Body += @"<tr><td style=""border-bottom=1px solid gray;border-right=1px solid black;width: 200px"" valign=""top""><b>Interruption Start Time</b></td>";
                        mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageStart + @"</td></tr>";
                        mail.Body += @"<tr><td style=""border-bottom=1px solid gray;border-right=1px solid black;width: 200px"" valign=""top""><b>Interruption End Time</b></td>";
                        mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageEnd + @"</td></tr>";

                        if (outageTrackingRef != "")
                        {
                            mail.Body += @"<tr><td style=""border-bottom=1px solid black;border-right=1px solid black;width: 200px"" valign=""top""><b>Tracking Reference</b></td>";
                            mail.Body += @"<td style=""border-bottom=1px solid black; width: 600px"">" + outageTrackingRef + "</td></tr>";
                        }
       

                        mail.Body += @"</table>";
                        mail.Body += @"<br><p><font color=""#BBBBBB""><font face=""Verdana""><font size=""2"">To unsubscribe from notifications related to this system, click <a href=""https://dolbyconnect.dolby.net/LiveStat"">here</a></p>";
                        mail.Body += @"</BODY></HTML>";
                        SmtpClient smtp = new SmtpClient("mail.dolby.net");
                        smtp.UseDefaultCredentials = false;


                        mail.To.Add(recipient + "@dolby.com");
                        smtp.Send(mail);
                        count++;

                    }
                    
                    // Email are all sent so update
                    
                    thisItem["EmailsSent"] = count.ToString();
                    thisItem["EmailsSend"] = false;
                    thisItem["LastEmailSent"] = DateTime.Now;
                    thisItem.Update();
                    

                }

                

            }

        }

    }

}