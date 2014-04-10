using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;
using System.Net.Mail;          // Added for the email
using System.Linq;


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
            // Only do something if 'Activate Emails' is set to yes
            if (prop.ListItem["Activate Emails"].ToString() == "True")
            {
                if (prop.ListItem["Title"].ToString() != null) outageTitle = prop.ListItem["Title"].ToString();
                outageSystem = prop.ListItem["System"].ToString();
                if (prop.ListItem["Update"] != null) outageUpdate = prop.ListItem["Update"].ToString();
                if (prop.ListItem["Scope of Impact"] != null) outageScope = prop.ListItem["Scope of Impact"].ToString();
                if (prop.ListItem["User Action Required"] != null) outageAction = prop.ListItem["User Action Required"].ToString();
                if (prop.ListItem["Start"] != null) outageStart = prop.ListItem["Start"].ToString();
                if (prop.ListItem["End"] != null) outageEnd = prop.ListItem["End"].ToString();
                if (prop.ListItem["Details"] != null) outageDetails = prop.ListItem["Details"].ToString();
                outageDefcon = prop.ListItem["Defcon"].ToString();
                if (prop.ListItem["TrackingRef"] != null) outageTrackingRef = prop.ListItem["TrackingRef"].ToString();


                // As system is a lookup field it has the format <id>;#Text the below fixes that to text
                SPFieldLookupValue systemlookup = new SPFieldLookupValue(outageSystem);
                outageSystem = systemlookup.LookupValue;




                

                // Loop through recipients here
                using (StatusDataContext context = new StatusDataContext(prop.Web.Url))
                {
                    string subscription;
                    string usersname;
                    string trackID="x";

                    // Lookup the system for this outage so we can get the 
                    var systemquery = from systems in context.Systems where systems.Id == systemlookup.LookupId select systems;
                    foreach (var systems in systemquery) { trackID = systems.TrackID; }


                    var userquery = from subscriptions in context.Subscriptions select subscriptions;
                    foreach (var subscriptions in userquery)
                    {
                        usersname = "dolbynet\\" + subscriptions.Title;
                        subscription = subscriptions.SubscribedTo;
                        if (subscription == null) subscription = ""; // guard against null entries

                        if (subscription.Contains(";" + trackID + ";")) {
                            // This person needs to be emailed

                            // Now we start building the email
                            MailMessage mail = new MailMessage();
                            mail.From = new MailAddress("rs@dolby.com");
                            mail.IsBodyHtml = true;
                            mail.BodyEncoding = System.Text.Encoding.UTF8;

                            mail.Subject = "STATUS ALERT (" + outageSystem + ") " + outageTitle;
                            mail.Body = "Current Status: " + outageDefcon + Environment.NewLine +
                                "Update: " + outageUpdate + Environment.NewLine +
                                "Scope of Impact: " + outageScope + Environment.NewLine +
                                "User Action Required: " + outageAction + Environment.NewLine +
                                "Interruption Start Time: " + outageStart + Environment.NewLine +
                                "Interruption End Time: " + outageEnd + Environment.NewLine +
                                "Details: " + Environment.NewLine + outageDetails + Environment.NewLine +
                                "Tracking Ref: " + outageTrackingRef;
                            SmtpClient smtp = new SmtpClient("mail.dolby.net");
                            smtp.UseDefaultCredentials = true;


                            mail.To.Add(subscriptions.Title + "@dolby.com");
                            smtp.Send(mail);
                        }
                    }

                }

                

            }

        }

    }
}